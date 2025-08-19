using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

// Using the Binary space partitioning algorithm
// Reference: https://www.roguebasin.com/index.php?title=Basic_BSP_Dungeon_generation
public class MapGenerator : MonoBehaviour
{
    [System.Serializable]
    public class BSP_Config
    {
        [Header("Tree Generation")]
        public Vector2Int Size;
        [HideInInspector]
        public int MaxAxisSize;
        [Range(0, 25)]
        public int Depth;
        [Tooltip("Size of Min and Max Ratio")]
        [Range(0, 1f)]
        public float RatioDiffSize;
        [HideInInspector]
        public float MinRatio;
        [HideInInspector]
        public float MaxRatio;
        public Vector2Int NodeMinSize;

        [Tooltip("If true, will try to balance between vertical and horizontal slicing based on the size")]
        public bool IsBalanced;

        [Header("Room Settings")] 
        public Vector2 RoomSizeRatioRange;
        [Range(0, 3)]
        public int RoomPadding; // For corridors
        [Range(1, 5)]
        public int CorridorSize; // Size of corridor on cross axis
        [Range(0, 3)]
        public int CorridorPadding; // Not enforced, prefer to have padding. but sometimes might not be able to have
        
        [Header("Debug")]
        [Range(0, 1f)]
        public float DebugGizmoAlpha;
        [Range(0, 2f)]
        public float DebugSpacing;
        public bool DebugShowId;
        public bool DebugTestBool;
        public int DebugTestInt;
        public int DebugTestCounter;
        public float DebugTestFloat;
    }
    
    [SerializeField] private TilemapPainter tilemapPainter;
    [SerializeField] private BSP_Config config;
    [SerializeField] private bool generateOnValidate;
    [SerializeField] private bool generateTilemap;
    [SerializeField] private bool generateRooms;
    [SerializeField] private bool generateCorridors;
    [SerializeField] private bool paintToTilemap;
    // -2   = Don't draw gizmos
    // -1   = Draw all leafs
    // >= 0 = Draw node when debugViewDepth == Depth
    [Range(-2, 10)] 
    [SerializeField] private int debugViewDepth;
    [SerializeField] private bool debugPrintTimes;
    [SerializeField] private int editorSeed;
    
    private RoomManager _roomManager;
    public BSP_Tree Tree {get; private set;}

    public void Init()
    {
        Tree = new BSP_Tree();
        _roomManager = new RoomManager();
    }

    [ContextMenu("Generate")]
    public void EditorGenerate()
    {
        if (Tree == null || _roomManager == null)
            Init();
        
        Generate(editorSeed);
    }

    public void Generate(int seed)
    {
        config.DebugTestCounter = 0;
        Stopwatch stopwatch = Stopwatch.StartNew();
        Random.InitState(seed);
        Tree.Generate(config);
        
        LogTiming(stopwatch, "Generate tree");
        if (generateRooms)
        {
            _roomManager.GenerateRooms(Tree, tilemapPainter, config);
            LogTiming(stopwatch, "Generate rooms");
        }
        else
            _roomManager.Clear();

        if (generateTilemap && generateRooms)
        {
            tilemapPainter.SetupMap(config);
            tilemapPainter.PaintRoom(Tree);
            LogTiming(stopwatch, "Paint room");
            if (generateCorridors)
                _roomManager.ConnectRooms(Tree.Root, tilemapPainter.Map);
            
            LogTiming(stopwatch, "Connect rooms");
            if (paintToTilemap)
                tilemapPainter.FinishPainting();
            LogTiming(stopwatch, "Finished painting");
        }
        else
            tilemapPainter.Clear();
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        Tree?.Clear();
        Tree = null;
        _roomManager?.Clear();
        _roomManager = null;
        tilemapPainter.Clear();
    }

    public void SetSeed(float s) => this.editorSeed = (int)s;

    private void OnValidate()
    {
        // Limit values
        config.Size.x = Mathf.Max(1, config.Size.x);
        config.Size.y = Mathf.Max(1, config.Size.y);
        
        config.NodeMinSize.x = Mathf.Max(1, config.NodeMinSize.x);
        config.NodeMinSize.y = Mathf.Max(1, config.NodeMinSize.y);
        
        config.RoomSizeRatioRange.x = Mathf.Clamp(config.RoomSizeRatioRange.x, 0.2f, 1f);
        config.RoomSizeRatioRange.y = Mathf.Clamp(config.RoomSizeRatioRange.y, 0.2f, 1f);
        
        // Pre-calculate values
        config.MinRatio = 0.5f - config.RatioDiffSize * 0.5f;
        config.MaxRatio = 0.5f + config.RatioDiffSize * 0.5f;
        config.MaxAxisSize = Mathf.Max(config.Size.x, config.Size.y);
        
        // Warn
        if (config.NodeMinSize.x < config.RoomPadding * 2 + config.CorridorSize + config.CorridorPadding ||
            config.NodeMinSize.y < config.RoomPadding * 2 + config.CorridorSize + config.CorridorPadding)
            Debug.LogError("NodeMinSize is too small fit RoomPadding, CorridorSize and CorridorPadding");
        
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif
        if (generateOnValidate)
            EditorGenerate();
    }

    private void LogTiming(Stopwatch stopwatch, string msg)
    {
        if (debugPrintTimes)
            print($"TIME - {msg}: {stopwatch.Elapsed}");
    }

    private void OnDrawGizmos() => Tree?.DebugDraw(debugViewDepth);
}
