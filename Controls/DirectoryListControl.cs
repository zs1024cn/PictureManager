//Controls\DirectoryListControl.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PictureManager.Controls
{
    /// <summary>
    /// 目录列表控件 - 支持多选、增删、JSON 持久化
    /// </summary>
    public partial class DirectoryListControl : UserControl
    {
        private string _jsonPath;
        public string LastFile { get; set; } = "";
        /// <summary>
        /// 获取勾选的目录列表
        /// </summary>
        public List<string> CheckedDirectories
        {
            get
            {
                var list = new List<string>();
                foreach (var item in clbDirectories.CheckedItems)
                {
                    list.Add(item.ToString());
                }
                return list;
            }
        }

        public DirectoryListControl()
        {
            InitializeComponent();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string newDir = txtNewDirectory.Text.Trim();
            if (string.IsNullOrEmpty(newDir))
            {
                MessageBox.Show("请输入目录路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(newDir))
            {
                MessageBox.Show($"目录不存在:\n{newDir}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!clbDirectories.Items.Contains(newDir))
            {
                clbDirectories.Items.Add(newDir, true);
                txtNewDirectory.Clear();
            }
            else
            {
                int index = clbDirectories.Items.IndexOf(newDir);
                clbDirectories.SetItemChecked(index, true);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "选择目录",
                ShowNewFolderButton = true
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtNewDirectory.Text = dialog.SelectedPath;
                    btnAdd_Click(sender, e);
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            for (int i = clbDirectories.CheckedIndices.Count - 1; i >= 0; i--)
            {
                int index = clbDirectories.CheckedIndices[i];
                clbDirectories.Items.RemoveAt(index);
            }
        }

        /// <summary>
        /// 从 JSON 文件加载目录列表
        /// </summary>
        public void LoadFromJson(string jsonPath = null)
        {
            _jsonPath = jsonPath ?? Path.Combine(Application.StartupPath, "directories.json");

            try
            {
                if (File.Exists(_jsonPath))
                {
                    string json = File.ReadAllText(_jsonPath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<DirectoryListData>(json);

                    clbDirectories.Items.Clear();
                    if (data?.Directories != null)
                    {
                        foreach (var dir in data.Directories)
                        {
                            //if (Directory.Exists(dir.Path))
                            {
                                clbDirectories.Items.Add(dir.Path, dir.Checked);
                            }
                        }
                    }
                    LastFile = data?.LastFile ?? "";   // ★ 新增
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载目录列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存目录列表到 JSON 文件
        /// </summary>
        public void SaveToJson()
        {
            if (string.IsNullOrEmpty(_jsonPath))
            {
                _jsonPath = Path.Combine(Application.StartupPath, "directories.json");
            }

            try
            {
                var data = new DirectoryListData
                {
                    Directories = clbDirectories.Items.Cast<string>()
                        .Select((path, index) => new DirectoryItem
                        {
                            Path = path,
                            Checked = clbDirectories.GetItemChecked(index)
                        })
                        .ToList(),                        
                    LastFile = this.LastFile   // ★ 新增
                };

                string json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_jsonPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存目录列表失败: {ex.Message}");
            }
        }

        private class DirectoryListData
        {
            public List<DirectoryItem> Directories { get; set; }
            public string LastFile { get; set; }   // ★ 新增
        }

        private class DirectoryItem
        {
            public string Path { get; set; }
            public bool Checked { get; set; }
        }

        private void DirectoryListControl_SizeChanged(object sender, EventArgs e)
        {
            this.clbDirectories.Width = this.clbDirectories.Parent.Width - this.clbDirectories.Left * 2;
            this.btnAdd.Top = this.btnAdd.Parent.Height - btnAdd.Height - 20;
            this.btnBrowse.Top = this.btnAdd.Top;
            this.btnRemove.Top = this.btnAdd.Top;
            this.txtNewDirectory.Top = this.btnAdd.Top;
            this.clbDirectories.Height = this.btnAdd.Top - 20;
        }
    }
}