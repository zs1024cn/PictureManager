//Core\FileFingerprint.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PictureManager
{
    /// <summary>
    /// 文件指纹计算工具。
    /// </summary>
    public static class FileFingerprint
    {
        /// <summary>
        /// 计算文件指纹：取头 1KB + 中 2KB + 尾 1KB 计算 MD5。
        /// </summary>
        public static byte[] Compute(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess))
                {
                    long fileSize = fs.Length;
                    int headSize = 1024;
                    int midSize = 2048;
                    int tailSize = 1024;

                    byte[] buffer = new byte[headSize + midSize + tailSize];
                    int offset = 0;

                    // 头部 1KB
                    fs.Seek(0, SeekOrigin.Begin);
                    offset += fs.Read(buffer, offset, (int)Math.Min(headSize, fileSize));

                    // 中部 2KB
                    if (fileSize > headSize + midSize + tailSize)
                    {
                        long midStart = fileSize / 2 - midSize / 2;
                        fs.Seek(midStart, SeekOrigin.Begin);
                        offset += fs.Read(buffer, offset, midSize);
                    }

                    // 尾部 1KB
                    if (fileSize > tailSize)
                    {
                        fs.Seek(-tailSize, SeekOrigin.End);
                        offset += fs.Read(buffer, offset, tailSize);
                    }

                    using (var md5 = MD5.Create())
                    {
                        return md5.ComputeHash(buffer, 0, offset);
                    }
                }
            }
            catch
            {
                // 读取失败退回到路径哈希
                using (var md5 = MD5.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(filePath);
                    return md5.ComputeHash(bytes);
                }
            }
        }

        /// <summary>
        /// 获取文件指纹，优先从数据库取，没有则计算并存入。
        /// </summary>
        public static byte[] GetOrRegister(string filePath, MyDb db)
        {
            var hash = db.GetMeta(filePath);
            if (hash != null)
                return hash;

            hash = Compute(filePath);

            if (hash != null)
            {
                db.AddMeta(filePath, hash);
            }

            return hash;
        }
    }
}