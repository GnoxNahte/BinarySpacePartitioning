using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/Neighbour Aware Rule Tile")]
public class NeighbourAwareTile : RuleTile<NeighbourAwareTile.Neighbor> {
    public TileBase TileToCheck;
    
    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int Any = 3;
        public const int Specified = 4;
        public const int NotSpecified = 5;
        public const int Null = 6;
    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        // From base class, not using override tile now though, so skip
        // if (tile is RuleOverrideTile t)
        //     tile = t.m_InstanceTile;

        return neighbor switch
        {
            Neighbor.This => tile == this,
            Neighbor.NotThis => tile != this,
            Neighbor.Any => tile != null,
            Neighbor.Specified => tile == TileToCheck,
            Neighbor.NotSpecified => tile != TileToCheck,
            Neighbor.Null => tile == null,
            _ => base.RuleMatch(neighbor, tile)
        };
    }
} 