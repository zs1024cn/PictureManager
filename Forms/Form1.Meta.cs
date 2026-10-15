//Forms\Form1.Meta.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureManager
{
    public partial class Form1
    {
        // 元数据相关方法（从 Form1.cs 移入）
        // ========== 元数据管理 ==========
        /// <summary>
        /// 加载 progress.json 到内存
        /// </summary>
        private void LoadMetaJson()
        {
            try
            {
                if (File.Exists(_metaJsonPath))
                {
                    string json = File.ReadAllText(_metaJsonPath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, FileMeta>>(json);
                    if (data != null)
                    {
                        _fileMeta = data;
                    }
                }
            }
            catch (Exception ex)
            {
                this.toolStripStatusLabelStatus.Text = "加载 progress.json 失败";
                _fileMeta = new Dictionary<string, FileMeta>();
            }
        }

        /// <summary>
        /// 保存 _fileMeta 到 progress.json（只保存有效数据）
        /// </summary>
        private void SaveMetaJson()
        {
            try
            {
                var toSave = _fileMeta
    .Select(kv => new { kv.Key, Value = (FileMeta)kv.Value })
    .Where(x => x.Value.Heart > 0 || x.Value.Break > 0 ||
                (x.Value.Progress ?? 0) > 0 || (x.Value.TextScroll ?? 0) > 0)
    .ToDictionary(x => x.Key, x => x.Value);

                if (toSave.Count > 0)
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(toSave, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_metaJsonPath, json);
                }
                else if (File.Exists(_metaJsonPath))
                {
                    File.Delete(_metaJsonPath);
                }
            }
            catch (Exception ex)
            {
                this.toolStripStatusLabelStatus.Text = "保存 progress.json 失败";
            }
        }
        private void btnMeta_Click(object sender, EventArgs e)
        {
            if (!_MetaEnabled) return;

            btnMeta.Enabled = false;
            DateTime d0 = DateTime.Now;
            this.toolStripStatusLabelStatus.Text = "开始加载元数据...";

            var fileList = new List<Tuple<int, string>>();
            for (int r = 0; r < dg1.Rows.Count; r++)
            {
                string filePath = dg1.Rows[r].Cells["Name"].Value?.ToString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    fileList.Add(Tuple.Create(r, filePath));
                }
            }
            int total = fileList.Count;

            Task.Run(() =>
            {
                // ⭐ 第一步：批量查数据库（一次 SQL，不走 lock）
                var allPaths = fileList.Select(f => f.Item2).ToArray();
                var dbMetas = _myDb.GetMeta(allPaths);

                var heartResults = new ConcurrentDictionary<int, int>();
                var breakResults = new ConcurrentDictionary<int, int>();
                var metaResults = new ConcurrentDictionary<int, string>();

                Parallel.For(0, total, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
                {
                    int r = fileList[i].Item1;
                    string filePath = fileList[i].Item2;

                    string fingerprint;

                    // 1. 内存缓存
                    if (_fingerprintCache.TryGetValue(filePath, out fingerprint))
                    {
                        // 命中，什么都不用做
                    }
                    // 2. 数据库缓存（批量查的结果）
                    else if (dbMetas.TryGetValue(filePath, out byte[] hash) && hash != null)
                    {
                        fingerprint = Convert.ToBase64String(hash);
                        _fingerprintCache.TryAdd(filePath, fingerprint);
                    }
                    // 3. 读磁盘
                    else
                    {
                        fingerprint = GetFileFingerprint(filePath);
                        if (!string.IsNullOrEmpty(fingerprint))
                        {
                            _fingerprintCache.TryAdd(filePath, fingerprint);
                            _myDb.AddMetaDeferred(filePath, Convert.FromBase64String(fingerprint));
                        }
                    }

                    metaResults[r] = fingerprint;

                    if (!string.IsNullOrEmpty(fingerprint) && _fileMeta.TryGetValue(fingerprint, out FileMeta meta))
                    {
                        heartResults[r] = meta.Heart;
                        breakResults[r] = meta.Break;
                    }

                    if (i % 500 == 0)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            this.toolStripStatusLabelStatus.Text = $"加载元数据 {i}/{total}...";
                        }));
                    }
                });

                BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        foreach (var kvp in heartResults)
                        {
                            if (kvp.Key < dg1.Rows.Count)
                                dg1.Rows[kvp.Key].Cells["heart"].Value = kvp.Value;
                        }
                        foreach (var kvp in breakResults)
                        {
                            if (kvp.Key < dg1.Rows.Count)
                                dg1.Rows[kvp.Key].Cells["break"].Value = kvp.Value;
                        }
                        foreach (var kvp in metaResults)
                        {
                            if (kvp.Key < dg1.Rows.Count)
                                dg1.Rows[kvp.Key].Cells["Meta"].Value = kvp.Value;
                        }

                        // ★ FlushBatch 挪后台，await 等它完成
                        await Task.Run(() => _myDb.FlushBatch());

                        // ★ 所有任务完成，才更新状态栏 + 按钮
                        this.toolStripStatusLabelStatus.Text = $"元数据加载完成 in {(DateTime.Now - d0).TotalSeconds:F1}s";
                        btnMeta.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Meta] BeginInvoke 异常: {ex.Message}");
                        btnMeta.Enabled = true;   // 异常也要恢复按钮
                    }
                }));
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 更新元数据并刷新单元格
        /// </summary>
        private void UpdateMetaAndCell(Action<FileMeta> updateAction, string columnName)
        {
            if (dg1.CurrentRow == null) return;

            int r = dg1.CurrentRow.Index;
            string filePath = dg1.Rows[r].Cells["Name"].Value?.ToString();
            if (string.IsNullOrEmpty(filePath)) return;

            string fingerprint = GetFileFingerprintCached(filePath);

            if (!_fileMeta.TryGetValue(fingerprint, out FileMeta meta))
            {
                meta = new FileMeta();
                _fileMeta[fingerprint] = meta;
            }

            updateAction(meta);

            if (columnName == "heart")
                dg1.Rows[r].Cells["heart"].Value = meta.Heart;
            else if (columnName == "break")
                dg1.Rows[r].Cells["break"].Value = meta.Break;
        }
        /// <summary>
        /// 计算文件指纹：取中间 4KB 计算 MD5
        /// </summary>
        private string GetFileFingerprint(string filePath)
        {
            byte[] hash = FileFingerprint.Compute(filePath);
            return hash != null ? Convert.ToBase64String(hash) : null;
        }

        private string GetFileFingerprintCached(string filePath)
        {
            // 1. 内存缓存（最快）
            if (_fingerprintCache.TryGetValue(filePath, out string cached))
                return cached;

            // 2. 查数据库
            var hash = _myDb.GetMeta(filePath);
            if (hash != null)
            {
                string fp = Convert.ToBase64String(hash);
                _fingerprintCache.TryAdd(filePath, fp);
                return fp;
            }

            // 3. 读磁盘
            string fingerprint = GetFileFingerprint(filePath);

            if (!string.IsNullOrEmpty(fingerprint))
            {
                _fingerprintCache.TryAdd(filePath, fingerprint);
                _myDb.AddMetaDeferred(filePath, Convert.FromBase64String(fingerprint));
            }

            return fingerprint;
        }

    }
}