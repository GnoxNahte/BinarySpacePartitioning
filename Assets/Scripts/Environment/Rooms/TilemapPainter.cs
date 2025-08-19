using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapPainter : MonoBehaviour
{
    [Flags]
    public enum TileType
    {
        Empty = 0,

        Room     = 1 << 0,
        Corridor = 1 << 1,
        Door     = 1 << 2,
        
        Solid = Room | Corridor | Door,
    }
    
    [System.Serializable]
    public class TilePair
    {
        public TileType Type;
        public TileBase Tile;
    }
    
    [SerializeField] Tilemap tilemap;
    [SerializeField] TilemapCollider2D tilemapCollider;
    
    [SerializeField] TilePair[] tilePairs;
    private Dictionary<TileType, TileBase> _tileDict;

    public TileType[,] Map { get; private set; }
    
    private MapGenerator.BSP_Config _config;

    public void SetupMap(MapGenerator.BSP_Config config)
    {
        _config = config;
        tilemap.ClearAllTiles();
        tilemap.size = new Vector3Int(_config.Size.x, _config.Size.y, 1);
        tilemap.origin = Vector3Int.zero;
        
        Map = new TileType[_config.Size.x, _config.Size.y];
        FillArea(TileType.Empty, Vector2Int.zero, _config.Size - Vector2Int.one);
    }
    
    public void FinishPainting()
    {
        if (!Application.isPlaying)
            Awake();
        
        TileBase[] tiles = new TileBase[_config.Size.x * _config.Size.y];
        for (int x = 0; x < _config.Size.x; x++)
        {
            for (int y = 0; y < _config.Size.y; y++)
            {
                tiles[x + y * _config.Size.x] = _tileDict[Map[x, y]];
            }
        }
        
        Vector3Int size = new Vector3Int(_config.Size.x, _config.Size.y, 1);
        tilemap.SetTilesBlock(new BoundsInt(Vector3Int.zero, size), tiles);
        tilemap.ResizeBounds();
        tilemapCollider.ProcessTilemapChanges();
    }
    
    public void PaintRoom(BSP_Tree tree)
    {
        foreach (TreeNode leaf in tree.Leafs)
            FillArea(TileType.Room, leaf.Room.Start, leaf.Room.End);
    }

    public void Clear() => tilemap.ClearAllTiles();

    // Fill using pos and size. 
    public void FillArea(TileType type, Vector2 pos, Vector2Int size)
    {
        Vector2Int start = new Vector2Int((int)(pos.x - size.x * 0.5f), (int)(pos.y - size.y * 0.5f));
        Vector2Int end = start + size;
        FillArea(type, start, end);
    }
    
    // Fill using start and end
    public void FillArea(TileType type, Vector2Int start, Vector2Int end)
    {
        for (int x = start.x; x <= end.x; x++)
        {
            for (int y = start.y; y <= end.y; y++)
            {
// #if UNITY_EDITOR
//                 Debug.Assert(Map[x, y] == TileType.Empty, "Tile is not empty. Tile: " + Map[x, y]);
// #endif

                // if (x >= _config.Size.x || y >= _config.Size.y || x < 0 || y < 0)
                // {
                //     print("OUT OF BOUNDS: "  + x + " - " + y);
                //     continue;
                // }

                Map[x, y] = type;
            }
        }
    }

    private void Awake()
    {
        _tileDict = new Dictionary<TileType, TileBase>();
        foreach (var pair in tilePairs)
            _tileDict.Add(pair.Type, pair.Tile);
    }

    // private void OnValidate()
    // {
    //     if (_tree != null && _config != null)
    //         Paint(_tree, _config);
    // }
}
