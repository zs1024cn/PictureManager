using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;

namespace PictureManager
{
    /// <summary>
    /// 文件指纹登记簿 - 数据库操作封装。
    /// </summary>
    public class MyDb : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _dbPath;
        private readonly object _batchLock = new object();
        private List<Tuple<string, byte[]>> _pendingBatch = new List<Tuple<string, byte[]>>();
        private readonly object _readLock = new object();
        public int _RegistryCount = 0;
        public int _DeletedRegistryCount = 0;
        private List<Tuple<List<string>, int>> _pendingRemoveTags = new List<Tuple<List<string>, int>>();
        private object _removeLock = new object();

        public MyDb(string dbPath = null)
        {
            _dbPath = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "file_registry.db");

            string dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _connectionString = $"Data Source={_dbPath};Version=3;Journal Mode=WAL;Busy Timeout=5000;";

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
             CREATE TABLE IF NOT EXISTS file_registry (
                content_hash BLOB NOT NULL,
                file_path TEXT PRIMARY KEY,
                status INTEGER DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_file_path ON file_registry(file_path);
                CREATE INDEX IF NOT EXISTS idx_status ON file_registry(status);
            ";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM file_registry WHERE content_hash IS NULL";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT COUNT(*) FROM file_registry";
                    _RegistryCount = Convert.ToInt32(cmd.ExecuteScalar());

                    cmd.CommandText = "SELECT COUNT(*) FROM file_registry WHERE status != 0";
                    _DeletedRegistryCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void ClearDBRecord()
        {
            File.Delete(_dbPath);
        }

        public void ClearDBStatus()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE file_registry set status=0 ";
                    cmd.ExecuteNonQuery();
                    _DeletedRegistryCount = 0;
                }
            }
        }

        // ========== 删除标记 ==========

        public void SetRemoveTagBatchDeferred(List<string> filenames, int status)
        {
            if (filenames == null || filenames.Count == 0) return;
            lock (_removeLock)
            {
                _pendingRemoveTags.Add(Tuple.Create(filenames, status));
            }
        }

        private void FlushRemoveTags()
        {
            List<Tuple<List<string>, int>> batch;
            lock (_removeLock)
            {
                if (_pendingRemoveTags.Count == 0) return;
                batch = _pendingRemoveTags;
                _pendingRemoveTags = new List<Tuple<List<string>, int>>();
            }

            int maxRetry = 5;
            for (int retry = 0; retry < maxRetry; retry++)
            {
                try
                {
                    using (var conn = new SQLiteConnection(_connectionString))
                    {
                        conn.Open();
                        using (var tx = conn.BeginTransaction())
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"UPDATE file_registry SET status = @status WHERE file_path = @path";
                            var statusParam = cmd.Parameters.Add("@status", DbType.Int32);
                            var pathParam = cmd.Parameters.Add("@path", DbType.String);

                            foreach (var item in batch)
                            {
                                statusParam.Value = item.Item2;
                                foreach (var path in item.Item1)
                                {
                                    pathParam.Value = path;
                                    cmd.ExecuteNonQuery();
                                }
                                if (item.Item2 == 1)
                                    _DeletedRegistryCount += item.Item1.Count;
                            }

                            tx.Commit();
                        }
                    }
                    return;
                }
                catch (SQLiteException ex) when (ex.Message.Contains("locked"))
                {
                    if (retry == maxRetry - 1) throw;
                    Thread.Sleep(100 * (retry + 1));
                }
            }
        }

        // ========== 批量写入 ==========

        public void AddMetaDeferred(string filename, byte[] meta)
        {
            lock (_batchLock)
            {
                _pendingBatch.Add(Tuple.Create(filename, meta));
            }
        }

        public void FlushBatch()
        {
            List<Tuple<string, byte[]>> batch;
            lock (_batchLock)
            {
                if (_pendingBatch.Count == 0) return;
                batch = _pendingBatch;
                _pendingBatch = new List<Tuple<string, byte[]>>();
            }

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                INSERT OR REPLACE INTO file_registry (content_hash, file_path, status)
                VALUES (@hash, @path, 0)
            ";
                    var hashParam = cmd.Parameters.Add("@hash", DbType.Binary);
                    var pathParam = cmd.Parameters.Add("@path", DbType.String);

                    foreach (var item in batch)
                    {
                        pathParam.Value = item.Item1;
                        hashParam.Value = item.Item2;
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();

                    _RegistryCount += batch.Count;
                }
            }

            FlushRemoveTags();
        }

        // ========== 查询 ==========

        public Dictionary<string, byte[]> GetMeta(string[] filenames)
        {
            var result = new Dictionary<string, byte[]>(filenames.Length);

            foreach (var f in filenames)
                result[f] = null;

            if (filenames.Length == 0)
                return result;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TEMP TABLE IF NOT EXISTS _lookup (file_path TEXT)";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "DELETE FROM _lookup";
                    cmd.ExecuteNonQuery();

                    using (var tx = conn.BeginTransaction())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO _lookup (file_path) VALUES (@path)";
                        var param = cmd.Parameters.Add("@path", DbType.String);

                        for (int i = 0; i < filenames.Length; i += 1000)
                        {
                            int batchSize = Math.Min(1000, filenames.Length - i);
                            for (int j = 0; j < batchSize; j++)
                            {
                                param.Value = filenames[i + j];
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                    }

                    cmd.CommandText = @"
                        SELECT r.file_path, r.content_hash
                        FROM file_registry r
                        INNER JOIN _lookup l ON r.file_path = l.file_path
                    ";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string path = reader.GetString(0);
                            byte[] hash = new byte[16];
                            reader.GetBytes(1, 0, hash, 0, 16);
                            result[path] = hash;
                        }
                    }
                }
            }

            return result;
        }

        public byte[] GetMeta(string filename)
        {
            lock (_readLock)
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                    SELECT content_hash FROM file_registry 
                    WHERE file_path = @path LIMIT 1
                ";
                        cmd.Parameters.AddWithValue("@path", filename);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return (byte[])result;
                    }
                }
            }
            return null;
        }

        // ========== 指纹添加 ==========

        public void AddMeta(Dictionary<string, byte[]> metas)
        {
            if (metas == null || metas.Count == 0)
                return;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT OR IGNORE INTO file_registry (content_hash, file_path, status)
                        VALUES (@hash, @path, 0)
                    ";
                    var hashParam = cmd.Parameters.Add("@hash", DbType.Binary);
                    var pathParam = cmd.Parameters.Add("@path", DbType.String);

                    foreach (var kvp in metas)
                    {
                        pathParam.Value = kvp.Key;
                        hashParam.Value = kvp.Value;
                        cmd.ExecuteNonQuery();
                    }

                    _RegistryCount += metas.Count;
                    tx.Commit();
                }
            }
        }

        public void AddMeta(string filename, byte[] meta)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT OR IGNORE INTO file_registry (content_hash, file_path, status)
                        VALUES (@hash, @path, 0)
                    ";
                    cmd.Parameters.AddWithValue("@hash", meta);
                    cmd.Parameters.AddWithValue("@path", filename);
                    cmd.ExecuteNonQuery();
                    _RegistryCount++;
                }
            }
        }

        // ========== 垃圾标记 ==========

        public HashSet<string> GetAllGarbageHashes()
        {
            var result = new HashSet<string>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT content_hash FROM file_registry WHERE status != 0 AND content_hash IS NOT NULL";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] hash = (byte[])reader[0];
                            result.Add(Convert.ToBase64String(hash));
                        }
                    }
                }
            }
            return result;
        }

        public void SetRemoveTag(string filename, int status)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                INSERT OR REPLACE INTO file_registry (content_hash, file_path, status)
                VALUES (
                    (SELECT content_hash FROM file_registry WHERE file_path = @path LIMIT 1),
                    @path,
                    @status
                )
            ";
                    cmd.Parameters.AddWithValue("@path", filename);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.ExecuteNonQuery();

                    if (status == 1)
                        _DeletedRegistryCount++;
                }
            }
        }

        public void SetRemoveTag(string filename)
        {
            SetRemoveTag(filename, 1);
        }

        // ========== 批量垃圾检查 ==========

        public HashSet<string> GetExistHashes(byte[][] hashes)
        {
            var result = new HashSet<string>();

            if (hashes.Length == 0)
                return result;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TEMP TABLE IF NOT EXISTS _hash_lookup (hash BLOB)";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "DELETE FROM _hash_lookup";
                    cmd.ExecuteNonQuery();

                    using (var tx = conn.BeginTransaction())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO _hash_lookup (hash) VALUES (@hash)";
                        var param = cmd.Parameters.Add("@hash", DbType.Binary);

                        for (int i = 0; i < hashes.Length; i += 1000)
                        {
                            int batchSize = Math.Min(1000, hashes.Length - i);
                            for (int j = 0; j < batchSize; j++)
                            {
                                param.Value = hashes[i + j];
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                    }

                    cmd.CommandText = @"
                        SELECT DISTINCT r.content_hash
                        FROM file_registry r
                        INNER JOIN _hash_lookup l ON r.content_hash = l.hash
                    ";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] hash = new byte[16];
                            reader.GetBytes(0, 0, hash, 0, 16);
                            result.Add(BitConverter.ToString(hash).Replace("-", ""));
                        }
                    }
                }
            }

            return result;
        }

        public bool IsGarbage(string fingerprintBase64)
        {
            byte[] hash;
            try
            {
                hash = Convert.FromBase64String(fingerprintBase64);
            }
            catch
            {
                return false;
            }

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT COUNT(*) FROM file_registry 
                WHERE content_hash = @hash AND status != 0
            ";
                    cmd.Parameters.AddWithValue("@hash", hash);
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }

        public HashSet<string> GetGarbageHashes(byte[][] hashes)
        {
            var result = new HashSet<string>();

            if (hashes.Length == 0) return result;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TEMP TABLE IF NOT EXISTS _garbage_lookup (hash BLOB)";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "DELETE FROM _garbage_lookup";
                    cmd.ExecuteNonQuery();

                    using (var tx = conn.BeginTransaction())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO _garbage_lookup (hash) VALUES (@hash)";
                        var param = cmd.Parameters.Add("@hash", DbType.Binary);

                        for (int i = 0; i < hashes.Length; i += 1000)
                        {
                            int batchSize = Math.Min(1000, hashes.Length - i);
                            for (int j = 0; j < batchSize; j++)
                            {
                                param.Value = hashes[i + j];
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                    }

                    cmd.Transaction = null;
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"
                SELECT l.hash FROM _garbage_lookup l
                INNER JOIN file_registry r ON l.hash = r.content_hash
                WHERE r.status != 0";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] hash = (byte[])reader[0];
                            result.Add(Convert.ToBase64String(hash));
                        }
                    }
                }
            }
            return result;
        }

        public void Dispose()
        {
        }
    }
}