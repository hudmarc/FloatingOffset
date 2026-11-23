using System;
using System.Collections.Generic;
using FloatingOffset.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace FloatingOffset.Editor.Tests
{
    public class OffsetProcessorTests
    {
        private const int INITIAL_SCENE_KEY = 100;
        private const int REBASE_CRITERIA = 2048;
        private const int MAX_SCENES = 100;

        [Test]
        public void OffsetProcessorInitialization()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            OffsetProcessor<int> server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);
            Assert.NotNull(server);
        }

        [Test]
        public void BasicApiMethods_Test()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            OffsetProcessor<int> server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);

            // 1. RegisterView & HasScene
            server.RegisterView(view);
            server.Process();
            Assert.IsTrue(server.HasScene(INITIAL_SCENE_KEY), "Server should track the initial scene after processing.");

            // 2. GetSceneViewCount
            Assert.AreEqual(1, server.GetSceneViewCount(INITIAL_SCENE_KEY), "Scene should contain exactly 1 registered view.");

            // 3. GetSceneOffset
            Vector3d currentOffset = server.GetSceneOffset(INITIAL_SCENE_KEY);
            Assert.AreEqual(Vector3d.zero, currentOffset, "Initial offset should be zero.");

            // Move the view out of bounds to trigger a rebase
            view.SetEnginePosition(new Vector3d(5000, 0, 0));
            server.Process();

            Assert.AreNotEqual(Vector3d.zero, server.GetSceneOffset(INITIAL_SCENE_KEY), "Scene offset should have updated after rebasing.");

            // 4. UnregisterView
            server.UnregisterView(view);
            server.Process();
            Assert.AreEqual(0, server.GetSceneViewCount(INITIAL_SCENE_KEY), "Scene view count should be 0 after unregistration.");
        }

        [Test]
        public void OffsetTest()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(view);
            mock_handler.Add(view);
            Vector3d targetTruePos = Vector3d.zero;
            double moveAmount = 10.0;

            for (int i = 0; i < 128; i++)
            {
                moveAmount *= 1.1; // Exponential growth
                Vector3d delta = new Vector3d(moveAmount, 0, 0);

                targetTruePos += delta;
                view.SetEnginePosition(view.GetEnginePosition() + delta);

                server.Process();

                Vector3d calculatedTruePos = mock_handler.RealPosition(view);
                double error = Vector3d.Distance(calculatedTruePos, targetTruePos);

                Assert.Less(error, 2.0, $"Error exceeded 2.0 units at iteration {i}. Expected: {targetTruePos}, Got: {calculatedTruePos}");

                // Explicitly fail if rebasing didn't happen when threshold crossed
                if (targetTruePos.x > REBASE_CRITERIA)
                {
                    Assert.LessOrEqual(Math.Abs(view.GetEnginePosition().x), REBASE_CRITERIA + moveAmount, "Rebasing failed to keep local EnginePosition small.");
                }
            }
        }

        [Test]
        public void ErrorAccumulator()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(view);
            mock_handler.Add(view);

            Vector3d targetTruePos = Vector3d.zero;
            double jumpDistance = 20000.0;

            for (int i = 0; i < 100; i++)
            {
                // Jump Forward
                Vector3d delta = new Vector3d(jumpDistance, 0, 0);
                view.SetEnginePosition(view.GetEnginePosition() + delta);
                targetTruePos += delta;
                server.Process();

                // Jump Backward
                view.SetEnginePosition(view.GetEnginePosition() - delta);
                targetTruePos -= delta;
                server.Process();

                Vector3d calculatedTruePos = mock_handler.RealPosition(view);
                double error = Vector3d.Distance(calculatedTruePos, targetTruePos);

                Assert.Less(error, 0.01, $"Floating-point drift accumulated beyond 0.01 margin at loop {i}.");
            }
        }

        [Test]
        public void MultipleViewsSameClient()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            const int VIEWS = 50;
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, VIEWS);


            MockOffsetObject[] views = new MockOffsetObject[VIEWS];
            Vector3d[] targetPositions = new Vector3d[VIEWS];

            for (int i = 0; i < VIEWS; i++)
            {
                views[i] = new MockOffsetObject(INITIAL_SCENE_KEY);
                targetPositions[i] = Vector3d.zero;
                server.RegisterView(views[i]);
                mock_handler.Add(views[i]);
            }

            double moveAmount = 5.0;

            for (int loop = 0; loop < 50; loop++)
            {
                moveAmount *= 1.2;

                for (int i = 0; i < VIEWS; i++)
                {
                    // Move them in different directions to ensure distinct spaces
                    Vector3d dir = new Vector3d(i % 2 == 0 ? 1 : -1, (i / 2) % 2 == 0 ? 1 : -1, i > 3 ? 1 : -1);
                    Vector3d delta = dir * moveAmount;

                    views[i].SetEnginePosition(views[i].GetEnginePosition() + delta);
                    targetPositions[i] += delta;
                }
                server.Process();
                server.Process();

                for (int i = 0; i < VIEWS; i++)
                {
                    double absolute_error = Vector3d.Distance(mock_handler.RealPosition(views[i]), targetPositions[i]);
                    double local_error = Vector3d.Magnitude(views[i].GetEnginePosition()); // Local distance from origin

                    Assert.Less(absolute_error, 2.0, $"View {i} deviated in real space. Error: {absolute_error}");
                    if (local_error >= 4000)
                    {
                        Debug.Log($"Error {local_error} on iteration {loop} on view {i}: {mock_handler.RealPosition(views[i])} {Math.Floor(Vector3d.Magnitude(mock_handler.RealPosition(views[i])))}, engine: {views[i].GetEnginePosition()} {Math.Floor(Vector3d.Magnitude(views[i].GetEnginePosition()))}, offset: {mock_handler.SceneOffsets[views[i].GetSceneKey()]}");
                        // for (int j = 0; j < VIEWS; j++)
                        // {
                        //     Debug.Log($" View {j}: {mock_handler.RealPosition(views[j])} {views[j].GetSceneKey()}");
                        // }
                    }
                    Assert.Less(local_error, 5000.0, $"View {i} is off-center in engine space. Local magnitude: {local_error}");
                }
            }
        }

        [Test]
        public void MultipleViewsSameClientStressTestCloseTogether()
        {
            RunStressTestSuite(
                label: "CloseTogether",
                spread: 1,
                maxSumMs: 40000
            );
        }

        [Test]
        public void MultipleViewsSameClientStressTestSpreadOut()
        {
            RunStressTestSuite(
                label: "SpreadOut",
                spread: 20000,
                maxSumMs: 40000
            );
        }

        private void RunStressTestSuite(string label, int spread, double maxSumMs)
        {
            const double frameBudgetMs = 4.0;

            int count = 1;
            var lastThreeFrames = new Queue<double>(3);
            var frameTimes = new List<double>(60);

            double worstFrame = 0;
            int worstCount = 0;
            int worstFrameIndex = -1;
            double bestFrame = double.MaxValue;
            int bestCount = 0;
            double sumAllFrames = 0;
            int totalFramesRecorded = 0;
            int spikeCount = 0;
            int skippedWarmupSpikes = 0;

            OffsetProcessor<int> server = null;

            double GetLastThreeAverage()
            {
                if (lastThreeFrames.Count == 0)
                    return 0.0;

                double total = 0.0;
                foreach (double t in lastThreeFrames)
                    total += t;

                return total / lastThreeFrames.Count;
            }

            while (GetLastThreeAverage() < frameBudgetMs && count < 1000 && sumAllFrames < maxSumMs)
            {
                count++;
                int viewsCount = count * 20;

                server = spread == 1
                    ? StressTest(viewsCount, out frameTimes)
                    : StressTest(viewsCount, spread, out frameTimes);

                for (int f = 0; f < frameTimes.Count; f++)
                {
                    double frameMs = frameTimes[f];
                    double priorAvg = GetLastThreeAverage();

                    sumAllFrames += frameMs;
                    totalFramesRecorded++;

                    if (frameMs < bestFrame)
                    {
                        bestFrame = frameMs;
                        bestCount = viewsCount;
                    }
                    if (frameMs > worstFrame)
                    {
                        worstFrame = frameMs;
                        worstCount = viewsCount;
                        worstFrameIndex = f;
                    }

                    if (frameMs > frameBudgetMs * 2)
                    {
                        if (f == 0)
                        {
                            skippedWarmupSpikes++;
                        }
                        else
                        {
                            spikeCount++;
                            Debug.LogWarning(
                                $"[{label}] SPIKE #{spikeCount} @ {viewsCount} players (frame {f}): " +
                                $"{frameMs:F3}ms (prior 3-frame avg: {priorAvg:F3}ms) Below avg, in ms"
                            );

                            if (server != null && server.subloop_count > 0)
                            {
                                var sb = new System.Text.StringBuilder();
                                for (int i = 0; i < server.subloop_count; i++)
                                {
                                    if (i > 0) sb.Append(" | ");
                                    var runtime = server.averageRuntime(i);
                                    sb.Append($"{runtime.Item1}: {runtime.Item2:0.####}");
                                }

                                Debug.Log(sb.ToString());
                            }
                        }
                    }

                    if (lastThreeFrames.Count == 3)
                        lastThreeFrames.Dequeue();
                    lastThreeFrames.Enqueue(frameMs);
                }
            }

            double finalLastThreeAvg = GetLastThreeAverage();
            double overallAvg = totalFramesRecorded > 0 ? sumAllFrames / totalFramesRecorded : 0.0;

            Debug.Log(
                $"[{label}] Stopped @ {count * 20} players with last-3-frame avg {finalLastThreeAvg:F3}ms.\n" +
                $"Average (all frames): {overallAvg:F3}ms\n" +
                $"Worst: {worstFrame:F3}ms @ {worstCount} players (frame {worstFrameIndex})\n" +
                $"Best: {bestFrame:F3}ms @ {bestCount} players\n" +
                $"Spikes (> {frameBudgetMs:F1}ms) printed: {spikeCount}\n" +
                $"Skipped printing {skippedWarmupSpikes} warmup frames"
            );

            if (server != null)
            {
                Debug.Log($"--- DETAILED PROFILE (RUN-AVG @ {count * 20} players) ---");
                for (int i = 0; i < server.subloop_count; i++)
                {
                    var runtime = server.averageRuntime(i);
                    Debug.Log($"{runtime.Item1}: {runtime.Item2}ms avg");
                }
            }
        }
        OffsetProcessor<int> StressTest(int VIEWS, out List<double> frameTimes)
        {
            return StressTest(VIEWS, 1, out frameTimes);
        }

        OffsetProcessor<int> StressTest(int VIEWS, int SPREAD, out List<double> frameTimes)
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, VIEWS);
            var frameSw = new System.Diagnostics.Stopwatch();
            frameTimes = new List<double>(60);

            MockOffsetObject[] views = new MockOffsetObject[VIEWS];
            Vector3d[] targetPositions = new Vector3d[VIEWS];

            for (int i = 0; i < VIEWS; i++)
            {
                views[i] = new MockOffsetObject(INITIAL_SCENE_KEY);
                targetPositions[i] = Vector3d.zero;
                server.RegisterView(views[i]);
                mock_handler.Add(views[i]);
            }

            double moveAmount = 5.0;

            for (int loop = 0; loop < 60; loop++)
            {
                moveAmount *= 1.2;

                frameSw.Restart();
                for (int i = 0; i < VIEWS; i++)
                {
                    // Move them in different directions to ensure distinct spaces
                    Vector3d dir = new Vector3d(i % 2 == 0 ? SPREAD : -SPREAD, (i / 2) % 2 == 0 ? SPREAD : -SPREAD, i > 3 ? SPREAD : -SPREAD) * (1 + (i * (SPREAD - 1)));
                    Vector3d delta = dir * moveAmount;

                    views[i].SetEnginePosition(views[i].GetEnginePosition() + delta);
                    targetPositions[i] += delta;
                }

                server.Process();
                frameSw.Stop();
                frameTimes.Add(frameSw.Elapsed.TotalMilliseconds);
            }

            return server;
        }


        [Test]
        public void OffsetTransformGroupChange()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);

            var viewA = new MockOffsetObject(INITIAL_SCENE_KEY); //INITIAL_SCENE_KEY =100
            var viewB = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(viewA);
            server.RegisterView(viewB);
            mock_handler.Add(viewA);
            mock_handler.Add(viewB);
            server.Process();

            // 1. Massive separation
            viewA.SetEnginePosition(new Vector3d(100000, 0, 0));
            // simulated scene loading
            for (int i = 0; i < 5; i++)
            {
                server.Process();
            }

            Assert.AreNotEqual(viewA.GetSceneKey(), viewB.GetSceneKey(), "Views failed to separate into different scenes when moved far apart.");

            // 2. Snap back together
            // To properly snap back, we set their absolute world positions to match.
            // Since ViewB is at zero, we move ViewA's true position to zero.
            Vector3d currentA_True = mock_handler.RealPosition(viewA);
            viewA.SetEnginePosition(viewA.GetEnginePosition() - currentA_True); // Local move to negate world offset
            server.Process();

            Assert.AreEqual(viewA.GetSceneKey(), viewB.GetSceneKey(), "Views failed to merge back into the same scene handle.");
        }

        [Test]
        public void StragglersVsGroup()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);

            var control = new MockOffsetObject(INITIAL_SCENE_KEY);
            var v0 = new MockOffsetObject(INITIAL_SCENE_KEY);
            var v1 = new MockOffsetObject(INITIAL_SCENE_KEY);
            var v2 = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(control);
            server.RegisterView(v0);
            server.RegisterView(v1);
            server.RegisterView(v2);

            mock_handler.Add(control);
            mock_handler.Add(v0);
            mock_handler.Add(v1);
            mock_handler.Add(v2);

            // Move v0 and v1 together massively
            Vector3d moveDelta = new Vector3d(50000, 0, 0);
            v0.SetEnginePosition(v0.GetEnginePosition() + moveDelta);
            v1.SetEnginePosition(v1.GetEnginePosition() + moveDelta);

            //simulated delay
            for (int i = 0; i < 5; i++)
            {
                server.Process();
            }

            // v0 and v1 must be in the same scene
            Assert.AreEqual(v0.GetSceneKey(), v1.GetSceneKey(), "V0 and V1 should be grouped together.");

            // v2 must not be in their scene
            Assert.AreNotEqual(v2.GetSceneKey(), v0.GetSceneKey(), "V2 should have been left behind in a separate scene.");

            // v2 must remain with the control
            Assert.AreEqual(v2.GetSceneKey(), control.GetSceneKey(), "V2 should remain grouped with the static control object.");
        }

        [Test]
        public void MergeTestOffline()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);

            var control = new MockOffsetObject(INITIAL_SCENE_KEY);
            var erraticView = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(control);
            server.RegisterView(erraticView);

            mock_handler.Add(control);

            mock_handler.Add(erraticView);

            int consecutiveCorruptedFrames = 0;
            Vector3d controlTrueTarget = Vector3d.zero;

            for (int i = 0; i < 200; i++)
            {
                // Violent, erratic modulo movement
                double xJump = (i % 3 == 0) ? 80000.0 : ((i % 5 == 0) ? -60000.0 : 500.0);
                erraticView.SetEnginePosition(erraticView.GetEnginePosition() + new Vector3d(xJump, 0, 0));

                server.Process();

                // Check Control View integrity
                double error = Vector3d.Distance(mock_handler.RealPosition(control), controlTrueTarget);

                if (error > 10.0)
                {
                    consecutiveCorruptedFrames++;
                }
                else
                {
                    consecutiveCorruptedFrames = 0; // Reset on recovery
                }

                if (consecutiveCorruptedFrames > 5)
                {
                    Assert.Fail($"Control object lost coordinate sync due to another view's erratic movement. Desynced for {consecutiveCorruptedFrames} frames. Error amount: {error}");
                }
            }
        }

        [Test]
        public void GetNearbySceneTest_InitialState()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);

            server.Process();

            int invalid = server.GetNearbyScene(Vector3d.zero);

            Assert.AreEqual(0, invalid, "Initial scene is expected to be default (0) if no views are yet registered.");

            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            server.RegisterView(view);
            mock_handler.Add(view);

            server.Process(); // Call Process once to build hash grids

            // Edge Case: Querying when only the initial scene exists at zero position.
            int nearest = server.GetNearbyScene(Vector3d.zero);

            Assert.AreEqual(INITIAL_SCENE_KEY, nearest, "Initial scene should be the nearest scene to origin.");
        }

        [Test]
        public void GetNearbySceneTest_MultipleSplitScenes()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);

            var viewA = new MockOffsetObject(INITIAL_SCENE_KEY);
            var viewB = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(viewA);
            server.RegisterView(viewB);
            mock_handler.Add(viewA);
            mock_handler.Add(viewB);

            server.Process(); // Call Process once to build hash grids

            // Move viewA far away to trigger a split into a new scene
            viewA.SetEnginePosition(new Vector3d(100000, 0, 0));
            for (int i = 0; i < 5; i++)
            {
                server.Process();
            }

            int sceneA = viewA.GetSceneKey();
            int sceneB = viewB.GetSceneKey();
            Assert.AreNotEqual(sceneA, sceneB, "Views failed to separate into distinct scenes.");

            // Edge Case: Querying a position adjacent to View A's real-space coordinate
            Vector3d realPosA = mock_handler.RealPosition(viewA);
            int nearestToA = server.GetNearbyScene(realPosA);
            Assert.AreEqual(sceneA, nearestToA, "GetNearestScene should resolve to sceneA when queried near viewA.");

            // Edge Case: Querying a position adjacent to View B's real-space coordinate
            Vector3d realPosB = mock_handler.RealPosition(viewB);
            int nearestToB = server.GetNearbyScene(realPosB);
            Assert.AreEqual(sceneB, nearestToB, "GetNearestScene should resolve to sceneB when queried near viewB.");
        }

        [Test]
        public void GetNearbySceneTest_MidpointBetweenScenes()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetProcessor<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);

            var viewA = new MockOffsetObject(INITIAL_SCENE_KEY);
            var viewB = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(viewA);
            server.RegisterView(viewB);
            mock_handler.Add(viewA);
            mock_handler.Add(viewB);
            server.Process();

            // Separate scenes along the X axis
            viewA.SetEnginePosition(new Vector3d(20000, 0, 0));
            for (int i = 0; i < 5; i++)
            {
                server.Process();
            }

            int sceneA = viewA.GetSceneKey();
            int sceneB = viewB.GetSceneKey();

            Vector3d realPosA = mock_handler.RealPosition(viewA);
            Vector3d realPosB = mock_handler.RealPosition(viewB);

            // Edge Case: Querying the exact real-space midpoint between two active scenes
            Vector3d midpoint = (realPosA + realPosB) * 0.5;
            int nearestAtMidpoint = server.GetNearbyScene(midpoint);

            Assert.AreNotEqual(sceneA, nearestAtMidpoint, "Midpoint query must not resolve to scene A");
            Assert.AreNotEqual(sceneB, nearestAtMidpoint, "Midpoint query must not resolve to scene B");
        }
    }
}