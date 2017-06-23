
namespace Alachisoft.NCache.Common.DataStructures
{
    public interface ITreeNode
    {
        ITreeNode Parent { get; }
        ITreeNode Left { get; }
        ITreeNode Right { get; }
        object Key { get; set; }
        object Value { get; set; }
    }
}
