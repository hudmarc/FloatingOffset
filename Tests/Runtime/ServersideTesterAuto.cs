#if UNITY_EDITOR
using System;
using System.Collections;
using System.Text;
using FishNet.Managing;
using FishNet.Object;
using NUnit.Framework;
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

            Assert.AreEqual(0, universe.state.CountOffsettables());
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

            GameObject.Destroy(view.gameObject);
            GameObject.Destroy(origin.gameObject);

            yield return null; //one frame

            Assert.AreEqual(0, universe.state.CountOffsettables());
            Assert.AreEqual(0, universe.manager.CountRegisteredViews());

            yield return null; //one frame

            Assert.AreEqual(0, universe.manager.CountViews());
        }


        [UnityTest]
        public IEnumerator OffsetTest()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Step; Error (mm);Error at Origin (meters); Distance; Delta");

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

                var distanceFromOrigin = Vector3d.Distance(Vector3d.zero, view.GetRealPosition());
                var errorAtOrigin = Vector3.Distance(Vector3.zero, origin.transform.position);

                sb.Append($"{i};{error * 1000};{errorAtOrigin};{distanceFromOrigin};{val}\n");
            }

            Debug.Log("--------RESULTS--------");
            Debug.Log(Application.persistentDataPath + "/output.csv");
            System.IO.File.WriteAllText(Application.persistentDataPath + "/output.csv", sb.ToString());

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

                    double error = Vector3d.Distance(expectedPositions[viewIndex], currentView.GetRealPosition());
                    while (error > 2.0)
                    {
                        Debug.LogWarning($"Precision failure on iteration {i}. View {viewIndex} is off by {error} units.");
                        yield return new WaitForEndOfFrame();
                        error = Vector3d.Distance(expectedPositions[viewIndex], currentView.GetRealPosition());
                        error_frames++;
                    }
                    Assert.Less(error, 2.0, $"Precision failure on iteration {i}. View {viewIndex} is off by {error} units.");
                    Debug.Log($"Iteration {i} passed. View {viewIndex} tracking perfectly at {currentView.GetRealPosition()}");
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

        /// <summary>
        /// Extracted logic for the merge test to prevent testing framework confusion.
        /// </summary>
        private IEnumerator MergeTestLogic(OffsetView test, OffsetView control)
        {
            test.transform.position = Vector3.zero;
            control.transform.position = Vector3.zero;

            Assert.AreEqual(control.gameObject.scene, test.gameObject.scene);
            Vector3d controlReal = control.GetRealPosition();

            Vector3 move = Vector3.zero;
            int desyncFrameCount = 0;

            for (int i = 0; i < 32; i++)
            {
                if (test == null) break;

                if (i % 2 != 0)
                {
                    move = new Vector3(((i % 29) * OFFSET_DISTANCE) + i, ((i % 31) * OFFSET_DISTANCE) + i, ((i % 37) * OFFSET_DISTANCE) + i);
                    if (test.GetRealPosition() != Vector3d.zero)
                    {
                        Vector3d offset = universe.GetOffset(test.gameObject.scene);
                        test.transform.position = UnityFunctions.RealToUnity(Vector3d.zero, offset);
                    }
                }
                else
                {
                    move = -move;
                }

                test.transform.position += move;

                if (control.IsValid())
                {
                    if (Vector3d.Magnitude(controlReal - control.GetRealPosition()) > 10)
                    {
                        desyncFrameCount++;
                    }
                    else
                    {
                        desyncFrameCount = 0;
                    }

                    if (desyncFrameCount > 5)
                    {
                        throw new Exception("Desynchronization lasted for more than 5 frames!");
                    }
                    yield return null;
                }
            }
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