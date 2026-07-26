using FloatingOffset.Runtime;
using FloatingOffset.Runtime.Types;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Editor.Tests
{
    // A simple mock handler to satisfy the IOffsetHandler interface during testing
    public class MockOffsetHandler : IOffsetHandler<int>
    {
        // Generates new scene IDs when Clone() is called
        private int _sceneKeyCounter;

        private IOffsetObject<int> main;

        // Keeps track of the mathematical origin of each scene
        public Dictionary<int, Vector3d> SceneOffsets { get; } = new Dictionary<int, Vector3d>();

        // For the unit tests to work, the handler needs to know about the views 
        // so it can shift them when a rebase occurs.
        public List<IOffsetObject<int>> TrackedObjects { get; } = new List<IOffsetObject<int>>();

        public MockOffsetHandler(int initialSceneKey)
        {
            _sceneKeyCounter = initialSceneKey;
            SceneOffsets[initialSceneKey] = Vector3d.zero;
        }

        public Vector3d RealPosition(Vector3d engine_position, int scene_index) => SceneOffsets[scene_index] + engine_position;
        public Vector3d RealPosition(IOffsetObject<int> offsetObject) => SceneOffsets[offsetObject.GetSceneKey()] + offsetObject.GetEnginePosition();


        public void Clone(int scene, Action<int> onSceneReady)
        {
            // Simulate an async scene load by generating a new ID and firing the callback
            _sceneKeyCounter++;
            int newScene = _sceneKeyCounter;

            // Initialize the new scene at the origin
            SceneOffsets[newScene] = Vector3d.zero;

            onSceneReady?.Invoke(newScene);
        }

        public void RegisterOffsettable(IOffsettable<int> offsettable, int scene)
        {
            // In a real Unity implementation, this would track components that need 
            // special handling during a rebase (like TrailRenderers or ParticleSystems).
            // It is largely unused in the pure math unit tests.
        }

        public void TransferTo(IOffsetObject<int> offsettable, int from, int to, bool reposition = false)
        {
            if (reposition)
            {
                // Calculate the true global position: (Origin of Old Scene) + (Local Position)
                Vector3d oldOrigin = SceneOffsets.ContainsKey(from) ? SceneOffsets[from] : Vector3d.zero;
                Vector3d trueGlobalPos = oldOrigin + offsettable.GetEnginePosition();

                // Calculate new local position: (True Global Position) - (Origin of New Scene)
                Vector3d newOrigin = SceneOffsets.ContainsKey(to) ? SceneOffsets[to] : Vector3d.zero;
                offsettable.SetEnginePosition(trueGlobalPos - newOrigin);
            }

            offsettable.SetSceneKey(to);
        }

        public void UpdateOffset(OffsetScene<int> scene)
        {
            Vector3d oldOrigin = SceneOffsets.ContainsKey(scene.key) ? SceneOffsets[scene.key] : Vector3d.zero;
            Vector3d newOrigin = scene.offset;

            // How much the mathematical center of the world moved
            Vector3d delta = newOrigin - oldOrigin;

            // Update our internal tracking
            SceneOffsets[scene.key] = newOrigin;

            // Apply the reverse delta to all physical objects in the scene.
            // If the origin shifts +2000 on the X axis, the objects must be teleported -2000 
            // on the X axis so they appear to stay completely still to the player.
            foreach (var obj in TrackedObjects)
            {
                if (obj.GetSceneKey() == scene.key)
                {
                    obj.SetEnginePosition(obj.GetEnginePosition() - delta);
                }
            }
        }

        public void Unload(int scene)
        {
            SceneOffsets.Remove(scene);
        }

        public void SetMainView(IOffsetObject<int> view)
        {
            main = view;
        }
    }

    public class MockOffsetObject : IOffsetObject<int>
    {
        private int _sceneKey;
        private Vector3 _enginePosition;

        public bool IsViewFlag { get; set; } = true;
        public bool IsValidFlag { get; set; } = true;

        public MockOffsetObject(int sceneKey)
        {
            _sceneKey = sceneKey;
            _enginePosition = Vector3.zero;
        }

        public Vector3d GetEnginePosition() => Mathd.toVector3d(_enginePosition);
        public void SetEnginePosition(Vector3d position) => _enginePosition = Mathd.toVector3(position);

        public int GetSceneKey() => _sceneKey;
        public void SetSceneKey(int key) => _sceneKey = key;

        public bool IsView() => IsViewFlag;
        public bool IsValid() => IsValidFlag;

        public void Destroy()
        {
            throw new NotImplementedException();
        }
    }



    [TestFixture]
    public class OffsetSceneCollectionTests
    {
        private OffsetSceneCollection<int> _collection;
        private MockOffsetHandler _initialHandler;
        private const int INITIAL_SCENE_KEY = 100;

        [SetUp]
        public void Setup()
        {
            // Runs before every single test to guarantee a clean, isolated state
            _initialHandler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            _collection = new OffsetSceneCollection<int>();
            _collection.Register(INITIAL_SCENE_KEY);
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up any loose references if necessary
            _collection = null;
        }
        #region View Management

        [Test]
        public void AddView_WhenValidView_IncrementsViewCount()
        {
            // Arrange
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            int initialCount = _collection.GetViewCount(INITIAL_SCENE_KEY);

            // Act
            _collection.AddView(view.GetSceneKey());

            // Assert
            Assert.AreEqual(initialCount + 1, _collection.GetViewCount(INITIAL_SCENE_KEY));
        }

        [Test]
        public void RemoveView_WhenValidView_DecrementsViewCount()
        {
            // Arrange
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            _collection.AddView(view.GetSceneKey()); // Add it first
            int countAfterAdd = _collection.GetViewCount(INITIAL_SCENE_KEY);

            // Act
            _collection.RemoveView(view.GetSceneKey());

            // Assert
            Assert.AreEqual(countAfterAdd - 1, _collection.GetViewCount(INITIAL_SCENE_KEY));
        }

        [Test]
        public void SetEmpty_WhenSceneIsActive_SetsViewCountToZeroAndMovesToEmptyZone()
        {
            // Arrange
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            _collection.AddView(view.GetSceneKey()); // Force it to be active

            // Act
            _collection.SetEmpty(0); // Assuming it's at index 0

            // Assert
            Assert.AreEqual(0, _collection.GetViewCount(INITIAL_SCENE_KEY));
        }

        #endregion

        #region Data Retrieval

        [Test]
        public void GetOffset_WhenRequested_ReturnsCorrectVector3d()
        {
            // Arrange (Requires a way to set the offset first, assume it defaults to zero)
            Vector3d expectedOffset = Vector3d.zero;

            // Act
            Vector3d actualOffset = _collection.GetOffset(INITIAL_SCENE_KEY);

            // Assert
            Assert.AreEqual(expectedOffset, actualOffset);
        }

        #endregion

        #region Array Resizing

        [Test]
        public void RegisterHandler_WhenCapacityExceeded_SuccessfullyResizesArray()
        {
            // Arrange
            int initialCapacity = _collection.Capacity;

            // Act
            // Force the collection to double in size
            for (int i = 0; i < initialCapacity + 1; i++)
            {
                _collection.Register(200 + i);
            }

            // Assert
            Assert.Greater(_collection.Capacity, initialCapacity, "The array should have doubled in size to accommodate new elements.");
        }

        #endregion
    }
}