//Core\Comparers.cs
using System.Collections;
using System.IO;

namespace PictureManager
{
    /// <summary>
    /// 按创建时间排序文件
    /// </summary>
    public class FileComparerByCreateTime : IComparer
    {
        int IComparer.Compare(object o1, object o2)
        {
            FileInfo fi1 = o1 as FileInfo;
            FileInfo fi2 = o2 as FileInfo;
            return fi1.CreationTime.CompareTo(fi2.CreationTime);
        }
    }

    /// <summary>
    /// 按大小排序文件
    /// </summary>
    public class FileComparerBySize : IComparer
    {
        int IComparer.Compare(object o1, object o2)
        {
            FileInfo fi1 = o1 as FileInfo;
            FileInfo fi2 = o2 as FileInfo;
            return fi1.Length.CompareTo(fi2.Length);
        }
    }

    /// <summary>
    /// 按创建时间排序目录
    /// </summary>
    public class DirectoryComparer : IComparer
    {
        int IComparer.Compare(object o1, object o2)
        {
            DirectoryInfo fi1 = o1 as DirectoryInfo;
            DirectoryInfo fi2 = o2 as DirectoryInfo;
            return fi1.CreationTime.CompareTo(fi2.CreationTime);
        }
    }
}