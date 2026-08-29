using UnityEngine;

namespace RealLiquidChanged
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RealLiquidSurface : MonoBehaviour
    {
        [Range(8, 48)] public int radialSegments = 28;
        [Range(2, 12)] public int rings = 5;
        [Range(0.1f, 2f)] public float radius = 0.42f;
        [Range(0.05f, 2f)] public float height = 1f;

        [Range(0.05f, 0.95f)] public float Fill = 0.55f;
        public Vector3 Tilt;
        public float TimeValue;

        [Header("Slosh")]
        public float waveHeight = 0.035f;
        public float waveFrequency = 2.7f;
        public float waveSpeed = 1.8f;

        Mesh mesh;
        Vector3[] vertices;
        int[] triangles;

        void Awake() => Build();

        void OnValidate()
        {
            radialSegments = Mathf.Clamp(radialSegments, 8, 48);
            rings = Mathf.Clamp(rings, 2, 12);
            if (Application.isPlaying) Build();
        }

        void Build()
        {
            radialSegments = Mathf.Clamp(radialSegments, 8, 48);
            rings = Mathf.Clamp(rings, 2, 12);

            int count = (rings + 1) * radialSegments;
            vertices = new Vector3[count];
            triangles = new int[rings * radialSegments * 6];

            int t = 0;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < radialSegments; s++)
                {
                    int a = r * radialSegments + s;
                    int b = r * radialSegments + (s + 1) % radialSegments;
                    int c = (r + 1) * radialSegments + s;
                    int d = (r + 1) * radialSegments + (s + 1) % radialSegments;

                    triangles[t++] = a;
                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = d;
                }
            }

            mesh = new Mesh { name = "RTLC_SmoothLiquidSurface" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            UpdateMesh();
        }

        void LateUpdate()
        {
            if (mesh) UpdateMesh();
        }

        void UpdateMesh()
        {
            float baseY = Mathf.Lerp(-height * 0.5f, height * 0.5f, Fill);

            for (int r = 0; r <= rings; r++)
            {
                float rr = (float)r / rings;
                float edge = Mathf.Lerp(0.72f, 1f, rr);

                for (int s = 0; s < radialSegments; s++)
                {
                    float a = s * Mathf.PI * 2f / radialSegments;
                    float ca = Mathf.Cos(a);
                    float sa = Mathf.Sin(a);

                    float wave =
                        Mathf.Sin(a * waveFrequency + TimeValue * waveSpeed) * waveHeight +
                        Mathf.Sin(a * 3.0f - TimeValue * 1.1f) * waveHeight * 0.35f;

                    float y = baseY + wave * rr;
                    Vector3 p = new Vector3(ca * radius * edge, y, sa * radius * edge);

                    // Slosh opposite to the container tilt.
                    p.y += (-Tilt.x * p.z - Tilt.z * p.x) * rr;

                    vertices[r * radialSegments + s] = p;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
