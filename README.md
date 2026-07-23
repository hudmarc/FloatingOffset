# Quickstart
- [Install FishNet](https://assetstore.unity.com/packages/tools/network/fishnet-networking-evolved-207815)
- Click "Add package from git URL..." in the Unity Package Manager (UPM) and paste in [https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git](https://github.com/hudmarc/FFO-FishNet-Floating-Origin.git)
<img width="451" alt="image" src="https://user-images.githubusercontent.com/44267994/228247674-b075e104-a93a-4a9f-bdbe-5d0b2c8a49ba.png">

### Want to see this package in action?

#### Check out the [Techdemo Here](https://github.com/hudmarc/FishNet-FloatingOffset---Car-Controller-Prediction-Test/tree/master)

## What is this?
By default, Unity can handle ~20km by 20km game worlds without running into floating point precision limitations.

This package extends the possible world size to ~`2.114e+35` light years. The known universe is only `4.651e+10` light years (as of writing this README)

It does this using scene stacking (to support multiplayer games) and floating origin (i.e. the world moves around the players, not the other way around)

That's pretty much it.

### Is this package fast enough for my game? I want to host around 400 players on one world on my server.

Assuming a 4ms frame budget and a midrange server (in other words, the same cost as the default Unity Physics loop) yes.

### Benchmarks:

> If players are in one spot (clustered, worst case)
```
MultipleViewsSameClientStressTestWorstCase (2.279s)
---
Stopped at 380 players with simulated frametime 4ms.
Average: 1.99035087719298ms
Worst: 4.2ms @ 380 players
Best: 0.45ms @ 60 players
```

> If players are spread out evenly (not clustered, average case)
```
MultipleViewsSameClientStressTestSpreadOut (2.424s)
---
Stopped at 500 players with simulated frametime 4ms.
Average: 1.598ms
Worst: 4.45ms @ 500 players
Best: 0.1ms @ 60 players
```

> Note: These benchmarks were using mock classes, not Unity libraries, so YMMV. If you manage to reach 400 players on an actual Unity game with this package, please let me know!

`Tested on 6-Core Mobile Core i7 (I7-9750H) @ 4.5Ghz Turbo Boost`

## Multiplayer Setup
- Create an Offline Scene, this should have your FishNet `NetworkManager`, add the `OffsetManagerNetworking` component and untick Unity Physics if you want to use TimeManager physics.
- Add an OffsetTransform to your player, and tick 'isView'
- If you have static structures that can be duplicated, anchor them in real space using the `OffsetAnchor`
- If your game uses line renderers or particle effects and you want them to be offset correctly add the `ffectOffsetter` to your manager object and assign it to the `OffsetManager`
- To configure the Floating Offset backend, set your preferences to the `OffsetUniverse`. It should be automatically created at the root of your project. If not, you can create your own under  `Assets/Create/Floating Offset/OffsetUniverse`
- To teleport views to specific real positions, use `universe.TeleportTo(OffsetTransform offsetTransform, Vector3d position)`

<img width="747" height="771" alt="image" src="https://github.com/user-attachments/assets/e1c23c88-0a73-4a33-9ce6-fd838c6d8fc3" />

<img width="309" height="223" alt="Screenshot 2026-05-10 at 21 00 31" src="https://github.com/user-attachments/assets/00ba01c7-183e-4c16-b5af-229625b91048" />

## Singleplayer Setup

Same as above, but instead of adding an `OffsetManagerNetworking` to the NetworkManager object you just need to set up an empty GameObject marked Do Not Destroy on Load and add the plain `OffsetManager` to it. In my opinion using this package for singleplayer is a bit overkill but it does work just fine! Maybe if you have a lot of AI's in you world that need to be constantly rendered even if they are far away from the player? Either way it works well as a plain floating origin package also.

## Performance considerations

- This package scales generally ~~linearly~~ (currently polynomial worst case, unsure if this is avoidable because we unfortunately need to run nearest neighbor search) with evenly distributed players but if all your players cluster in one place performance can dip. See the benchmarks for more details.
- The OffsetManager class is still being optimized. If you can reduce the number of root GameObjects in your scenes that should improve peformance.

---

## OffsetScene

You can think of an offset scene as a normal Unity scene with a particular offset from 0,0,0 represented in 64-bit doubles. The point of this package is to keep all players as close to the centers of their scenes as possible, and it does this using a variety of algorithms and datastructures.

These are implemented in the OffsetServer as collections, you can view the offset of a given scene in the Editor when you have an OffsetTransform or OffsetAnchor selected. If you need to see the offset of a scene in code you can do this with `Vector3d GetSceneOffset(Scene scene)` (currently this is located on OffsetServer but will be moved, update to follow soon)

## OffsetManager

Implements Unity functions on behalf of the OffsetServer.

Bootstraps the OffsetServer.

Default update mode is Unity, can also be Custom. There is also an OffsetManagerFishNet that overrides the setup and updates subscribed to the FishNet OnTick and also handles network synchronisation for clients (default is first View spawned by the client is considered the local player, but methods are provided to change the View registered for any given player)

See also `OffsetManagerNetworking`

## OffsetManagerNetworking

Same as the OffsetManager but it uses the update loop from FishNet instead of the internal Unity update loops.

## OffsetServer (C# class)

Implements the low level logic of this package. The core logic of this package could in the future be ported to Godot or another C# engine, and in the meantime this class is very testable.

The Offset Server essentially manages the pooling of Offset Scenes and the transfer of Offset Transforms between all active Offset Scenes as well as keeping the scenes properly rebased. The Offset Server is not actually a Monobehaviour so it is instantiated by the OffsetManager as a plain C# object and its instance lives on the OffsetUniverse.

## OffsetTransform

Will exist in the `OffsetScene` nearest its origin. If it is not near any OffsetScenes it will be destroyed. If you do not want an OffsetTransform to be destroyed (for example, a player or a super-important boss character in your game) you should mark `isView = true`. More details below.

### OffsetTransform.isView = true

Attach an `OffsetTransform` with `isView = true` to your player. The first player spawned on the server will be considered the local player. The first player spawned on clients that is owned by that client is considered the local player on clients and is used for determining when to send rebase commands to clients from the server.

The `OffsetScene` will be rebased to the centroid of all game objects with `OffsetTransform` and `isView = true` in the scene.

Tracks the real position (relative to real zero) and the real velocity (in absolute space, relative to real zero velocity) of itself.

### OffsetAnchor

Offset Anchors ensure that the object they are attached to are always at the exact position specified in the OffsetAnchor's target position. Great for things like cities or other POI's that need to exist at specific points in space. If you didn't use this you would notice that cities that exist very very far from the origin are not where you put them (for example, clipping into the terrain) because their native Unity position (which is a Vector3) is not precise enough to store their exact location.

## IgnoreOffset

Marks an object as ignored by the Offset system, when a scene is rebased this object will not be moved. Great for terrains that need to stay near the origin and use some custom system to render themselves. (for exmaple, offsetting the terrain by the scene offset, I'll port an example as soon as I can)

---

### 0.2.0 TODO

### Networking

✅ Test offset syncing to clients

✅ Implement and test OffsetCondition for hiding objects not in your offset scene.

### Preferences

✅ OffsetUniverse for centralized preferences and state

### Management

✅ ~~Thin OffsetSceneHandler that is decoupled from the internal data representation of OffsetScenes~~ It was so thin that functionality was moved into OffsetManager, ~~`OffsetSceneBootstrapper` only handles registration of scenes now.~~ You don't need to add anything to your game scenes. The package automatically detects which scene to run offset on based on the first view that registers itself.

✅ Thin OffsetManager (only bootstraps OffsetServer and adds reference to OffsetUniverse)

### DevEx

✅ ~~Helper methods/extension~~ (See `OffsetUniverse`)

🔲 Clean up helper methods: Methods that require the `OffsetServer` to function (i.e. methods that must be called on the authoritative client in a multiplayer environment) should be moved out of the `OffsetUniverse` and into the `OffsetServer`. Currently there is a mix of 'safe' and 'unsafe' methods in the `OffsetUniverse`, for example `TeleportTo` only works on the authoritative client.

🔲 Clean up access modifiers on `OffsetManager` and `OffsetUniverse`

### Transforms

✅ OffsetTransform (Formally FOObject, now also performs tasks of FOObserver)

🔲 ~~TODO: how to 're-discover' offset transforms after they have been moved out of bounds? hash grid? (or: keep a cap on the # of disabled offset transforms and just do an O(n) search on the few left over? could also be sped up with sweep and prune)~~ Changed: Offset Transforms are now destroyed if out of range. If you are being pursued by NPC's they will be offset with you, but if they are out of range they will be despawned. If you need to support Offset Transform persistence the best solution would probably be building your own spawning/persistence system with OffsetAnchors.

✅ Deprecate FOObserver in favor of boolean on OffsetTransform

✅ OffsetAnchor (Formally FOAnchor)

✅ IgnoreOffset

### Example

✅ TestingSetup.unitypackage (test harness, example setup, included in the UPM package)

✅ FishNetDemo.unitypackage (example FishNet demo)

✅ [OffsetDemo](https://github.com/hudmarc/FishNet-FloatingOffset---Car-Controller-Prediction-Test/tree/master) (small singleplayer demo)

### Abstractions

✅ Platform independent lightweight OffsetServer core

### Testing

✅ Rewrite tests to work with new API

✅ Maximize test coverage

✅ Review tests and make sure they are actually testing what they say they are

### Code Quality

✅ Make sure access modifiers are as restrictive as possible (done on core classes)

✅ Remove var keyword where unnecessary

✅ Documentation

✅ Lint everything

🔲 Clean up unity interop on `OffsetManager` and clean up `OffsetManagerNetworking`, both classes are currently too bulky.
