using System;
using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using static FloatingOffset.Runtime.FastUnionFind;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The OffsetServer manages all offset calculations for the FloatingOffset package. This class should exist as a single instance.
    /// In a multiplayer environment, the OffsetServer will not be active on clients.
    /// </summary>
    public class OffsetServer<TSceneKey>
    {
        /// <summary>
        /// Quickly iterable list of views. An OffsetTransform is here <==> it is considered a view.
        /// </summary>
        private List<IOffsetObject<TSceneKey>> views = new List<IOffsetObject<TSceneKey>>();
        private HashSet<IOffsetObject<TSceneKey>> views_to_remove = new HashSet<IOffsetObject<TSceneKey>>();

        /// <summary>
        /// Tracks all OffsetScenes in one fast array.
        /// </summary>
        private OffsetSceneCollection<TSceneKey> scenes = new OffsetSceneCollection<TSceneKey>();

        private readonly int MaximumJoinDistance;
        private readonly int MaximumJoinDistanceSquared;

        private readonly int SceneRadius;
        private readonly int SceneRadiusSquared;

        private readonly int MaxScenes;
        private TSceneKey source;

        public IOffsetHandler<TSceneKey> handler { get; private set; }

        private HashGrid view_grid;

        // Passing the base values as parameters with default values gives you the 
        // exact same out-of-the-box behavior, but allows for injection later if needed.
        public OffsetServer(IOffsetHandler<TSceneKey> handler, int MinimumJoinDistance = 5000, int MaxScenes = 200, int Hysteresis = 1000)
        {
            this.MaximumJoinDistance = MinimumJoinDistance;
            this.MaximumJoinDistanceSquared = MinimumJoinDistance * MinimumJoinDistance;

            this.SceneRadius = MinimumJoinDistance + Hysteresis;
            this.SceneRadiusSquared = SceneRadius * SceneRadius;

            this.MaxScenes = MaxScenes;

            this.handler = handler;
            this.view_grid = new HashGrid(64, 64, MinimumJoinDistance);
        }
        /// <summary>
        /// Gets the offset for the given scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public Vector3d GetSceneOffset(TSceneKey scene)
        {
            return scenes.GetOffset(scene);
        }
        /// <summary>
        /// Gets the number of views in the given scene.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public int GetSceneViewCount(TSceneKey scene)
        {
            return scenes.GetViewCount(scene);
        }
        /// <summary>
        /// Registers the given offset offsettable as a view.
        /// </summary>
        /// <param name="view"></param>
        public void RegisterView(IOffsetObject<TSceneKey> view)
        {
            if (scenes.Count < 1)
            {
                scenes.Register(view.GetSceneKey());
                source = scenes.GetSceneAt(0).key;
            }

            scenes.AddView(view.GetSceneKey());
            views.Add(view);
        }
        /// <summary>
        /// Downgrades the given view to a transform
        /// </summary>
        /// <param name="offsettable"></param>
        public void UnregisterView(IOffsetObject<TSceneKey> offsettable)
        {
            scenes.RemoveView(offsettable.GetSceneKey());
            views_to_remove.Add(offsettable);
        }

        private Vector3d[] view_positions = new Vector3d[8];
        private int[] view_scene_indexes = new int[8];
        private int[] union_counts = new int[8];
        private Vector3d[] union_sums = new Vector3d[8];
        private ScenedUnion[] union_scene_tuples = new ScenedUnion[8];
        private FastUnionFind union = new FastUnionFind(8);

        int[] neighborsBuffer = new int[8];

        // Tracks the current winning scene for a given root
        // Key: root | Value: (Winning Scene, Max Count, Representative View Index)
        Dictionary<int, SceneWinner> winners = new Dictionary<int, SceneWinner>();
        private void EnsureCapacity(int count)
        {
            if (view_positions.Length < count)
            {
                int newSize = Mathd.GetNextPowerOfTwo(count);
                Array.Resize(ref view_positions, newSize);
                Array.Resize(ref view_scene_indexes, newSize);
                Array.Resize(ref union_counts, newSize);
                Array.Resize(ref union_sums, newSize);
                Array.Resize(ref union_scene_tuples, newSize);
            }
        }
        public void Process()
        {
            union.Clear();
            // prune views scheduled for removal
            for (int i = 0; i < views.Count; i++)
            {
                if (views_to_remove.Contains(views[i]))
                {
                    int lastIndex = views.Count - 1;
                    views[i] = views[lastIndex];
                    views.RemoveAt(lastIndex);
                    i--;
                    continue;
                }
            }

            int view_count = views.Count;

            // prune unused empty scenes
            if (scenes.Count > view_count * 2 && scenes.TryPopEmpty(out int empty_index))
            {
                scenes.UnregisterAt(empty_index);
                handler.Unload(scenes.GetKeyAt(empty_index));
            }


            EnsureCapacity(views.Count);

            // Initialize caches

            view_positions = new Vector3d[view_count];

            view_grid.Clear();

            union.EnsureCapacity(view_count);

            for (int i = 0; i < view_count; i++)
            {
                // Get positions
                IOffsetObject<TSceneKey> view = views[i];
                view_scene_indexes[i] = scenes.IndexOf(view.GetSceneKey());

                Vector3d position = view_positions[i] = GetSceneOffset(scenes.GetKeyAt(view_scene_indexes[i])) + view.GetEnginePosition();
                view_positions[i] = position;

                // Populate hashgrid
                view_grid.Add(position, i);

                // Reset caches
                union_counts[i] = 0;
                union_sums[i] = Vector3d.zero;
            }

            // Populate union-find, compute offsets for unions
            for (int i = 0; i < view_count; i++)
            {

                // Get the root using path compression
                int myRoot = union.Find(i);
                int my_scene_index = view_scene_indexes[i];
                Vector3d my_pos = view_positions[i];

                // The grid ignores anyone already in myRoot.
                view_grid.FindNeighbors(view_positions[i], view_positions, ref neighborsBuffer, out int neighbourCount, myRoot, union.unions);

                for (int j = 0; j < neighbourCount; j++)
                {
                    int neighborIndex = neighborsBuffer[j];

                    if (neighborIndex != i)
                    {
                        // Resolve the neighbor's true root
                        int neighborRoot = union.Find(neighborIndex);

                        // If we are already in the same union, skip all heavy math.
                        if (myRoot != neighborRoot)
                        {
                            // Layer Check
                            if (scenes.SameLayer(my_scene_index, view_scene_indexes[neighborIndex]))
                            {
                                // Real position distance check
                                double distance_from_neighbor = (my_pos - view_positions[neighborIndex]).sqrMagnitude;

                                // If we don't merge with them this iteration, they will merge with us next iteration.
                                if (distance_from_neighbor < MaximumJoinDistanceSquared)
                                {
                                    double our_separation = (my_pos - scenes.GetOffsetAt(my_scene_index)).sqrMagnitude;
                                    double their_separation = (view_positions[neighborIndex] - scenes.GetOffsetAt(view_scene_indexes[neighborIndex])).sqrMagnitude;

                                    if (our_separation <= their_separation)
                                    {
                                        // Merge them.
                                        union.Union(i, neighborIndex);

                                        // Because we just absorbed someone, our root might have changed.
                                        // Update myRoot so the next iteration of the grid uses the new, larger group.
                                        myRoot = union.Find(i);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Aggregate data for each union
            for (int i = 0; i < view_count; i++)
            {
                int rep = union.Find(i);
                union_counts[rep]++;
                union_sums[rep] += view_positions[i];
            }


            winners.Clear();

            Array.Clear(union_scene_tuples, 0, union_scene_tuples.Length);

            for (int i = 0; i < view_count; i++)
            {
                union_scene_tuples[i] = new ScenedUnion { scene_index = view_scene_indexes[i], representative = union.Find(i) };
            }

            int current_scene = union_scene_tuples[0].scene_index;
            int current_union = union_scene_tuples[0].representative;
            int current_run_count = 1;

            int scene_champion_union = current_union;
            int scene_champion_count = 1;

            // Scan and Reduce
            for (int i = 1; i < union_scene_tuples.Length; i++)
            {
                ScenedUnion item = union_scene_tuples[i];

                if (item.scene_index == current_scene && item.representative == current_union)
                {
                    // Still looking at the same union in the same scene
                    current_run_count++;
                }
                else
                {
                    // The union changed OR the scene changed. 
                    // First, see if the run that just finished beats the current scene champion.
                    if (current_run_count > scene_champion_count)
                    {
                        scene_champion_union = current_union;
                        scene_champion_count = current_run_count;
                    }

                    // If the SCENE changed, the battle for the previous scene is officially over.
                    if (item.scene_index != current_scene)
                    {
                        // Lock in the winner for the old scene
                        winners[scene_champion_union] = new SceneWinner(current_scene, scene_champion_count, scene_champion_union);

                        // Reset champion tracking for the brand new scene
                        current_scene = item.scene_index;
                        scene_champion_union = item.representative;
                        scene_champion_count = 0;
                    }

                    // Reset the run tracking for the new union
                    current_union = item.representative;
                    current_run_count = 1;
                }
            }

            // Resolve the Tail
            // Evaluate the final run that was active when the loop ended
            if (current_run_count > scene_champion_count)
            {
                scene_champion_union = current_union;
                scene_champion_count = current_run_count;
            }
            // Lock in the final scene
            winners[scene_champion_union] = new SceneWinner(current_scene, scene_champion_count, scene_champion_union);





            // Transfer all views that are not in the right scene && compute merges
            for (int i = 0; i < view_count; i++)
            {
                int rep = union.Find(i);

                if (winners.TryGetValue(rep, out SceneWinner winner))
                {
                    TSceneKey scene = scenes.GetKeyAt(winner.scene_index);

                    if (!view_scene_indexes[i].Equals(winner.scene_index))
                    {
                        // transfer the view to the scene
                        Transfer(views[i], views[i].GetSceneKey(), scene);
                    }
                    if (winner.winner_index == i)
                    {
                        Vector3d average = union_sums[rep] / (double)union_counts[rep];
                        if ((average - scenes.GetOffsetAt(winner.scene_index)).sqrMagnitude > MaximumJoinDistanceSquared)
                            scenes.Offset(scene, average);
                    }
                }
                // transfer with hysteresis
                else if ((view_positions[i] - scenes.GetOffsetAt(view_scene_indexes[i])).sqrMagnitude > SceneRadiusSquared)
                {
                    // request new scenes for stragglers who are not part of a union or unions without assigned scenes
                    // if we find an empty scene, great! we return it.
                    // if not, we do nothing because this view will be moved to the first available scene as soon as the scene loads.
                    // the flip-side: if you a player was just interacting with a bunch of other players and then they warp-speed out
                    // they might have a frame hitch as they warp out. keep this in mind as the gamedev, or use the Teleport(view,real_position);
                    // function on the OffsetManager.
                    if (RequestScene(source, union_sums[rep] / (double)union_counts[rep], out int found))
                    {
                        // transfer the view to the scene
                        Transfer(views[i], views[i].GetSceneKey(), scenes.GetSceneAt(found).key);
                    }
                }
            }

            // rebase all scenes whose actual offset does not match their expected offset
            for (int i = 0; i < scenes.Count; i++)
            {
                // updateOffset does nothing if the offset is already updated.
                handler.UpdateOffset(scenes.GetSceneAt(i));
            }
        }


        /// <summary>
        /// Transfers the given offsettable to the given scene.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="handler"></param>
        private void Transfer(IOffsetObject<TSceneKey> offsettable, TSceneKey from, TSceneKey to, bool reposition = true)
        {
            if (from.Equals(to))
                return;


            scenes.RemoveView(offsettable.GetSceneKey());

            // Note the 'true'! This repositions the handler when it arrives at the target scene.
            handler.TransferTo(offsettable, from, to, reposition);


            scenes.AddView(to);
        }

        /// <summary>
        /// Fetch a scene from the empty scenes and set its position.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="offset"></param>
        private bool RequestScene(TSceneKey source, Vector3d offset, out int found_scene, Action<TSceneKey> onSceneReady = null)
        {
            // 1. Check for empty scenes
            if (scenes.TryPopEmpty(out int empty_index))
            {
                OffsetScene<TSceneKey> empty_scene = scenes.OffsetAt(empty_index, offset);
                handler.UpdateOffset(empty_scene);
                found_scene = empty_index;
                return true;
            }
            else
            {
                found_scene = -1;
                // 2. Prevent infinite cloning
                if (scenes.Count <= MaxScenes)
                {
                    handler.Clone(source, scene =>
                    {
                        scenes.Register(scene);

                        int index = scenes.IndexOf(scene);

                        OffsetScene<TSceneKey> empty_scene = scenes.OffsetAt(index, offset);
                        handler.UpdateOffset(empty_scene);

                        onSceneReady?.Invoke(scene);
                    });
                }
                else if (scenes.Count > Mathd.GetNextPowerOfTwo(scenes.Capacity))
                {
                    throw new Exception($"Exceeded maximum number of active Offset Scenes! Limit: {MaxScenes} LF: {scenes.Count}:{scenes.Capacity} Hard limit: {Mathd.GetNextPowerOfTwo(scenes.Capacity)}");
                }

                return false;
            }
        }
        /// <summary>
        /// Is the given scene tracked by this offset server?
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool HasScene(TSceneKey scene)
        {
            return scenes.HasScene(scene);
        }
        public void TeleportTo(IOffsetObject<TSceneKey> offsetObject, Vector3d real_position)
        {
            TSceneKey origin = offsetObject.GetSceneKey();
            Vector3d offset = scenes.GetOffset(origin);
            if ((offset + offsetObject.GetEnginePosition() - real_position).sqrMagnitude < MaximumJoinDistanceSquared)
            {
                offsetObject.SetEnginePosition(real_position - offset);
                return;
            }
            bool request = RequestScene(source, real_position, out int found, target =>
            {
                Transfer(offsetObject, origin, target, false);
                handler.UpdateOffset(scenes.GetScene(target));
            });
            if (request)
            {
                Transfer(offsetObject, origin, scenes.GetKeyAt(found), false);
                handler.UpdateOffset(scenes.GetSceneAt(found));
            }

        }

        private struct ScenedUnion : IComparable<ScenedUnion>
        {
            public int scene_index;
            public int representative;

            public int CompareTo(ScenedUnion other)
            {
                // Default generic comparers are safe and optimized here
                int cmp = System.Collections.Generic.Comparer<int>.Default.Compare(scene_index, other.scene_index);
                if (cmp == 0)
                {
                    cmp = representative.CompareTo(other.representative);
                }
                return cmp;
            }
        }
    }



    public struct SceneWinner : IComparable<SceneWinner>, IEquatable<SceneWinner>
    {
        public int scene_index;
        public int count;
        public int winner_index;

        public SceneWinner(int scene_index, int count, int winner_index)
        {
            this.scene_index = scene_index;
            this.count = count;
            this.winner_index = winner_index;
        }

        public int CompareTo(SceneWinner other)
        {
            // Primary sort: Scene Index
            int cmp = scene_index.CompareTo(other.scene_index);
            if (cmp != 0) return cmp;

            // Secondary sort: Count 
            // (Note: To sort by highest count first, swap to: other.count.CompareTo(count))
            cmp = count.CompareTo(other.count);
            if (cmp != 0) return cmp;

            // Tertiary sort: Winner Index
            return winner_index.CompareTo(other.winner_index);
        }

        // IEquatable<T>: Reflection-Free Equality
        public bool Equals(SceneWinner other)
        {
            return scene_index == other.scene_index &&
                   count == other.count &&
                   winner_index == other.winner_index;
        }

        // Fallback override to prevent boxing if passed as an object
        public override bool Equals(object obj)
        {
            return obj is SceneWinner other && Equals(other);
        }

        // Fast HashCode
        public override int GetHashCode()
        {
            // Using unchecked integer math is the fastest way to hash in Unity
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + scene_index;
                hash = hash * 31 + count;
                hash = hash * 31 + winner_index;
                return hash;
            }
        }

        public static bool operator ==(SceneWinner left, SceneWinner right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SceneWinner left, SceneWinner right)
        {
            return !left.Equals(right);
        }
    }

}