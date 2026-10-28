
namespace PictureManager 
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnResize = new System.Windows.Forms.Button();
            this.tbMinSize = new System.Windows.Forms.TextBox();
            this.dg1 = new System.Windows.Forms.DataGridView();
            this.btnLoadView = new System.Windows.Forms.Button();
            this.tbMaxSize = new System.Windows.Forms.TextBox();
            this.btnDeleteMinFile = new System.Windows.Forms.Button();
            this.cbFileType = new System.Windows.Forms.ComboBox();
            this.btnShow = new System.Windows.Forms.Button();
            this.tmrShow = new System.Windows.Forms.Timer(this.components);
            this.chromBrowser = new CefSharp.WinForms.ChromiumWebBrowser();
            this.tbRed = new System.Windows.Forms.TextBox();
            this.tbGreen = new System.Windows.Forms.TextBox();
            this.tbBlue = new System.Windows.Forms.TextBox();
            this.tbHashSize = new System.Windows.Forms.TextBox();
            this.tbGray = new System.Windows.Forms.TextBox();
            this.tbHashRatio = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cbCrop = new System.Windows.Forms.ComboBox();
            this.videoView1 = new LibVLCSharp.WinForms.VideoView();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.btnMeta = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnClearGarbage = new System.Windows.Forms.Button();
            this.btnNameSize = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabFiles = new System.Windows.Forms.TabPage();
            this.btnBucket = new System.Windows.Forms.Button();
            this.btnCompare = new System.Windows.Forms.Button();
            this.tbFilenameFilter = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.btnDirectory = new System.Windows.Forms.Button();
            this.btnCalc = new System.Windows.Forms.Button();
            this.tabDup = new System.Windows.Forms.TabPage();
            this.tbMaxBucketLimit = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.tbSiftMatchPoint = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.tbSiftFeatures = new System.Windows.Forms.TextBox();
            this.tbSiftMaxDim = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.cbCompareDctMode = new System.Windows.Forms.ComboBox();
            this.label17 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tabTools = new System.Windows.Forms.TabPage();
            this.labelDBRecords = new System.Windows.Forms.Label();
            this.btnClearDBRecord = new System.Windows.Forms.Button();
            this.btnClearDBStatus = new System.Windows.Forms.Button();
            this.tbTmpDirectory = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.cb删除文件扩大化 = new System.Windows.Forms.CheckBox();
            this.tbForumThreadUrl = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.tbForumThreadRegex = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.tbForumSavePath = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.btnForumDownloader = new System.Windows.Forms.Button();
            this.tbForumGroup = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tbForumGroupUrl = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.tbForumCookie = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.tbForumEndPage = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tbForumStartPage = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tbPageRows = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbPage = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.menuWorkflow = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabelNo = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelPictureMode = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelRecycleCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelFileName = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelSize = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelTrack = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabelVolume = new System.Windows.Forms.ToolStripStatusLabel();
            this.timerDownloadPicture = new System.Windows.Forms.Timer(this.components);
            this.btnESC = new System.Windows.Forms.Button();
            this.directoryListControl1 = new PictureManager.Controls.DirectoryListControl();
            ((System.ComponentModel.ISupportInitialize)(this.dg1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.videoView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabFiles.SuspendLayout();
            this.tabDup.SuspendLayout();
            this.tabTools.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnResize
            // 
            this.btnResize.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnResize.Location = new System.Drawing.Point(111, 44);
            this.btnResize.Margin = new System.Windows.Forms.Padding(2);
            this.btnResize.Name = "btnResize";
            this.btnResize.Size = new System.Drawing.Size(96, 29);
            this.btnResize.TabIndex = 4;
            this.btnResize.Text = "缩小大文件";
            this.btnResize.UseVisualStyleBackColor = true;
            this.btnResize.Click += new System.EventHandler(this.btnResize_Click);
            // 
            // tbMinSize
            // 
            this.tbMinSize.Location = new System.Drawing.Point(17, 11);
            this.tbMinSize.Margin = new System.Windows.Forms.Padding(2);
            this.tbMinSize.Name = "tbMinSize";
            this.tbMinSize.Size = new System.Drawing.Size(90, 29);
            this.tbMinSize.TabIndex = 6;
            this.tbMinSize.Text = "60000";
            // 
            // dg1
            // 
            this.dg1.AllowUserToAddRows = false;
            this.dg1.AllowUserToResizeRows = false;
            this.dg1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dg1.ColumnHeadersHeight = 29;
            this.dg1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dg1.Location = new System.Drawing.Point(3, 115);
            this.dg1.Margin = new System.Windows.Forms.Padding(2);
            this.dg1.Name = "dg1";
            this.dg1.ReadOnly = true;
            this.dg1.RowHeadersVisible = false;
            this.dg1.RowHeadersWidth = 51;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dg1.RowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dg1.RowTemplate.Height = 27;
            this.dg1.Size = new System.Drawing.Size(220, 749);
            this.dg1.TabIndex = 1;
            this.dg1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dg1_CellClick);
            this.dg1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dg1_KeyDown);
            // 
            // btnLoadView
            // 
            this.btnLoadView.BackColor = System.Drawing.Color.LightSalmon;
            this.btnLoadView.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnLoadView.Location = new System.Drawing.Point(82, 33);
            this.btnLoadView.Margin = new System.Windows.Forms.Padding(2);
            this.btnLoadView.Name = "btnLoadView";
            this.btnLoadView.Size = new System.Drawing.Size(139, 34);
            this.btnLoadView.TabIndex = 9;
            this.btnLoadView.Text = "Load";
            this.btnLoadView.UseVisualStyleBackColor = false;
            this.btnLoadView.Click += new System.EventHandler(this.btnLoadView_Click);
            // 
            // tbMaxSize
            // 
            this.tbMaxSize.Location = new System.Drawing.Point(17, 44);
            this.tbMaxSize.Margin = new System.Windows.Forms.Padding(2);
            this.tbMaxSize.Name = "tbMaxSize";
            this.tbMaxSize.Size = new System.Drawing.Size(90, 29);
            this.tbMaxSize.TabIndex = 16;
            this.tbMaxSize.Text = "1200000";
            // 
            // btnDeleteMinFile
            // 
            this.btnDeleteMinFile.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnDeleteMinFile.Location = new System.Drawing.Point(111, 11);
            this.btnDeleteMinFile.Margin = new System.Windows.Forms.Padding(2);
            this.btnDeleteMinFile.Name = "btnDeleteMinFile";
            this.btnDeleteMinFile.Size = new System.Drawing.Size(96, 29);
            this.btnDeleteMinFile.TabIndex = 20;
            this.btnDeleteMinFile.Text = "删除小文件";
            this.btnDeleteMinFile.UseVisualStyleBackColor = true;
            this.btnDeleteMinFile.Click += new System.EventHandler(this.btnDeleteMinFile_Click);
            // 
            // cbFileType
            // 
            this.cbFileType.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbFileType.ForeColor = System.Drawing.Color.Red;
            this.cbFileType.FormattingEnabled = true;
            this.cbFileType.Items.AddRange(new object[] {
            "*.*",
            "*.jpg",
            "*.jpeg",
            "*.png",
            "*.gif",
            "*.webp",
            "*.mp4",
            "*.avi",
            "*.mov",
            "*.css",
            "*.下载",
            "*.html",
            "*.php",
            "*.txt",
            "*."});
            this.cbFileType.Location = new System.Drawing.Point(5, 5);
            this.cbFileType.Margin = new System.Windows.Forms.Padding(2);
            this.cbFileType.Name = "cbFileType";
            this.cbFileType.Size = new System.Drawing.Size(70, 24);
            this.cbFileType.TabIndex = 21;
            // 
            // btnShow
            // 
            this.btnShow.BackColor = System.Drawing.Color.LightCoral;
            this.btnShow.Location = new System.Drawing.Point(11, 808);
            this.btnShow.Margin = new System.Windows.Forms.Padding(2);
            this.btnShow.Name = "btnShow";
            this.btnShow.Size = new System.Drawing.Size(74, 33);
            this.btnShow.TabIndex = 22;
            this.btnShow.Text = "Show";
            this.btnShow.UseVisualStyleBackColor = false;
            this.btnShow.Click += new System.EventHandler(this.btnShow_Click);
            // 
            // tmrShow
            // 
            this.tmrShow.Interval = 3000;
            this.tmrShow.Tick += new System.EventHandler(this.tmrShow_Tick);
            // 
            // chromBrowser
            // 
            this.chromBrowser.ActivateBrowserOnCreation = false;
            this.chromBrowser.Location = new System.Drawing.Point(236, 0);
            this.chromBrowser.Name = "chromBrowser";
            this.chromBrowser.Size = new System.Drawing.Size(732, 688);
            this.chromBrowser.TabIndex = 25;
            // 
            // tbRed
            // 
            this.tbRed.BackColor = System.Drawing.Color.MistyRose;
            this.tbRed.ForeColor = System.Drawing.Color.Red;
            this.tbRed.Location = new System.Drawing.Point(6, 6);
            this.tbRed.Name = "tbRed";
            this.tbRed.Size = new System.Drawing.Size(71, 29);
            this.tbRed.TabIndex = 105;
            this.tbRed.Text = "0.299";
            this.tbRed.TextChanged += new System.EventHandler(this.tbRed_TextChanged);
            // 
            // tbGreen
            // 
            this.tbGreen.BackColor = System.Drawing.Color.MistyRose;
            this.tbGreen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.tbGreen.Location = new System.Drawing.Point(7, 41);
            this.tbGreen.Name = "tbGreen";
            this.tbGreen.Size = new System.Drawing.Size(70, 29);
            this.tbGreen.TabIndex = 107;
            this.tbGreen.Text = "0.587";
            this.tbGreen.TextChanged += new System.EventHandler(this.tbGreen_TextChanged);
            // 
            // tbBlue
            // 
            this.tbBlue.BackColor = System.Drawing.Color.MistyRose;
            this.tbBlue.ForeColor = System.Drawing.Color.Blue;
            this.tbBlue.Location = new System.Drawing.Point(7, 76);
            this.tbBlue.Name = "tbBlue";
            this.tbBlue.Size = new System.Drawing.Size(70, 29);
            this.tbBlue.TabIndex = 108;
            this.tbBlue.Text = "0.114";
            this.tbBlue.TextChanged += new System.EventHandler(this.tbBlue_TextChanged);
            // 
            // tbHashSize
            // 
            this.tbHashSize.BackColor = System.Drawing.Color.MistyRose;
            this.tbHashSize.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbHashSize.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbHashSize.Location = new System.Drawing.Point(100, 166);
            this.tbHashSize.Name = "tbHashSize";
            this.tbHashSize.Size = new System.Drawing.Size(103, 26);
            this.tbHashSize.TabIndex = 109;
            this.tbHashSize.Tag = "";
            this.tbHashSize.Text = "10";
            this.tbHashSize.TextChanged += new System.EventHandler(this.tbHashSize_TextChanged);
            // 
            // tbGray
            // 
            this.tbGray.BackColor = System.Drawing.Color.MistyRose;
            this.tbGray.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbGray.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbGray.Location = new System.Drawing.Point(98, 198);
            this.tbGray.Name = "tbGray";
            this.tbGray.Size = new System.Drawing.Size(105, 26);
            this.tbGray.TabIndex = 111;
            this.tbGray.Tag = "";
            this.tbGray.Text = "10";
            this.tbGray.TextChanged += new System.EventHandler(this.tbGray_TextChanged);
            // 
            // tbHashRatio
            // 
            this.tbHashRatio.BackColor = System.Drawing.Color.White;
            this.tbHashRatio.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbHashRatio.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbHashRatio.Location = new System.Drawing.Point(100, 503);
            this.tbHashRatio.Name = "tbHashRatio";
            this.tbHashRatio.Size = new System.Drawing.Size(103, 26);
            this.tbHashRatio.TabIndex = 112;
            this.tbHashRatio.Tag = "";
            this.tbHashRatio.Text = "0.9";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(6, 167);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 19);
            this.label1.TabIndex = 118;
            this.label1.Text = "尺寸切割";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(7, 198);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 19);
            this.label2.TabIndex = 119;
            this.label2.Text = "颜色分级";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Location = new System.Drawing.Point(11, 504);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 19);
            this.label3.TabIndex = 120;
            this.label3.Text = "相似度";
            // 
            // cbCrop
            // 
            this.cbCrop.BackColor = System.Drawing.Color.MistyRose;
            this.cbCrop.FormattingEnabled = true;
            this.cbCrop.Items.AddRange(new object[] {
            "全图",
            "左上",
            "右上",
            "左下",
            "右下",
            "中心"});
            this.cbCrop.Location = new System.Drawing.Point(98, 6);
            this.cbCrop.Name = "cbCrop";
            this.cbCrop.Size = new System.Drawing.Size(105, 27);
            this.cbCrop.TabIndex = 122;
            this.cbCrop.SelectedIndexChanged += new System.EventHandler(this.cbCrop_SelectedIndexChanged);
            // 
            // videoView1
            // 
            this.videoView1.BackColor = System.Drawing.Color.Black;
            this.videoView1.Location = new System.Drawing.Point(236, 0);
            this.videoView1.MediaPlayer = null;
            this.videoView1.Name = "videoView1";
            this.videoView1.Size = new System.Drawing.Size(492, 378);
            this.videoView1.TabIndex = 123;
            this.videoView1.Text = "videoView1";
            // 
            // trackBar1
            // 
            this.trackBar1.BackColor = System.Drawing.Color.Black;
            this.trackBar1.Location = new System.Drawing.Point(222, 859);
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(412, 45);
            this.trackBar1.TabIndex = 124;
            this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.None;
            this.toolTip1.SetToolTip(this.trackBar1, "Ctrl +- 调节音量");
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            this.trackBar1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.trackBar1_KeyDown);
            this.trackBar1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.trackBar1_MouseDown);
            this.trackBar1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.trackBar1_MouseUp);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // btnMeta
            // 
            this.btnMeta.BackColor = System.Drawing.Color.LightCoral;
            this.btnMeta.Location = new System.Drawing.Point(100, 41);
            this.btnMeta.Margin = new System.Windows.Forms.Padding(2);
            this.btnMeta.Name = "btnMeta";
            this.btnMeta.Size = new System.Drawing.Size(72, 67);
            this.btnMeta.TabIndex = 128;
            this.btnMeta.Text = "&Meta";
            this.toolTip1.SetToolTip(this.btnMeta, "读取列表所有文件的指纹");
            this.btnMeta.UseVisualStyleBackColor = false;
            this.btnMeta.Click += new System.EventHandler(this.btnMeta_Click);
            // 
            // btnClearGarbage
            // 
            this.btnClearGarbage.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.btnClearGarbage.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnClearGarbage.Location = new System.Drawing.Point(15, 581);
            this.btnClearGarbage.Margin = new System.Windows.Forms.Padding(2);
            this.btnClearGarbage.Name = "btnClearGarbage";
            this.btnClearGarbage.Size = new System.Drawing.Size(155, 38);
            this.btnClearGarbage.TabIndex = 145;
            this.btnClearGarbage.Text = "全面清理垃圾";
            this.toolTip1.SetToolTip(this.btnClearGarbage, "删除Meta（Name）重复的，同类型同尺寸文件");
            this.btnClearGarbage.UseVisualStyleBackColor = false;
            this.btnClearGarbage.Click += new System.EventHandler(this.btnClearGarbage_Click);
            // 
            // btnNameSize
            // 
            this.btnNameSize.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.btnNameSize.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnNameSize.Location = new System.Drawing.Point(15, 623);
            this.btnNameSize.Margin = new System.Windows.Forms.Padding(2);
            this.btnNameSize.Name = "btnNameSize";
            this.btnNameSize.Size = new System.Drawing.Size(57, 38);
            this.btnNameSize.TabIndex = 146;
            this.btnNameSize.Text = "粗筛";
            this.toolTip1.SetToolTip(this.btnNameSize, "删除Meta（Name）重复的，同类型同尺寸文件");
            this.btnNameSize.UseVisualStyleBackColor = false;
            this.btnNameSize.Click += new System.EventHandler(this.btnNameSize_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabFiles);
            this.tabControl1.Controls.Add(this.tabDup);
            this.tabControl1.Controls.Add(this.tabTools);
            this.tabControl1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabControl1.Location = new System.Drawing.Point(2, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(234, 913);
            this.tabControl1.TabIndex = 129;
            // 
            // tabFiles
            // 
            this.tabFiles.BackColor = System.Drawing.Color.Transparent;
            this.tabFiles.Controls.Add(this.btnBucket);
            this.tabFiles.Controls.Add(this.btnCompare);
            this.tabFiles.Controls.Add(this.tbFilenameFilter);
            this.tabFiles.Controls.Add(this.label16);
            this.tabFiles.Controls.Add(this.btnDirectory);
            this.tabFiles.Controls.Add(this.dg1);
            this.tabFiles.Controls.Add(this.btnCalc);
            this.tabFiles.Controls.Add(this.btnLoadView);
            this.tabFiles.Controls.Add(this.cbFileType);
            this.tabFiles.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabFiles.Location = new System.Drawing.Point(4, 26);
            this.tabFiles.Name = "tabFiles";
            this.tabFiles.Padding = new System.Windows.Forms.Padding(3);
            this.tabFiles.Size = new System.Drawing.Size(226, 883);
            this.tabFiles.TabIndex = 0;
            this.tabFiles.Text = "列表";
            // 
            // btnBucket
            // 
            this.btnBucket.BackColor = System.Drawing.Color.PowderBlue;
            this.btnBucket.Location = new System.Drawing.Point(79, 68);
            this.btnBucket.Margin = new System.Windows.Forms.Padding(2);
            this.btnBucket.Name = "btnBucket";
            this.btnBucket.Size = new System.Drawing.Size(64, 43);
            this.btnBucket.TabIndex = 136;
            this.btnBucket.Text = "分桶";
            this.btnBucket.UseVisualStyleBackColor = false;
            this.btnBucket.Click += new System.EventHandler(this.btnBucket_Click);
            // 
            // btnCompare
            // 
            this.btnCompare.BackColor = System.Drawing.Color.PowderBlue;
            this.btnCompare.Location = new System.Drawing.Point(147, 68);
            this.btnCompare.Margin = new System.Windows.Forms.Padding(2);
            this.btnCompare.Name = "btnCompare";
            this.btnCompare.Size = new System.Drawing.Size(74, 43);
            this.btnCompare.TabIndex = 135;
            this.btnCompare.Text = "细筛";
            this.btnCompare.UseVisualStyleBackColor = false;
            this.btnCompare.Click += new System.EventHandler(this.btnCompare_Click);
            // 
            // tbFilenameFilter
            // 
            this.tbFilenameFilter.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbFilenameFilter.ForeColor = System.Drawing.Color.Red;
            this.tbFilenameFilter.Location = new System.Drawing.Point(121, 7);
            this.tbFilenameFilter.Name = "tbFilenameFilter";
            this.tbFilenameFilter.Size = new System.Drawing.Size(99, 23);
            this.tbFilenameFilter.TabIndex = 134;
            this.tbFilenameFilter.Text = "\\";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label16.Location = new System.Drawing.Point(86, 12);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(29, 12);
            this.label16.TabIndex = 134;
            this.label16.Text = "匹配";
            // 
            // btnDirectory
            // 
            this.btnDirectory.BackColor = System.Drawing.Color.LightSalmon;
            this.btnDirectory.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnDirectory.Location = new System.Drawing.Point(5, 33);
            this.btnDirectory.Margin = new System.Windows.Forms.Padding(2);
            this.btnDirectory.Name = "btnDirectory";
            this.btnDirectory.Size = new System.Drawing.Size(70, 34);
            this.btnDirectory.TabIndex = 131;
            this.btnDirectory.Text = "...";
            this.btnDirectory.UseVisualStyleBackColor = false;
            this.btnDirectory.Click += new System.EventHandler(this.btnDirectory_Click);
            // 
            // btnCalc
            // 
            this.btnCalc.BackColor = System.Drawing.Color.PowderBlue;
            this.btnCalc.Location = new System.Drawing.Point(5, 68);
            this.btnCalc.Margin = new System.Windows.Forms.Padding(2);
            this.btnCalc.Name = "btnCalc";
            this.btnCalc.Size = new System.Drawing.Size(70, 43);
            this.btnCalc.TabIndex = 101;
            this.btnCalc.Text = "Hash";
            this.btnCalc.UseVisualStyleBackColor = false;
            this.btnCalc.Click += new System.EventHandler(this.btnCalc_Click);
            // 
            // tabDup
            // 
            this.tabDup.Controls.Add(this.tbMaxBucketLimit);
            this.tabDup.Controls.Add(this.label22);
            this.tabDup.Controls.Add(this.btnNameSize);
            this.tabDup.Controls.Add(this.btnClearGarbage);
            this.tabDup.Controls.Add(this.tbSiftMatchPoint);
            this.tabDup.Controls.Add(this.label21);
            this.tabDup.Controls.Add(this.tbSiftFeatures);
            this.tabDup.Controls.Add(this.tbSiftMaxDim);
            this.tabDup.Controls.Add(this.label19);
            this.tabDup.Controls.Add(this.label20);
            this.tabDup.Controls.Add(this.label18);
            this.tabDup.Controls.Add(this.cbCompareDctMode);
            this.tabDup.Controls.Add(this.label17);
            this.tabDup.Controls.Add(this.label4);
            this.tabDup.Controls.Add(this.tbHashRatio);
            this.tabDup.Controls.Add(this.btnMeta);
            this.tabDup.Controls.Add(this.tbRed);
            this.tabDup.Controls.Add(this.tbGreen);
            this.tabDup.Controls.Add(this.tbBlue);
            this.tabDup.Controls.Add(this.tbHashSize);
            this.tabDup.Controls.Add(this.tbGray);
            this.tabDup.Controls.Add(this.cbCrop);
            this.tabDup.Controls.Add(this.label1);
            this.tabDup.Controls.Add(this.label2);
            this.tabDup.Controls.Add(this.label3);
            this.tabDup.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabDup.Location = new System.Drawing.Point(4, 26);
            this.tabDup.Name = "tabDup";
            this.tabDup.Padding = new System.Windows.Forms.Padding(3);
            this.tabDup.Size = new System.Drawing.Size(226, 883);
            this.tabDup.TabIndex = 1;
            this.tabDup.Text = "查重";
            this.tabDup.UseVisualStyleBackColor = true;
            // 
            // tbMaxBucketLimit
            // 
            this.tbMaxBucketLimit.BackColor = System.Drawing.Color.White;
            this.tbMaxBucketLimit.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbMaxBucketLimit.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbMaxBucketLimit.Location = new System.Drawing.Point(126, 540);
            this.tbMaxBucketLimit.Name = "tbMaxBucketLimit";
            this.tbMaxBucketLimit.Size = new System.Drawing.Size(77, 26);
            this.tbMaxBucketLimit.TabIndex = 147;
            this.tbMaxBucketLimit.Tag = "";
            this.tbMaxBucketLimit.Text = "500";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label22.ForeColor = System.Drawing.Color.Black;
            this.label22.Location = new System.Drawing.Point(11, 541);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(104, 19);
            this.label22.TabIndex = 148;
            this.label22.Text = "超大桶限制";
            // 
            // tbSiftMatchPoint
            // 
            this.tbSiftMatchPoint.BackColor = System.Drawing.Color.White;
            this.tbSiftMatchPoint.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbSiftMatchPoint.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbSiftMatchPoint.Location = new System.Drawing.Point(100, 432);
            this.tbSiftMatchPoint.Name = "tbSiftMatchPoint";
            this.tbSiftMatchPoint.Size = new System.Drawing.Size(103, 26);
            this.tbSiftMatchPoint.TabIndex = 143;
            this.tbSiftMatchPoint.Tag = "";
            this.tbSiftMatchPoint.Text = "7";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label21.ForeColor = System.Drawing.Color.Black;
            this.label21.Location = new System.Drawing.Point(6, 433);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(85, 19);
            this.label21.TabIndex = 144;
            this.label21.Text = "匹配点数";
            // 
            // tbSiftFeatures
            // 
            this.tbSiftFeatures.BackColor = System.Drawing.Color.MistyRose;
            this.tbSiftFeatures.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbSiftFeatures.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbSiftFeatures.Location = new System.Drawing.Point(100, 368);
            this.tbSiftFeatures.Name = "tbSiftFeatures";
            this.tbSiftFeatures.Size = new System.Drawing.Size(103, 26);
            this.tbSiftFeatures.TabIndex = 139;
            this.tbSiftFeatures.Tag = "";
            this.tbSiftFeatures.Text = "20";
            this.tbSiftFeatures.TextChanged += new System.EventHandler(this.tbSiftFeatures_TextChanged);
            // 
            // tbSiftMaxDim
            // 
            this.tbSiftMaxDim.BackColor = System.Drawing.Color.MistyRose;
            this.tbSiftMaxDim.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbSiftMaxDim.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbSiftMaxDim.Location = new System.Drawing.Point(98, 400);
            this.tbSiftMaxDim.Name = "tbSiftMaxDim";
            this.tbSiftMaxDim.Size = new System.Drawing.Size(105, 26);
            this.tbSiftMaxDim.TabIndex = 140;
            this.tbSiftMaxDim.Tag = "";
            this.tbSiftMaxDim.Text = "200";
            this.tbSiftMaxDim.TextChanged += new System.EventHandler(this.tbSiftMaxDim_TextChanged);
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label19.ForeColor = System.Drawing.Color.Black;
            this.label19.Location = new System.Drawing.Point(6, 369);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(85, 19);
            this.label19.TabIndex = 141;
            this.label19.Text = "采样点数";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label20.ForeColor = System.Drawing.Color.Black;
            this.label20.Location = new System.Drawing.Point(7, 400);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(85, 19);
            this.label20.TabIndex = 142;
            this.label20.Text = "尺寸缩减";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label18.ForeColor = System.Drawing.Color.Black;
            this.label18.Location = new System.Drawing.Point(11, 333);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(106, 19);
            this.label18.TabIndex = 138;
            this.label18.Text = "Sift设置：";
            // 
            // cbCompareDctMode
            // 
            this.cbCompareDctMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCompareDctMode.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbCompareDctMode.ForeColor = System.Drawing.Color.DarkCyan;
            this.cbCompareDctMode.FormattingEnabled = true;
            this.cbCompareDctMode.Items.AddRange(new object[] {
            "Correl",
            "ChiSqr",
            "Bhattacharyya",
            "Hellinger",
            "KLDiv"});
            this.cbCompareDctMode.Location = new System.Drawing.Point(15, 274);
            this.cbCompareDctMode.Margin = new System.Windows.Forms.Padding(2);
            this.cbCompareDctMode.Name = "cbCompareDctMode";
            this.cbCompareDctMode.Size = new System.Drawing.Size(188, 27);
            this.cbCompareDctMode.TabIndex = 137;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label17.ForeColor = System.Drawing.Color.Black;
            this.label17.Location = new System.Drawing.Point(11, 244);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(96, 19);
            this.label17.TabIndex = 130;
            this.label17.Text = "Dct设置：";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(11, 136);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(106, 19);
            this.label4.TabIndex = 129;
            this.label4.Text = "Hash设置：";
            // 
            // tabTools
            // 
            this.tabTools.Controls.Add(this.labelDBRecords);
            this.tabTools.Controls.Add(this.btnClearDBRecord);
            this.tabTools.Controls.Add(this.btnClearDBStatus);
            this.tabTools.Controls.Add(this.tbTmpDirectory);
            this.tabTools.Controls.Add(this.label15);
            this.tabTools.Controls.Add(this.cb删除文件扩大化);
            this.tabTools.Controls.Add(this.tbForumThreadUrl);
            this.tabTools.Controls.Add(this.label14);
            this.tabTools.Controls.Add(this.tbForumThreadRegex);
            this.tabTools.Controls.Add(this.label13);
            this.tabTools.Controls.Add(this.tbForumSavePath);
            this.tabTools.Controls.Add(this.label12);
            this.tabTools.Controls.Add(this.btnForumDownloader);
            this.tabTools.Controls.Add(this.tbForumGroup);
            this.tabTools.Controls.Add(this.label11);
            this.tabTools.Controls.Add(this.tbForumGroupUrl);
            this.tabTools.Controls.Add(this.label10);
            this.tabTools.Controls.Add(this.tbForumCookie);
            this.tabTools.Controls.Add(this.label9);
            this.tabTools.Controls.Add(this.tbForumEndPage);
            this.tabTools.Controls.Add(this.label8);
            this.tabTools.Controls.Add(this.tbForumStartPage);
            this.tabTools.Controls.Add(this.label7);
            this.tabTools.Controls.Add(this.tbPageRows);
            this.tabTools.Controls.Add(this.label6);
            this.tabTools.Controls.Add(this.tbPage);
            this.tabTools.Controls.Add(this.label5);
            this.tabTools.Controls.Add(this.btnResize);
            this.tabTools.Controls.Add(this.tbMinSize);
            this.tabTools.Controls.Add(this.tbMaxSize);
            this.tabTools.Controls.Add(this.btnDeleteMinFile);
            this.tabTools.Controls.Add(this.btnShow);
            this.tabTools.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabTools.Location = new System.Drawing.Point(4, 26);
            this.tabTools.Name = "tabTools";
            this.tabTools.Size = new System.Drawing.Size(226, 883);
            this.tabTools.TabIndex = 2;
            this.tabTools.Text = "工具";
            this.tabTools.UseVisualStyleBackColor = true;
            // 
            // labelDBRecords
            // 
            this.labelDBRecords.AutoSize = true;
            this.labelDBRecords.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.labelDBRecords.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelDBRecords.ForeColor = System.Drawing.Color.Black;
            this.labelDBRecords.Location = new System.Drawing.Point(15, 770);
            this.labelDBRecords.Name = "labelDBRecords";
            this.labelDBRecords.Size = new System.Drawing.Size(135, 16);
            this.labelDBRecords.TabIndex = 146;
            this.labelDBRecords.Text = "下载论坛的版块ID";
            // 
            // btnClearDBRecord
            // 
            this.btnClearDBRecord.BackColor = System.Drawing.Color.CadetBlue;
            this.btnClearDBRecord.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnClearDBRecord.ForeColor = System.Drawing.Color.White;
            this.btnClearDBRecord.Location = new System.Drawing.Point(89, 693);
            this.btnClearDBRecord.Margin = new System.Windows.Forms.Padding(2);
            this.btnClearDBRecord.Name = "btnClearDBRecord";
            this.btnClearDBRecord.Size = new System.Drawing.Size(74, 75);
            this.btnClearDBRecord.TabIndex = 145;
            this.btnClearDBRecord.Text = "重置数据库";
            this.btnClearDBRecord.UseVisualStyleBackColor = false;
            this.btnClearDBRecord.Click += new System.EventHandler(this.btnClearDBRecord_Click);
            // 
            // btnClearDBStatus
            // 
            this.btnClearDBStatus.BackColor = System.Drawing.Color.CadetBlue;
            this.btnClearDBStatus.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnClearDBStatus.ForeColor = System.Drawing.Color.White;
            this.btnClearDBStatus.Location = new System.Drawing.Point(11, 693);
            this.btnClearDBStatus.Margin = new System.Windows.Forms.Padding(2);
            this.btnClearDBStatus.Name = "btnClearDBStatus";
            this.btnClearDBStatus.Size = new System.Drawing.Size(74, 75);
            this.btnClearDBStatus.TabIndex = 144;
            this.btnClearDBStatus.Text = "清空数据库Status标记";
            this.btnClearDBStatus.UseVisualStyleBackColor = false;
            this.btnClearDBStatus.Click += new System.EventHandler(this.btnClearDBStatus_Click);
            // 
            // tbTmpDirectory
            // 
            this.tbTmpDirectory.BackColor = System.Drawing.Color.MistyRose;
            this.tbTmpDirectory.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbTmpDirectory.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbTmpDirectory.Location = new System.Drawing.Point(63, 662);
            this.tbTmpDirectory.Name = "tbTmpDirectory";
            this.tbTmpDirectory.Size = new System.Drawing.Size(134, 26);
            this.tbTmpDirectory.TabIndex = 142;
            this.tbTmpDirectory.Tag = "";
            this.tbTmpDirectory.Text = "F:\\Tmp";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label15.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label15.ForeColor = System.Drawing.Color.Black;
            this.label15.Location = new System.Drawing.Point(6, 669);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(53, 12);
            this.label15.TabIndex = 143;
            this.label15.Text = "临时目录";
            // 
            // cb删除文件扩大化
            // 
            this.cb删除文件扩大化.AutoSize = true;
            this.cb删除文件扩大化.Checked = true;
            this.cb删除文件扩大化.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cb删除文件扩大化.Location = new System.Drawing.Point(23, 144);
            this.cb删除文件扩大化.Name = "cb删除文件扩大化";
            this.cb删除文件扩大化.Size = new System.Drawing.Size(161, 23);
            this.cb删除文件扩大化.TabIndex = 133;
            this.cb删除文件扩大化.Text = "删除文件扩大化";
            this.cb删除文件扩大化.UseVisualStyleBackColor = true;
            // 
            // tbForumThreadUrl
            // 
            this.tbForumThreadUrl.BackColor = System.Drawing.Color.MistyRose;
            this.tbForumThreadUrl.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbForumThreadUrl.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbForumThreadUrl.Location = new System.Drawing.Point(17, 528);
            this.tbForumThreadUrl.Multiline = true;
            this.tbForumThreadUrl.Name = "tbForumThreadUrl";
            this.tbForumThreadUrl.Size = new System.Drawing.Size(180, 68);
            this.tbForumThreadUrl.TabIndex = 140;
            this.tbForumThreadUrl.Tag = "";
            this.tbForumThreadUrl.Text = "https://byoo.3a004a3395044cb3.com/read.php?tid={tid}";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label14.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label14.ForeColor = System.Drawing.Color.Black;
            this.label14.Location = new System.Drawing.Point(9, 513);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(119, 12);
            this.label14.TabIndex = 141;
            this.label14.Text = "下载论坛的ThreadUrl";
            // 
            // tbForumThreadRegex
            // 
            this.tbForumThreadRegex.BackColor = System.Drawing.Color.MistyRose;
            this.tbForumThreadRegex.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbForumThreadRegex.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbForumThreadRegex.Location = new System.Drawing.Point(14, 474);
            this.tbForumThreadRegex.Multiline = true;
            this.tbForumThreadRegex.Name = "tbForumThreadRegex";
            this.tbForumThreadRegex.Size = new System.Drawing.Size(180, 31);
            this.tbForumThreadRegex.TabIndex = 138;
            this.tbForumThreadRegex.Tag = "";
            this.tbForumThreadRegex.Text = "/read.php?tid=(\\d+)";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label13.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label13.ForeColor = System.Drawing.Color.Black;
            this.label13.Location = new System.Drawing.Point(6, 459);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(131, 12);
            this.label13.TabIndex = 139;
            this.label13.Text = "下载论坛的ThreadRegex";
            // 
            // tbForumSavePath
            // 
            this.tbForumSavePath.BackColor = System.Drawing.Color.MistyRose;
            this.tbForumSavePath.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbForumSavePath.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbForumSavePath.Location = new System.Drawing.Point(129, 174);
            this.tbForumSavePath.Name = "tbForumSavePath";
            this.tbForumSavePath.Size = new System.Drawing.Size(65, 26);
            this.tbForumSavePath.TabIndex = 136;
            this.tbForumSavePath.Tag = "";
            this.tbForumSavePath.Text = "F:\\byoo";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label12.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label12.ForeColor = System.Drawing.Color.Black;
            this.label12.Location = new System.Drawing.Point(6, 181);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(101, 12);
            this.label12.TabIndex = 137;
            this.label12.Text = "下载论坛保存路径";
            // 
            // btnForumDownloader
            // 
            this.btnForumDownloader.BackColor = System.Drawing.Color.LightCoral;
            this.btnForumDownloader.Location = new System.Drawing.Point(14, 601);
            this.btnForumDownloader.Margin = new System.Windows.Forms.Padding(2);
            this.btnForumDownloader.Name = "btnForumDownloader";
            this.btnForumDownloader.Size = new System.Drawing.Size(186, 56);
            this.btnForumDownloader.TabIndex = 135;
            this.btnForumDownloader.Text = "下载论坛图片";
            this.btnForumDownloader.UseVisualStyleBackColor = false;
            this.btnForumDownloader.Click += new System.EventHandler(this.btnForumDownloader_Click);
            // 
            // tbForumGroup
            // 
            this.tbForumGroup.BackColor = System.Drawing.Color.MistyRose;
            this.tbForumGroup.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbForumGroup.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbForumGroup.Location = new System.Drawing.Point(126, 291);
            this.tbForumGroup.Name = "tbForumGroup";
            this.tbForumGroup.Size = new System.Drawing.Size(65, 26);
            this.tbForumGroup.TabIndex = 133;
            this.tbForumGroup.Tag = "";
            this.tbForumGroup.Text = "345";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label11.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label11.ForeColor = System.Drawing.Color.Black;
            this.label11.Location = new System.Drawing.Point(6, 298);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(101, 12);
            this.label11.TabIndex = 134;
            this.label11.Text = "下载论坛的版块ID";
            // 
            // tbForumGroupUrl
            // 
            this.tbForumGroupUrl.BackColor = System.Drawing.Color.MistyRose;
            this.tbForumGroupUrl.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbForumGroupUrl.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbForumGroupUrl.Location = new System.Drawing.Point(11, 339);
            this.tbForumGroupUrl.Multiline = true;
            this.tbForumGroupUrl.Name = "tbForumGroupUrl";
            this.tbForumGroupUrl.Size = new System.Drawing.Size(180, 108);
            this.tbForumGroupUrl.TabIndex = 131;
            this.tbForumGroupUrl.Tag = "";
            this.tbForumGroupUrl.Text = "https://byoo.3a004a3395044cb3.com/thread.php?fid={版块}&page={页}";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label10.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label10.ForeColor = System.Drawing.Color.Black;
            this.label10.Location = new System.Drawing.Point(6, 324);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(107, 12);
            this.label10.TabIndex = 132;
            this.label10.Text = "下载论坛的版块Url";
            // 
            // tbForumCookie
            // 
            this.tbForumCookie.BackColor = System.Drawing.Color.MistyRose;
            this.tbForumCookie.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbForumCookie.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbForumCookie.Location = new System.Drawing.Point(126, 264);
            this.tbForumCookie.Name = "tbForumCookie";
            this.tbForumCookie.Size = new System.Drawing.Size(65, 26);
            this.tbForumCookie.TabIndex = 129;
            this.tbForumCookie.Tag = "";
            this.tbForumCookie.Text = "_safe=61B07F5CD3DA41EC4B22828CDBE7A8D8,1778297015;zh_choose=n;";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label9.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label9.ForeColor = System.Drawing.Color.Black;
            this.label9.Location = new System.Drawing.Point(6, 271);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(101, 12);
            this.label9.TabIndex = 130;
            this.label9.Text = "下载论坛的cookie";
            // 
            // tbForumEndPage
            // 
            this.tbForumEndPage.BackColor = System.Drawing.Color.MistyRose;
            this.tbForumEndPage.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbForumEndPage.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbForumEndPage.Location = new System.Drawing.Point(126, 235);
            this.tbForumEndPage.Name = "tbForumEndPage";
            this.tbForumEndPage.Size = new System.Drawing.Size(65, 26);
            this.tbForumEndPage.TabIndex = 127;
            this.tbForumEndPage.Tag = "";
            this.tbForumEndPage.Text = "1000";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label8.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label8.ForeColor = System.Drawing.Color.Black;
            this.label8.Location = new System.Drawing.Point(6, 242);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(101, 12);
            this.label8.TabIndex = 128;
            this.label8.Text = "论坛版块的终止页";
            // 
            // tbForumStartPage
            // 
            this.tbForumStartPage.BackColor = System.Drawing.Color.MistyRose;
            this.tbForumStartPage.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbForumStartPage.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbForumStartPage.Location = new System.Drawing.Point(126, 201);
            this.tbForumStartPage.Name = "tbForumStartPage";
            this.tbForumStartPage.Size = new System.Drawing.Size(65, 26);
            this.tbForumStartPage.TabIndex = 125;
            this.tbForumStartPage.Tag = "";
            this.tbForumStartPage.Text = "1";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label7.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label7.ForeColor = System.Drawing.Color.Black;
            this.label7.Location = new System.Drawing.Point(6, 208);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(101, 12);
            this.label7.TabIndex = 126;
            this.label7.Text = "论坛版块的起始页";
            // 
            // tbPageRows
            // 
            this.tbPageRows.BackColor = System.Drawing.Color.MistyRose;
            this.tbPageRows.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbPageRows.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbPageRows.Location = new System.Drawing.Point(72, 112);
            this.tbPageRows.Name = "tbPageRows";
            this.tbPageRows.Size = new System.Drawing.Size(101, 26);
            this.tbPageRows.TabIndex = 123;
            this.tbPageRows.Tag = "";
            this.tbPageRows.Text = "40000";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Location = new System.Drawing.Point(19, 113);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(47, 19);
            this.label6.TabIndex = 124;
            this.label6.Text = "行数";
            // 
            // tbPage
            // 
            this.tbPage.BackColor = System.Drawing.Color.MistyRose;
            this.tbPage.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbPage.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tbPage.Location = new System.Drawing.Point(72, 80);
            this.tbPage.Name = "tbPage";
            this.tbPage.Size = new System.Drawing.Size(101, 26);
            this.tbPage.TabIndex = 121;
            this.tbPage.Tag = "";
            this.tbPage.Text = "1";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.label5.ForeColor = System.Drawing.Color.Black;
            this.label5.Location = new System.Drawing.Point(19, 81);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(47, 19);
            this.label5.TabIndex = 122;
            this.label5.Text = "页码";
            // 
            // menuWorkflow
            // 
            this.menuWorkflow.Name = "menuWorkflow";
            this.menuWorkflow.Size = new System.Drawing.Size(61, 4);
            // 
            // statusStrip1
            // 
            this.statusStrip1.BackColor = System.Drawing.Color.Black;
            this.statusStrip1.Font = new System.Drawing.Font("Microsoft YaHei UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabelNo,
            this.toolStripStatusLabelPictureMode,
            this.toolStripStatusLabelRecycleCount,
            this.toolStripStatusLabelStatus,
            this.toolStripStatusLabelFileName,
            this.toolStripStatusLabelSize,
            this.toolStripStatusLabelTrack,
            this.toolStripStatusLabelVolume});
            this.statusStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.statusStrip1.Location = new System.Drawing.Point(0, 1299);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.ShowItemToolTips = true;
            this.statusStrip1.Size = new System.Drawing.Size(2144, 22);
            this.statusStrip1.TabIndex = 132;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
            // 
            // toolStripStatusLabelNo
            // 
            this.toolStripStatusLabelNo.ForeColor = System.Drawing.Color.White;
            this.toolStripStatusLabelNo.Name = "toolStripStatusLabelNo";
            this.toolStripStatusLabelNo.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabelPictureMode
            // 
            this.toolStripStatusLabelPictureMode.ForeColor = System.Drawing.Color.MistyRose;
            this.toolStripStatusLabelPictureMode.Name = "toolStripStatusLabelPictureMode";
            this.toolStripStatusLabelPictureMode.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabelRecycleCount
            // 
            this.toolStripStatusLabelRecycleCount.Font = new System.Drawing.Font("Microsoft YaHei UI", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabelRecycleCount.ForeColor = System.Drawing.Color.LightCoral;
            this.toolStripStatusLabelRecycleCount.Name = "toolStripStatusLabelRecycleCount";
            this.toolStripStatusLabelRecycleCount.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabelStatus
            // 
            this.toolStripStatusLabelStatus.BackColor = System.Drawing.Color.Maroon;
            this.toolStripStatusLabelStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabelStatus.ForeColor = System.Drawing.Color.BlanchedAlmond;
            this.toolStripStatusLabelStatus.Name = "toolStripStatusLabelStatus";
            this.toolStripStatusLabelStatus.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabelFileName
            // 
            this.toolStripStatusLabelFileName.AutoToolTip = true;
            this.toolStripStatusLabelFileName.BackColor = System.Drawing.Color.SeaGreen;
            this.toolStripStatusLabelFileName.Font = new System.Drawing.Font("Microsoft YaHei UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabelFileName.ForeColor = System.Drawing.Color.LightCyan;
            this.toolStripStatusLabelFileName.Name = "toolStripStatusLabelFileName";
            this.toolStripStatusLabelFileName.Size = new System.Drawing.Size(0, 17);
            this.toolStripStatusLabelFileName.ToolTipText = "点击复制文件路径";
            // 
            // toolStripStatusLabelSize
            // 
            this.toolStripStatusLabelSize.BackColor = System.Drawing.Color.OliveDrab;
            this.toolStripStatusLabelSize.Font = new System.Drawing.Font("Microsoft YaHei UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabelSize.ForeColor = System.Drawing.Color.SpringGreen;
            this.toolStripStatusLabelSize.Name = "toolStripStatusLabelSize";
            this.toolStripStatusLabelSize.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabelTrack
            // 
            this.toolStripStatusLabelTrack.BackColor = System.Drawing.Color.DarkCyan;
            this.toolStripStatusLabelTrack.Font = new System.Drawing.Font("Microsoft YaHei UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabelTrack.ForeColor = System.Drawing.Color.White;
            this.toolStripStatusLabelTrack.Name = "toolStripStatusLabelTrack";
            this.toolStripStatusLabelTrack.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabelVolume
            // 
            this.toolStripStatusLabelVolume.BackColor = System.Drawing.Color.SteelBlue;
            this.toolStripStatusLabelVolume.Font = new System.Drawing.Font("Microsoft YaHei UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabelVolume.ForeColor = System.Drawing.Color.White;
            this.toolStripStatusLabelVolume.Name = "toolStripStatusLabelVolume";
            this.toolStripStatusLabelVolume.Size = new System.Drawing.Size(0, 17);
            // 
            // timerDownloadPicture
            // 
            this.timerDownloadPicture.Interval = 2000;
            this.timerDownloadPicture.Tick += new System.EventHandler(this.timerDownloadPicture_Tick);
            // 
            // btnESC
            // 
            this.btnESC.BackColor = System.Drawing.Color.Black;
            this.btnESC.FlatAppearance.BorderColor = System.Drawing.Color.DimGray;
            this.btnESC.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnESC.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnESC.ForeColor = System.Drawing.Color.Red;
            this.btnESC.Location = new System.Drawing.Point(872, 0);
            this.btnESC.Name = "btnESC";
            this.btnESC.Size = new System.Drawing.Size(57, 34);
            this.btnESC.TabIndex = 133;
            this.btnESC.Text = "ESC";
            this.btnESC.UseVisualStyleBackColor = false;
            this.btnESC.Visible = false;
            this.btnESC.Click += new System.EventHandler(this.btnESC_Click);
            // 
            // directoryListControl1
            // 
            this.directoryListControl1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.directoryListControl1.LastFile = "";
            this.directoryListControl1.Location = new System.Drawing.Point(234, 0);
            this.directoryListControl1.Name = "directoryListControl1";
            this.directoryListControl1.Size = new System.Drawing.Size(632, 549);
            this.directoryListControl1.TabIndex = 130;
            this.directoryListControl1.Visible = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gainsboro;
            this.ClientSize = new System.Drawing.Size(2144, 1321);
            this.Controls.Add(this.btnESC);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.directoryListControl1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.trackBar1);
            this.Controls.Add(this.videoView1);
            this.Controls.Add(this.chromBrowser);
            this.KeyPreview = true;
            this.Location = new System.Drawing.Point(390, 0);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "MediaManager2026";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseMove);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.dg1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.videoView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabFiles.ResumeLayout(false);
            this.tabFiles.PerformLayout();
            this.tabDup.ResumeLayout(false);
            this.tabDup.PerformLayout();
            this.tabTools.ResumeLayout(false);
            this.tabTools.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnResize;
        private System.Windows.Forms.TextBox tbMinSize;
        private System.Windows.Forms.DataGridView dg1;
        private System.Windows.Forms.Button btnLoadView;
        private System.Windows.Forms.TextBox tbMaxSize;
        private System.Windows.Forms.Button btnDeleteMinFile;
        private System.Windows.Forms.ComboBox cbFileType;
        private System.Windows.Forms.Button btnShow;
        private System.Windows.Forms.Timer tmrShow;
        private CefSharp.WinForms.ChromiumWebBrowser chromBrowser;
        private System.Windows.Forms.TextBox tbRed;
        private System.Windows.Forms.TextBox tbGreen;
        private System.Windows.Forms.TextBox tbBlue;
        private System.Windows.Forms.TextBox tbHashSize;
        private System.Windows.Forms.TextBox tbGray;
        private System.Windows.Forms.TextBox tbHashRatio;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbCrop;
        private LibVLCSharp.WinForms.VideoView videoView1;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button btnMeta;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabFiles;
        private System.Windows.Forms.TabPage tabDup;
        private System.Windows.Forms.TabPage tabTools;
        private Controls.DirectoryListControl directoryListControl1;
        private System.Windows.Forms.Button btnDirectory;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelSize;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelVolume;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelTrack;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelFileName;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelStatus;
        private System.Windows.Forms.TextBox tbPageRows;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbPage;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbForumGroupUrl;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox tbForumCookie;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox tbForumEndPage;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbForumStartPage;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox tbForumGroup;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btnForumDownloader;
        private System.Windows.Forms.TextBox tbForumSavePath;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox tbForumThreadRegex;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox tbForumThreadUrl;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox cb删除文件扩大化;
        private System.Windows.Forms.TextBox tbTmpDirectory;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelRecycleCount;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelNo;
        private System.Windows.Forms.Button btnClearDBRecord;
        private System.Windows.Forms.Button btnClearDBStatus;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelPictureMode;
        private System.Windows.Forms.Timer timerDownloadPicture;
        private System.Windows.Forms.Button btnESC;
        private System.Windows.Forms.TextBox tbFilenameFilter;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Button btnCompare;
        private System.Windows.Forms.ComboBox cbCompareDctMode;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbSiftFeatures;
        private System.Windows.Forms.TextBox tbSiftMaxDim;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox tbSiftMatchPoint;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label labelDBRecords;
        private System.Windows.Forms.Button btnClearGarbage;
        private System.Windows.Forms.Button btnNameSize;
        private System.Windows.Forms.TextBox tbMaxBucketLimit;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Button btnBucket;
    }
}

