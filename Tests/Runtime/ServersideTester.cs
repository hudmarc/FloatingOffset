#if UNITY_EDITOR
using System.Collections;
using FloatingOffset.Runtime;
using FishNet;
using FishNet.Object;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FishNet.Managing;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// These tests require a manually connected client to function.
/// I recommend using a tool like Parrelsync to test this locally.
/// </summary>
public class ServersideTester
// : NetworkTestFixture <-- Recommended: Inherit your setup/teardown from a base class
{
    private OffsetManager manager;
    private OffsetUniverse universe;
    private NetworkManager networkManager;
    private const float OFFSET_DISTANCE = 20000;
    private const float TEST_ITERATIONS = 128;
    public const string TEST_SCENE_NAME = "Offline Automated Testing Scene";
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

        while (manager == null)
        {
            var offsetScene = GameObject.Find("OffsetScene");
            if (offsetScene != null)
            {
                manager = offsetScene.GetComponent<OffsetManager>();
            }
            yield return new WaitForFixedUpdate();
        }

        universe = manager.universe;
        Debug.Log("------- Setup complete -------");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Debug.LogWarning("------- Starting test teardown -------");

        if (networkManager != null)
        {
            if (networkManager.ClientManager.Started)
                networkManager.ClientManager.StopConnection();

            if (networkManager.ServerManager.Started)
                networkManager.ServerManager.StopConnection(true);
        }

        // Allow FishNet time to clean up sockets and objects
        yield return new WaitForSeconds(0.2f);

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
    private const float WAIT_FOR_CLIENT_TIMEOUT = 10f;

    [UnityTest]
    public IEnumerator NetworkedMergeUntilFailServer()
    {
        yield return WaitForClientsAndRunMerge(true);
    }

    [UnityTest]
    public IEnumerator NetworkedMergeUntilFailClient()
    {
        yield return WaitForClientsAndRunMerge(false);
    }

    /// <summary>
    /// Core logic for polling the client connection and executing the merge.
    /// </summary>
    /// <param name="serverIsActiveView">If true, the server view moves. If false, the client view moves.</param>
    private IEnumerator WaitForClientsAndRunMerge(bool serverIsActiveView)
    {
        // 1. Explicitly fail the test if the environment is broken. Do not fail silently.
        if (!InstanceFinder.IsServerStarted)
        {
            Assert.Fail("Server is not started. Ensure your test bootstrap initialized the network.");
        }

        Debug.Log($"Waiting up to {WAIT_FOR_CLIENT_TIMEOUT} seconds for a manual client to connect...");

        OffsetView serverView = null;
        OffsetView clientView = null;
        float timer = 0f;

        // 2. Poll for the required state rather than waiting a rigid amount of time.
        while (timer < WAIT_FOR_CLIENT_TIMEOUT)
        {
            var views = UnityEngine.Object.FindObjectsOfType<OffsetView>();

            // Wait until we have at least 2 views (Host + Client)
            if (views.Length >= 2)
            {
                foreach (OffsetView view in views)
                {
                    if (view.TryGetComponent<NetworkObject>(out var networkObject))
                    {
                        if (networkObject.IsOwner)
                        {
                            serverView = view;
                        }
                        else
                        {
                            clientView = view;
                        }
                    }
                }

                if (serverView != null && clientView != null)
                {
                    Debug.Log($"Client detected after {timer:F2} seconds.");
                    break;
                }
            }

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 3. Ensure we actually found both objects before proceeding to the math test.
        Assert.NotNull(serverView, "Timed out waiting for the server view to establish.");
        Assert.NotNull(clientView, $"Timed out after {WAIT_FOR_CLIENT_TIMEOUT} seconds waiting for a manual client to connect.");
    }
}
#endif