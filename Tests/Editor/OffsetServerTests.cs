using System;
using FloatingOffset.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace FloatingOffset.Editor.Tests
{
    public class OffsetServerTests
    {
        private const int INITIAL_SCENE_KEY = 100;
        private const int REBASE_CRITERIA = 2048;
        private const int MAX_SCENES = 100;

        [Test]
        public void OffsetServerInitialization()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            OffsetServer<int> server = new OffsetServer<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);
            Assert.NotNull(server);
        }

        [Test]
        public void BasicApiMethods_Test()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            OffsetServer<int> server = new OffsetServer<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);
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
            var server = new OffsetServer<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(view);
            mock_handler.TrackedObjects.Add(view);
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
            var server = new OffsetServer<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(view);
            mock_handler.TrackedObjects.Add(view);

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
            var server = new OffsetServer<int>(mock_handler, REBASE_CRITERIA, VIEWS);


            MockOffsetObject[] views = new MockOffsetObject[VIEWS];
            Vector3d[] targetPositions = new Vector3d[VIEWS];

            for (int i = 0; i < VIEWS; i++)
            {
                views[i] = new MockOffsetObject(INITIAL_SCENE_KEY);
                targetPositions[i] = Vector3d.zero;
                server.RegisterView(views[i]);
                mock_handler.TrackedObjects.Add(views[i]);
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

                for (int i = 0; i < VIEWS; i++)
                {
                    double error = Vector3d.Distance(mock_handler.RealPosition(views[i]), targetPositions[i]);
                    Assert.Less(error, 2.0, $"View {i} deviated from intended position. Error: {error}");
                }
            }
        }

        [Test]
        public void MultipleViewsSameClientStressTestWorstCase()
        {
            int count = 1;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            double worst = 0;
            int worst_count = 0;
            int best_count = 0;
            double best = int.MaxValue;
            double sum = 0;

            while (sw.ElapsedMilliseconds < 240 && count < 2000) //300 = 60 * 4. Assume 4ms budget per simulated frame.
            {
                count++;
                sw.Restart();
                StressTest(count * 20);
                sw.Stop();
                double processing_time = sw.ElapsedMilliseconds;
                if (processing_time < best)
                {
                    best = processing_time;
                    best_count = count;
                }
                if (processing_time > worst)
                {
                    worst = processing_time;
                    worst_count = count;
                }
                sum += processing_time;
            }
            Debug.Log($"Stopped at {count * 20} players with simulated frametime {sw.ElapsedMilliseconds / 60}ms.\nAverage: {(sum / count) / 60f}ms\nWorst: {worst / 60f}ms @ {worst_count * 20} players\nBest: {best / 60f}ms @ {best_count * 20} players");
        }

        [Test]
        public void MultipleViewsSameClientStressTestSpreadOut()
        {
            int count = 1;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            double worst = 0;
            int worst_count = 0;
            int best_count = 0;
            double best = int.MaxValue;
            double sum = 0;

            while (sw.ElapsedMilliseconds < 240 && count < 2000) //300 = 60 * 4. Assume 4ms budget per simulated frame.
            {
                count++;
                sw.Restart();
                StressTest(count * 20, 20000);
                sw.Stop();
                double processing_time = sw.ElapsedMilliseconds;
                if (processing_time < best)
                {
                    best = processing_time;
                    best_count = count;
                }
                if (processing_time > worst)
                {
                    worst = processing_time;
                    worst_count = count;
                }
                sum += processing_time;
            }
            Debug.Log($"Stopped at {count * 20} players with simulated frametime {sw.ElapsedMilliseconds / 60}ms.\nAverage: {(sum / count) / 60f}ms\nWorst: {worst / 60f}ms @ {worst_count * 20} players\nBest: {best / 60f}ms @ {best_count * 20} players");
        }

        void StressTest(int VIEWS, int SPREAD = 1)
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetServer<int>(mock_handler, REBASE_CRITERIA, VIEWS);


            MockOffsetObject[] views = new MockOffsetObject[VIEWS];
            Vector3d[] targetPositions = new Vector3d[VIEWS];

            for (int i = 0; i < VIEWS; i++)
            {
                views[i] = new MockOffsetObject(INITIAL_SCENE_KEY);
                targetPositions[i] = Vector3d.zero;
                server.RegisterView(views[i]);
                mock_handler.TrackedObjects.Add(views[i]);
            }

            double moveAmount = 5.0;

            for (int loop = 0; loop < 60; loop++)
            {
                moveAmount *= 1.2;

                for (int i = 0; i < VIEWS; i++)
                {
                    // Move them in different directions to ensure distinct spaces
                    Vector3d dir = new Vector3d(i % 2 == 0 ? SPREAD : -SPREAD, (i / 2) % 2 == 0 ? SPREAD : -SPREAD, i > 3 ? SPREAD : -SPREAD) * (1 + (i * (SPREAD - 1)));
                    Vector3d delta = dir * moveAmount;

                    views[i].SetEnginePosition(views[i].GetEnginePosition() + delta);
                    targetPositions[i] += delta;
                }

                server.Process();

                for (int i = 0; i < VIEWS; i++)
                {
                    if (SPREAD == 1)
                    {
                        double error = Vector3d.Distance(mock_handler.RealPosition(views[i]), targetPositions[i]);
                        Assert.Less(error, 2.0, $"View {i} deviated from intended position. Error: {error}");
                    }

                }
            }
            // Debug.Log($" Scene count {SceneManager.sceneCount}");
        }


        [Test]
        public void OffsetTransformGroupChange()
        {
            var mock_handler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            var server = new OffsetServer<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);

            var viewA = new MockOffsetObject(INITIAL_SCENE_KEY);
            var viewB = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(viewA);
            server.RegisterView(viewB);
            mock_handler.TrackedObjects.Add(viewA);
            mock_handler.TrackedObjects.Add(viewB);
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
            var server = new OffsetServer<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);

            var control = new MockOffsetObject(INITIAL_SCENE_KEY);
            var v0 = new MockOffsetObject(INITIAL_SCENE_KEY);
            var v1 = new MockOffsetObject(INITIAL_SCENE_KEY);
            var v2 = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(control);
            server.RegisterView(v0);
            server.RegisterView(v1);
            server.RegisterView(v2);

            mock_handler.TrackedObjects.Add(control);
            mock_handler.TrackedObjects.Add(v0);
            mock_handler.TrackedObjects.Add(v1);
            mock_handler.TrackedObjects.Add(v2);

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
            var server = new OffsetServer<int>(mock_handler, REBASE_CRITERIA, MAX_SCENES);

            var control = new MockOffsetObject(INITIAL_SCENE_KEY);
            var erraticView = new MockOffsetObject(INITIAL_SCENE_KEY);

            server.RegisterView(control);
            server.RegisterView(erraticView);

            mock_handler.TrackedObjects.Add(control);

            mock_handler.TrackedObjects.Add(erraticView);

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
    }
}