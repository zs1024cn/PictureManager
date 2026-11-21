using CefSharp;
using CefSharp.WinForms;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureManager
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppLog.Init();   // 日志文件：logs\yyyy-MM-dd.log

            // 全局异常捕获：任何未处理异常都写日志 + 弹窗，避免程序无声退出
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (s, e) =>
            {
                LogCrash("UI Thread Exception", e.Exception);
                MessageBox.Show(e.Exception.ToString(), "程序异常",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogCrash("AppDomain Exception", e.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogCrash("Task Exception", e.Exception);
                e.SetObserved();
            };

            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            try
            {
                string cachePath = Path.Combine(Application.StartupPath, "CefCache");
                if (Directory.Exists(cachePath))
                {
                    try { Directory.Delete(cachePath, true); } catch { }
                }

                var settings = new CefSettings();
                settings.LogSeverity = LogSeverity.Disable;
                settings.CefCommandLineArgs.Add("disable-web-security", "1");
                settings.CefCommandLineArgs.Add("allow-file-access-from-files", "1");
                settings.CachePath = Path.Combine(Application.StartupPath, "CefCache");
                settings.LogFile = Path.Combine(Application.StartupPath, "cef_log.txt");
                settings.CefCommandLineArgs.Add("disable-extensions", "1");
                settings.CefCommandLineArgs.Add("disable-pdf-extension", "1");
                settings.CefCommandLineArgs.Add("disable-images", "1");
                settings.LogSeverity = LogSeverity.Error;
                Cef.Initialize(settings);
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        /// <summary>
        /// 把异常写到 crash.log，便于排查
        /// </summary>
        private static void LogCrash(string tag, Exception ex)
        {
            try
            {
                AppLog.Write("crash", tag + "\r\n" + ex);
            }
            catch { }
        }
    }
}