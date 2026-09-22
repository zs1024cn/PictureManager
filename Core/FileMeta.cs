//Core\FileMeta.cs
namespace PictureManager
{
    /// <summary>
    /// 文件元数据（点赞、踩、视频进度、文本滚动）
    /// </summary>
    public class FileMeta
    {
        public int Heart { get; set; }
        public int Break { get; set; }
        public double? Progress { get; set; }   // 视频进度（秒）
        public double? TextScroll { get; set; } // 文本滚动比例 0-1
    }
}