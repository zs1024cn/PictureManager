//Forms\CompareForm.cs
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureManager
{
    public partial class CompareForm : Form
    {
        private string _modelRootPath = Path.Combine(Application.StartupPath, "Upscayl", "resources", "models");
        private List<string> _modelPaths = new List<string>();
        private Bitmap _leftImage;
        private Bitmap _rightImage;
        private BufferedPanel _panel;
        private int _splitX;
        private string _leftFilePath;
        private string _rightFilePath;

        private ListBox _modelListBox;
        private Button _enhanceButton;
        private TextBox tbFileName;
        private TextBox tbParam;
        private Label lblSize;
        private string _currentModel;
        private Panel _dehazePanel;
        private Panel _skinPanel;
        private TextBox _dehazeInfo;
        public string AlbumReturnValue = "";
        public CompareForm(string leftFile, string rightFile)
        {
            _leftFilePath = leftFile;
            _rightFilePath = rightFile;
            this.Text = "图片对比";
            this.KeyPreview = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // 右侧对比面板
            _panel = new BufferedPanel { Dock = DockStyle.Fill };
            _panel.Paint += Panel_Paint;
            _panel.MouseMove += Panel_MouseMove;
            _panel.MouseDown += Panel_MouseDown;
            this.Controls.Add(_panel);

            this.WindowState = FormWindowState.Maximized;
            this.Text = "红线左旧右新。按F1删除新，按F2新换旧。ESC保留两个。";
            this.BackColor = Color.FromArgb(86, 116, 172);   // Win11 默认蓝 #0078D4

            // 加载左图
            _leftImage?.Dispose();
            _leftImage = FileUtility.ReadPictureToBitmap(_leftFilePath);

            // 加载右图
            _rightImage?.Dispose();
            _rightImage = FileUtility.ReadPictureToBitmap(_rightFilePath);

            _panel.Invalidate();

            this.Shown += (s, e) =>
            {
                _splitX = _panel.Width / 2;
            };

            this.FormClosing += (s, e) =>
            {
                _leftImage?.Dispose();
                _rightImage?.Dispose();
            };

            this.KeyDown += FormAlbum_KeyDown;
        }

        public CompareForm(string leftFile)
        {
            _leftFilePath = leftFile;
            
            this.Text = "图片对比增强";
            this.BackColor = Color.FromArgb(86, 116, 172);   // Win11 默认蓝 #0078D4
            this.KeyPreview = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // 左侧面板
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 200, BackColor = Color.FromArgb(40, 40, 40) };
            var headerFont = new Font("宋体", 12F, FontStyle.Bold);

            _modelListBox = new ListBox
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(180, 150),
                BackColor = Color.FromArgb(60, 60, 60),
                Font = headerFont,
                ForeColor = Color.White
            };
            if (!Directory.Exists(_modelRootPath)) return;

            // 获取目录及子目录下所有 .bin 文件
            string[] binFiles = Directory.GetFiles(_modelRootPath , "*.bin", SearchOption.AllDirectories);

            foreach (string file in binFiles)
            {
                // 去掉 .bin 扩展名
                string fileName = Path.GetFileNameWithoutExtension(file);
                _modelListBox.Items.Add(fileName);
                _modelPaths.Add(Path.GetDirectoryName(file)) ;
            }
            _modelListBox.SelectedIndex = 0;
            leftPanel.Controls.Add(_modelListBox);

            tbFileName = new TextBox
            {
                Text = _leftFilePath,
                Location = new System.Drawing.Point(10, _modelListBox.Bottom + 20),
                Multiline = true,
                Size = new System.Drawing.Size(180, 100),
                ForeColor = Color.Black,
                Font = headerFont,
                AutoSize = false
            };
            leftPanel.Controls.Add(tbFileName);

            var lblParam = new Label
            {
                Text = "-s 4 / -x TTA Mode / -t Tile Size",
                Location = new System.Drawing.Point(10, tbFileName.Bottom + 20),
                Size = new System.Drawing.Size(180, 40),
                ForeColor = Color.White,
                Font = headerFont,
                BackColor = Color.Transparent
            };
            leftPanel.Controls.Add(lblParam);

            tbParam = new TextBox
            {
                Text = " -s 1 ",
                Location = new System.Drawing.Point(10, lblParam.Bottom + 20),
                Multiline = true,
                Size = new System.Drawing.Size(180, 40),
                ForeColor = Color.Black,
                Font = headerFont,
                AutoSize = false
            };
            leftPanel.Controls.Add(tbParam);

            lblSize = new Label
            {
                Location = new System.Drawing.Point(10, tbParam.Bottom + 20),
                Size = new System.Drawing.Size(180, 25),
                ForeColor = Color.White,
                Font = headerFont,
                BackColor = Color.Transparent
            };
            leftPanel.Controls.Add(lblSize);

            _enhanceButton = new Button
            {
                Text = "增强(&T)",
                Location = new System.Drawing.Point(10, lblSize.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(180, 40)
            };
            _enhanceButton.Click += EnhanceButton_Click;
            leftPanel.Controls.Add(_enhanceButton);

            var mergeButton = new Button
            {
                Text = "复合",
                Location = new System.Drawing.Point(10, _enhanceButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(180, 40)
            };
            mergeButton.Click += MergeButton_Click;
            leftPanel.Controls.Add(mergeButton);

            var ApplyWhiteningRedButton = new Button
            {
                Text = "增红",
                Location = new System.Drawing.Point(10, mergeButton.Bottom + 20),
                ForeColor = Color.Red,
                Font = headerFont,
                Size = new System.Drawing.Size(50, 40)
            };
            ApplyWhiteningRedButton.Click += ApplyWhiteningRedButton_Click;
            leftPanel.Controls.Add(ApplyWhiteningRedButton);

            var ApplyWhiteningGreenButton = new Button
            {
                Text = "增绿",
                Location = new System.Drawing.Point(70, mergeButton.Bottom + 20),
                ForeColor = Color.Green,
                Font = headerFont,
                Size = new System.Drawing.Size(50, 40)
            };
            ApplyWhiteningGreenButton.Click += ApplyWhiteningGreenButton_Click;
            leftPanel.Controls.Add(ApplyWhiteningGreenButton);

            var ApplyWhiteningBlueButton = new Button
            {
                Text = "增蓝",
                Location = new System.Drawing.Point(130, mergeButton.Bottom + 20),
                ForeColor = Color.Blue,
                Font = headerFont,
                Size = new System.Drawing.Size(50, 40)
            };
            ApplyWhiteningBlueButton.Click += ApplyWhiteningBlueButton_Click;
            leftPanel.Controls.Add(ApplyWhiteningBlueButton);

            var ApplyWarmToneButton = new Button
            {
                Text = "暖色",
                Location = new System.Drawing.Point(10, ApplyWhiteningRedButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(80, 40)
            };
            ApplyWarmToneButton.Click += ApplyWarmToneButton_Click;
            leftPanel.Controls.Add(ApplyWarmToneButton);

            var ApplyCoolToneButton = new Button
            {
                Text = "冷色",
                Location = new System.Drawing.Point(mergeButton.Right - 80, ApplyWhiteningRedButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(80, 40)
            };
            ApplyCoolToneButton.Click += ApplyCoolToneButton_Click;
            leftPanel.Controls.Add(ApplyCoolToneButton);

            var ApplyPinkSoftButton = new Button
            {
                Text = "粉嫩",
                Location = new System.Drawing.Point(10, ApplyCoolToneButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(180, 40)
            };
            ApplyPinkSoftButton.Click += ApplyPinkSoftButton_Click;
            leftPanel.Controls.Add(ApplyPinkSoftButton);

            var ApplySoftButton = new Button
            {
                Text = "磨皮",
                Location = new System.Drawing.Point(10, ApplyPinkSoftButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(80, 40)
            };
            ApplySoftButton.Click += ApplySoftButton_Click;
            leftPanel.Controls.Add(ApplySoftButton);

            var ApplySharpenButton = new Button
            {
                Text = "锐化",
                Location = new System.Drawing.Point(mergeButton.Right - 80, ApplyPinkSoftButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(80, 40)
            };
            ApplySharpenButton.Click += ApplySharpenButton_Click;
            leftPanel.Controls.Add(ApplySharpenButton);

            var ClearButton = new Button
            {
                Text = "通透",
                Location = new System.Drawing.Point(10, ApplySharpenButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(80, 40)
            };
            ClearButton.Click += ClearButton_Click;
            leftPanel.Controls.Add(ClearButton);

            var VividButton = new Button
            {
                Text = "鲜艳",
                Location = new System.Drawing.Point(mergeButton.Right - 80, ApplySharpenButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(80, 40)
            };
            VividButton.Click += ApplyVividButton_Click;
            leftPanel.Controls.Add(VividButton);

            var PhotoButton = new Button
            {
                Text = "照片",
                Location = new System.Drawing.Point(10, ClearButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(180, 40)
            };
            PhotoButton.Click += PhotoButton_Click;
            leftPanel.Controls.Add(PhotoButton);

            // 曝光标签
            var lblExposure = new Label
            {
                Text = "曝光（点箭头调整）",
                Location = new System.Drawing.Point(10, PhotoButton.Bottom + 20),
                Size = new System.Drawing.Size(180, 25),
                ForeColor = Color.White,
                Font = headerFont,
                BackColor = Color.Transparent
            };
            leftPanel.Controls.Add(lblExposure);

            // 曝光滚动条（只允许点箭头）
            var exposureBar = new TrackBar
            {
                Minimum = -10,
                Maximum = 10,
                Value = 0,
                TickFrequency = 1,
                SmallChange = 1,
                LargeChange = 1,
                Location = new System.Drawing.Point(10, lblExposure.Bottom + 5),
                Size = new System.Drawing.Size(180, 40),
                BackColor = Color.FromArgb(40, 40, 40)
            };

            // 禁止拖动（用 MouseDown 判断是否点击了滑块）
            exposureBar.MouseDown += (s, e) =>
            {
                // 如果点击位置不在左右箭头上，取消
                // TrackBar 宽度 180，左右箭头约各占 20px
                // 简单方法：记录初始值，拖动后恢复
            };

            exposureBar.Scroll += (s, e) =>
            {
                int delta = exposureBar.Value;
                if (delta == 0) return;

                // 立即应用
                FileUtility.ApplyExposure(_rightFilePath, delta > 0 ? 1 : -1);
                LoadRightImage();

                // 重置到 0（准备下次点击）
                exposureBar.Value = 0;
            };

            leftPanel.Controls.Add(exposureBar);
            // 去雾按钮
            var DehazeButton = new Button
            {
                Text = "去雾",
                Location = new System.Drawing.Point(10, exposureBar.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(80, 40)
            };
            DehazeButton.Click += (s, e) => ApplyDehazeWithBackup();
            leftPanel.Controls.Add(DehazeButton);

            var DehazeParamButton = new Button
            {
                Text = "...",
                Location = new System.Drawing.Point(mergeButton.Right - 60, exposureBar.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(60, 40)
            };
            DehazeParamButton.Click += (s, e) => ShowDehazePanel();
            leftPanel.Controls.Add(DehazeParamButton);

            // 肤色嫩白按钮
            var LocalContrastButton = new Button
            {
                Text = "嫩白",
                Location = new System.Drawing.Point(10, DehazeButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(80, 40)
            };
            LocalContrastButton.Click += (s, e) =>
            {
                if (_skinPanel != null)
                    _skinPanel.Visible = false;

                ApplySkinWhitenWithBackup();
            };
            leftPanel.Controls.Add(LocalContrastButton);

            var SkinParamButton = new Button
            {
                Text = "...",
                Location = new System.Drawing.Point(mergeButton.Right - 60, DehazeButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(60, 40)
            };
            SkinParamButton.Click += (s, e) => ShowSkinPanel();
            leftPanel.Controls.Add(SkinParamButton);

            var RefreshButton = new Button
            {
                Text = "刷新",
                Location = new System.Drawing.Point(10, LocalContrastButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(180, 40)
            };
            RefreshButton.Click += RefreshButton_Click;
            leftPanel.Controls.Add(RefreshButton);

            this.Controls.Add(leftPanel);

            // 右侧对比面板
            _panel = new BufferedPanel { Dock = DockStyle.Fill };
            _panel.Paint += Panel_Paint;
            _panel.MouseMove += Panel_MouseMove;
            _panel.MouseDown += Panel_MouseDown;
            this.Controls.Add(_panel);

            this.WindowState = FormWindowState.Maximized;
            this.Text = "红线左旧右新。按F1删除新，按F2新换旧。ESC保留两个。";

            this.Shown += (s, e) =>
            {
                _splitX = _panel.Width / 2;
                EnhanceButton_Click(null, EventArgs.Empty);  // 自动执行首次增强
            };

            this.FormClosing += (s, e) =>
            {
                _leftImage?.Dispose();
                _rightImage?.Dispose();

                try
                {
                    string bak = _rightFilePath + ".dehaze.bak";
                    if (File.Exists(bak)) File.Delete(bak);

                    string wbak = _rightFilePath + ".whiten.bak";
                    if (File.Exists(wbak)) File.Delete(wbak);
                }
                catch { }
            };

            this.KeyDown += Form_KeyDown;
        }
        private void ApplyWhiteningRedButton_Click(object sender, EventArgs e)
        {
            FileUtility.ApplyColorEnhance(_rightFilePath,Color.Red);
            LoadRightImage();
        }
        private void ApplyWhiteningGreenButton_Click(object sender, EventArgs e)
        {
            FileUtility.ApplyColorEnhance(_rightFilePath, Color.Green);
            LoadRightImage();
        }
        private void ApplyWhiteningBlueButton_Click(object sender, EventArgs e)
        {
            FileUtility.ApplyColorEnhance(_rightFilePath, Color.Blue);
            LoadRightImage();
        }
        private void ApplyWarmToneButton_Click(object sender, EventArgs e)
        {
            FileUtility.ApplyWarmTone(_rightFilePath);
            LoadRightImage();
        }
        private void ApplyCoolToneButton_Click(object sender, EventArgs e)
        {
            FileUtility.ApplyCoolTone(_rightFilePath);
            LoadRightImage();
        }
        private void ApplyPinkSoftButton_Click(object sender, EventArgs e)
        {
            FileUtility.ApplyPinkSoft(_rightFilePath);
            LoadRightImage();
        }
        private void ApplySoftButton_Click(object sender, EventArgs e)
        {
            FileUtility.ApplySoftSkin(_rightFilePath);
            LoadRightImage();
        }
        private void ApplySharpenButton_Click(object sender, EventArgs e)
        {
            FileUtility.ApplySharpen(_rightFilePath);
            LoadRightImage();
        }
        private void ClearButton_Click(object sender, EventArgs e)
        {
            FileUtility.MakeClear(_rightFilePath);
            LoadRightImage();
        }
        // 嫩白：第一次点先把当前右图备份，之后每次都从备份重算，连点不叠加
        private void ApplySkinWhitenWithBackup()
        {
            try
            {
                string bak = _rightFilePath + ".whiten.bak";

                if (!File.Exists(bak))
                    File.Copy(_rightFilePath, bak, true);   // 记录"第一次嫩白前"的状态

                File.Copy(bak, _rightFilePath, true);       // 回到基准
                FileUtility.ApplySkinWhiten(_rightFilePath, FileUtility.SkinStrength);
                LoadRightImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show("嫩白失败: " + ex.Message);
            }
        }
        // 去雾：第一次点先把原图备份，之后每次都从备份重算，参数改了可以反复试
        private void ApplyDehazeWithBackup()
        {
            if (_dehazePanel != null && !_dehazePanel.IsDisposed)
                _dehazePanel.Visible = false;

            try
            {
                // ★ 诊断：点击时把当前参数写进标题栏
                this.Text = string.Format(
                    "参数 omega={0:F2} tMin={1:F2} blend={2:F2} clahe={3:F2} sat={4:F2} warm={5} expo={6:F2} skin={7:F2}",
                    FileUtility.DehazeOmega, FileUtility.DehazeTMin, FileUtility.DehazeBlend,
                    FileUtility.DehazeClaheClip, FileUtility.DehazeSatBoost, FileUtility.DehazeWarm,
                    FileUtility.DehazeExposure, FileUtility.DehazeSkinLift);

                string bak = _rightFilePath + ".dehaze.bak";
                if (!File.Exists(bak))
                    File.Copy(_rightFilePath, bak, true);

                File.Copy(bak, _rightFilePath, true);
                FileUtility.ApplyDehaze(_rightFilePath);
                LoadRightImage();

                // ★ 诊断：出图后把文件大小附在标题上（能看出是否重新写过文件）
                this.Text += "  |  " + new FileInfo(_rightFilePath).Length + " bytes";
            }
            catch (Exception ex)
            {
                MessageBox.Show("去雾失败: " + ex.Message);
            }
        }

        private void ShowDehazePanel()
        {
            if (_dehazePanel == null || _dehazePanel.IsDisposed)
                _dehazePanel = BuildDehazePanel();

            _dehazePanel.Visible = true;
            _dehazePanel.BringToFront();
        }

        private Panel BuildDehazePanel()
        {
            double ds = this.DeviceDpi / 96.0;   // 125% 缩放 → 1.25
            if (ds < 1) ds = 1;
            if (ds > 3) ds = 3;

            var titleFont = new Font("宋体", 12F, FontStyle.Bold);
            var textFont = new Font("宋体", 9F);

            int lblH = (int)(24 * ds);
            int tbH = (int)(48 * ds);
            int gap = (int)(10 * ds);
            int rowH = lblH + tbH + gap;

            int headerH = (int)(48 * ds);
            int footerH = (int)(86 * ds);
            int hostW = (int)(420 * ds);
            int hostH = headerH + 8 * rowH + footerH + 40;
            var host = new Panel
            {
                Size = new System.Drawing.Size(hostW, hostH),
                BackColor = Color.FromArgb(45, 45, 45),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            var title = new Label
            {
                Text = "去雾参数 (DPI " + this.DeviceDpi + ")",
                ForeColor = Color.White,
                Font = titleFont,
                Location = new System.Drawing.Point((int)(12 * ds), (int)(10 * ds)),
                AutoSize = true
            };
            host.Controls.Add(title);

            var close = new Button
            {
                Text = "×",
                Location = new System.Drawing.Point(hostW - (int)(44 * ds), (int)(6 * ds)),
                Size = new System.Drawing.Size((int)(32 * ds), (int)(28 * ds)),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                FlatStyle = FlatStyle.Flat,
                Font = titleFont
            };
            close.Click += (s, e) =>
            {
                if (_dehazePanel != null && !_dehazePanel.IsDisposed)
                    _dehazePanel.Visible = false;
            };
            host.Controls.Add(close);

            int y = headerH;
            AddDehazeSlider(host, "总强度DehazeStrength", 0.00, 1.00, 0.01, textFont, ds,
    () => FileUtility.DehazeStrength, v => FileUtility.DehazeStrength = v, ref y);
            AddDehazeSlider(host, "去雾强度DehazeOmega", 0.50, 0.95, 0.01, textFont, ds,
                () => FileUtility.DehazeOmega, v => FileUtility.DehazeOmega = v, ref y);
            AddDehazeSlider(host, "传输下限DehazeTMin", 0.30, 0.70, 0.01, textFont, ds,
                () => FileUtility.DehazeTMin, v => FileUtility.DehazeTMin = v, ref y);
            AddDehazeSlider(host, "与原图混合DehazeBlend", 0.20, 0.90, 0.01, textFont, ds,
                () => FileUtility.DehazeBlend, v => FileUtility.DehazeBlend = v, ref y);
            AddDehazeSlider(host, "曝光补偿DehazeExposure", 0.90, 1.20, 0.01, textFont, ds,
                () => FileUtility.DehazeExposure, v => FileUtility.DehazeExposure = v, ref y);
            AddDehazeSlider(host, "局部对比DehazeClaheClip", 1.00, 3.00, 0.05, textFont, ds,
                () => FileUtility.DehazeClaheClip, v => FileUtility.DehazeClaheClip = v, ref y);
            AddDehazeSlider(host, "饱和度DehazeSatBoost", 0.00, 0.30, 0.01, textFont, ds,
                () => FileUtility.DehazeSatBoost, v => FileUtility.DehazeSatBoost = v, ref y);
            AddDehazeSlider(host, "暖调DehazeWarm", -6, 6, 1, textFont, ds,
                () => FileUtility.DehazeWarm, v => FileUtility.DehazeWarm = (int)Math.Round(v), ref y);
            AddDehazeSlider(host, "肤色保护DehazeSkinLift", 0.50, 1.00, 0.01, textFont, ds,
                () => FileUtility.DehazeSkinLift, v => FileUtility.DehazeSkinLift = v, ref y);

            int btnW = (int)(120 * ds);
            int btnH = (int)(34 * ds);

            var restore = new Button
            {
                Text = "恢复原图",
                Location = new System.Drawing.Point((int)(12 * ds), y + (int)(6 * ds)),
                Size = new System.Drawing.Size(btnW, btnH),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                FlatStyle = FlatStyle.Flat,
                Font = textFont
            };
            restore.Click += (s, e) =>
            {
                try
                {
                    string bak = _rightFilePath + ".dehaze.bak";
                    if (File.Exists(bak))
                    {
                        File.Copy(bak, _rightFilePath, true);
                        LoadRightImage();
                    }
                }
                catch (Exception ex) { MessageBox.Show("恢复失败: " + ex.Message); }
            };
            host.Controls.Add(restore);

            var reset = new Button
            {
                Text = "重置参数",
                Location = new System.Drawing.Point((int)(12 * ds) + btnW + (int)(10 * ds), y + (int)(6 * ds)),
                Size = new System.Drawing.Size(btnW, btnH),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                FlatStyle = FlatStyle.Flat,
                Font = textFont
            };
            reset.Click += (s, e) =>
            {
                FileUtility.ResetDehazeDefaults();

                _dehazePanel.Visible = false;
                _dehazePanel.Dispose();
                _dehazePanel = null;

                if (_dehazeInfo != null && !_dehazeInfo.IsDisposed)
                {
                    _dehazeInfo.Visible = false;
                    _dehazeInfo.Dispose();
                }
                _dehazeInfo = null;

                ShowDehazePanel();
            };
            host.Controls.Add(reset);

            // ===== 右侧：说明框 + 下方按钮 =====
            int infoW = (int)(360 * ds);
            int gapW = (int)(8 * ds);
            int btnGapY = (int)(8 * ds);
            btnH = restore.Height;
            int infoH = hostH - btnH - btnGapY;

            var info = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Gainsboro,
                Font = new Font("宋体", 9F),
                Size = new System.Drawing.Size(infoW, infoH),
                Text =
                    "想要的效果  →  改哪个参数" + Environment.NewLine + Environment.NewLine +
                    "总强度DehazeStrength：0=原图，1=满效果" + Environment.NewLine + Environment.NewLine +
                    "更通透（雾更少）" + Environment.NewLine +
                    "   DehazeOmega 0.78 → 0.85" + Environment.NewLine +
                    "   或 DehazeBlend 0.50 → 0.60" + Environment.NewLine + Environment.NewLine +
                    "更自然（少动原图）" + Environment.NewLine +
                    "   DehazeBlend → 0.40" + Environment.NewLine + Environment.NewLine +
                    "暗部死黑" + Environment.NewLine +
                    "   DehazeTMin 0.50 → 0.55" + Environment.NewLine + Environment.NewLine +
                    "更鲜艳" + Environment.NewLine +
                    "   DehazeSatBoost 0.08 → 0.12" + Environment.NewLine + Environment.NewLine +
                    "更暖 / 更冷" + Environment.NewLine +
                    "   DehazeWarm 0 → 2 / -2" + Environment.NewLine + Environment.NewLine +
                    "脸还是偏色" + Environment.NewLine +
                    "   DehazeSkinLift 0.85 → 0.95"
            };
            _dehazeInfo = info;

            int totalW = hostW + gapW + infoW;
            int left = Math.Max(0, (this.ClientSize.Width - totalW) / 2);
            int top = Math.Max(0, (this.ClientSize.Height - hostH) / 2);

            // 外层容器：统一承载参数面板、说明框、两个按钮，显隐只需管它
            var outer = new Panel
            {
                Location = new System.Drawing.Point(left, top),
                Size = new System.Drawing.Size(totalW, hostH),
                BackColor = Color.Transparent
            };

            host.Location = new System.Drawing.Point(0, 0);
            info.Location = new System.Drawing.Point(hostW + gapW, 0);

            outer.Controls.Add(host);
            outer.Controls.Add(info);
            outer.Controls.Add(restore);   // 从 host 里搬出来
            outer.Controls.Add(reset);     // 同上

            restore.Location = new System.Drawing.Point(hostW + gapW, infoH + btnGapY);
            reset.Location = new System.Drawing.Point(
                hostW + gapW + restore.Width + (int)(8 * ds), infoH + btnGapY);

            this.Controls.Add(outer);
            outer.BringToFront();
            return outer;
        }
        private void ShowSkinPanel()
        {
            if (_skinPanel == null || _skinPanel.IsDisposed)
                _skinPanel = BuildSkinPanel();

            _skinPanel.Visible = true;
            _skinPanel.BringToFront();
        }

        private Panel BuildSkinPanel()
        {
            double ds = this.DeviceDpi / 96.0;
            if (ds < 1) ds = 1;
            if (ds > 3) ds = 3;

            var titleFont = new Font("宋体", 12F, FontStyle.Bold);
            var textFont = new Font("宋体", 9F);

            int lblH = (int)(24 * ds);
            int tbH = (int)(48 * ds);
            int gap = (int)(16 * ds);
            int rowH = lblH + tbH + gap;

            int headerH = (int)(48 * ds);
            int footerH = (int)(70 * ds);
            int hostW = (int)(420 * ds);
            int hostH = headerH + 6 * rowH + footerH;

            var host = new Panel
            {
                Size = new System.Drawing.Size(hostW, hostH),
                BackColor = Color.FromArgb(45, 45, 45),
                BorderStyle = BorderStyle.FixedSingle
            };
            host.Location = new System.Drawing.Point(
                Math.Max(0, (this.ClientSize.Width - hostW) / 2),
                Math.Max(0, (this.ClientSize.Height - hostH) / 2));

            var title = new Label
            {
                Text = "嫩白参数 (DPI " + this.DeviceDpi + ")",
                ForeColor = Color.White,
                Font = titleFont,
                Location = new System.Drawing.Point((int)(12 * ds), (int)(10 * ds)),
                AutoSize = true
            };
            host.Controls.Add(title);

            var close = new Button
            {
                Text = "×",
                Location = new System.Drawing.Point(hostW - (int)(44 * ds), (int)(6 * ds)),
                Size = new System.Drawing.Size((int)(32 * ds), (int)(28 * ds)),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                FlatStyle = FlatStyle.Flat,
                Font = titleFont
            };
            close.Click += (s, e) => { if (_skinPanel != null && !_skinPanel.IsDisposed) _skinPanel.Visible = false; };
            host.Controls.Add(close);

            int y = headerH;

            AddDehazeSlider(host, "提亮强度SkinLGamma", 0.30, 0.95, 0.01, textFont, ds,
    () => FileUtility.SkinLGamma, v => FileUtility.SkinLGamma = v, ref y);
            AddDehazeSlider(host, "去黄SkinBAlpha", 0.40, 0.95, 0.01, textFont, ds,
                () => FileUtility.SkinBAlpha, v => FileUtility.SkinBAlpha = v, ref y);
            AddDehazeSlider(host, "加红SkinABeta", -5, 15, 0.5, textFont, ds,
    () => FileUtility.SkinABeta, v => FileUtility.SkinABeta = v, ref y);
            AddDehazeSlider(host, "加黄SkinBShift", -10, 10, 0.5, textFont, ds,
                () => FileUtility.SkinBShift, v => FileUtility.SkinBShift = v, ref y);
            AddDehazeSlider(host, "偏色SkinAAlpha", 0.70, 1.00, 0.01, textFont, ds,
                () => FileUtility.SkinAAlpha, v => FileUtility.SkinAAlpha = v, ref y);
            AddDehazeSlider(host, "整体强度", 0.20, 1.50, 0.01, textFont, ds,
                () => FileUtility.SkinStrength, v => FileUtility.SkinStrength = v, ref y);

            var reset = new Button
            {
                Text = "重置参数",
                Location = new System.Drawing.Point((int)(12 * ds), y + (int)(6 * ds)),
                Size = new System.Drawing.Size((int)(120 * ds), (int)(34 * ds)),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                FlatStyle = FlatStyle.Flat,
                Font = textFont
            };
            reset.Click += (s, e) =>
            {
                FileUtility.ResetSkinDefaults();

                _skinPanel.Visible = false;
                _skinPanel.Dispose();
                _skinPanel = null;

                ShowSkinPanel();
            };
            host.Controls.Add(reset);

            this.Controls.Add(host);
            host.BringToFront();
            return host;
        }
        private void AddDehazeSlider(Panel host, string caption, double min, double max, double step,
                             Font font, double ds, Func<double> getter, Action<double> setter, ref int y)
        {
            int lblH = (int)(24 * ds);
            int tbH = (int)(48 * ds);
            int gap = (int)(16 * ds);
            int rowH = lblH + tbH + gap;

            var lbl = new Label
            {
                Text = caption,
                ForeColor = Color.White,
                Font = font,
                Location = new System.Drawing.Point((int)(12 * ds), y),
                AutoSize = true
            };
            host.Controls.Add(lbl);

            var val = new Label
            {
                Text = getter().ToString("F2"),
                ForeColor = Color.LightGreen,
                Font = font,
                Location = new System.Drawing.Point((int)(310 * ds), y),
                AutoSize = true
            };
            host.Controls.Add(val);

            int steps = (int)Math.Round((max - min) / step);
            if (steps < 1) steps = 1;

            var tb = new TrackBar
            {
                Minimum = 0,
                Maximum = steps,
                Value = Math.Max(0, Math.Min(steps, (int)Math.Round((getter() - min) / step))),
                Location = new System.Drawing.Point((int)(8 * ds), y + lblH),
                Size = new System.Drawing.Size((int)(384 * ds), tbH),
                AutoSize = false,          // ★ 关键：不许它自己改高度
                TickStyle = TickStyle.None,
                SmallChange = 1,
                LargeChange = Math.Max(1, steps / 20)
            };
            tb.Height = tbH;               // ★ 显式再设一次，保险
            tb.Scroll += (s, e) =>
            {
                double v = min + tb.Value * step;
                setter(v);
                val.Text = v.ToString("F2");
            };
            host.Controls.Add(tb);

            y += rowH;                     // ★ 行距 = 标签 + 滑块 + 间距
        }
        private void ApplyVividButton_Click(object sender, EventArgs e)
        {
            FileUtility.ApplyVivid(_rightFilePath);
            LoadRightImage();
        }
        private async void PhotoButton_Click(object sender, EventArgs e)
        {
            _leftFilePath = tbFileName.Text.Trim();
            _rightFilePath = _leftFilePath + $".Upscayl.jpg";

            File.Delete(_rightFilePath);
            File.Copy(_leftFilePath, _rightFilePath);

            // 打开照片应用
            Process.Start($"ms-photos:viewer?fileName={Uri.EscapeDataString(_rightFilePath)}");
        }
        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadRightImage();
        }
        private void LoadRightImage()
        {
            try
            {
                _rightImage?.Dispose();
                _rightImage = FileUtility.ReadPictureToBitmap(_rightFilePath);
                _panel.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载右图失败：{ex.Message}");
            }
        }
        private void EnhanceButton_Click(object sender, EventArgs e)
        {
            _leftFilePath = tbFileName.Text.Trim();
            _rightFilePath = _leftFilePath + $".Upscayl.jpg";

            _currentModel = _modelListBox.SelectedItem?.ToString() ?? "realesrgan-x4plus";
            string currentModelPath = _modelPaths[_modelListBox.SelectedIndex];
            if (FileUtility.EnhanceByUpScaylbin(_leftFilePath, _rightFilePath, _currentModel, currentModelPath, tbParam.Text))
            {
                // 加载左图
                _leftImage?.Dispose();
                _leftImage = FileUtility.ReadPictureToBitmap(_leftFilePath);
                if (_leftImage != null)
                    this.lblSize.Text = $"{_leftImage.Width}×{_leftImage.Height}";

                // 加载右图
                _rightImage?.Dispose();
                _rightImage = FileUtility.ReadPictureToBitmap(_rightFilePath);

                _panel.Invalidate();
            }
        }
        private void MergeButton_Click(object sender, EventArgs e)
        {
            _leftFilePath = tbFileName.Text.Trim();
            _rightFilePath = _leftFilePath + $".Upscayl.jpg";

            var filelist = new List<string>();
            for (int i = 0; i < _modelListBox.Items.Count; i++)
            {
                string currentModel = _modelListBox.Items[i].ToString();
                string currentModelPath = _modelPaths[i];
                string output = _leftFilePath + "upscayl-" + i.ToString() + ".jpg";
                if (FileUtility.EnhanceByUpScaylbin(_leftFilePath, output, currentModel, currentModelPath,tbParam.Text))
                    filelist.Add(output);
            }
            FileUtility.MergeImagesAverage(filelist, _rightFilePath);

            // 删除源文件
            foreach (var file in filelist)
            {
                try { File.Delete(file); }
                catch { }
            }
            
            // 加载左图
            _leftImage?.Dispose();
            _leftImage = FileUtility.ReadPictureToBitmap(_leftFilePath);
            if (_leftImage != null)
                this.lblSize.Text = $"{_leftImage.Width}×{_leftImage.Height}";

            // 加载右图
            _rightImage?.Dispose();
            _rightImage = FileUtility.ReadPictureToBitmap(_rightFilePath);

            _panel.Invalidate();
        }
        private void FormAlbum_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                AlbumReturnValue = "both";
                this.Close();
            }
            else if (e.KeyCode == Keys.F1)
            {
                AlbumReturnValue = "left";
                this.Close();
            }
            else if (e.KeyCode == Keys.F2)
            {
                AlbumReturnValue = "right";
                this.Close();
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
            else if (e.KeyCode == Keys.F1)
            {
                _rightImage?.Dispose();
                File.Delete(_rightFilePath);
                this.Close();
            }
            else if (e.KeyCode == Keys.F2)
            {
                _leftImage?.Dispose();
                File.Delete(_leftFilePath);
                File.Move(_rightFilePath, _leftFilePath);
                this.Close();
            }
            else if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.R)
                && (!(this.ActiveControl is TextBox) && !(this.ActiveControl is TextBox) && !e.Alt))
            {
                _splitX -= 100;
                if (_splitX < 0) _splitX = 0;
                _panel.Invalidate();
                e.Handled = true;
                e.SuppressKeyPress = true;  // 防止 ListBox 响应 R 键
            }
            else if ((e.KeyCode == Keys.Right || e.KeyCode == Keys.T)
                && (!(this.ActiveControl is TextBox) && !(this.ActiveControl is TextBox) && !e.Alt))
            {
                _splitX += 100;
                if (_splitX > _panel.Width) _splitX = _panel.Width;
                _panel.Invalidate();
                e.Handled = true;
                e.SuppressKeyPress = true;  // 防止 ListBox 响应 T 键
            }
        }

        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            if (_leftImage == null) return;

            var g = e.Graphics;

            // 只有左图
            if (_rightImage == null)
            {
                DrawSingleImage(g, _leftImage);
                return;
            }

            // 左右对比
            float ratio = Math.Min(
                (float)_panel.Width / Math.Max(_leftImage.Width, _rightImage.Width),
                (float)_panel.Height / Math.Max(_leftImage.Height, _rightImage.Height)
            );

            int imgW = (int)(Math.Max(_leftImage.Width, _rightImage.Width) * ratio);
            int imgH = (int)(Math.Max(_leftImage.Height, _rightImage.Height) * ratio);
            int offsetX = (_panel.Width - imgW) / 2;
            int offsetY = (_panel.Height - imgH) / 2;

            var leftRect = new Rectangle(offsetX, offsetY, imgW, imgH);

            g.SetClip(new Rectangle(0, 0, _splitX, _panel.Height));
            g.DrawImage(_leftImage, leftRect);
            g.ResetClip();

            g.SetClip(new Rectangle(_splitX, 0, _panel.Width - _splitX, _panel.Height));
            g.DrawImage(_rightImage, leftRect);
            g.ResetClip();

            using (var pen = new Pen(Color.Red, 2))
            {
                g.DrawLine(pen, _splitX, 0, _splitX, _panel.Height);
            }
        }

        private void DrawSingleImage(Graphics g, Image img)
        {
            float ratio = Math.Min(
                (float)_panel.Width / img.Width,
                (float)_panel.Height / img.Height
            );
            int imgW = (int)(img.Width * ratio);
            int imgH = (int)(img.Height * ratio);
            int offsetX = (_panel.Width - imgW) / 2;
            int offsetY = (_panel.Height - imgH) / 2;
            g.DrawImage(img, offsetX, offsetY, imgW, imgH);
        }

        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            _splitX = e.X;
            _panel.Invalidate();
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _splitX = e.X;
                _panel.Invalidate();
            }
        }

        private class BufferedPanel : Panel
        {
            public BufferedPanel()
            {
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            }
        }
    }
}