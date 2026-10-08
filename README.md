<img width="104" height="20" alt="build - passing" src="https://github.com/user-attachments/assets/1f0801be-dd19-4172-a41b-35589ee96643" />
<img width="96" height="20" alt="badge_shieldsio_linecoverage_yellow" src="https://github.com/user-attachments/assets/ba324f0a-8554-4a58-884a-8d983a2bd803" />


# Quickstart (FishNet)
- [Install FishNet](https://assetstore.unity.com/packages/tools/network/fishnet-networking-evolved-207815)
- Click "Add package from git URL..." in the Unity Package Manager (UPM) and paste in [https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git](https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git)
- Add a `FishNetOffsetManager`  (located at `Packages > Floating Offset for Unity > Runtime > Networking > FishNet > Management`) to the GameObject holding your FishNet `NetworkManager`. This will allow you to also set your configuration.
- Add an `OffsetView` to all your players and any GameObjects you spawn in with a `NetworkTransform` that need to move long distances (for example, AIs that can chase the player)
- Utility functions live on `OffsetUtils`, if you want to teleport the player you also call that through the `OffsetUtils`. (note that Teleport in particular is only callable on the network server!)
<img width="451" alt="image" src="https://user-images.githubusercontent.com/44267994/228247674-b075e104-a93a-4a9f-bdbe-5d0b2c8a49ba.png">

Setup tutorial video coming soon.

### Want to see this package in action?

#### Check out the [Server Authoritative Client Side Prediction Demo Here](https://github.com/hudmarc/FishNet-FloatingOffset---Car-Controller-Prediction-Test/tree/master)

## What is this?
By default, Unity can handle ~20km by 20km game worlds without running into floating point precision limitations.

This package extends the possible world size to ~`2.114e+35` light years. The known universe is only `4.651e+10` light years (as of writing this README)

This is currently the only open-source Unity package that can do this while maintaining full server authority. It is also compatible with client-side prediction, but you may need to adjust your CSP scripts.

## What's different about this package?

At the time of writing, this package is the only open source origin-shifting/world rebasing solution that supports *full server authority* in a multiplayer environment. Other solutions generally require client-side authority and physics (by storing offsets client-side), but this package uses a fast server-side neighborhood clustering algorithm to ensure all players that can interact exist in the same scene on the server. If you want to learn more, the main `Process` loop in `OffsetServer` contains the bulk of the implementation.

### Is this package fast enough for my game? I want to host my small friend group of 2500 people on one world on my server.

Assuming a 4ms frame budget and a midrange server (in other words, the same cost as the default Unity Physics loop) yes.

### Benchmarks:

> If all players are clustered in one spot (all players in the same scene, generally best case)
```
MultipleViewsSameClientStressTestCloseTogether (2.578s)
---
Stopped at 3820 players with simulated frametime 5ms.
Average: 1.57897033158813ms
Worst: 5.25ms @ 3820 players
Best: 0.0333333333333333ms @ 40 players
```

> If players are all far from each other (1 scene/player, worst case)
```
MultipleViewsSameClientStressTestSpreadOut (4.803s)
---
Stopped at 2640 players with simulated frametime 4ms.
Average: 1.67272727272727ms
Worst: 4.23333333333333ms @ 2640 players
Best: 0.0666666666666667ms @ 40 players
```

> Note: These benchmarks were using mock classes, not Unity libraries, so YMMV. If you manage to reach this many players on an actual Unity game with this package, please let me know!

`Tested on 6-Core Mobile Core i7 (I7-9750H) @ 4.5Ghz Turbo Boost`

## Multiplayer Setup
- Create an Offline Scene, this should have your FishNet `NetworkManager`, add the `OffsetManagerNetworking` component and untick Unity Physics if you want to use TimeManager physics.
- Add an OffsetTransform to your player, and tick 'isView'
- If you have static structures that can be duplicated, anchor them in real space using the `OffsetAnchor`
- If your game uses line renderers or particle effects and you want them to be offset correctly add the `EffectOffsetter` to your manager object and assign it to the `OffsetManager`
- To configure the Floating Offset backend, set your preferences to the `OffsetUniverse`. It should be automatically created at the root of your project. If not, you can create your own under  `Assets/Create/Floating Offset/OffsetUniverse`
- To teleport views to specific real positions, use `universe.TeleportTo(OffsetTransform offsetTransform, Vector3d position)`

---

## Glossary

### OffsetScene

You can think of an offset scene as a normal Unity scene with a particular offset from 0,0,0 represented in 64-bit doubles. The point of this package is to keep all players as close to the centers of their scenes as possible, and it does this using a variety of algorithms and datastructures.

These are implemented in the OffsetServer as collections, you can view the offset of a given scene in the Editor when you have an OffsetTransform or OffsetAnchor selected. If you need to see the offset of a scene in code you can do this with `Vector3d GetSceneOffset(Scene scene)` (currently this is located on OffsetServer but will be moved, update to follow soon)

### OffsetManager

Manages the offset server and configuration. See `FishNetOffsetManager` for the core multiplayer version of this class.

### OffsetServer (C# class)

Implements the low level neighborhood clustering of this package. The core logic of this package could in the future be ported to Godot or another C# engine, and in the meantime this class is very testable.

The Offset Server manages the pooling of Offset Scenes and the transfer of Offset Transforms between all active Offset Scenes as well as keeping the scenes properly rebased. The Offset Server is not actually a Monobehaviour so it is instantiated by the OffsetManager as a plain C# object and its instance lives on the OffsetUniverse.

### OffsetView

Add an `OffsetView` to all your players and any GameObjects you spawn in with a `NetworkTransform` that need to move long distances (for example, AIs that can chase the player)

### OffsetAnchor

Offset Anchors ensure that the object they are attached to are always at the exact position specified in the OffsetAnchor's target position. Great for things like cities or other POI's that need to exist at specific points in space. If you didn't use this you would notice that cities that exist very very far from the origin are not where you put them (for example, clipping into the terrain) because their native Unity position (which is a Vector3) is not precise enough to store their exact location.

### IgnoreOffset

Marks an object as ignored by the Offset system, when a scene is rebased this object will not be moved. Great for terrains that need to stay near the origin and use some custom system to render themselves. (for exmaple, offsetting the terrain by the scene offset, I'll port an example as soon as I can)
