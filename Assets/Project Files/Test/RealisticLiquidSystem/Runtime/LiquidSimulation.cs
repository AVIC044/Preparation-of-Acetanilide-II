using System.Collections.Generic;
using UnityEngine;

namespace RealisticLiquidSystem
{
    /// Lightweight CPU SPH-style solver intended as a WebGL-friendly starting point.
    /// Uses a uniform grid for neighbor lookup and does not allocate during FixedUpdate.
    public class LiquidSimulation : MonoBehaviour
    {
        [Header("Simulation")]
        [Min(1)] public int maxParticles = 1200;
        [Min(0.005f)] public float particleRadius = 0.045f;
        [Min(0.01f)] public float smoothingRadius = 0.11f;
        [Min(0.01f)] public float restDensity = 1000f;
        [Min(0f)] public float pressureStiffness = 0.9f;
        [Range(0f, 1f)] public float viscosity = 0.08f;
        public Vector3 gravity = new Vector3(0, -9.81f, 0);
        [Range(10, 120)] public int simulationHz = 60;

        [Header("Container")]
        public Transform container;
        public Bounds localBounds = new Bounds(Vector3.zero, new Vector3(1, 1, 1));

        public FluidParticle[] Particles { get; private set; }
        public int ActiveCount { get; private set; }

        readonly Dictionary<int, List<int>> grid = new Dictionary<int, List<int>>(4096);
        readonly List<int> candidateBuffer = new List<int>(128);
        float cellSize;

        void Awake()
        {
            Particles = new FluidParticle[maxParticles];
            cellSize = smoothingRadius;
        }

        public void Fill(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            int target = Mathf.RoundToInt(maxParticles * normalized);
            ActiveCount = target;

            int side = Mathf.Max(1, Mathf.CeilToInt(Mathf.Pow(target, 1f / 3f)));
            int i = 0;
            for (int y = 0; y < side && i < target; y++)
            for (int z = 0; z < side && i < target; z++)
            for (int x = 0; x < side && i < target; x++)
            {
                Vector3 p = localBounds.min + new Vector3(
                    (x + .5f) * localBounds.size.x / side,
                    (y + .5f) * localBounds.size.y * normalized / Mathf.Max(0.01f, 1f),
                    (z + .5f) * localBounds.size.z / side);
                p.y = Mathf.Lerp(localBounds.min.y + particleRadius, localBounds.max.y - particleRadius, (y + .5f) / side) * normalized
                      + localBounds.min.y * (1f - normalized);
                Particles[i] = new FluidParticle { position = p, velocity = Vector3.zero, active = true };
                i++;
            }
            for (; i < maxParticles; i++) Particles[i].active = false;
        }

        public void ResetSimulation()
        {
            ActiveCount = 0;
            for (int i = 0; i < Particles.Length; i++) Particles[i].active = false;
        }

        void FixedUpdate()
        {
            if (ActiveCount <= 0) return;
            float dt = 1f / Mathf.Max(10, simulationHz);
            BuildGrid();
            CalculateDensityAndPressure();
            Integrate(dt);
        }

        void BuildGrid()
        {
            foreach (var kv in grid) kv.Value.Clear();
            for (int i = 0; i < ActiveCount; i++)
            {
                if (!Particles[i].active) continue;
                int key = Hash(Cell(Particles[i].position));
                if (!grid.TryGetValue(key, out var list))
                {
                    list = new List<int>(16);
                    grid.Add(key, list);
                }
                list.Add(i);
            }
        }

        void CalculateDensityAndPressure()
        {
            float h2 = smoothingRadius * smoothingRadius;
            float poly6 = 315f / (64f * Mathf.PI * Mathf.Pow(smoothingRadius, 9));
            for (int i = 0; i < ActiveCount; i++)
            {
                var pi = Particles[i];
                float density = 0f;
                CollectNeighbors(pi.position);
                for (int n = 0; n < candidateBuffer.Count; n++)
                {
                    var pj = Particles[candidateBuffer[n]];
                    Vector3 d = pi.position - pj.position;
                    float r2 = d.sqrMagnitude;
                    if (r2 < h2) density += poly6 * Mathf.Pow(h2 - r2, 3);
                }
                pi.density = Mathf.Max(0.001f, density * 1000f);
                pi.pressure = Mathf.Max(0f, pressureStiffness * (pi.density - restDensity));
                Particles[i] = pi;
            }
        }

        void Integrate(float dt)
        {
            float h = smoothingRadius;
            float spiky = -45f / (Mathf.PI * Mathf.Pow(h, 6));
            for (int i = 0; i < ActiveCount; i++)
            {
                var pi = Particles[i];
                Vector3 force = gravity * pi.density;
                CollectNeighbors(pi.position);

                for (int n = 0; n < candidateBuffer.Count; n++)
                {
                    int j = candidateBuffer[n];
                    if (j == i) continue;
                    var pj = Particles[j];
                    Vector3 delta = pi.position - pj.position;
                    float r = delta.magnitude;
                    if (r <= 0.0001f || r >= h) continue;

                    Vector3 dir = delta / r;
                    float grad = spiky * Mathf.Pow(h - r, 2);
                    force += -dir * (pi.pressure + pj.pressure) * 0.5f * grad / Mathf.Max(1f, pj.density);

                    Vector3 visc = (pj.velocity - pi.velocity) * viscosity * (h - r);
                    force += visc;
                }

                pi.velocity += force / Mathf.Max(1f, pi.density) * dt;
                pi.position += pi.velocity * dt;

                Vector3 min = localBounds.min + Vector3.one * particleRadius;
                Vector3 max = localBounds.max - Vector3.one * particleRadius;
                if (pi.position.x < min.x) { pi.position.x = min.x; pi.velocity.x *= -0.25f; }
                if (pi.position.y < min.y) { pi.position.y = min.y; pi.velocity.y *= -0.2f; }
                if (pi.position.z < min.z) { pi.position.z = min.z; pi.velocity.z *= -0.25f; }
                if (pi.position.x > max.x) { pi.position.x = max.x; pi.velocity.x *= -0.25f; }
                if (pi.position.y > max.y) { pi.position.y = max.y; pi.velocity.y *= -0.2f; }
                if (pi.position.z > max.z) { pi.position.z = max.z; pi.velocity.z *= -0.25f; }

                Particles[i] = pi;
            }
        }

        void CollectNeighbors(Vector3 p)
        {
            candidateBuffer.Clear();
            Vector3Int c = Cell(p);
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                int key = Hash(c + new Vector3Int(x, y, z));
                if (grid.TryGetValue(key, out var list))
                    candidateBuffer.AddRange(list);
            }
        }

        Vector3Int Cell(Vector3 p) => new Vector3Int(
            Mathf.FloorToInt(p.x / cellSize),
            Mathf.FloorToInt(p.y / cellSize),
            Mathf.FloorToInt(p.z / cellSize));

        static int Hash(Vector3Int c)
        {
            unchecked { return c.x * 73856093 ^ c.y * 19349663 ^ c.z * 83492791; }
        }
    }
}
