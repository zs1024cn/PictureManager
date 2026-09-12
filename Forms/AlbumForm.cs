using Microsoft.VisualBasic.FileIO;
//using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureManager
{
    public partial class AlbumForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam); public string ReturnValue { get; private set; } = "";

        public List<string> DirectoryList { get; set; }
        public string TmpDirectory { get; set; }
        public string SelectedFile = "";

        private string _directory;
        private List<string> _imageFiles = new List<string>();
        private List<PictureBox> _thumbnails = new List<PictureBox>();
        private int _thumbnailSize = 240;
        private HashSet<string> _selectedFiles = new HashSet<string>();
        private int _FitCount = 30;
        private PictureBox _previewBox;
        private static int _previewBoxWidth = 1024;
        private static int _previewBoxHeight = 768;
        private Form1 _mainForm;
        private FlowLayoutPanel _panel;
        private ToolTip _toolTip;
        private ToolStrip _toolStrip;
        private int _pagePosition = 0;
        //private bool _isCreating = false;
        private int _lastThumbnailSize = 0;
        //private System.Windows.Forms.Timer _refreshTimer;

        // === 全局缩略图缓存（不分目录）===
        private static Dictionary<string, Image> _thumbnailCacheFile = new Dictionary<string, Image>();
        private static List<string> _thumbnailCacheFileOrder = new List<string>();
        private static int _maxCachedFiles = 1000;
        public static int ThumbnailLimit = 100; //每页显示几个缩略图
        private List<string> _sourceFiles;
        private bool _filesDeletedFired = false;
        // === 分页 ===
        private int _currentPage = 0;
        private int _pageSize => ThumbnailLimit;
        private int _totalPages => (_imageFiles.Count + _pageSize - 1) / _pageSize;

        private List<string> _deletedFiles = new List<string>();
        public event Action<List<string>> FilesDeleted;

        // 多文件构造
        public AlbumForm(List<string> files, Form1 mainForm)
        {
            InitCommon(mainForm);
            _sourceFiles = files;

            // 从文件列表推断目录
            var dirs = files.Select(f => Path.GetDirectoryName(f)).Distinct().ToList();
            if (dirs.Count == 1)
            {
                _directory = dirs[0];
                AddDeleteByTagButton();  // 单目录时自动分词
            }
            else
            {
                _directory = "多目录预览";
            }
        }

        public void InitCommon(Form1 mainForm)
        {
            typeof(ImageAnimator).GetField("s_anyAnimation", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.SetValue(null, false);

            _toolTip = new ToolTip { AutoPopDelay = 3000, InitialDelay = 500 };
            _mainForm = mainForm;

            _previewBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new System.Drawing.Size(_previewBoxWidth, _previewBoxHeight),
                Visible = false,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
            _previewBox.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    _previewBox.Visible = false;
                    _previewBox.Image = null;
                    return;
                }
                var ctrl = _previewBox.Tag as Control;
                if (ctrl != null)
                {
                    if (ctrl is PictureBox pb)
                        HandleThumbnailClick(pb);
                    else if (ctrl is Panel pnl)
                        TogglePanelSelect(pnl);
                }
                _previewBox.Visible = false;
                _previewBox.Image = null;
            };
            _previewBox.PreviewKeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F4 && !e.Alt )
                {
                    var ctrl = _previewBox.Tag as Control;
                    if (ctrl != null)
                    {
                        if (ctrl?.Tag is string file)
                        {
                            string directory = Path.GetDirectoryName(file);
                            FileUtility.ExploreDirectory(directory);
                        }
                    }
                }
                else if (e.KeyCode == Keys.Return)
                {
                    var ctrl = _previewBox.Tag as Control;
                    if (ctrl != null)
                    {
                        if (ctrl?.Tag is string file)
                        {
                            SelectedFile = file;
                            ReturnValue = "Select";
                            this.Close();
                        }
                    }
                }
            };
            this.Controls.Add(_previewBox);
            _previewBox.BringToFront();

            InitializeComponent();
            this.KeyPreview = true;

            _toolStrip = new ToolStrip();
            var btnSelectAll = new ToolStripButton("全选(Ctrl+A)");
            var btnSelectNone = new ToolStripButton("全不选");
            var btnDelete = new ToolStripButton("删除未选的图片(F3)");
            var lblInfo = new ToolStripLabel();
            btnSelectAll.Click += (s, e) => SetAllChecked(true);
            btnSelectNone.Click += (s, e) => SetAllChecked(false);
            btnDelete.Click += BtnDelete_Click;
            _toolStrip.Items.Add(btnSelectAll);
            _toolStrip.Items.Add(btnSelectNone);
            _toolStrip.Items.Add(btnDelete);
            _toolStrip.Items.Add(lblInfo);
            var btnDeleteByInput = new ToolStripButton("删除关键字(输入)");
            btnDeleteByInput.Click += (s, e) =>
            {
                string tag = Microsoft.VisualBasic.Interaction.InputBox("请输入关键字", "删除关键字", "");
                if (!string.IsNullOrEmpty(tag))
                    FileUtility.DeleteDirectoryByTag(DirectoryList, tag, TmpDirectory);
            };
            _toolStrip.Items.Add(btnDeleteByInput);
            this.Controls.Add(_toolStrip);

            _panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Black };
            this.Controls.Add(_panel);
            _panel.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    _previewBox.Visible = false;
                    _previewBox.Image = null;
                    return;
                }
            };

            this.Load += (s, e) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    LoadImages();
                    CalculateThumbnailSize();
                    ShowPage(0);
                }));
            };

            this.FormClosing += (s, e) =>
            {
                if (!_filesDeletedFired && _deletedFiles.Count > 0)
                {
                    _filesDeletedFired = true;
                    FilesDeleted?.Invoke(_deletedFiles);
                }
            };

            this.Resize += (s, e) => AdjustThumbnailSize(_panel);
            this.KeyDown += AlbumForm_KeyDown;
        }

        private void AddDeleteByTagButton()
        {
            if (string.IsNullOrEmpty(_directory) || _directory == "相似图片" || _directory == "多目录预览") return;
            string dirName = Path.GetFileName(_directory);
            var tags = dirName.Split(new[] { ' ', '_', '-', '+', '[', ']', '（', '）', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                              .Where(t => t.Length > 1)
                              .Distinct()
                              .ToList();
            if (tags.Count == 0) return;

            var btnDeleteByTag = new ToolStripDropDownButton("删除关键字");
            var tagMenu = new ContextMenuStrip();
            foreach (var tag in tags)
            {
                var item = tagMenu.Items.Add(tag);
                item.Click += (s, e) =>
                {
                    FileUtility.DeleteDirectoryByTag(DirectoryList, tag, TmpDirectory);
                };
            }
            btnDeleteByTag.DropDown = tagMenu;
            _toolStrip.Items.Add(btnDeleteByTag);
        }

        private void CalculateThumbnailSize()
        {
            if (_imageFiles == null || _imageFiles.Count == 0) return;
            _panel.Padding = new Padding(0, 25, 0, 25);

            int count = Math.Min(_imageFiles.Count, _pageSize);
            int clientWidth = _panel.ClientSize.Width - 20;
            int clientHeight = _panel.ClientSize.Height - 50;

            if (count <= _FitCount)
            {
                int cols = Math.Max(1, (int)Math.Sqrt(count * (clientWidth / (double)clientHeight)));
                int rows = (int)Math.Ceiling((double)count / cols);
                int w = (clientWidth - cols * 6) / cols;
                int h = (clientHeight - rows * 6) / rows;
                _thumbnailSize = Math.Min(w, h);
            }

            if (_thumbnailSize < 50) _thumbnailSize = 50;
            if (_thumbnailSize > 400) _thumbnailSize = 400;
            _lastThumbnailSize = _thumbnailSize;
        }

        // === 键盘事件 ===
        private void AlbumForm_KeyDown(object sender, KeyEventArgs e)
        {
            //Debug.WriteLine($"KeyDown sender: {sender.GetType().Name}, key={e.KeyCode}");

            if (e.KeyCode == Keys.F1)
            {
                string s = "Ctrl+A：全选\nF3：删除未选中的文件\n";
                s += "F4：调用资源管理器打开目录\nF5：刷新\nF8：对比\n";
                s += "A/D 或 ←/→：翻页\nESC or V：关闭";
                MessageBox.Show(s);
            }
            else if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.V)
            {
                if (this._previewBox.Visible)
                    this._previewBox.Visible = false;
                else
                    this.Close();
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                SetAllChecked(true);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                if (true || _selectedFiles.Count != 0 || _directory == "多目录预览" || _directory == "相似图片" || _totalPages > 1)
                {
                    BtnDelete_Click(sender, e);
                    if (_imageFiles.Count == 0 && _totalPages == 0) this.Close();
                }
                /*                else //单目录且分页为1,传递F3出去，让外面去删除该目录
                                {
                                    ReturnValue = "F3";
                                    this.Close();
                                }
                */
            }
            else if (e.KeyCode == Keys.F4 && !e.Alt)
            {
                if (_directory != "多目录预览" && _directory != "相似图片")
                {
                    //string directory = Path.GetDirectoryName(_directory);
                    FileUtility.ExploreDirectory(_directory);
                }

            }
            else if (e.KeyCode == Keys.F5)
            {
                _thumbnailCacheFile.Clear();
                _thumbnailCacheFileOrder.Clear();
                LoadImages();
                ShowPage(_currentPage);
            }
            else if (e.KeyCode == Keys.F8)
            {
                // 取当前页最后2个选中的图片
                var selectedOnPage = _imageFiles
                    .Where(f => _selectedFiles.Contains(f))
                    .ToList();

                if (selectedOnPage.Count < 2)
                {
                    MessageBox.Show("当前页至少需要选中2张图片");
                    return;
                }

                string leftFile = selectedOnPage[selectedOnPage.Count - 2];
                string rightFile = selectedOnPage[selectedOnPage.Count - 1];

                using (var compare = new CompareForm(leftFile, rightFile))
                {
                    compare.ShowDialog();
                    // 根据返回值处理选中状态
                    switch (compare.AlbumReturnValue)
                    {
                        case "left":
                            // 右图设为非选中
                            _selectedFiles.Remove(rightFile);
                            UpdateThumbnailState(rightFile, false);
                            break;
                        case "right":
                            // 左图设为非选中
                            _selectedFiles.Remove(leftFile);
                            UpdateThumbnailState(leftFile, false);
                            break;
                        case "both":
                            // 保持两个选中状态，什么都不做
                            break;
                    }
                }
            }
            else if (e.KeyCode == Keys.Home || e.KeyCode == Keys.D4)
            {
                _pagePosition = 0;
                _panel.AutoScrollPosition = new System.Drawing.Point(0, 0);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.End || e.KeyCode == Keys.D3)
            {
                _pagePosition = 3;
                _panel.AutoScrollPosition = new System.Drawing.Point(0, _panel.VerticalScroll.Maximum);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Down || e.KeyCode == Keys.S)
            {
                _pagePosition = Math.Min(3, _pagePosition + 1);
                ScrollDown();
                //ScrollToPosition();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageUp || e.KeyCode == Keys.Up || e.KeyCode == Keys.W)
            {
                _pagePosition = Math.Max(0, _pagePosition - 1);
                ScrollUp();
                //ScrollToPosition();
                e.Handled = true;
            }
            // 翻页
            else if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
            {
                if (_currentPage > 0)
                {
                    _currentPage--;
                    ShowPage(_currentPage);
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right)
            {
                if (_currentPage < _totalPages - 1)
                {
                    _currentPage++;
                    ShowPage(_currentPage);
                }
                e.Handled = true;
            }
            //Console.WriteLine($"_pagePosition={_pagePosition}");
        }
        private void UpdateThumbnailState(string file, bool selected)
        {
            var pb = _thumbnails.FirstOrDefault(t => (string)t.Tag == file);
            if (pb == null) return;

            if (selected)
            {
                SetPicturebox(file, pb, true);
                pb.Paint -= DrawCheckMark;  // 防止重复订阅
                pb.Paint += DrawCheckMark;
            }
            else
            {
                SetPicturebox(file, pb, false);
                pb.Paint -= DrawCheckMark;
            }
            pb.Invalidate();
        }
        private int _lastPagePosition = 0;

        private void ScrollToPosition()
        {
            if (_thumbnails.Count == 0) return;

            int index;
            switch (_pagePosition)
            {
                case 0:
                    _panel.AutoScrollPosition = new System.Drawing.Point(0, 0);
                    _lastPagePosition = 0;
                    return;
                case 1:
                    index = _thumbnails.Count * 1 / 3;
                    if (_lastPagePosition == 0)  // 从顶部来，先到底再回 1/3
                        _panel.ScrollControlIntoView(_thumbnails[_thumbnails.Count - 1]);
                    else if (_lastPagePosition > 1)  // 从底部来，先到顶再下到 1/3
                        _panel.AutoScrollPosition = new System.Drawing.Point(0, 0);
                    break;
                case 2:
                    index = _thumbnails.Count * 2 / 3;
                    if (_lastPagePosition == 3)  // 从底部来，先到顶再下到 2/3
                        _panel.AutoScrollPosition = new System.Drawing.Point(0, 0);
                    else if (_lastPagePosition < 2)  // 从顶部来，先到底再回 2/3
                        _panel.ScrollControlIntoView(_thumbnails[_thumbnails.Count - 1]);
                    break;
                case 3:
                    _panel.ScrollControlIntoView(_thumbnails[_thumbnails.Count - 1]);
                    _lastPagePosition = 3;
                    return;
                default:
                    return;
            }

            var target = _thumbnails[index];
            if (target != null)
                _panel.ScrollControlIntoView(target);
            _lastPagePosition = _pagePosition;
        }
        // 向上键 → 模拟鼠标滚轮向上
        private void ScrollDown()
        {
            SendMessage(_panel.Handle, 0x020A, (IntPtr)(-120 << 16), IntPtr.Zero);
        }
        public void SetSelectedFiles(List<string> files)
        {
            _selectedFiles = new HashSet<string>(files);
        }
        private void ScrollUp()
        {
            SendMessage(_panel.Handle, 0x020A, (IntPtr)(120 << 16), IntPtr.Zero);
        }
        private void LoadImages()
        {
            if (_sourceFiles != null)
            {
                _imageFiles = _sourceFiles
                    .Where(f => FileUtility.IsImageFile(Path.GetExtension(f)))
                    .ToList();
            }
            else
            {
                _imageFiles = Directory.GetFiles(_directory, "*.*")
                    .Where(f => FileUtility.IsImageFile(Path.GetExtension(f)))
                    .ToList();
            }
            if (this._previewBox.Visible)
                this._previewBox.Visible = false;
        }

        // === 显示一页 ===
        private void ShowPage(int page)
        {
            if (_imageFiles.Count == 0) return;
            int newTotalPages = (_imageFiles.Count + _pageSize - 1) / _pageSize;
            page = Math.Max(0, Math.Min(page, newTotalPages - 1));
            _currentPage = page;

            _panel.AutoScrollPosition = new System.Drawing.Point(0, 0);
            _pagePosition = 0;

            int start = page * _pageSize;
            int end = Math.Min(start + _pageSize, _imageFiles.Count);
            if (start >= end) return;

            this.Text = $"{_directory}【{_imageFiles.Count} / {ThumbnailLimit}】第{page + 1}/{_totalPages}页";

            foreach (PictureBox pb in _panel.Controls.OfType<PictureBox>())
            {
                pb.Image?.Dispose();
                pb.Image = null;
            }
            _panel.Controls.Clear();
            _thumbnails.Clear();

            var controlsToAdd = new List<Control>();
            for (int i = start; i < end; i++)
            {
                string file = _imageFiles[i];
                var pb = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new System.Drawing.Size(_thumbnailSize, _thumbnailSize),
                    Tag = file,
                    Margin = new Padding(3),
                    BackColor = Color.DimGray,
                    ImageLocation = null
                };

                FileInfo fi = new FileInfo(file);
                if (fi.Exists)
                    _toolTip.SetToolTip(pb, file + $"<{FileUtility.FormatFileSize(fi.Length)}>");

                pb.MouseClick += Pb_MouseClick;

                controlsToAdd.Add(pb);
                _thumbnails.Add(pb);

                var thumb = GetThumbnailInternal(file, _thumbnailSize);
                if (thumb != null)
                {
                    try
                    {
                        pb.Image = new Bitmap(thumb);
                        pb.BackColor = Color.Transparent;
                    }
                    catch { }
                }
                if (_selectedFiles.Contains(file))
                {
                    SetPicturebox(file, pb, true);
                    pb.Paint += DrawCheckMark;
                }
                else
                {
                    SetPicturebox(file, pb, false);
                }
            }

            _panel.SuspendLayout();
            _panel.Controls.AddRange(controlsToAdd.ToArray());
            _panel.ResumeLayout();

            int totalHeight = 0;
            foreach (var pb in _thumbnails)
            {
                int bottom = pb.Bottom + pb.Margin.Bottom;
                if (bottom > totalHeight) totalHeight = bottom;
            }
            _panel.AutoScrollMinSize = new System.Drawing.Size(0, totalHeight + 25);
        }

        private void SetPicturebox(string file, Control pb, bool selected)
        {
            if (selected)
            {
                pb.BackColor = Color.DarkBlue;
                pb.Padding = new Padding(0);  // 内边距让背景色可见
            }
            else
            {
                pb.Padding = new Padding(0);
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".gif")
                    pb.BackColor = Color.PowderBlue;
                else
                    pb.BackColor = Color.Transparent;
            }
        }
        // === 点击缩略图 ===
        private void Pb_MouseClick(object sender, MouseEventArgs e)
        {
            var ctrl = sender as Control;
            if (ctrl?.Tag is string file)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (_previewBox.Visible && _previewBox.Tag == ctrl)
                    {
                        _previewBox.Visible = false;
                        _previewBox.Image = null;
                        return;
                    }

                    if (!_selectedFiles.Contains(file))
                    {
                        _selectedFiles.Add(file);
                        SetPicturebox(file, ctrl, true);
                        ctrl.Paint += DrawCheckMark;
                        ctrl.Invalidate();
                    }

                    var thumb = FileUtility.ReadPictureToImage(file, _previewBoxWidth);
                    if (thumb != null) ShowPreviewWithImage(thumb, ctrl);
                    _previewBox.Focus();
                }
                else if (e.Button == MouseButtons.Right)
                {
                    if (_selectedFiles.Contains(file))
                    {
                        _selectedFiles.Remove(file);
                        SetPicturebox(file, ctrl, false);
                        ctrl.Paint -= DrawCheckMark;
                        ctrl.Invalidate();
                    }
                    _previewBox.Visible = false;
                    _previewBox.Image = null;
                }
            }
            else
            {
                if (e.Button == MouseButtons.Left)
                    HandleThumbnailClick(sender as PictureBox);
                else if (e.Button == MouseButtons.Right)
                {
                    _previewBox.Visible = false;
                    _previewBox.Image = null;
                }
            }
        }

        private void ShowPreviewWithImage(Image img, Control ctrl)
        {
            int maxW = this.ClientSize.Width * 2 / 3 - 40;
            int maxH = this.ClientSize.Height * 2 / 3 - 40;

            float ratio = Math.Min((float)maxW / img.Width, (float)maxH / img.Height);
            int w = (int)(img.Width * ratio);
            int h = (int)(img.Height * ratio);

            _previewBox.Size = new System.Drawing.Size(w, h);
            _previewBox.Image = img;
            _previewBox.Tag = ctrl;
            _previewBox.Left = (this.ClientSize.Width - _previewBox.Width) / 2;
            _previewBox.Top = (this.ClientSize.Height - _previewBox.Height) / 2;
            _previewBox.Visible = true;
        }

        private void HandleThumbnailClick(PictureBox pb)
        {
            if (pb?.Tag == null) return;
            if (!_previewBox.Visible)
            {
                ShowPreview(pb);
                return;
            }
            if (_previewBox.Tag == pb)
            {
                ToggleSelect(pb);
                _previewBox.Visible = false;
                _previewBox.Image = null;
            }
            else
            {
                ShowPreview(pb);
            }
        }

        private void ShowPreview(PictureBox pb)
        {
            string file = (string)pb.Tag;
            var img = FileUtility.ReadPictureToImage(file, _previewBoxWidth);
            if (img == null) return;

            int maxW = this.ClientSize.Width * 2 / 3 - 40;
            int maxH = this.ClientSize.Height * 2 / 3 - 40;
            float ratio = Math.Min((float)maxW / img.Width, (float)maxH / img.Height);
            int w = (int)(img.Width * ratio);
            int h = (int)(img.Height * ratio);

            _previewBox.Size = new System.Drawing.Size(w, h);
            _previewBox.Image = img;
            _previewBox.Tag = pb;
            _previewBox.Left = (this.ClientSize.Width - _previewBox.Width) / 2;
            _previewBox.Top = (this.ClientSize.Height - _previewBox.Height) / 2;
            _previewBox.Visible = true;
        }

        private void TogglePanelSelect(Panel pnl)
        {
            string f = (string)pnl.Tag;
            if (_selectedFiles.Contains(f))
            {
                _selectedFiles.Remove(f);
                pnl.BackColor = Color.DimGray;
                pnl.Paint -= DrawCheckMark;
            }
            else
            {
                _selectedFiles.Add(f);
                pnl.BackColor = Color.DarkBlue;
                pnl.Paint += DrawCheckMark;
            }
            pnl.Invalidate();
        }

        private void ToggleSelect(PictureBox pb)
        {
            if (pb == null) return;
            string file = (string)pb.Tag;
            if (_selectedFiles.Contains(file))
            {
                _selectedFiles.Remove(file);
                pb.Padding = new Padding(0);
                SetPicturebox(file, pb, false);
                pb.Paint -= DrawCheckMark;
            }
            else
            {
                _selectedFiles.Add(file);
                pb.Padding = new Padding(3);
                SetPicturebox(file, pb, true);
                pb.Paint += DrawCheckMark;
            }
            pb.Invalidate();
        }

        private void DrawCheckMark(object sender, PaintEventArgs e)
        {
            var ctrl = sender as Control;
            if (ctrl == null) return;

            // 画蓝色边框
            using (var borderPen = new Pen(Color.DarkBlue, 8))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
            }

            // 画绿色对勾
            using (var pen = new Pen(Color.LawnGreen, 3))
            {
                int w = ctrl.Width;
                int h = ctrl.Height;
                e.Graphics.DrawLine(pen, w - 20, h - 10, w - 10, h - 2);
                e.Graphics.DrawLine(pen, w - 10, h - 2, w - 4, h - 16);
            }
        }

        private void AdjustThumbnailSize(FlowLayoutPanel panel)
        {
            int oldSize = _thumbnailSize;
            CalculateThumbnailSize();
            if (_thumbnailSize != oldSize)
                ShowPage(_currentPage);
            else
            {
                int totalHeight = 0;
                foreach (var pb in _thumbnails)
                {
                    int bottom = pb.Bottom + pb.Margin.Bottom;
                    if (bottom > totalHeight) totalHeight = bottom;
                }
                panel.AutoScrollMinSize = new System.Drawing.Size(0, totalHeight + 25);
            }
        }

        private void SetAllChecked(bool isChecked)
        {
            // 先取消所有旧订阅
            foreach (var ctrl in _thumbnails)
            {
                if (ctrl != null)
                {
                    ctrl.Paint -= DrawCheckMark;
                    ctrl.Paint -= DrawCheckMark;  // 双保险，清掉重复订阅
                }
            }

            _selectedFiles.Clear();
            if (isChecked) _selectedFiles = new HashSet<string>(_imageFiles);

            foreach (var ctrl in _thumbnails)
            {
                if (ctrl == null) continue;
                string file = (string)ctrl.Tag;
                SetPicturebox(file, ctrl, isChecked);
                if (isChecked)
                    ctrl.Paint += DrawCheckMark;
                ctrl.Invalidate();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            DateTime d0 = DateTime.Now;
            int start = _currentPage * _pageSize;
            int end = Math.Min(start + _pageSize, _imageFiles.Count);
            var currentPageFiles = _imageFiles.Skip(start).Take(end - start).ToList();
            var notSelected = currentPageFiles.Where(f => !_selectedFiles.Contains(f)).ToList();

            if (notSelected.Count == 0)
            {
                MessageBox.Show("没有未选中的图片");
                return;
            }

            // 入队后台删除
            foreach (var file in notSelected)
            {
                FileUtility.EnqueueDelete(file, TmpDirectory, null);
            }
            _deletedFiles.AddRange(notSelected);

            // 从文件列表中移除已删除的
            _imageFiles = _imageFiles.Where(f => !notSelected.Contains(f)).ToList();

            // 从选中集合中移除已删除的，保留未删除的选中状态
            _selectedFiles.RemoveWhere(f => notSelected.Contains(f));

            if (_imageFiles.Count == 0)
            {
                this.Close();
                return;
            }

            // 刷新当前页，恢复选中状态
            if (_currentPage >= _totalPages)
                _currentPage = _totalPages - 1;
            ShowPage(_currentPage);

            Console.WriteLine($"album Delete use {DateTime.Now.Subtract(d0).TotalMilliseconds} ms");
        }
        // === 缩略图缓存 ===
        private static Image GetThumbnailInternal(string file, int size)
        {
            if (_thumbnailCacheFile.TryGetValue(file, out Image thumbnail))
            {
                try { return new Bitmap(thumbnail); }
                catch { _thumbnailCacheFile.Remove(file); thumbnail = null; }
            }

            thumbnail = FileUtility.ReadPictureToImage(file, size);

            if (thumbnail != null)
            {
                using (var temp = new Bitmap(thumbnail))
                    thumbnail = new Bitmap(temp);

                _thumbnailCacheFile[file] = thumbnail;
                _thumbnailCacheFileOrder.Remove(file);
                _thumbnailCacheFileOrder.Add(file);
                if (_thumbnailCacheFileOrder.Count > _maxCachedFiles)
                {
                    string oldest = _thumbnailCacheFileOrder[0];
                    _thumbnailCacheFileOrder.RemoveAt(0);
                    _thumbnailCacheFile[oldest]?.Dispose();
                    _thumbnailCacheFile.Remove(oldest);
                }
            }
            return thumbnail;
        }

        public static void Clear(string dir = "")
        {
            lock (_thumbnailCacheFile)
            {
                foreach (var img in _thumbnailCacheFile.Values) img?.Dispose();
                _thumbnailCacheFile.Clear();
                _thumbnailCacheFileOrder.Clear();
            }
        }
    }
}