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
                Size = new System.Drawing.Size(180, 40)
            };
            ClearButton.Click += ClearButton_Click;
            leftPanel.Controls.Add(ClearButton);

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
                Size = new System.Drawing.Size(180, 40)
            };
            DehazeButton.Click += (s, e) =>
            {
                FileUtility.ApplyDehaze(_rightFilePath);
                LoadRightImage();
            };
            leftPanel.Controls.Add(DehazeButton);

            // 局部提亮按钮
            var LocalContrastButton = new Button
            {
                Text = "局部提亮",
                Location = new System.Drawing.Point(10, DehazeButton.Bottom + 20),
                ForeColor = Color.White,
                Font = headerFont,
                Size = new System.Drawing.Size(180, 40)
            };
            LocalContrastButton.Click += (s, e) =>
            {
                FileUtility.ApplyLocalContrast(_rightFilePath);
                LoadRightImage();
            };
            leftPanel.Controls.Add(LocalContrastButton);
            
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