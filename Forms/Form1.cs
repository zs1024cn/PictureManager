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
        private string _PictureMode = "S";//simple,middle,heavy
        private int _PictureSizeRatio = 100;
        private readonly System.Diagnostics.Stopwatch _picLoadWatch = new System.Diagnostics.Stopwatch();
        private bool _awaitingRender = false;
        private string _lastFileSizeText = "";
        private long _lastLoadViewMs = 0;
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
        private bool _isClosing = false;
        private string _pendingVideoFile = null;

        private long _cachedVideoLength = 0;
        private float _cachedVideoPosition = 0;
        private long _lastSavedProgressMs = -1;
        private bool _libVLCReady = false;
        private volatile bool _isStoppingVideo = false;        // 
        public Form1()
        {
            InitializeComponent();
        }
       
        private async void Form1_Load(object sender, EventArgs e)
        {
            this.dg1.ImeMode = System.Windows.Forms.ImeMode.NoControl;

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
            this.toolStripStatusLabelPictureMode.Text = "PM:" + _PictureMode + $" {_PictureSizeRatio}%";
            this.chromBrowser.LoadingStateChanged += ChromBrowser_LoadingStateChanged;
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

            // ★ VLC 初始化后台跑，不 await，不阻塞 Form1_Load
            _ = Task.Run(() =>
            {
                if (_libVLC == null)
                {
                    _libVLC = new LibVLC();
                    _mediaPlayer = new MediaPlayer(_libVLC);

                    // ★ 事件里更新缓存 + BeginInvoke 更新 UI
                    _mediaPlayer.LengthChanged += (s, ev) =>
                    {
                        _cachedVideoLength = ev.Length;
                        BeginInvoke(new Action(() => UpdateVideoUi()));
                    };

                    _mediaPlayer.PositionChanged += (s, ev) =>
                    {
                        _cachedVideoPosition = ev.Position;
                        BeginInvoke(new Action(() => UpdateVideoUi()));
                    };

                    _mediaPlayer.TimeChanged += (s, ev) =>
                    {
                        // ★ 每 10 秒保存一次进度（不碰 UI，不读 _mediaPlayer）
                        if (_currentVideoFingerprint == null) return;
                        if (ev.Time - _lastSavedProgressMs >= 10000)
                        {
                            _lastSavedProgressMs = ev.Time;
                            SaveCurrentVideoProgressByTime(ev.Time);
                        }
                    };
                    _mediaPlayer.EndReached += (s, ev) =>
                    {
                        string fileAtEnd = sCurrentFileName;

                        Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(100);
                                if (string.IsNullOrEmpty(fileAtEnd) || fileAtEnd != sCurrentFileName) return;
                                if (!File.Exists(fileAtEnd)) return;
                                if (!videoView1.Visible) return;
                                if (_isStoppingVideo) return;

                                _mediaPlayer.Position = 0;
                                _mediaPlayer.Play();

                                await Task.Delay(200);
                                if (_mediaPlayer.State != VLCState.Playing)
                                {
                                    if (fileAtEnd != sCurrentFileName) return;
                                    if (!File.Exists(fileAtEnd)) return;
                                    if (!videoView1.Visible) return;
                                    if (_isStoppingVideo) return;

                                    var media = new Media(_libVLC, fileAtEnd, FromType.FromPath);
                                    _mediaPlayer.Play(media);
                                }
                            }
                            catch { }
                        });

                        BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                trackBar1.Value = 0;
                                toolStripStatusLabelTrack.Text = "00:00:00/" + TimeSpan.FromSeconds((int)(_cachedVideoLength / 1000)).ToString(@"hh\:mm\:ss");
                            }
                            catch { }
                        }));
                    };

                    _libVLCReady = true;
                }

                var warmupBrowser = new ChromiumWebBrowser("about:blank");
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
            FileUtility.ShowCursor();
            if (_isClosing)
            {
                e.Cancel = true;   // 已在退出流程中，忽略重复关闭
                return;
            }

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

            _isClosing = true;

            SaveLastFile();

            // 最多等 3 秒，让正在处理的删除批次收尾
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (FileUtility._hasDeleteBatch && DateTime.UtcNow < deadline)
            {
                Application.DoEvents();
                Thread.Sleep(100);
            }

            FileUtility.ShowCursor();

            SaveForumConfig();

            _myDb?.FlushBatch();
            _videoLoadCts?.Cancel();
            _videoLoadCts?.Dispose();
            _videoLoadCts = null;

            directoryListControl1.SaveToJson();
            SaveMetaJson();

            // ★ 关闭前保存最终进度
            if (_mediaPlayer != null && _cachedVideoLength > 0 && !string.IsNullOrEmpty(_currentVideoFingerprint))
            {
                long timeMs = (long)(_cachedVideoPosition * _cachedVideoLength);
                SaveCurrentVideoProgressByTime(timeMs);
            }
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

        private void btnBucket_Click(object sender, EventArgs e)
        {
            var files = GetValidImageFiles();
            if (files.Count == 0)
            {
                MessageBox.Show("没有图片");
                return;
            }

            using (var form = new BucketForm(files))
            {
                form.ShowDialog(this);
            }
        }
    }
}
