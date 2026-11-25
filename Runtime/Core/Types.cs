using System;

namespace FloatingOffset.Runtime
{
    namespace Types
    {
        /// <summary>
        /// Handles the scene and offset.
        /// </summary>
        /// <typeparam name="TSceneKey"></typeparam>
        public struct OffsetScene<TSceneKey>
        {
            /// <summary>
            /// The real offset of this offset scene.
            /// </summary>
            public Vector3d offset;
            /// <summary>
            /// The only source of truth for the number of views in this offset scene. Assigned to in the view loop.
            /// </summary>
            public int view_count;
            /// <summary>
            /// The Scene associated with this offset scene.
            /// </summary>
            public TSceneKey key;
            /// <summary>
            /// Whether this offset scene is valid (ready to use) or not (i.e. still loading or broken)
            /// </summary>
            internal bool valid;
            /// <summary>
            /// If two scenes have different layers then they will not merge
            /// </summary>
            public int layer;
        }
        /// <summary>
        /// IOffsetObject is the generic form of the OffsetView used in the core.
        /// </summary>
        /// <typeparam name="TSceneKey"></typeparam>
        public interface IOffsetObject<TSceneKey> : ISceneKeyedObject<TSceneKey>
        {
            /// <summary>
            /// The local scene position (converted from Vector3 into Vector3d) of this offset object.
            /// </summary>
            /// <returns></returns>
            Vector3d GetEnginePosition();
            /// <summary>
            /// Sets local scene position (converted from Vector3d into Vector3) of this offset object.
            /// </summary>
            /// <returns></returns>
            void SetEnginePosition(Vector3d position);
            /// <summary>
            /// Destroys this OffsetObject in the target engine
            /// </summary>
            void Destroy();
            /// <summary>
            /// Called right before this object will be transferred to another scene.
            /// </summary>
            void OnPreSceneTransfer();
            /// <summary>
            /// Called when this object has been transferred to another scene.
            /// </summary>
            void OnSceneTransfer();
            /// <summary>
            /// Is this View a player? Used to determine which View will be the client's local view.
            /// </summary>
            /// <returns></returns>
            bool IsPlayer();
            /// <summary>
            /// A friendly display name to show for this object (useful for debugging)
            /// </summary>
            /// <returns></returns>
            string GetName();
        }
        /// <summary>
        /// An Offsettable listens to changes in its scene from the Offset system.
        /// </summary>
        /// <typeparam name="TSceneKey"></typeparam>
        [Obsolete("Prefer to use ISceneKeyedObject<TSceneKey>. Use IPreSceneOffsetListener<TSceneKey>, ISceneOffsetListener<TSceneKey> individually for fine-grained callback control.")]
        public interface IOffsettable<TSceneKey> : IPreSceneOffsetListener<TSceneKey>, ISceneOffsetListener<TSceneKey> { }
        /// <summary>
        /// Base interface for all objects which are keyed according to their current scene.
        /// </summary>
        /// <typeparam name="TSceneKey"></typeparam>
        public interface ISceneKeyedObject<TSceneKey>
        {
            /// <summary>
            /// The key of the scene this offsettable object resides in.
            /// </summary>
            /// <returns></returns>
            TSceneKey GetSceneKey();

            /// <summary>
            /// The runtime object, used for keying.
            /// </summary>
            /// <returns></returns>
            object GetEngineObject();

            /// <summary>
            /// Returns true if the game object associated with this offsettable exists, otherwise false.
            /// </summary>
            /// <returns></returns>
            bool IsValid();

            /// <summary>
            /// A friendly display name to show for this object (useful for debugging)
            /// </summary>
            /// <returns></returns>
            string GetName();
        }
        /// <summary>
        /// Exposes listeners for OnPreOffset which is called on subscribed objects in a scene about to be offset.
        /// </summary>
        /// <typeparam name="TSceneKey"></typeparam>
        public interface IPreSceneOffsetListener<TSceneKey> : ISceneKeyedObject<TSceneKey>
        {
            /// <summary>
            /// Called before this offsettable's scene is offset.
            /// </summary>
            /// <param name="old_offset">The offset of the object's current scene (The previous scene)</param>
            /// <param name="new_offset">The offset of the destination scene (The scene the object will move into)</param>
            /// <param name="destination">The scene the object will move into</param>
            void OnPreOffset(Vector3d old_offset, Vector3d new_offset, TSceneKey destination);

        }
        /// <summary>
        /// Exposes listeners for OnPreOffset which is called on subscribed objects in a scene that has been offset.
        /// </summary>
        /// <typeparam name="TSceneKey"></typeparam>
        public interface ISceneOffsetListener<TSceneKey> : ISceneKeyedObject<TSceneKey>
        {
            /// <summary>
            /// Called when this offsettable's scene is offset.
            /// </summary>
            /// <param name="old_offset">The offset of the scene the object is moving from</param>
            /// <param name="new_offset">The offset of the scene the object is moving to</param>
            /// <param name="destination">The scene the object is moving into</param>
            void OnOffset(Vector3d old_offset, Vector3d new_offset, TSceneKey destination);
        }
        /// <summary>
        /// Exposes listeners for OnPreOffset which is called on subscribed objects in a scene after it has been offset.
        /// </summary>
        /// <typeparam name="TSceneKey"></typeparam>
        public interface IPostSceneOffsetListener<TSceneKey> : ISceneKeyedObject<TSceneKey>
        {
            /// <summary>
            /// Called after this offsettable's scene is offset.
            /// </summary>
            /// <param name="old_offset">The offset of the object's previous scene</param>
            /// <param name="new_offset">The offset of the object's now current scene (the destination scene)</param>
            /// <param name="destination">The scene the object has been moved into</param>
            void OnPostOffset(Vector3d old_offset, Vector3d new_offset, TSceneKey destination);
        }
        public interface IOffsetHandler<TSceneKey>
        {
            /// <summary>
            /// Applies the offset for the given scene.
            /// </summary>
            /// <param name="scene"></param>
            void UpdateOffset(OffsetScene<TSceneKey> scene);
            /// <summary>
            /// Transfer the given OffsetView from the 'from' scene to the 'to' scene.
            /// </summary>
            /// <param name="offsettable"></param>
            /// <param name="from"></param>
            /// <param name="to"></param>
            /// <param name="reposition"></param>
            void TransferTo(IOffsetObject<TSceneKey> offsettable, TSceneKey from, TSceneKey to);
            /// <summary>
            /// Clone the given scene. Calls the callback when done.
            /// </summary>
            /// <param name="scene"></param>
            /// <param name="onSceneReady"></param>
            void Clone(TSceneKey scene, Action<TSceneKey> onSceneReady);
            /// <summary>
            /// Unload the given scene.
            /// </summary>
            /// <param name="scene"></param>
            void Unload(TSceneKey scene);
        }
    }
}
