//Compare\ImageFeatureExtractor.cs
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PictureManager
{
    /// <summary>
    /// 特征提取器。
    /// 基础特征（Hash/DCT/SIFT/HSV）一次读图提取。
    /// DNN 特征（Face/Pose/Scene）独立方法，按需调用。
    /// 模型懒加载。
    /// </summary>
    public static class ImageFeatureExtractor
    {
        // 模型路径
        private static readonly string ProtoFace = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy.prototxt");
        private static readonly string ModelFace = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res10_300x300_ssd_iter_140000.caffemodel");
        private static readonly string ModelOpenFace = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nn4.small2.v1.t7");

        private static readonly string ProtoPose = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pose_deploy_linevec.prototxt");
        private static readonly string ModelPose = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pose_iter_440000.caffemodel");

        private static readonly string ProtoScene = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy_googlenet_places365.prototxt");
        private static readonly string ModelScene = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "googlenet_places365.caffemodel");

        private static Net _netFaceDetect;
        private static Net _netOpenFace;
        private static Net _netPose;
        private static Net _netScene;
        private static readonly object _lock = new object();
        private static bool _modelsReady = false;

        private static void EnsureFaceModels()
        {
            if (_netFaceDetect != null && _netOpenFace != null) return;
            lock (_lock)
            {
                if (_netFaceDetect == null)
                {
                    try { if (File.Exists(ProtoFace) && File.Exists(ModelFace)) _netFaceDetect = CvDnn.ReadNetFromCaffe(ProtoFace, ModelFace); } catch (Exception ex) { Debug.WriteLine($"Face detect load: {ex.Message}"); }
                }
                if (_netOpenFace == null)
                {
                    try { if (File.Exists(ModelOpenFace)) _netOpenFace = CvDnn.ReadNetFromTorch(ModelOpenFace); } catch (Exception ex) { Debug.WriteLine($"OpenFace load: {ex.Message}"); }
                }
            }
        }

        private static void EnsurePoseModel()
        {
            if (_netPose != null) return;
            lock (_lock)
            {
                if (_netPose == null)
                {
                    try { if (File.Exists(ProtoPose) && File.Exists(ModelPose)) _netPose = CvDnn.ReadNetFromCaffe(ProtoPose, ModelPose); } catch (Exception ex) { Debug.WriteLine($"Pose load: {ex.Message}"); }
                }
            }
        }

        private static void EnsureSceneModel()
        {
            if (_netScene != null) return;
            lock (_lock)
            {
                if (_netScene == null)
                {
                    try { if (File.Exists(ProtoScene) && File.Exists(ModelScene)) _netScene = CvDnn.ReadNetFromCaffe(ProtoScene, ModelScene); } catch (Exception ex) { Debug.WriteLine($"Scene load: {ex.Message}"); }
                }
            }
        }

        // ========== 基础特征：一次读图提取 Hash + DCT + SIFT + HSV ==========

        /// <summary>
        /// 提取基础特征（Hash + DCT + SIFT + HSV）。
        /// 一次读图，四种特征全从同一个 Mat 提取，并写入 Duplicate 缓存。
        /// </summary>
        public static void ExtractBasic(ImageFeature feature, string file)
        {
            if (feature == null || string.IsNullOrEmpty(file) || !File.Exists(file)) return;

            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (mat == null || mat.Empty()) return;

                    feature.FilePath = file;
                    feature.ExtractTime = DateTime.Now;

                    using (var cropped = Duplicate.CropByIndex(mat, DuplicateSettings.HashCrop))
                    {
                        feature.Hash = Duplicate.ExtractHashByMat(
                            cropped, file,
                            DuplicateSettings.HashSize,
                            DuplicateSettings.HashGray);

                        feature.Dct = Duplicate.ExtractDctByMat(cropped, file);

                        feature.Sift = Duplicate.ExtractSiftByMat(
                            cropped, file,
                            DuplicateSettings.SiftMaxFeatures,
                            DuplicateSettings.SiftMaxDim);

                        //feature.Hsv = Duplicate.ExtractHsvByMat(cropped, file);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExtractBasic 异常: {file}, {ex.Message}");
            }
        }

        /// <summary>
        /// 单独提取 Hash（内部会触发 DCT/SIFT/HSV，因为 GetHash 是一体化提取）
        /// </summary>
        public static void ExtractHash(ImageFeature feature, string file)
        {
            ExtractBasic(feature, file);
        }
        /// <summary>单独提取 DCT（一次读图）</summary>
        public static void ExtractDct(ImageFeature feature, string file)
        {
            if (feature == null || string.IsNullOrEmpty(file) || !File.Exists(file)) return;
            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (mat == null || mat.Empty()) return;
                    using (var cropped = Duplicate.CropByIndex(mat, DuplicateSettings.HashCrop))
                    {
                        feature.Dct = Duplicate.ExtractDctByMat(cropped, file);
                    }
                }
                feature.FilePath = file;
                feature.ExtractTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExtractDct 异常: {file}, {ex.Message}");
            }
        }

        /// <summary>单独提取 SIFT（一次读图）</summary>
        public static void ExtractSift(ImageFeature feature, string file)
        {
            if (feature == null || string.IsNullOrEmpty(file) || !File.Exists(file)) return;
            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (mat == null || mat.Empty()) return;
                    using (var cropped = Duplicate.CropByIndex(mat, DuplicateSettings.HashCrop))
                    {
                        feature.Sift = Duplicate.ExtractSiftByMat(
                            cropped, file,
                            DuplicateSettings.SiftMaxFeatures,
                            DuplicateSettings.SiftMaxDim);
                    }
                }
                feature.FilePath = file;
                feature.ExtractTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExtractSift 异常: {file}, {ex.Message}");
            }
        }

        /// <summary>单独提取 HSV（一次读图）</summary>
        public static void ExtractHsv(ImageFeature feature, string file)
        {
            if (feature == null || string.IsNullOrEmpty(file) || !File.Exists(file)) return;
            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (mat == null || mat.Empty()) return;
                    using (var cropped = Duplicate.CropByIndex(mat, DuplicateSettings.HashCrop))
                    {
                        feature.Hsv = Duplicate.ExtractHsvByMat(cropped, file);
                    }
                }
                feature.FilePath = file;
                feature.ExtractTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExtractHsv 异常: {file}, {ex.Message}");
            }
        }

        // ========== DNN 特征：独立方法 ==========

        /// <summary>
        /// 提取人脸特征。无论成功失败，都会给 Face 赋值（成功为 N 维，失败为 float[0]）。
        /// </summary>
        public static void ExtractFace(ImageFeature feature, string file)
        {
            if (feature == null || string.IsNullOrEmpty(file)) return;

            EnsureFaceModels();
            if (_netFaceDetect == null || _netOpenFace == null)
            {
                feature.Face = new float[0];
                return;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (mat == null || mat.Empty())
                    {
                        feature.Face = new float[0];
                        return;
                    }

                    var result = ExtractFaceFeatureInternal(mat);
                    feature.Face = result ?? new float[0];
                    feature.FilePath = file;
                    feature.ExtractTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExtractFace 异常: {file}, {ex.Message}");
                feature.Face = new float[0];
            }
        }

        /// <summary>提取姿态特征。失败时 Pose = float[0]。</summary>
        public static void ExtractPose(ImageFeature feature, string file)
        {
            if (feature == null || string.IsNullOrEmpty(file)) return;

            EnsurePoseModel();
            if (_netPose == null)
            {
                feature.Pose = new float[0];
                return;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (mat == null || mat.Empty())
                    {
                        feature.Pose = new float[0];
                        return;
                    }

                    var result = ExtractPoseFeatureInternal(mat);
                    feature.Pose = result ?? new float[0];
                    feature.FilePath = file;
                    feature.ExtractTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExtractPose 异常: {file}, {ex.Message}");
                feature.Pose = new float[0];
            }
        }

        /// <summary>提取场景特征。失败时 Scene = float[0]。</summary>
        public static void ExtractScene(ImageFeature feature, string file)
        {
            if (feature == null || string.IsNullOrEmpty(file)) return;

            EnsureSceneModel();
            if (_netScene == null)
            {
                feature.Scene = new float[0];
                return;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(file);
                using (var mat = Cv2.ImDecode(fileData, ImreadModes.Color))
                {
                    if (mat == null || mat.Empty())
                    {
                        feature.Scene = new float[0];
                        return;
                    }

                    var result = ExtractSceneFeatureInternal(mat);
                    feature.Scene = result ?? new float[0];
                    feature.FilePath = file;
                    feature.ExtractTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExtractScene 异常: {file}, {ex.Message}");
                feature.Scene = new float[0];
            }
        }

        // ========== 内部实现 ==========

        private static float[] ExtractFaceFeatureInternal(Mat src)
        {
            using (var blob = CvDnn.BlobFromImage(src, 1.0, new OpenCvSharp.Size(300, 300),
                new Scalar(104.0, 177.0, 123.0), false, false))
            {
                _netFaceDetect.SetInput(blob);
                using (var detections = _netFaceDetect.Forward())
                {
                    int numDet = detections.Size(2);
                    float bestConf = 0;
                    Rect bestFace = new Rect();

                    for (int i = 0; i < numDet; i++)
                    {
                        float conf = detections.At<float>(0, 0, i, 2);
                        if (conf > bestConf && conf > 0.5f)
                        {
                            int x1 = (int)(detections.At<float>(0, 0, i, 3) * src.Width);
                            int y1 = (int)(detections.At<float>(0, 0, i, 4) * src.Height);
                            int x2 = (int)(detections.At<float>(0, 0, i, 5) * src.Width);
                            int y2 = (int)(detections.At<float>(0, 0, i, 6) * src.Height);

                            x1 = Math.Max(0, x1);
                            y1 = Math.Max(0, y1);
                            x2 = Math.Min(src.Width, x2);
                            y2 = Math.Min(src.Height, y2);

                            if (x2 - x1 > 20 && y2 - y1 > 20)
                            {
                                bestConf = conf;
                                bestFace = new Rect(x1, y1, x2 - x1, y2 - y1);
                            }
                        }
                    }

                    if (bestConf == 0) return null;

                    using (var faceMat = new Mat(src, bestFace))
                    using (var resized = new Mat())
                    {
                        Cv2.Resize(faceMat, resized, new OpenCvSharp.Size(96, 96));

                        using (var blob2 = CvDnn.BlobFromImage(resized, 1.0 / 255.0,
                            new OpenCvSharp.Size(96, 96), new Scalar(0, 0, 0), true, false))
                        {
                            _netOpenFace.SetInput(blob2);
                            using (var output = _netOpenFace.Forward())
                            {
                                int total = (int)output.Total();
                                float[] vec = new float[total];
                                for (int i = 0; i < total; i++)
                                    vec[i] = output.At<float>(0, i);

                                double norm = 0;
                                for (int i = 0; i < vec.Length; i++) norm += vec[i] * vec[i];
                                norm = Math.Sqrt(norm);
                                if (norm > 0)
                                    for (int i = 0; i < vec.Length; i++) vec[i] = (float)(vec[i] / norm);

                                return vec;
                            }
                        }
                    }
                }
            }
        }

        private static float[] ExtractPoseFeatureInternal(Mat src)
        {
            using (var blob = CvDnn.BlobFromImage(src, 1.0 / 255.0,
                new OpenCvSharp.Size(368, 368), new Scalar(0, 0, 0), false, false))
            {
                _netPose.SetInput(blob);
                using (var output = _netPose.Forward())
                {
                    int h = output.Size(2);
                    int w = output.Size(3);

                    float[] keypoints = new float[18 * 3];
                    for (int k = 0; k < 18; k++)
                    {
                        float maxConf = 0;
                        int maxX = 0, maxY = 0;

                        for (int y = 0; y < h; y++)
                            for (int x = 0; x < w; x++)
                            {
                                float conf = output.At<float>(0, k, y, x);
                                if (conf > maxConf)
                                {
                                    maxConf = conf;
                                    maxX = x;
                                    maxY = y;
                                }
                            }

                        keypoints[k * 3 + 0] = (float)maxX / w;
                        keypoints[k * 3 + 1] = (float)maxY / h;
                        keypoints[k * 3 + 2] = maxConf;
                    }

                    return keypoints;
                }
            }
        }

        private static float[] ExtractSceneFeatureInternal(Mat src)
        {
            using (var blob = CvDnn.BlobFromImage(src, 1.0,
                new OpenCvSharp.Size(224, 224),
                new Scalar(104, 117, 123), false, false))
            {
                _netScene.SetInput(blob);
                using (var output = _netScene.Forward())
                {
                    int total = (int)output.Total();
                    float[] vec = new float[total];
                    for (int i = 0; i < total; i++)
                        vec[i] = output.At<float>(0, i);

                    float maxV = vec.Max();
                    double sum = 0;
                    for (int i = 0; i < vec.Length; i++)
                    {
                        vec[i] = (float)Math.Exp(vec[i] - maxV);
                        sum += vec[i];
                    }
                    if (sum > 0)
                        for (int i = 0; i < vec.Length; i++) vec[i] = (float)(vec[i] / sum);

                    return vec;
                }
            }
        }
    }
}