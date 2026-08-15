using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    // Loosely based on the Unity Wiki FloatingOrigin script by Peter Stirling
    // URL: http://wiki.unity3d.com/index.php/Floating_Origin
    public class EffectOffsetter : Offsetter
    {
        private ParticleSystem[] worldSpaceParticles;
        private LineRenderer[] worldSpaceLines;
        private TrailRenderer[] trails;

        // A shared, reusable buffer to prevent memory allocations during the hot path.
        // It will automatically grow if a trail or line has more vertices than its current size.
        private static Vector3[] vertexBuffer = new Vector3[2048];
        private Scene scene = default;

        private void Awake()
        {
            CacheWorldSpaceParticles();
            CacheWorldSpaceLines();
            CacheTrails();
            scene = gameObject.scene;
        }
        protected override void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene, IOffsettable<Scene>[] offsettables)
        {
            base.OnOffset(old_offset, new_offset, scene, offsettables);
            Vector3 delta = UnityFunctions.toVector3(new_offset - old_offset);
            // 2. Move world-space visual components
            if (worldSpaceParticles.Length > 0) ShiftWorldParticles(delta);
            if (worldSpaceLines.Length > 0) ShiftLines(delta);
            if (trails.Length > 0) ShiftTrails(delta);
        }

        private void CacheWorldSpaceParticles()
        {
            var allParticles = GetComponentsInChildren<ParticleSystem>(true);
            var list = new List<ParticleSystem>();

            for (int i = 0; i < allParticles.Length; i++)
            {
                if (allParticles[i].main.simulationSpace == ParticleSystemSimulationSpace.World)
                {
                    list.Add(allParticles[i]);
                }
            }
            worldSpaceParticles = list.ToArray();
        }

        private void CacheWorldSpaceLines()
        {
            var allLines = GetComponentsInChildren<LineRenderer>(true);
            var list = new List<LineRenderer>();

            for (int i = 0; i < allLines.Length; i++)
            {
                if (allLines[i].useWorldSpace)
                {
                    list.Add(allLines[i]);
                }
            }
            worldSpaceLines = list.ToArray();
        }

        private void CacheTrails()
        {
            // TrailRenderers live in world space, so we need all of them.
            trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        private void ShiftWorldParticles(Vector3 offset)
        {
            for (int i = 0; i < worldSpaceParticles.Length; i++)
            {
                ParticleSystem ps = worldSpaceParticles[i];
                if (!ps.isPlaying && ps.particleCount == 0) continue;

                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
                int count = ps.GetParticles(particles);

                for (int p = 0; p < count; p++)
                {
                    particles[p].position += offset;
                }

                ps.SetParticles(particles, count);
            }
        }

        private void ShiftLines(Vector3 offset)
        {
            for (int i = 0; i < worldSpaceLines.Length; i++)
            {
                LineRenderer line = worldSpaceLines[i];
                int count = line.positionCount;
                if (count == 0) continue;

                EnsureBufferSize(count);

                line.GetPositions(vertexBuffer);
                for (int p = 0; p < count; p++)
                {
                    vertexBuffer[p] += offset;
                }
                line.SetPositions(vertexBuffer);
            }
        }

        private void ShiftTrails(Vector3 offset)
        {
            for (int i = 0; i < trails.Length; i++)
            {
                TrailRenderer trail = trails[i];
                int count = trail.positionCount;
                if (count == 0) continue;

                EnsureBufferSize(count);

                trail.GetPositions(vertexBuffer);
                for (int p = 0; p < count; p++)
                {
                    vertexBuffer[p] += offset;
                }
                trail.SetPositions(vertexBuffer);
            }
        }

        // Ensures the static array is large enough to hold the vertices without allocating every frame
        private void EnsureBufferSize(int requiredSize)
        {
            if (vertexBuffer.Length < requiredSize)
            {
                // Double the size until it fits, preventing frequent re-allocations
                int newSize = vertexBuffer.Length;
                while (newSize < requiredSize)
                {
                    newSize *= 2;
                }
                vertexBuffer = new Vector3[newSize];
            }
        }
    }
}
