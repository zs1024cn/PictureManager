//git reset --hard <commit-hash>
//git diff id0 id1 > changes.txt
//git apply changes.txt

//发布github
//git add .
//git commit -m "描述本次改动"
//git push

//Forms\Form1.cs
using CefSharp;
using CefSharp.DevTools.Browser;
using CefSharp.DevTools.Debugger;
using CefSharp.DevTools.Profiler;
using CefSharp.WinForms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.XFeatures2D;  // SIFT 需要这个命名空间
using PictureManager;
using PictureManager.Controls;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ForumDownloader;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace PictureManager 
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
        private ConcurrentDictionary<string, string> _fingerprintCache = new ConcurrentDictionary<string, string>(); //从数据库全部载入
        private string _forumConfigPath = Path.Combine(Application.StartupPath, "forum_config.json");
        private string _PictureMode = "M";//simple,middle,heavy
        private int _PictureSizeRatio = 100;
        private Microsoft.Web.WebView2.WinForms.WebView2 _downloadBrowser;
        private bool _downloadBrowserReady = false;
        private static readonly Random Random = new Random();

        private MyDb _myDb;
        private int iVideoVolume = 70;
        private ArrayList alFile = new ArrayList();
        private int iEmptyDirectoryCount = 0;
        private string sEmptyDirectory = "";
        private string sCurrentFileName = "";
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private DateTime dCurrentVideoPlayTime = DateTime.MinValue;
        private int ialFilesAllCount = 0;
        private string _TmpDirectory = "";
        private string _MainDirectory = "";
        private bool _isFlushing = false;
        private bool _isSeeking = false;  // 防止拖动时与定时器冲突
                                          // 元数据缓存
        private DateTime _lastCefActivity = DateTime.Now;

        private Dictionary<string, FileMeta> _fileMeta = new Dictionary<string, FileMeta>();
        private string _metaJsonPath = Path.Combine(Application.StartupPath, "progress.json");
        private string _currentVideoFingerprint = null;
        private CancellationTokenSource _videoLoadCts;

        private System.Windows.Forms.Button btnCalc;
        private System.Windows.Forms.ContextMenuStrip menuWorkflow;
        // 工作流状态
        private string _currentCompareMode = "Hash";
        private CancellationTokenSource _calcCts;
        private CancellationTokenSource _compareCts;
        private HashSet<string> _deletedMetaList = new HashSet<string>();

        private ForumDownloader _downloader = new ForumDownloader();
        private DateTime _lastShowPictureTime = DateTime.Now;
        private System.Drawing.Point _lastCursorPos = Cursor.Position;
        private DateTime _lastMouseMoveTime = DateTime.Now;

        private bool _MetaEnabled = true;//是否启动meta,当false时，btnNamesize只检查文件名+size。否则用meta值来判定重复

        private bool _isFormLoadFinished = false;
        private string _pendingVideoFile = null;
        // 
        public Form1()
        {
            InitializeComponent();
        }
       
        private async void Form1_Load(object sender, EventArgs e)
        {
            this.tbPageRows.Text = "400000";
            _downloadBrowser = new Microsoft.Web.WebView2.WinForms.WebView2();
            _downloadBrowser.Size = new System.Drawing.Size(1024, 768);
            _downloadBrowser.Visible = false;
            this.Controls.Add(_downloadBrowser);

            await _downloadBrowser.EnsureCoreWebView2Async(null);
            _downloadBrowserReady = true;

            dg1.CurrentCellChanged -= dg1_CurrentCellChanged;

            Form1_Resize(sender, e);
            Application.DoEvents();

            toolStripStatusLabelStatus.Alignment = ToolStripItemAlignment.Right;
            toolStripStatusLabelVolume.Alignment = ToolStripItemAlignment.Right;
            toolStripStatusLabelSize.Alignment = ToolStripItemAlignment.Right;
            toolStripStatusLabelTrack.Alignment = ToolStripItemAlignment.Right;

            // 绑定工作流按钮的右键菜单
            this.btnCalc.MouseDown += btnWorkflow_MouseDown;
            // 初始化加载模式
            btnLoadView.Text = "Load 🔄";
            // 初始化对比器
            PictureCompareManager.RegisterDefaults();

            // 动态生成工作流菜单
            BuildWorkflowMenu();
           
            LoadMetaJson();

            // 初始化数据库
            _myDb = new MyDb(Path.Combine(Application.StartupPath, "file_registry.db"));
            // 加载已保存的目录
            directoryListControl1.LoadFromJson();

            this.cbFileType.Text = this.cbFileType.Items[0].ToString();
            this.cbBucket.SelectedIndex = 0;
            this.cbCrop.SelectedIndex = 0;
            cbCompareDctMode.SelectedIndex = 0;

            this.btnLoadView_Click(sender, e);
            // 立即完成窗体加载
            dg1.CurrentCellChanged += dg1_CurrentCellChanged;

            trackBar1.Minimum = 0;
            trackBar1.Maximum = 10000;
            trackBar1.Value = 0;
            timer1.Interval = 500;

            LoadForumConfig();

            // 预热放到后台，不阻塞窗体
            await Task.Run(() =>
            {
                // 初始化 VLC（不耗时）
                if (_libVLC == null)
                {
                    _libVLC = new LibVLC();
                    _mediaPlayer = new MediaPlayer(_libVLC);
                }
                var warmupBrowser = new ChromiumWebBrowser("about:blank");
                // 不需要加到 Controls，后台默默初始化即可
                Thread.Sleep(2000);
                warmupBrowser.Dispose();
            });

            Debug.WriteLine("ID.SortMode after load:" + this.dg1.Columns["ID"].SortMode.ToString());
            _isFormLoadFinished = true;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            btnESC.Left = 5;
            btnESC.Top = 5;

            int iLeft = 0;
            if (tabControl1.Visible)
                iLeft = tabControl1.Width;

            this.chromBrowser.Left = iLeft;
            this.chromBrowser.Width = this.Width - iLeft;
            this.chromBrowser.Height = this.chromBrowser.Parent.ClientSize.Height - this.chromBrowser.Top - 51;// this.statusStrip1.Height;

            this.tabControl1.Height = this.tabControl1.Parent.ClientSize.Height - this.tabControl1.Top - 51;// this.statusStrip1.Height ;
            this.dg1.Height = this.dg1.Parent.ClientSize.Height - dg1.Top;

            this.videoView1.Left = this.chromBrowser.Left;
            this.videoView1.Width = this.chromBrowser.Width;
            this.videoView1.Height = this.chromBrowser.Height - trackBar1.Height;

            this.trackBar1.Left = this.chromBrowser.Left;
            this.trackBar1.Width = this.chromBrowser.Width;
            this.trackBar1.Top = videoView1.Top + videoView1.Height;
        }
     
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void SaveLastFile()
        {
            string lastFile = dg1.CurrentRow?.Cells["Name"].Value?.ToString();
            if (!string.IsNullOrEmpty(lastFile))
            {
                directoryListControl1.LastFile = lastFile;
                //directoryListControl1.SaveToJson();
            }

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveLastFile();

            // 等待队列清空
            while (!FileUtility._filesToDelete.IsEmpty)
            {
                Application.DoEvents();
                Thread.Sleep(100);
            }
            
            FileUtility.ShowCursor();

            var result = MessageBox.Show(
       "确定要退出程序吗？",
       "MediaManager2026",
       MessageBoxButtons.YesNo,
       MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                e.Cancel = true;  // 取消关闭
                return;
            }

            SaveForumConfig();

            _myDb?.FlushBatch();
            _videoLoadCts?.Cancel();
            _videoLoadCts?.Dispose();
            _videoLoadCts = null;

            directoryListControl1.SaveToJson();
            SaveMetaJson();

            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();

            FileUtility.CleanWebView2Cache();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            Form1_Resize(this, EventArgs.Empty);
            ShowPicture();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            FileUtility.ShowCursor();
        }
    }
}
