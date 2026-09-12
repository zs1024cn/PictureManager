# PictureManager

一个功能强大的 Windows 图片管理器，支持 7 种查重算法、缩略图浏览、AI 增强、人脸检测等功能。

## ✨ 功能特性

### 图片查重（7 种算法）

| 算法 | 判定依据 | 适用场景 | 提取方式 |
|------|---------|---------|---------|
| **Hash** | 感知哈希（纹理/亮度分布） | 几乎相同的图 | 一次提 4 种基础特征 |
| **DCT** | RGB 直方图分布 | 色域分布相似 | 单独提取 |
| **SIFT** | 特征点几何关系 | 旋转 / 缩放 / 裁剪 | 单独提取 |
| **HSV** | HSV 网格色调分布 | 整体色调相似 | 单独提取 |
| **Face** | OpenFace 128 维人脸特征 | 同一个人 | 单独提取（慢） |
| **Pose** | OpenPose 18 关键点 | 相似姿势 | 单独提取（很慢） |
| **Scene** | Places365 场景分类向量 | 相似场景 | 单独提取 |

**分桶策略**：分桶方式和比较算法独立，可选 Hash / DCT / SIFT 三种分桶。

- 先提取 Hash → 分桶生效
- 只提单特征（不先提 Hash）→ 所有图同桶，全库比较

### 图片处理
- 人脸马赛克（DNN 检测 + 椭圆马赛克 + 高斯模糊 + 褪色）
- Gamma 通透增强
- 超分辨率（Upscayl）
- 暖色 / 冷色 / 粉嫩 / 磨皮 / 锐化

### 文件管理
- 缩略图相册（分页、多选、LRU 缓存）
- 视频播放（LibVLC）
- 文本阅读（CefSharp）
- 论坛图片下载器
- SQLite 文件指纹数据库

## 🖥️ 环境要求

- Windows 10/11
- .NET Framework 4.7.2
- Visual Studio 2019/2022

## 📦 依赖

- [OpenCvSharp4](https://github.com/shimat/opencvsharp)
- [CefSharp.WinForms](https://github.com/cefsharp/CefSharp)
- [LibVLCSharp.WinForms](https://github.com/videolan/libvlcsharp)
- [System.Data.SQLite](https://system.data.sqlite.org/)
- [Microsoft.Web.WebView2](https://developer.microsoft.com/microsoft-edge/webview2/)

## 🧠 模型文件

需要下载以下模型文件，放到 `bin/Debug/` 或 `bin/Release/` 目录：

| 文件 | 用途 |
|------|------|
| `deploy.prototxt` | 人脸检测结构 |
| `res10_300x300_ssd_iter_140000.caffemodel` | 人脸检测权重 |
| `nn4.small2.v1.t7` | OpenFace 人脸特征 |
| `pose_deploy_linevec.prototxt` | OpenPose 结构 |
| `pose_iter_440000.caffemodel` | OpenPose 权重 |
| `deploy_googlenet_places365.prototxt` | Places365 结构 |
| `googlenet_places365.caffemodel` | Places365 权重 |

下载地址见 [CHANGELOG.md](CHANGELOG.md) 或提交 Issue 询问。

## 🔨 构建

```bash
git clone https://github.com/yourname/PictureManager.git
cd PictureManager
# 下载模型文件到 bin/Debug/
# 用 VS 打开 PictureManager.sln
# F5 运行
⌨️ 快捷键
键	功能
W / S / ↑ / ↓	上下移动
A / D / ← / →	翻页 / 快进
Space	暂停 / 播放 / DCT 直方图
V	查找相似图（Hash + DCT + SIFT）
F	查找 HSV 相似图
T	AI 增强
Z	点赞
X	踩
D / Delete	删除
E	画图打开
Ctrl+E	人脸马赛克
Ctrl+S	重建图片
F1	帮助
F4	当前目录缩略图
Ctrl+F4	全部缩略图
F9	切换显示模式
📁 项目结构
text
PictureManager/
├── Compare/            查重算法（策略模式）
│   ├── PictureCompare.cs
│   ├── PictureCompareManager.cs
│   ├── FeatureCacheManager.cs
│   ├── ImageFeature.cs
│   ├── ImageFeatureExtractor.cs
│   └── [7 种 Compare 实现]
├── Core/               核心业务
│   ├── Duplicate.cs
│   ├── DuplicateSettings.cs
│   ├── FileMeta.cs
│   ├── Comparers.cs
│   ├── MyDb.cs
│   └── ForumDownloader.cs
├── Controls/           自定义控件
├── Forms/              窗体
├── Models/             模型文件（不提交）
└── Properties/         程序集信息
📜 许可证
MIT License

text

**反馈**：`完成` 或 `报错 [信息]`

---

### [M025] 更新 `CHANGELOG.md`

**路径**：`PictureManager/CHANGELOG.md`
**操作**：整体替换

```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-09

### Added
- **统一 Compare 架构**：`PictureCompare` 抽象基类 + 7 个实现
  - Hash / DCT / SIFT / HSV / Face / Pose / Scene
- **`ImageFeature`**：一次读图提取所有特征，节省磁盘 IO
- **`FeatureCacheManager`**：统一缓存管理，支持回收站
- **`DuplicateSettings`**：参数集中管理
- **分桶与算法解耦**：分桶方式（Hash / DCT / SIFT）和比较算法独立
- **按模式提取**：
  - Hash 模式：一次提 4 种基础特征（Hash + DCT + SIFT + HSV）
  - 其他模式：单独提取
  - DNN 特征用 `float[0]` 表示"计算过但无结果"
- **FaceCompare**：OpenFace 128 维人脸特征
- **PoseCompare**：OpenPose 18 关键点姿态特征
- **SceneCompare**：Places365 场景分类向量
- **MaskFace**：DNN 人脸检测 + 椭圆马赛克 + 高斯模糊 + 褪色

### Changed
- 重构 `Form1` 的查重工作流，从 `WorkflowMode` 枚举改为字符串模式
- 工作流菜单动态生成
- 查重参数从 UI 同步到 `DuplicateSettings`
- DNN 提取改为串行（`Net` 对象非线程安全）

### Removed
- 旧的 `CompareHash` / `CompareDct` / `CompareSift` 方法
- `WorkflowMode` 枚举
- `GetValidFiles`（被 `GetValidImageFiles` 替代）

### Fixed
- DNN 并发导致的 `AccessViolationException`
- `float[0]` 特征被误判为有效的问题

## [1.0.0] - 2026-08

### Added
- Initial release
- Hash / DCT / SIFT / HSV 四种查重
- 缩略图相册
- 视频播放
- 人脸马赛克
- 论坛下载器
- SQLite 指纹数据库