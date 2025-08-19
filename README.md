# BinarySpacePartitioning
![](GitHubAssets/Preview.gif)

An implementation of Binary Space Partitioning in Unity. 

[Reddit post](https://www.reddit.com/r/Unity2D/comments/1m9ntt1/made_a_dungeon_generator_using_binary_space/) where I explained how I did the corridor generation.

## Setup
1. Download and open the project in Unity.
2. If you don't see the sample scene, open it from "Assets/Scene/SampleScene".
3. Play with the map generation settings to try how the dungeon looks with different parameters.

For some parameters, you can try to hover over it to see what the parameter does.
Some are multiline so I just wrote them [in the comments](https://github.com/GnoxNahte/BinarySpacePartitioning/blob/03c28e952df81210a106b027cfec1469d0073372/Assets/Scripts/Environment/Rooms/MapGenerator.cs#L11-L65)

## Limitations
Some parameters generate dungeons with isolotated rooms and unconnected corridors. To prevent it:
- `Minimum Room Size >= Corridor Size + Corridor Padding` it'll be impossible to fit a corridor there.
- Recommened to have the minimum for `RoomSizeRatioRange` to be > 0.5. Currently, it doesn't have support for [slanted/right angled corridors](https://varav.in/archive/dungeon/#corridors) so if it's > 0.5, it'll guarantee that the rooms will intersect.
  - Might need to consider corridor size and corridor padding too. I just use a large value to be safe.
 
## Possible improvements
- Regeneration of parts of the dungeon that fails to generate proper corridors?
- Use a different corridor generation technique? I couldn't find a good algorithm for it at that time.

## Code Flow
TODO