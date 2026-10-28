using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PictureManager
{
    public partial class BucketForm : Form
    {
        private List<string> _files;
        private List<KeyValuePair<string, int>> _topBuckets = new List<KeyValuePair<string, int>>();
        private int _largeThreshold = 500;

        public BucketForm(List<string> files)
        {
            InitializeComponent();
            _files = files;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadBuckets();
            RedrawHistogram();
        }

        /// <summary>
        /// 加载桶：优先 BucketCache，空则自动分桶（默认 Hash）
        /// </summary>
        private void LoadBuckets()
        {
            _largeThreshold = Duplicate.LastLargeBucketThreshold; tbLargeBucket.Text = _largeThreshold.ToString();

            if (Duplicate.BucketCache == null || Duplicate.BucketCache.Count == 0)
            {
                // 自动分桶（默认 Hash）
                AutoSplit("Hash", 4, null);
            }
            UpdateTopBuckets();
        }
        // Hash 细分
        private void btnHashSplit_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(tbHashLevels.Text, out int levels) || levels < 2) return;
            SplitLargeBuckets("Hash", levels);
        }

        // DCT 细分
        private void btnDctSplit_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(tbDctLevels.Text, out int levels) || levels < 2) return;
            SplitLargeBuckets("DCT", levels);
        }

        // SIFT 细分
        private void btnSiftSplit_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(tbSiftPoints.Text, out int points) || points < 4) return;
            SplitLargeBuckets("SIFT", points);
        }

        /// <summary>
        /// 对所有 > tbLargeBucket 的桶，用指定模式 + 参数细分
        /// </summary>
        private void SplitLargeBuckets(string mode, int param)
        {
            // 按当前 BucketCache 分组
            var buckets = Duplicate.BucketCache
                .GroupBy(kv => kv.Value)
                .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

            var newCache = new Dictionary<string, string>();
            int splitCount = 0;

            foreach (var bucket in buckets.Values)
            {
                if (bucket.Count <= _largeThreshold)
                {
                    // 小桶，保留原 key
                    foreach (var file in bucket)
                        newCache[file] = Duplicate.BucketCache[file];
                    continue;
                }

                // 大桶，细分
                splitCount++;
                foreach (var file in bucket)
                {
                    var feature = FeatureCacheManager.Instance.GetOrExtract(file, mode);
                    if (feature == null) continue;

                    string key = Duplicate.GetBucketByMode(mode, feature, param);
                    if (!string.IsNullOrEmpty(key))
                        newCache[file] = key;
                    else
                        newCache[file] = Duplicate.BucketCache[file];   // 失败保留原 key
                }
            }

            // 更新 BucketCache
            Duplicate.BucketCache.Clear();
            foreach (var kv in newCache)
                Duplicate.BucketCache[kv.Key] = kv.Value;

            Debug.WriteLine($"[BucketForm] 细分 {mode} param={param}, 大桶 {splitCount} 个");

            // 刷新直方图
            UpdateTopBuckets();
            RedrawHistogram();
        }
        private void UpdateTopBuckets()
        {
            _topBuckets = Duplicate.BucketCache
                .GroupBy(kv => kv.Value)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .OrderByDescending(kv => kv.Value)
                .Take(100)
                .ToList();

            Debug.WriteLine($"[BucketForm] files={_files.Count}, BucketCache={Duplicate.BucketCache.Count}, topBuckets={_topBuckets.Count}, maxCount={(_topBuckets.Count > 0 ? _topBuckets.Max(kv => kv.Value) : 0)}");
        }

        /// <summary>
        /// 自动分桶（全量），用指定模式 + 档位
        /// </summary>
        private void AutoSplit(string mode, int levels, List<string> subset)
        {
            var targets = subset ?? _files;
            var comparer = PictureCompareManager.Get(mode);
            if (comparer == null) return;

            foreach (var file in targets)
            {
                var feature = FeatureCacheManager.Instance.GetOrExtract(file, mode);
                if (feature == null) continue;

                string key = Duplicate.GetBucketByMode(mode, feature, levels);   // ★ 需新增
                if (!string.IsNullOrEmpty(key))
                    Duplicate.BucketCache[file] = key;
            }
        }

        private void RedrawHistogram()
        {
            panelHistogram.Invalidate();
        }

        private void panelHistogram_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);

            if (_topBuckets.Count == 0)
            {
                g.DrawString("无桶数据", this.Font, Brushes.Gray, 10, 10);
                return;
            }

            int panelW = panelHistogram.ClientSize.Width;
            int panelH = panelHistogram.ClientSize.Height;
            int margin = 40;
            int chartW = panelW - margin * 2;
            int chartH = panelH - margin * 2;

            int maxCount = _topBuckets.Max(kv => kv.Value);
            int barW = Math.Max(1, chartW / _topBuckets.Count);

            using (var penAxis = new Pen(Color.Black))
            {
                g.DrawLine(penAxis, margin, panelH - margin, panelW - margin, panelH - margin);   // X 轴
                g.DrawLine(penAxis, margin, margin, margin, panelH - margin);                       // Y 轴
            }

            for (int i = 0; i < _topBuckets.Count; i++)
            {
                int count = _topBuckets[i].Value;
                int barH = (int)((double)count / maxCount * chartH);
                int x = margin + i * barW;
                int y = panelH - margin - barH;

                Brush brush = count > _largeThreshold ? Brushes.Red : Brushes.LightGray;
                g.FillRectangle(brush, x, y, barW - 1, barH);
            }

            g.DrawString($"Top {_topBuckets.Count} 桶，最大 {maxCount}，阈值 {_largeThreshold}",
                this.Font, Brushes.Black, margin, 10);
        }

        private void tbLargeBucket_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(tbLargeBucket.Text, out int v) && v > 0)
            {
                _largeThreshold = v;
                Duplicate.LastLargeBucketThreshold = v;

                RedrawHistogram();
            }
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            Duplicate.LastLargeBucketThreshold = _largeThreshold;
            // 阶段 1：BucketCache 已有值，直接关闭
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}