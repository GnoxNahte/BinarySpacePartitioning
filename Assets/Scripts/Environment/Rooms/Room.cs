using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Room
{
    [field: SerializeField] public TreeNode Node { get; private set; }
    [field: SerializeField] public Vector2Int Start { get; private set; }
    [field: SerializeField] public Vector2Int End { get; private set; }
    
    public Room(TreeNode node, Vector2 pos, Vector2Int size)
    {
        Node = node;
        
        Start = new Vector2Int((int)(pos.x - size.x * 0.5f), (int)(pos.y - size.y * 0.5f));
        End = Start + size - Vector2Int.one;
        
        Debug.Assert(Node.Room == null);
        Debug.Assert(Node.Children == null || Node.Children.Length == 0);
        Node.Room = this;
    }
}