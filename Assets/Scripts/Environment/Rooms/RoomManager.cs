using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomManager
{
    [field: NonSerialized] public Room[] Rooms { get; private set; }
    
    private BSP_Tree _tree;
    private TilemapPainter _painter;
    private MapGenerator.BSP_Config _config;

    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public void GenerateRooms(BSP_Tree tree, TilemapPainter painter, MapGenerator.BSP_Config config)
    {
        _tree = tree;
        _painter = painter;
        _config = config;
        
        Rooms = new Room[tree.Leafs.Count];
        for (var i = 0; i < tree.Leafs.Count; i++)
        {
            var leaf = tree.Leafs[i];
            Vector2Int maxSize = Vector2Int.Max(leaf.Size - _config.RoomPadding * Vector2Int.one * 2, Vector2Int.one);
            Vector2Int size = Vector2Int.FloorToInt((Vector2)maxSize * Random.Range(config.RoomSizeRatioRange.x, config.RoomSizeRatioRange.y));
            size = Vector2Int.Max(size, Vector2Int.one);
            Vector2Int moveableSpace = (leaf.Size - size - _config.RoomPadding * Vector2Int.one * 2) / 2;
            Vector2Int randOffset = new Vector2Int(Random.Range(-moveableSpace.x, moveableSpace.x), Random.Range(-moveableSpace.y, moveableSpace.y));
            Vector2 pos = leaf.Position + randOffset;
            // If the pos and size don't match:
            // Size is odd AND pos has 0.5f, Add size to match pos
            if (size.x % 2 == 0 && (int)pos.x != pos.x)
                ++size.x;
            // Size is even AND pos doesn't have 0.5f, Add 0.5f to pos
            // Note: Can add/subtract as it's in the middle. To be truly random, need to use Random again but just add 0.5f as its easier and slightly faster
            else if (size.x % 2 == 1 && (int)pos.x == pos.x) 
                pos.x += 0.5f;
            
            // Same for y coords
            if ((int)pos.y != pos.y && size.y % 2 == 0)
                ++size.y;
            else if (size.y % 2 == 1 && (int)pos.y == pos.y) 
                pos.y += 0.5f;
            Rooms[i] = new Room(leaf, pos, size);
        }
    }

    public void Clear()
    {
        Rooms = null;
    }

    public void ConnectRooms(TreeNode node, TilemapPainter.TileType[,] map)
    {
        if (node.Children == null || node.Children.Length == 0)
        {
            node.RoomRangeStart = node.Room.Start;
            node.RoomRangeEnd = node.Room.End;
            return;
        }

        foreach (TreeNode child in node.Children)
            ConnectRooms(child, map);
        
        TreeNode child0 = node.Children[0];
        TreeNode child1 = node.Children[1];

        node.RoomRangeStart = Vector2Int.Min(child0.RoomRangeStart, child1.RoomRangeStart);
        node.RoomRangeEnd = Vector2Int.Max(child0.RoomRangeEnd, child1.RoomRangeEnd);

        // If 
        int corridorPaddingLarge = Mathf.FloorToInt(_config.CorridorSize * 0.5f);
        int corridorPaddingSmall = _config.CorridorSize - corridorPaddingLarge - 1;

        // Split vertically, so horizontal corridor
        // X Pos - depend on size, where the walking hits
        // Y Pos - between 2 rooms
        // X Size - Between end of child 1 and start of child 0
        // Y Size - _config.CorridorSize
        if (node.IsSplitVertical)
        {
            // Find overlap for y
            int largestStart = Mathf.Max(child0.RoomRangeStart.y, child1.RoomRangeStart.y) + corridorPaddingSmall;
            int smallestEnd = Mathf.Min(child0.RoomRangeEnd.y, child1.RoomRangeEnd.y) - corridorPaddingLarge;
            
            // Debug.DrawLine(new Vector2(child0.RoomRangeEnd.x + 0.5f, largestStart + 0.5f), new Vector2(child1.RoomRangeStart.x + 0.5f, smallestEnd + 0.5f), Color.blue, 1f);

            if (largestStart > smallestEnd)
            {
                // print("Return Y: " + largestStart + " | " + smallestEnd);
                return;
            }
            
            int yPos = Random.Range(largestStart, smallestEnd);

            Vector2Int start = new (child0.RoomRangeEnd.x + 1, yPos - corridorPaddingSmall);
            Vector2Int end = new (child1.RoomRangeStart.x - 1, yPos + corridorPaddingLarge);

            // start = WalkUntilHit(map, start, Vector2Int.left);
            // end = WalkUntilHit(map, end, Vector2Int.right);
            (start, end) = TryFindCleanLine(map, start, end, true, new Vector2Int(smallestEnd - yPos, yPos - largestStart));
            
            _painter.FillArea(TilemapPainter.TileType.Corridor, start, end);
        }
        // Same thing as above, just flipped y and x. Not sure how to combine? Using different dimensions, making it difficult..
        else
        {
            // Find overlap for x
            int largestStart = Mathf.Max(child0.RoomRangeStart.x, child1.RoomRangeStart.x) + corridorPaddingSmall;
            int smallestEnd = Mathf.Min(child0.RoomRangeEnd.x, child1.RoomRangeEnd.x) - corridorPaddingLarge;

            // Debug.DrawLine(new Vector2(largestStart + 0.5f, child0.RoomRangeEnd.y + 0.5f), new Vector2(smallestEnd + 0.5f, child1.RoomRangeStart.y + 0.5f), Color.blue, 1f);

            if (largestStart > smallestEnd)
            {
                // print("Return X : " + largestStart + " | " + smallestEnd);
                return;
            }
            
            int xPos = Random.Range(largestStart, smallestEnd);

            Vector2Int start = new (xPos - corridorPaddingSmall, child0.RoomRangeEnd.y + 1);
            Vector2Int end = new (xPos + corridorPaddingLarge, child1.RoomRangeStart.y - 1);

            (start, end) = TryFindCleanLine(map, start, end, false, new Vector2Int(smallestEnd - xPos, xPos - largestStart));

            // start = WalkUntilHit(map, start, Vector2Int.down);
            // end = WalkUntilHit(map, end, Vector2Int.up);
        
            _painter.FillArea(TilemapPainter.TileType.Corridor, start, end);
        }
    }

    // "Walk" the whole row/column to find a "clean" line
    // For example (For a vertical cut - which means a horizontal corridor):
    // 1.  If CorridorSize == 1 && CorridorPadding == 0 just walk in the single direction and return that pos.
    // 2.  Walk from (top left and also bottom left) to left, to find if it hits a solid tile (room/corridor/door)
    // 3a. Get the hit position of both top left and bottom left.
    // 3b. If both hit a solid tile AND the x is the same, it means it found a clean spot where the corridor is aligned (e.g. One side isn't hanging off). Go to step 4.   
    // 3c. If 3a fail, move the whole corridor down and try again from step 2
    // 4.  Now, try top right and bottom right, but walk right. And repeat step 2 to 3b but for the right side
    // 5a. If both left and right find a clean corridor position, return that position
    // 5b. Else, try from 2 to 4 again but in step 3c, go up instead of down when it can't find.
    private (Vector2Int start, Vector2Int end) TryFindCleanLine(TilemapPainter.TileType[,] map, Vector2Int start, Vector2Int end, bool isVertical, Vector2Int range, TilemapPainter.TileType targetTile = TilemapPainter.TileType.Solid)
    {
        // Step 1: If CorridorSize == 1 && CorridorPadding == 0 just walk in the single direction and return that pos.
        if ((_config.CorridorSize == 1 && _config.CorridorPadding == 0))
        {
            if (isVertical)
            {
                return (WalkUntilHit(map, start, Vector2Int.left).pos,
                        WalkUntilHit(map, end, Vector2Int.right).pos);
            }
            else
            {
                return (WalkUntilHit(map, start, Vector2Int.down).pos,
                    WalkUntilHit(map, end, Vector2Int.up).pos);
            }
        }
        
        // Vertical and horizontal have the same logic but flipped
        if (isVertical)
        {
            start.y -= _config.CorridorPadding;
            end.y += _config.CorridorPadding;
        
            Vector2Int bottomLeft = start, topRight = end;
            Vector2Int topLeft = new Vector2Int(start.x, end.y);
            Vector2Int bottomRight = new Vector2Int(end.x, start.y);

            bool TryFindCorridor(int maxCount, int dir, out (Vector2Int, Vector2Int) r)
            {
                for (int i = 0; i < maxCount; i++)
                {
                    // 2.  Walk from (top left and also bottom left) to left, to find if it hits a solid tile (room/corridor/door)
                    var hitTopLeft = WalkUntilHit(map, topLeft, Vector2Int.left, targetTile);
                    var hitBottomLeft = WalkUntilHit(map, bottomLeft, Vector2Int.left, targetTile);

                    // 3a. Get the hit position of both top left and bottom left.
                    // 3b. If both hit a solid tile AND the x is the same, it means it found a clean spot where the corridor is aligned (e.g. One side isn't hanging off). Go to step 4.   
                    if (hitTopLeft.ifHit && hitBottomLeft.ifHit && hitTopLeft.pos.x == hitBottomLeft.pos.x)
                    {
                        // 4.  Now, try top right and bottom right, but walk right. And repeat step 2 to 3b but for the right side
                        var hitTopRight = WalkUntilHit(map, topRight, Vector2Int.right, targetTile);
                        var hitBottomRight = WalkUntilHit(map, bottomRight, Vector2Int.right, targetTile);

                        // 5a. If both left and right find a clean corridor position, return that position
                        if (hitTopRight.ifHit && hitBottomRight.ifHit && hitTopRight.pos.x == hitBottomRight.pos.x)
                        {
                            // Remove padding
                            hitBottomLeft.pos.y += _config.CorridorPadding;
                            hitTopRight.pos.y -= _config.CorridorPadding;
                            r = (hitBottomLeft.pos, hitTopRight.pos);
                            return true;
                        }
                    }

                    // 3c. If 3a fail, move the whole corridor down and try again from step 2
                    topLeft.y += dir;
                    topRight.y += dir;
                    bottomLeft.y += dir;
                    bottomRight.y += dir;
                }

                r = (start, end);
                return false;
            }

            if (TryFindCorridor(range.x, 1, out var result))
                return result;
            
            // Reverse the range since going to check the other side
            topLeft.y -= range.x;
            topRight.y -= range.x;
            bottomLeft.y -= range.x;
            bottomRight.y -= range.x;
            
            if (TryFindCorridor(range.y, -1, out result))
                return result;
        
            // If still can't find anything, just return start and end. 
            // Remove padding
            start.y += _config.CorridorPadding;
            end.y -= _config.CorridorPadding;
        }
        // Same process when it isn't vertical. But flip x and y
        else
        {
            start.x -= _config.CorridorPadding;
            end.x += _config.CorridorPadding;
        
            Vector2Int bottomLeft = start, topRight = end;
            Vector2Int topLeft = new Vector2Int(start.x, end.y);
            Vector2Int bottomRight = new Vector2Int(end.x, start.y);
            
            bool TryFindCorridor(int maxCount, int dir, out (Vector2Int, Vector2Int) r)
            {
                for (int i = 0; i < maxCount; i++)
                {
                    var hitBottomLeft = WalkUntilHit(map, bottomLeft, Vector2Int.down, targetTile);
                    var hitBottomRight = WalkUntilHit(map, bottomRight, Vector2Int.down, targetTile);

                    if (hitBottomLeft.ifHit && hitBottomRight.ifHit && hitBottomLeft.pos.y == hitBottomRight.pos.y)
                    {
                        var hitTopLeft = WalkUntilHit(map, topLeft, Vector2Int.up, targetTile);
                        var hitTopRight = WalkUntilHit(map, topRight, Vector2Int.up, targetTile);
                        if (hitTopLeft.ifHit && hitTopRight.ifHit && hitTopLeft.pos.y == hitTopRight.pos.y)
                        {
                            // Remove padding
                            hitBottomLeft.pos.x += _config.CorridorPadding;
                            hitTopRight.pos.x -= _config.CorridorPadding;
                            r = (hitBottomLeft.pos, hitTopRight.pos);
                            return true;
                        }
                    }

                    topLeft.x += dir;
                    topRight.x += dir;
                    bottomLeft.x += dir;
                    bottomRight.x += dir;
                }

                r = (start, end);
                return false;
            }

            if (TryFindCorridor(range.x, 1, out var result))
                return result;
            
            // Reverse the range since going to check the other side
            topLeft.x -= range.x;
            topRight.x -= range.x;
            bottomLeft.x -= range.x;
            bottomRight.x -= range.x;
            
            if (TryFindCorridor(range.y, -1, out result))
                return result;
            
            // Remove padding
            start.x += _config.CorridorPadding;
            end.x -= _config.CorridorPadding;
        }
        
        // Debug.LogError("Cannot find hit");
        return (start, end);
    }
    
    // Walk int the step direction and return the position before hitting the target tile
    private (bool ifHit, Vector2Int pos) WalkUntilHit(TilemapPainter.TileType[,] map, Vector2Int start, Vector2Int stepDirection, TilemapPainter.TileType targetTile = TilemapPainter.TileType.Solid)
    {
        // Vector2Int debugInitialStart = start;
        
        // List<TilemapPainter.TileType> debugTilesX = new List<TilemapPainter.TileType>(_config.Size.x);
        // List<TilemapPainter.TileType> debugTilesY = new List<TilemapPainter.TileType>(_config.Size.y);
        // for (int i = 0; i < _config.Size.x; i++)
        //     debugTilesY.Add(map[i, start.y]);
        // for (int i = 0; i < _config.Size.y; i++)
        //     debugTilesX.Add(map[start.x, i]);
        
        // For loop is just to prevent it from going forever. Shouldn't happen but just in case 
        for (int i = 0; i < _config.MaxAxisSize; i++)
        {
            start += stepDirection;
            if (start.x >= _config.Size.x || start.y >= _config.Size.y || start.x < 0 || start.y < 0)
            {
                // Debug.Log("FAIL");
                return (true, start - stepDirection);
            }

            // if (_config.DebugTestCounter++ < _config.DebugTestInt)
            // {
            //     // Vector2 rand = new(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));
            //     Vector2 rand = Vector2Int.zero;
            //     Debug.DrawLine(start + rand, start + Vector2.one + rand, _config.CorridorPadding == 1 ? Color.cyan : Color.green, 1f);
            //     Debug.DrawLine(start + Vector2.up + rand, start + Vector2.right + rand, _config.CorridorPadding == 1 ? Color.cyan : Color.green, 1f);
            // }
            // else
            // {
            //     print("A");
            //     return (false, debugInitialStart);
            // }
            TilemapPainter.TileType tile = map[start.x, start.y];
            if (targetTile.HasFlag(tile) && tile != TilemapPainter.TileType.Empty)
                return (true, start - stepDirection);
        }
        
        Debug.LogError("Cannot find hit");
        return (false, start);
    }
}
