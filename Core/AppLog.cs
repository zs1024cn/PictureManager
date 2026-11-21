//Core\AppLog.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PictureManager
{
    /// <summary>
    /// 统一日志：控制台、Debug、异常都落到 exe 所在目录的 logs\yyyy-MM-dd.log。
    /// </summary>
    public static class AppLog
    {
        private static readonly object _lock = new object();
        private static string _dir;

        public static void Init()
        {
            try
            {
                _dir = Path.Combine(Application.StartupPath, "logs");
                Directory.CreateDirectory(_dir);

                var listener = new FileListener();
                Debug.Listeners.Add(listener);
                Trace.Listeners.Add(listener);

                CleanOld(7);
            }
            catch { }
        }

        public static string CurrentFile
        {
            get { return _dir == null ? null : Path.Combine(_dir, DateTime.Now.ToString("yyyy-MM-dd") + ".log"); }
        }

        public static void Write(string tag, string message)
        {
            try
            {
                if (_dir == null) Init();
                string line = string.Format("{0:HH:mm:ss.fff} [{1}] {2}\r\n", DateTime.Now, tag, message);
                lock (_lock)
                {
                    File.AppendAllText(CurrentFile, line, Encoding.UTF8);
                }
            }
            catch { }
        }

        private static void CleanOld(int days)
        {
            try
            {
                foreach (var f in Directory.GetFiles(_dir, "*.log"))
                {
                    if ((DateTime.Now - File.GetLastWriteTime(f)).TotalDays > days)
                        File.Delete(f);
                }
            }
            catch { }
        }

        private sealed class FileListener : TraceListener
        {
            public override void Write(string message) { WriteLine(message); }
            public override void WriteLine(string message) { AppLog.Write("dbg", message); }
        }
    }
}