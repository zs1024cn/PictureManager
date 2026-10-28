namespace PictureManager
{
    partial class BucketForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelHistogram;
        private System.Windows.Forms.TextBox tbLargeBucket;
        private System.Windows.Forms.Label labelLargeBucket;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabHash;
        private System.Windows.Forms.TabPage tabDct;
        private System.Windows.Forms.TabPage tabSift;
        private System.Windows.Forms.TextBox tbHashLevels;
        private System.Windows.Forms.TextBox tbDctLevels;
        private System.Windows.Forms.TextBox tbSiftPoints;
        private System.Windows.Forms.Button btnHashSplit;
        private System.Windows.Forms.Button btnDctSplit;
        private System.Windows.Forms.Button btnSiftSplit;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHistogram = new System.Windows.Forms.Panel();
            this.tbLargeBucket = new System.Windows.Forms.TextBox();
            this.labelLargeBucket = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabHash = new System.Windows.Forms.TabPage();
            this.tabDct = new System.Windows.Forms.TabPage();
            this.tabSift = new System.Windows.Forms.TabPage();
            this.tbHashLevels = new System.Windows.Forms.TextBox();
            this.tbDctLevels = new System.Windows.Forms.TextBox();
            this.tbSiftPoints = new System.Windows.Forms.TextBox();
            this.btnHashSplit = new System.Windows.Forms.Button();
            this.btnDctSplit = new System.Windows.Forms.Button();
            this.btnSiftSplit = new System.Windows.Forms.Button();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            // panelHistogram
            this.panelHistogram.Location = new System.Drawing.Point(12, 12);
            this.panelHistogram.Size = new System.Drawing.Size(960, 380);
            this.panelHistogram.BackColor = System.Drawing.Color.White;
            this.panelHistogram.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelHistogram.Paint += new System.Windows.Forms.PaintEventHandler(this.panelHistogram_Paint);

            // labelLargeBucket
            this.labelLargeBucket.AutoSize = true;
            this.labelLargeBucket.Location = new System.Drawing.Point(12, 405);
            this.labelLargeBucket.Text = "大桶阈值:";

            // tbLargeBucket
            this.tbLargeBucket.Location = new System.Drawing.Point(90, 402);
            this.tbLargeBucket.Size = new System.Drawing.Size(80, 23);
            this.tbLargeBucket.Text = "500";
            this.tbLargeBucket.TextChanged += new System.EventHandler(this.tbLargeBucket_TextChanged);

            // tabControl1
            this.tabControl1.Location = new System.Drawing.Point(12, 435);
            this.tabControl1.Size = new System.Drawing.Size(960, 130);

            // tabHash
            this.tabHash.Text = "Hash";
            this.tabHash.Controls.Add(this.tbHashLevels);
            this.tabHash.Controls.Add(this.btnHashSplit);

            // tbHashLevels
            this.tbHashLevels.Location = new System.Drawing.Point(20, 20);
            this.tbHashLevels.Size = new System.Drawing.Size(80, 23);
            this.tbHashLevels.Text = "6";

            // btnHashSplit
            this.btnHashSplit.Location = new System.Drawing.Point(120, 18);
            this.btnHashSplit.Size = new System.Drawing.Size(80, 28);
            this.btnHashSplit.Text = "细分";
            this.btnHashSplit.Enabled = true;
            this.btnHashSplit.Click += new System.EventHandler(this.btnHashSplit_Click);

            // tabDct
            this.tabDct.Text = "DCT";
            this.tabDct.Controls.Add(this.tbDctLevels);
            this.tabDct.Controls.Add(this.btnDctSplit);

            // tbDctLevels
            this.tbDctLevels.Location = new System.Drawing.Point(20, 20);
            this.tbDctLevels.Size = new System.Drawing.Size(80, 23);
            this.tbDctLevels.Text = "3";

            // btnDctSplit
            this.btnDctSplit.Location = new System.Drawing.Point(120, 18);
            this.btnDctSplit.Size = new System.Drawing.Size(80, 28);
            this.btnDctSplit.Text = "细分";
            this.btnDctSplit.Enabled = true;
            this.btnDctSplit.Click += new System.EventHandler(this.btnDctSplit_Click);

            // tabSift
            this.tabSift.Text = "SIFT";
            this.tabSift.Controls.Add(this.tbSiftPoints);
            this.tabSift.Controls.Add(this.btnSiftSplit);

            // tbSiftPoints
            this.tbSiftPoints.Location = new System.Drawing.Point(20, 20);
            this.tbSiftPoints.Size = new System.Drawing.Size(80, 23);
            this.tbSiftPoints.Text = "9";

            // btnSiftSplit
            this.btnSiftSplit.Location = new System.Drawing.Point(120, 18);
            this.btnSiftSplit.Size = new System.Drawing.Size(80, 28);
            this.btnSiftSplit.Text = "细分";
            this.btnSiftSplit.Enabled = true;
            this.btnSiftSplit.Click += new System.EventHandler(this.btnSiftSplit_Click);

            // ★ 关键：TabPage 加到 TabControl
            this.tabControl1.Controls.Add(this.tabHash);
            this.tabControl1.Controls.Add(this.tabDct);
            this.tabControl1.Controls.Add(this.tabSift);

            // btnConfirm
            this.btnConfirm.Location = new System.Drawing.Point(800, 575);
            this.btnConfirm.Size = new System.Drawing.Size(80, 30);
            this.btnConfirm.Text = "确认";
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(892, 575);
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.Text = "取消";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // BucketForm
            this.ClientSize = new System.Drawing.Size(984, 620);
            this.Controls.Add(this.panelHistogram);
            this.Controls.Add(this.labelLargeBucket);
            this.Controls.Add(this.tbLargeBucket);
            this.Controls.Add(this.tabControl1);   // ★ 关键：TabControl 加到 Form
            this.Controls.Add(this.btnConfirm);
            this.Controls.Add(this.btnCancel);
            this.Text = "分桶设置";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }
    }
}