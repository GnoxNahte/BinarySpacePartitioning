using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class BSP_Tree
{
    // [field: SerializeField] 
    public TreeNode Root { get; set; }
    // [field: SerializeField] 
    public List<TreeNode> Leafs { get; set; }

    private MapGenerator.BSP_Config _config;

    public void Generate(MapGenerator.BSP_Config config)
    {
        _config = config;

        TreeNode.DebugCount = 0;
        
        Root = new TreeNode(null, 
            (Vector2)config.Size * 0.5f, 
            config.Size, 
            config.Depth, 
            config);
        Root.Generate(Random.value > 0.5f);
        
        Leafs = GetNodes(true);
    }

    public void Clear()
    {
        Root.Clear();
        Leafs?.Clear();
        Leafs = null;
    }

    public List<TreeNode> GetNodes(bool onlyLeafs)
    {
        // Breath-first search
        List<TreeNode> nodes = new ();
        Queue<TreeNode> queue = new Queue<TreeNode>((int)Mathf.Pow(2, _config.Depth));
        queue.Enqueue(Root);
        
        while (queue.Count > 0)
        {
            TreeNode current = queue.Dequeue();
            if (current.Children == null || current.Children.Length == 0)
                nodes.Add(current);
            else
            {
                foreach (TreeNode child in current.Children)
                    queue.Enqueue(child);
                
                if (!onlyLeafs) 
                    nodes.Add(current);
            }
        }
        
        return nodes;
    }
    
    public void DebugDraw(int debugViewDepth)
    {
        Root.DebugDraw(debugViewDepth);
    }
}
