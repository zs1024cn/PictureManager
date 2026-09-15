using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureManager
{
    public partial class Form1
    {
        // 查重工作流相关方法（从 Form1.cs 移入）
        /// <summary>
        /// 按当前模式批量提取特征。
        /// Hash 模式：一次提取 4 种基础特征（Hash/DCT/SIFT/HSV）
        /// 其他模式：只提取对应的一种
        /// </summary>
        private async Task CalcFeaturesAsync(CancellationToken token)
        {
            SyncSettingsFromUI();

            string mode = _currentCompareMode;
            var allFiles = GetValidImageFiles();

            var cache = FeatureCacheManager.Instance;
            var filesToProcess = new List<string>();

            foreach (var file in allFiles)
            {
                if (token.IsCancellationRequested) break;

                if (!cache.GetAll().TryGetValue(file, out var feature))
                {
                    filesToProcess.Add(file);
                    continue;
                }

                bool needExtract = false;
                switch (mode)
                {
                    case "Hash": needExtract = feature.Hash == null; break;
                    case "DCT": needExtract = feature.Dct == null; break;
                    case "SIFT": needExtract = feature.Sift == null; break;
                    case "HSV": needExtract = feature.Hsv == null; break;
                    case "Face": needExtract = feature.Face == null; break;
                    case "Pose": needExtract = feature.Pose == null; break;
                    case "Scene": needExtract = feature.Scene == null; break;
                }

                if (needExtract) filesToProcess.Add(file);
            }

            if (filesToProcess.Count == 0)
            {
                this.toolStripStatusLabelStatus.Text = $"{mode} 特征已全部提取，无需处理";
                return;
            }

            this.toolStripStatusLabelStatus.Text = $"开始提取 {mode} 特征...";

            DateTime d0 = DateTime.Now;
            int total = filesToProcess.Count;
            int processed = 0;

            await Task.Run(() =>
            {
                bool canParallel = (mode == "Hash" || mode == "DCT" || mode == "SIFT" || mode == "HSV");

                if (canParallel)
                {
                    try
                    {
                        Parallel.ForEach(filesToProcess,
                            new ParallelOptions
                            {
                                MaxDegreeOfParallelism = Environment.ProcessorCount,
                                CancellationToken = token
                            },
                            file =>
                            {
                                if (token.IsCancellationRequested) return;

                                try
                                {
                                    FeatureCacheManager.Instance.GetOrExtract(file, mode);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"提取失败: {file}, {ex.Message}");
                                }

                                int current = Interlocked.Increment(ref processed);
                                if (current % 50 == 0 || current == total)
                                {
                                    BeginInvoke(new Action(() =>
                                    {
                                        this.toolStripStatusLabelStatus.Text = $"{mode} 提取 {current}/{total}...";
                                    }));
                                }
                            });
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("提取已取消");
                    }
                }
                else
                {
                    foreach (var file in filesToProcess)
                    {
                        if (token.IsCancellationRequested) break;

                        try
                        {
                            FeatureCacheManager.Instance.GetOrExtract(file, mode);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"提取失败: {file}, {ex.Message}");
                        }

                        int current = Interlocked.Increment(ref processed);
                        if (current % 10 == 0 || current == total)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                this.toolStripStatusLabelStatus.Text = $"{mode} 提取 {current}/{total}...";
                            }));
                        }
                    }
                }
            }, token);

            if (token.IsCancellationRequested)
            {
                this.toolStripStatusLabelStatus.Text = $"{mode} 提取已取消. 已处理 {processed}/{total}";
            }
            else
            {
                this.toolStripStatusLabelStatus.Text =
                    $"{mode} 提取完成. {total} 张, 耗时{(DateTime.Now - d0).TotalSeconds:F1}s";
            }
        }
        /// <summary>
        /// 用当前模式查找重复组。
        /// </summary>
        private async Task CompareAllAsync(CancellationToken token)
        {
            var comparer = PictureCompareManager.Get(_currentCompareMode);
            if (comparer == null)
            {
                MessageBox.Show($"未找到对比器: {_currentCompareMode}");
                return;
            }

            SyncSettingsFromUI();

            double threshold = DuplicateSettings.DefaultThreshold;
            string bucketType = cbBucket.Text;
            string mode = _currentCompareMode;

            this.toolStripStatusLabelStatus.Text = $"开始 {mode} 查重（分桶：{bucketType}）...";

            await Task.Run(() =>
            {
                DateTime d0 = DateTime.Now;
                List<List<string>> groups;
                try
                {
                    groups = comparer.FindAllDuplicates(threshold, bucketType);
                }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"查重失败: {ex.Message}");
                    }));
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    BeginInvoke(new Action(() =>
                    {
                        this.toolStripStatusLabelStatus.Text = $"{mode} 查重已取消";
                    }));
                    return;
                }

                BeginInvoke(new Action(() =>
                {
                    this.toolStripStatusLabelStatus.Text =
                        $"{mode}完成（分桶：{bucketType}）. 重复组:{groups.Count}, 耗时{(DateTime.Now - d0).TotalSeconds:F1}s";

                    if (groups.Count == 0)
                    {
                        MessageBox.Show("没有找到重复组");
                        return;
                    }

                    ShowGroupsPictures(groups);
                }));
            }, token);
        }
        /// <summary>
        /// 把 UI 控件的值同步到 DuplicateSettings
        /// </summary>
        private void SyncSettingsFromUI()
        {
            // Hash 参数
            if (int.TryParse(tbHashSize.Text, out int size)) DuplicateSettings.HashSize = size;
            if (int.TryParse(tbGray.Text, out int gray)) DuplicateSettings.HashGray = gray;
            if (float.TryParse(tbRed.Text, out float r)) DuplicateSettings.HashRed = r;
            if (float.TryParse(tbGreen.Text, out float g)) DuplicateSettings.HashGreen = g;
            if (float.TryParse(tbBlue.Text, out float b)) DuplicateSettings.HashBlue = b;
            DuplicateSettings.HashCrop = cbCrop.SelectedIndex;

            // SIFT 参数
            if (int.TryParse(tbSiftFeatures.Text, out int maxF)) DuplicateSettings.SiftMaxFeatures = maxF;
            if (int.TryParse(tbSiftMaxDim.Text, out int maxDim)) DuplicateSettings.SiftMaxDim = maxDim;
            if (int.TryParse(tbSiftMatchPoint.Text, out int minMatch)) DuplicateSettings.SiftMinMatchPoints = minMatch;

            // DCT 参数
            DuplicateSettings.DctMode = cbCompareDctMode.Text;

            // 阈值
            if (double.TryParse(tbHashRatio.Text, out double ratio)) DuplicateSettings.DefaultThreshold = ratio;

            // 分桶
            DuplicateSettings.BucketType = cbBucket.Text;
        }
        /// <summary>
        /// 切换对比算法
        /// </summary>
        private void SetCompareMode(string mode)
        {
            _currentCompareMode = mode;
            btnCalc.Text = mode;
            UpdateMenuCheckState();
        }
        /// <summary>
        /// 动态生成工作流菜单（7 种算法）
        /// </summary>
        private void BuildWorkflowMenu()
        {
            if (menuWorkflow == null) return;

            menuWorkflow.Items.Clear();

            foreach (var name in PictureCompareManager.GetAllNames())
            {
                var item = new ToolStripMenuItem(name);
                item.Click += (s, e) => SetCompareMode(name);
                menuWorkflow.Items.Add(item);
            }

            // 设置初始勾选
            SetCompareMode(_currentCompareMode);
        }
        /// <summary>
        /// 更新工作流菜单的勾选状态
        /// </summary>
        private void UpdateMenuCheckState()
        {
            if (menuWorkflow == null) return;
            foreach (ToolStripMenuItem item in menuWorkflow.Items)
            {
                item.Checked = string.Equals(item.Text, _currentCompareMode, StringComparison.OrdinalIgnoreCase);
            }
        }
        /// <summary>
        /// 获取 dg1 中所有有效图片文件（仅路径）。
        /// </summary>
        private List<string> GetValidImageFiles()
        {
            var files = new List<string>();
            for (int r = 0; r < dg1.Rows.Count; r++)
            {
                string file = dg1.Rows[r].Cells["Name"].Value?.ToString();
                if (!string.IsNullOrEmpty(file)
                    && File.Exists(file)
                    && FileUtility.IsImageFile(Path.GetExtension(file)))
                {
                    files.Add(file);
                }
            }
            return files;
        }
        private async void btnCalc_Click(object sender, EventArgs e)
        {
            // 已在执行 → 点停止
            if (_calcCts != null && !_calcCts.IsCancellationRequested)
            {
                _calcCts.Cancel();
                return;
            }

            // 开始新任务
            string originalText = btnCalc.Text;
            _calcCts = new CancellationTokenSource();

            btnCalc.Text = "■";
            btnCalc.Enabled = true;

            try
            {
                await CalcFeaturesAsync(_calcCts.Token);
            }
            catch (OperationCanceledException)
            {
                // 已取消
            }
            catch (Exception ex)
            {
                MessageBox.Show($"提取失败: {ex.Message}");
            }
            finally
            {
                btnCalc.Text = originalText;
                _calcCts.Dispose();
                _calcCts = null;
            }
        }

        private async void btnCompare_Click(object sender, EventArgs e)
        {
            // 已在执行 → 点停止
            if (_compareCts != null && !_compareCts.IsCancellationRequested)
            {
                _compareCts.Cancel();
                return;
            }

            // 检查是否有图提取了当前模式对应的特征
            string mode = _currentCompareMode;
            int readyCount = 0;
            var cache = FeatureCacheManager.Instance;

            foreach (var kvp in cache.GetAll())
            {
                bool has = false;
                switch (mode)
                {
                    case "Hash": has = kvp.Value.Hash != null; break;
                    case "DCT": has = kvp.Value.Dct != null; break;
                    case "SIFT": has = kvp.Value.Sift != null; break;
                    case "HSV": has = kvp.Value.Hsv != null; break;
                    case "Face": has = kvp.Value.Face != null; break;
                    case "Pose": has = kvp.Value.Pose != null; break;
                    case "Scene": has = kvp.Value.Scene != null; break;
                }
                if (has) readyCount++;
            }

            if (readyCount == 0)
            {
                MessageBox.Show($"当前没有图片提取过 {mode} 特征。\r\n请先点计算按钮提取特征。");
                return;
            }

            // 开始新任务
            string originalText = btnCompare.Text;
            _compareCts = new CancellationTokenSource();

            btnCompare.Text = "■";
            btnCompare.Enabled = true;

            try
            {
                await CompareAllAsync(_compareCts.Token);
            }
            catch (OperationCanceledException)
            {
                // 已取消
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查重失败: {ex.Message}");
            }
            finally
            {
                btnCompare.Text = originalText;
                _compareCts.Dispose();
                _compareCts = null;
            }
        }
        private void btnWorkflow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                menuWorkflow.Show(btnCalc, e.Location);
            }
        }

        private void tbHashSize_TextChanged(object sender, EventArgs e)
        {
            FeatureCacheManager.Instance.ClearAll();
        }

        private void tbGray_TextChanged(object sender, EventArgs e)
        {
            FeatureCacheManager.Instance.ClearAll();
        }

        private void tbSiftFeatures_TextChanged(object sender, EventArgs e)
        {
            FeatureCacheManager.Instance.ClearAll();
        }

        private void tbSiftMaxDim_TextChanged(object sender, EventArgs e)
        {
            FeatureCacheManager.Instance.ClearAll();
        }

        private void cbCrop_SelectedIndexChanged(object sender, EventArgs e)
        {
            FeatureCacheManager.Instance.ClearAll();
        }

        private void tbRed_TextChanged(object sender, EventArgs e)
        {
            FeatureCacheManager.Instance.ClearAll();
        }

        private void tbGreen_TextChanged(object sender, EventArgs e)
        {
            FeatureCacheManager.Instance.ClearAll();
        }

        private void tbBlue_TextChanged(object sender, EventArgs e)
        {
            FeatureCacheManager.Instance.ClearAll();
        }

        private void dg1_CurrentCellChanged(object sender, EventArgs e)
        {
            ShowPicture();
        }

    }
}