#if UNITY_EDITOR
using System;
using System.Collections;
using System.Text;
using FishNet.Managing;
using FishNet.Object;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FloatingOffset.Runtime
{

    /// <summary>
    /// These tests can be run automatically on the server and do not require a client connection.
    /// </summary>
    public class ServersideTesterAuto
    {
        private const float OFFSET_DISTANCE = 20000;
        private const float TEST_ITERATIONS = 128;
        public const string TEST_SCENE_NAME = "Offline Automated Testing Scene";

        private AbstractOffsetManager manager;
        private OffsetUniverse universe;
        private NetworkManager networkManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.LogWarning("------- Starting test setup -------");

            // Load scene asynchronously and wait for completion
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(TEST_SCENE_NAME, LoadSceneMode.Single);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            if (networkManager == null)
                throw new Exception("NetworkManager not found in the test scene.");

            networkManager.ServerManager.StartConnection();
            while (!networkManager.ServerManager.Started)
            {
                yield return new WaitForFixedUpdate();
            }

            networkManager.ClientManager.StartConnection();
            while (!networkManager.ClientManager.Started)
            {
                yield return new WaitForFixedUpdate();
            }

            var manager = Component.FindFirstObjectByType<AbstractOffsetManager>();

            universe = manager.universe;
            Debug.Log("------- Setup complete -------");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.LogWarning("------- Starting test teardown -------");

            for (int i = 0; i < 10; i++) yield return null;

            if (networkManager != null)
            {
                if (networkManager.ClientManager.Started)
                    networkManager.ClientManager.StopConnection();

                if (networkManager.ServerManager.Started)
                    networkManager.ServerManager.StopConnection(true);
            }

            // Allow FishNet time to clean up sockets and objects
            for (int i = 0; i < 10; i++) yield return null;

            // Programmatically create a temporary scene so we don't rely on Build Settings
            Scene tempScene = SceneManager.CreateScene("TempTeardownScene");
            SceneManager.SetActiveScene(tempScene);

            // Find and safely unload the test scene to flush its state out of memory
            Scene testScene = SceneManager.GetSceneByName(TEST_SCENE_NAME);
            if (testScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(testScene);
            }

            manager = null;
            universe = null;
            networkManager = null;
        }
        /// <summary>
        /// Asserts that the objects are unregistered immediately when destroyed using DestroyImmediate.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator DestroyImmediateUnregisterTest()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Step; Error (mm);Error At Origin (meters); Distance From Origin; Position Before Rebase");

            OffsetView view = null;
            OffsetAnchor origin = null;

            while (view == null || origin == null)
            {
                view = FindView();
                origin = GameObject.Find("Origin")?.GetComponent<OffsetAnchor>();
                yield return new WaitForSeconds(1);
            }

            Vector3d position = UnityFunctions.toVector3d(view.transform.position);

            yield return new WaitForSeconds(1);
            Debug.Log("Starting test");

            GameObject.DestroyImmediate(view.gameObject);
            GameObject.DestroyImmediate(origin.gameObject);

            Assert.AreEqual(0, universe.manager.OffsettableCount());
            Assert.AreEqual(0, universe.manager.CountRegisteredViews());

            yield return null; //one frame

            Assert.AreEqual(0, universe.manager.CountViews());

        }
        /// <summary>
        /// Asserts that the objects are unregistered by the end of the frame.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator DestroyUnregisterTest()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Step; Error (mm);Error At Origin (meters); Distance From Origin; Position Before Rebase");

            OffsetView view = null;
            OffsetAnchor origin = null;

            while (view == null || origin == null)
            {
                view = FindView();
                origin = GameObject.Find("Origin")?.GetComponent<OffsetAnchor>();
                yield return new WaitForSeconds(1);
            }

            Vector3d position = UnityFunctions.toVector3d(view.transform.position);

            yield return new WaitForSeconds(1);
            Debug.Log("Starting test");

            Debug.Log($"Offsettables {universe.manager.OffsettableCount()}");
            Debug.Log($"Registered Views {universe.manager.CountRegisteredViews()}");
            Debug.Log($"Views {universe.manager.CountViews()}");

            Assert.AreEqual(1, universe.manager.OffsettableCount());
            Assert.AreEqual(1, universe.manager.CountRegisteredViews());

            GameObject.Destroy(view.gameObject);
            GameObject.Destroy(origin.gameObject);

            yield return null; //one frame

            Assert.AreEqual(0, universe.manager.OffsettableCount());
            Assert.AreEqual(0, universe.manager.CountRegisteredViews());

            yield return null; //one frame

            Assert.AreEqual(0, universe.manager.CountViews());
        }


        [UnityTest]
        public IEnumerator OffsetTest()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Step; Error (mm);Origin Offset (meters); Distance; Delta; Desync Count");

            OffsetView view = null;
            OffsetAnchor origin = null;

            while (view == null || origin == null)
            {
                view = FindView();
                origin = GameObject.Find("Origin")?.GetComponent<OffsetAnchor>();
                yield return new WaitForFixedUpdate();
            }

            Vector3d position = UnityFunctions.toVector3d(view.transform.position);

            yield return new WaitForSeconds(2);
            Debug.Log("Starting test");

            // Debug.Break();

            var val = 1;
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                Debug.Log($"OFFSET: Count {i}");
                Vector3 delta = new Vector3(val, val, val);
                view.transform.position += delta;
                position += UnityFunctions.toVector3d(delta);

                if (i < 21 && (val * 2) > 0)
                    val *= 2;

                var error = Vector3d.Distance(position, view.GetRealPosition());
                // Assert.Less(error, 2);

                int desync_count = 0;

                while (Math.Abs(view.transform.position.x) > universe.MinimumJoinDistance && desync_count < 100)
                {
                    yield return new WaitForFixedUpdate();
                    desync_count++;
                }
                if (desync_count >= 10)
                {
                    Debug.LogWarning($"Rebase not working properly, still desynchronized after {desync_count} frames. Was {view.transform.position.x}");
                }

                var view_error = Vector3d.Distance(Vector3d.zero, view.GetRealPosition());
                var origin_offset = Vector3.Distance(Vector3.zero, origin.transform.position);

                sb.Append($"{i};{error * 1000};{origin_offset};{view_error};{val};{desync_count}\n");
            }

            Debug.Log("--------RESULTS--------");
            Debug.Log(Application.persistentDataPath + "/output.csv");
            System.IO.File.WriteAllText(Application.persistentDataPath + "/output.csv", sb.ToString());
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [UnityTest]
        public IEnumerator ErrorAccumulator()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Step; Error (mm);Error At Origin (meters); Distance From Origin; Position Before Rebase");

            OffsetView view = null;
            OffsetAnchor origin = null;

            while (view == null || origin == null)
            {
                view = FindView();
                origin = GameObject.Find("Origin")?.GetComponent<OffsetAnchor>();
                yield return new WaitForSeconds(1);
            }

            Vector3d position = UnityFunctions.toVector3d(view.transform.position);

            yield return new WaitForSeconds(1);
            Debug.Log("Starting test");

            double total_desync_count = 0;
            double error = 0;

            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                Vector3 delta = (i % 2 == 0 ? -1 : 1) * OFFSET_DISTANCE * Vector3.right;
                view.transform.position += delta;
                position += UnityFunctions.toVector3d(delta);

                int desync_count = 0;

                while (Math.Abs(view.transform.position.x) > universe.MinimumJoinDistance && desync_count < 100)
                {
                    yield return new WaitForFixedUpdate();
                    desync_count++;
                }
                if (desync_count >= 10)
                {
                    Debug.LogWarning($"Rebase not working properly, still desynchronized after {desync_count} frames. Was {view.transform.position.x}");
                }

                total_desync_count += desync_count;

                error += Vector3d.Distance(position, view.GetRealPosition());


                var distanceFromOrigin = Vector3.Distance(view.transform.position, Vector3.zero);
                var errorAtOrigin = Vector3.Distance(Vector3.zero, origin.transform.position);

                sb.Append($"{i};{error * 1000};{errorAtOrigin};{distanceFromOrigin};{view.transform.position}\n");
            }

            Debug.Log($"Mean desynchronized frame count: {total_desync_count / ((double)TEST_ITERATIONS)}"); //always 0
            Debug.Log($"Total desynchronized frame count accross all frames: {total_desync_count}"); //always 0
            Debug.Log($"Total error {error}");

            Debug.Log("--------RESULTS--------");
            Debug.Log(Application.persistentDataPath + "/error_accumulator_output.csv");
            System.IO.File.WriteAllText(Application.persistentDataPath + "/error_accumulator_output.csv", sb.ToString());
            Assert.LessOrEqual(error, 1);
        }

        [UnityTest]
        public IEnumerator MultipleViewsSameClient()
        {
            OffsetView[] views = new OffsetView[8];
            OffsetView initialView = null;

            while (initialView == null)
            {
                initialView = FindView();
                yield return new WaitForSeconds(0.5f);
            }

            views[0] = initialView;
            var viewGameObject = initialView.gameObject;

            for (int i = 1; i < views.Length; i++)
            {
                views[i] = GameObject.Instantiate(viewGameObject).GetComponent<OffsetView>();
                networkManager.ServerManager.Spawn(views[i].GetComponent<NetworkObject>());
            }

            Vector3d[] expectedPositions = new Vector3d[8];
            for (int i = 0; i < 8; i++)
            {
                expectedPositions[i] = UnityFunctions.toVector3d(views[i].transform.position);
            }

            yield return new WaitForSeconds(2);
            Debug.Log("Starting test");

            // Debug.Break();

            var val = 1;
            int error_frames = 0;
            for (int i = 0; i < 25; i++)
            {

                int viewIndex = i % views.Length;
                OffsetView currentView = views[viewIndex];

                if (currentView.IsValid())
                {
                    Vector3 delta = new Vector3(val, val, val);
                    currentView.transform.position += delta;
                    expectedPositions[viewIndex] += UnityFunctions.toVector3d(delta);

                    val *= 2;

                    yield return new WaitForEndOfFrame();
                    yield return null;

                    double absolute_error = Vector3d.Distance(expectedPositions[viewIndex], currentView.GetRealPosition());
                    float local_error = currentView.transform.position.magnitude;
                    while (absolute_error > 2.0 || local_error > 5000)
                    {
                        if (absolute_error > 2.0)
                        {
                            Debug.LogWarning($"Precision failure on iteration {i}. View {viewIndex} is off by {absolute_error} units.");
                        }
                        if (local_error > 5000)
                        {
                            Debug.LogWarning($"Offset failure on iteration {i}. View {viewIndex} is off-center by {local_error} units.");
                        }

                        yield return new WaitForEndOfFrame();
                        absolute_error = Vector3d.Distance(expectedPositions[viewIndex], currentView.GetRealPosition());
                        local_error = currentView.transform.position.magnitude;
                        error_frames++;
                    }
                    Assert.Less(absolute_error, 2.0, $"Precision failure on iteration {i}. View {viewIndex} is off by {absolute_error} units.");
                    Assert.Less(local_error, 5000, $"Offset failure on iteration {i}. View {viewIndex} is off-center by {local_error} units.");
                    Debug.Log($"Iteration {i} passed. View {viewIndex} tracking at {currentView.GetRealPosition()}");
                }
            }
            Debug.Log($"Test passed with {error_frames} imprecise frames.");
            yield return new WaitForSeconds(1);
        }

        [UnityTest]
        public IEnumerator OffsetViewGroupChange()
        {
            OffsetView[] views = new OffsetView[2];
            OffsetView initialView = null;
            OffsetView staticObject = null;

            // 1. Find the initial view and the static object
            while (initialView == null || staticObject == null)
            {
                OffsetView[] objects = UnityEngine.Object.FindObjectsOfType<OffsetView>();

                foreach (var obj in objects)
                {
                    if (initialView == null)
                    {
                        initialView = obj;
                    }
                    else
                    {
                        staticObject = obj;
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }

            views[0] = initialView;
            var viewGameObject = initialView.gameObject;

            // 2. Instantiate and spawn the remaining views (views[1] in this case)
            for (int i = 1; i < views.Length; i++)
            {
                views[i] = GameObject.Instantiate(viewGameObject).GetComponent<OffsetView>();
                networkManager.ServerManager.Spawn(views[i].GetComponent<NetworkObject>());
            }

            yield return new WaitForSeconds(0.5f);
            Debug.Log("Starting test");

            // 3. Execute test logic
            views[0].TeleportTo(Vector3d.right * OFFSET_DISTANCE);
            views[1].TeleportTo(-Vector3d.right * OFFSET_DISTANCE);

            yield return new WaitForEndOfFrame();
            yield return null;

            views[0].TeleportTo(Vector3d.zero);
            views[1].TeleportTo(Vector3d.zero);

            bool together = true;

            for (int i = 0; i < 32; i++)
            {
                if (views[0].IsValid() && views[1].IsValid())
                {
                    views[0].TeleportTo(Vector3d.right * (together ? 0 : OFFSET_DISTANCE));
                    views[1].TeleportTo(-Vector3d.right * (together ? 0 : OFFSET_DISTANCE));

                    int desync_count = 0;

                    while (views[0].gameObject.scene.handle != views[1].gameObject.scene.handle && desync_count < 100)
                    {
                        yield return new WaitForFixedUpdate();
                        desync_count++;
                    }
                    if (desync_count >= 10)
                    {
                        Debug.LogWarning($"Views failed to merge, still in different scenes after {desync_count} frames.");
                    }

                    if (together && views[0].IsValid() && views[1].IsValid())
                    {
                        Assert.AreEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);
                        // Assert.AreEqual(views[0].gameObject.scene.handle, staticObject.gameObject.scene.handle);
                    }
                    together = !together;
                }
            }

            Debug.Log($"Final real position of staticObject: {staticObject.GetRealPosition()}");
        }

        [UnityTest]
        public IEnumerator StragglersVsGroup()
        {
            OffsetView[] views = new OffsetView[4];
            OffsetView initialView = null;
            OffsetView staticObject = null;

            // 1. Find the initial view and the static object efficiently
            while (initialView == null || staticObject == null)
            {
                OffsetView[] objects = UnityEngine.Object.FindObjectsOfType<OffsetView>();

                foreach (var obj in objects)
                {
                    if (initialView == null)
                    {
                        initialView = obj;
                    }
                    else
                    {
                        staticObject = obj;
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }

            views[0] = initialView;
            var viewGameObject = initialView.gameObject;

            // 2. Instantiate and spawn the remaining views (views[1] and views[2] in this case)
            for (int i = 1; i < views.Length; i++)
            {
                views[i] = GameObject.Instantiate(viewGameObject).GetComponent<OffsetView>();
                networkManager.ServerManager.Spawn(views[i].GetComponent<NetworkObject>());
            }

            staticObject.transform.position = Vector3.one;

            // Give the network and scene manager a moment to synchronize the new objects
            yield return new WaitForSeconds(1f);
            Debug.Log("Starting test");

            Assert.AreEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);

            // 3. Move the first two views far away together
            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                views[0].transform.position += Vector3.right * 100;
                views[1].transform.position += Vector3.right * 100;

                int desync_count = 0;

                while (views[0].gameObject.scene.handle != views[1].gameObject.scene.handle && desync_count < 100)
                {
                    yield return new WaitForFixedUpdate();
                    desync_count++;
                }
                if (desync_count >= 10)
                {
                    Debug.LogWarning($"Views failed to merge, still in different scenes after {desync_count} frames.");
                }

                Assert.AreEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);
            }

            views[1].transform.position = Vector3.right * 10000;

            int desync_count_separate = 0;

            while (views[0].gameObject.scene.handle == views[1].gameObject.scene.handle && desync_count_separate < 100)
            {
                yield return new WaitForFixedUpdate();
                desync_count_separate++;
            }
            if (desync_count_separate >= 10)
            {
                Debug.LogWarning($"Views failed to separate, still in same scene after {desync_count_separate} frames.");
            }


            Assert.AreNotEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);
            Assert.AreEqual(views[0].gameObject.scene.handle, staticObject.gameObject.scene.handle);
        }

        [UnityTest]
        public IEnumerator MergeTestOffline()
        {
            OffsetView[] views = new OffsetView[2];
            OffsetView initialView = null;
            OffsetView controlObject = null;

            while (initialView == null || controlObject == null)
            {
                initialView = FindView();
                OffsetView[] objects = UnityEngine.Object.FindObjectsOfType<OffsetView>();

                foreach (var obj in objects)
                {
                    if (controlObject == null)
                    {
                        controlObject = obj;
                    }
                }
                yield return new WaitForSeconds(2);
            }

            views[0] = initialView;
            var viewGameObject = initialView.gameObject;

            for (int i = 1; i < views.Length; i++)
            {
                views[i] = GameObject.Instantiate(viewGameObject).GetComponent<OffsetView>();
                networkManager.ServerManager.Spawn(views[i].GetComponent<NetworkObject>());
            }

            controlObject.transform.position = Vector3.one;

            yield return new WaitForSeconds(1);
            Debug.Log("Starting test");

            yield return MergeTestLogic(views[0], views[1]);
        }

        private IEnumerator MergeTestLogic(OffsetView test, OffsetView control)
        {
            test.transform.position = Vector3.zero;
            control.transform.position = Vector3.zero;

            // Wait one frame to ensure the system registers the initial placement
            yield return null;

            Assert.AreEqual(control.gameObject.scene, test.gameObject.scene, "Objects should start in the same scene.");

            // Separate objects
            Vector3 largeOffset = new Vector3(OFFSET_DISTANCE * 2, 0, 0);
            test.transform.position += largeOffset;

            bool inDifferentScenes = false;
            bool testIsRebased = false;
            bool controlIsRebased = false;

            // Wait up to 5 frames for the system to rebase the offsetviews
            for (int i = 0; i < 10; i++)
            {
                yield return null;

                inDifferentScenes = test.gameObject.scene != control.gameObject.scene;
                testIsRebased = test.transform.position.magnitude <= 10f;
                controlIsRebased = control.transform.position.magnitude <= 10f;

                Debug.Log($"({i}) In different scenes: {inDifferentScenes} Test Rebased: {testIsRebased} Control Rebased: {controlIsRebased}");

                if (inDifferentScenes && testIsRebased && controlIsRebased)
                    break;

            }

            Assert.IsTrue(inDifferentScenes, "The views were not in separate scenes within 5 frames");
            Assert.IsTrue(testIsRebased, $"The test view is not where it should be, was {test.transform.position}");
            Assert.IsTrue(controlIsRebased, $"The control view is not where it should be, was {control.transform.position}");


            // Rejoin objects
            test.transform.position -= largeOffset;

            inDifferentScenes = false;
            testIsRebased = false;
            controlIsRebased = false;

            // Wait up to 5 frames for everything to end up in the same offset scene
            for (int i = 0; i < 10; i++)
            {
                yield return null;

                inDifferentScenes = test.gameObject.scene != control.gameObject.scene;
                testIsRebased = test.transform.position.magnitude <= 10f;
                controlIsRebased = control.transform.position.magnitude <= 10f;

                Debug.Log($"({i}) In different scenes: {inDifferentScenes} Test Rebased: {testIsRebased} Control Rebased: {controlIsRebased}");

                if (!inDifferentScenes && testIsRebased && controlIsRebased)
                    break;
            }

            Assert.IsFalse(inDifferentScenes, "The views were still in separate scenes after rejoining, even after 5 frames");
            Assert.IsTrue(testIsRebased, $"The test view is not where it should be, was {test.transform.position}");
            Assert.IsTrue(controlIsRebased, $"The control view is not where it should be, was {control.transform.position}");
        }

        private OffsetView FindView()
        {
            var transforms = UnityEngine.Object.FindObjectsOfType<OffsetView>();
            foreach (OffsetView transform in transforms)
            {

                return transform;

            }
            return null;
        }
    }
}
#endif