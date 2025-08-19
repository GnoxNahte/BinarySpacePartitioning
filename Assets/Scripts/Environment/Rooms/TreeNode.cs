using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class TreeNode
{
    [field: SerializeField] public TreeNode Parent { get; private set; }
    [field: SerializeField] public TreeNode[] Children { get; private set; }
    // Use Vector2 instead of Vector2Int for position because it might be in between 2 tiles
    [field: SerializeField] public Vector2 Position { get; private set; }
    [field: SerializeField] public Vector2Int Size { get; private set; }
    // leaf is 0, root will be config.Depth
    [field: SerializeField, ReadOnly] public int Depth { get; private set; }
    public Room Room; 
    public bool IsSplitVertical { get; private set; }
    public Vector2Int RoomRangeStart;
    public Vector2Int RoomRangeEnd;
    
    public int DebugId { get; private set; } = -1; // Just for to easily id rects in debug

    // Just for debug, to assign incrementing ids
    public static int DebugCount;
    
    private MapGenerator.BSP_Config _config;

    public TreeNode(TreeNode parent, Vector2 position, Vector2Int size, int depth, MapGenerator.BSP_Config config)
    {
        Parent = parent;
        Position = position;
        Size = size;
        Depth = depth;
        _config = config;

        Room = null;
        Children = null;
    }

    public void Generate(bool ifSplitVertical)
    {
        if (Depth == 0)
        {
            DebugId = DebugCount++;
            return;
        }
        
        // For more balanced tree
        if (_config.IsBalanced)
            ifSplitVertical = Size.x > Size.y;

        // TODO: Consider to make it randomly return to make some large rooms which span multiple nodes
        if ((ifSplitVertical && Size.x < _config.NodeMinSize.x * 2) || 
            (!ifSplitVertical && Size.y < _config.NodeMinSize.y * 2))
        {
            DebugId = DebugCount++;
            return;

            // TODO? Switch to this so balanced doesn't mean square
            // If cannot expand in this dimension, try the other dimension
            // ifSplitVertical = !ifSplitVertical;
            // if (_config.IsBalanced && 
            //     ((ifSplitVertical && Size.x < _config.NodeMinSize.x * 2) ||
            //     (!ifSplitVertical && Size.y < _config.NodeMinSize.y * 2)))
            // {
            //     DebugId = DebugCount++;
            //
            //     return;
            // }
        }
        
        IsSplitVertical = ifSplitVertical;
        
        // TODO: Balance with more balanced ratios (closer to 0.5f) for the first few splits
        //   If the difference in the first split is very big, it'll create large sections where its very large/small
        Children = new TreeNode[2];
        float ratio = Random.Range(_config.MinRatio, _config.MaxRatio);
        
        // small illustration 
        // This is when splitting vertically, flip x with y when split horizontally
        // y is the same for all of them so not including it.
        // ==========================================================
        // x - center of this node
        // x0 - center of child 0
        // x1 - center of child 1
        // ==========================================================
        // |                  |                                     |
        // |                  |                                     |
        // |        x0        |         x        x1                 | // y stays the same
        // |                  |                                     |
        // |                  |                                     |
        // ==========================================================
        // <-- child0Size --> < ------------- child1Size ---------->
        // ^ start of rect

        // The pos & size of the axis that will change
        float axisPos; 
        int axisSize;
        int minSize;
        if (ifSplitVertical)
        {
            axisPos = Position.x;
            axisSize = Size.x;
            minSize = _config.NodeMinSize.x;
        }
        else
        {
            axisPos = Position.y;
            axisSize = Size.y;
            minSize = _config.NodeMinSize.y;
        }

        int startOfRect = (int)(axisPos - axisSize * 0.5f);
        // (axisSize - minSize * 2) = Available "moveable" space that can be random. 
        int child0Size = (int)(ratio * (axisSize - minSize * 2)) + minSize;
        float child0Pos = startOfRect + child0Size * 0.5f;

        int child1Size = axisSize - child0Size;
        float child1Pos = startOfRect + child0Size + child1Size * 0.5f;

        int remainingDepth = Depth - 1;
        
        if (Depth == 0)
        {
            DebugId = DebugCount++;
            return;
        }
        
        if (ifSplitVertical)
        {
            Children[0] = new TreeNode(this,
                new Vector2(child0Pos, Position.y),
                new Vector2Int(child0Size, Size.y),
                remainingDepth,
                _config);
            Children[1] = new TreeNode(this,
                new Vector2(child1Pos, Position.y),
                new Vector2Int(child1Size, Size.y),
                remainingDepth,
                _config);
        }
        else
        {
            Children[0] = new TreeNode(this,
                new Vector2(Position.x, child0Pos),
                new Vector2Int(Size.x, child0Size),
                remainingDepth,
                _config);
            Children[1] = new TreeNode(this,
                new Vector2(Position.x, child1Pos),
                new Vector2Int(Size.x, child1Size),
                remainingDepth,
                _config);
        }
        
        Children[0].Generate(!ifSplitVertical);
        Children[1].Generate(!ifSplitVertical);
    }

    public void Clear()
    {
        if (Children == null) 
            return;
        
        foreach (TreeNode child in Children)
            child.Clear();
        
        Children = null;
    }

    public void DebugDraw(int debugViewDepth)
    {
        if (debugViewDepth == -2)
            return;

        bool isLeaf = Children == null || Children.Length <= 0;

        if (debugViewDepth == Depth || (debugViewDepth == -1 && isLeaf))
        {
            Color color = Depth switch
            {
                0 => Color.red,
                1 => Color.orange,
                2 => Color.yellow,
                3 => Color.green,
                4 => Color.mediumSlateBlue,
                5 => Color.indianRed,
                6 => Color.khaki,
                7 => Color.lightBlue,
                8 => Color.lightGreen,
                _ => Color.white
            };
            color.a = _config.DebugAlpha;
            Gizmos.color = color;
            Gizmos.DrawCube(Position, Size - _config.DebugSpacing * Vector2.one);
#if UNITY_EDITOR
            if (_config.DebugShowId)
            {
                Color originalColor = GUI.color;
                GUI.color = Color.gray;
                Handles.Label(Position, DebugId.ToString());
                GUI.color = originalColor;
            }
#endif
        }
        
        if (!isLeaf)
        {
            foreach (TreeNode child in Children)
                child.DebugDraw(debugViewDepth);
        }
    }
}
