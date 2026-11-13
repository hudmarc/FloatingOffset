<img width="3584" height="500" src="https://github.com/user-attachments/assets/5b87aa95-7771-4d15-b542-e2f2de8e8794" />

<img width="104" height="20" alt="build - passing" src="https://github.com/user-attachments/assets/1f0801be-dd19-4172-a41b-35589ee96643" />
<img width="96" height="20" alt="badge_shieldsio_linecoverage_yellow" src="https://github.com/user-attachments/assets/ba324f0a-8554-4a58-884a-8d983a2bd803" />




# Quickstart (FishNet)
- [Install FishNet](https://assetstore.unity.com/packages/tools/network/fishnet-networking-evolved-207815)
- Click "Add package from git URL..." in the Unity Package Manager (UPM) and paste in [https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git](https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git)
- Add a `FishNetOffsetManager`  (located at `Packages > Floating Offset for Unity > Runtime > Networking > FishNet > Management`) to the GameObject holding your FishNet `NetworkManager`. This will allow you to also set your configuration.
- Add an `OffsetView` to all your players and any GameObjects you spawn in with a `NetworkTransform` that need to move long distances (for example, AIs that can chase the player, or rigidbodies that can only exist in one scene at a time)
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

At the time of writing, this package is the only open source origin-shifting/world rebasing solution that supports *full server authority* in a multiplayer environment. Other solutions generally require client-side authority and physics (by storing offsets client-side), but this package uses a fast server-side neighborhood clustering algorithm to ensure all players that can interact exist in the same scene on the server. If you want to learn more, the main `Process` loop in `OffsetProcessor` contains the bulk of the implementation.

### Is this package fast enough for my game? I want to host my small friend group of 2000 people on one world on my server.

Assuming a 4ms frame budget and a midrange server (in other words, the same cost as the default Unity Physics loop) yes.

### Benchmarks:

> If all players are clustered in one spot (all players in the same scene, generally best case)
```
MultipleViewsSameClientStressTestCloseTogether (11.725s)
---
Stopped at 2000 players with simulated frametime 4ms.
Average: 1.94433333333333ms
Worst: 4.05ms @ 2000 players
Best: 0.116666666666667ms @ 60 players
```

> If players are all far from each other (1 scene/player, worst case)
```
MultipleViewsSameClientStressTestSpreadOut (8.159s)
---
Stopped at 1360 players with simulated frametime 4ms.
Average: 1.98676470588235ms
Worst: 4.08333333333333ms @ 1360 players
Best: 0.116666666666667ms @ 40 players
```

> Note: These benchmarks were using mock classes, not Unity libraries, so YMMV. If you manage to reach this many players on an actual Unity game with this package, please let me know!

`Tested on 6-Core Mobile Core i7 (I7-9750H) @ 4.5Ghz Turbo Boost`

## Multiplayer Setup
- Create an Offline Scene, this should have your FishNet `NetworkManager`, add the `FishNetOffsetManager` component and untick Unity Physics if you want to use TimeManager physics.
- Add an `OffsetView` to your player
- If you have static structures that can be duplicated, anchor them in real space using the `OffsetAnchor`
- If your game uses line renderers or particle effects and you want them to be offset correctly add the `OffsetEffect` to the root object with the emitter(s)/line renderers. It will automatically detect line renderers or particle effects in all child objects as well.
- To configure the Floating Offset backend, set your preferences on the OffsetManager.
- To teleport views to specific real positions, use `OffsetUtils.TeleportTo(OffsetTransform offsetTransform, Vector3d position)`
