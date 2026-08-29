using UnityEngine;

namespace RealLiquidChanged
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RealLiquidPourStream : MonoBehaviour
    {
        [Header("Source")]
        public Transform Source;
        public bool Enabled;
        [Range(0, 1)] public float Strength = 1f;

        [Header("Stream")]
        [Range(5, 24)] public int segments = 14;
        [Range(6, 20)] public int radialSegments = 10;
        public float length = 1.7f;
        public float startRadius = 0.055f;
        public float endRadius = 0.09f;
        public float gravity = 9.81f;
        public float initialSpeed = 1.2f;
        public float wobble = 0.045f;

        Mesh mesh;
        Vector3[] vertices;
        int[] triangles;

        void Awake() => Build();

        void LateUpdate()
        {
            if (!Source || !Enabled || Strength <= 0.001f)
            {
                if (mesh) mesh.Clear();
                return;
            }

            UpdateMesh();
        }

        void Build()
        {
            segments = Mathf.Clamp(segments, 5, 24);
            radialSegments = Mathf.Clamp(radialSegments, 6, 20);

            vertices = new Vector3[(segments + 1) * radialSegments];
            triangles = new int[segments * radialSegments * 6];

            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < radialSegments; j++)
                {
                    int a = i * radialSegments + j;
                    int b = i * radialSegments + (j + 1) % radialSegments;
                    int c = (i + 1) * radialSegments + j;
                    int d = (i + 1) * radialSegments + (j + 1) % radialSegments;

                    triangles[t++] = a;
                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = d;
                }
            }

            mesh = new Mesh { name = "RTLC_DeformablePourStream" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        void UpdateMesh()
        {
            Vector3 p0 = Source.position;
            Vector3 v0 = Source.forward * initialSpeed + Vector3.down * (0.25f + Strength * 0.8f);

            Vector3 tangent = Source.forward.sqrMagnitude > 0.001f ? Source.forward.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(tangent, Vector3.up);
            if (side.sqrMagnitude < 0.001f) side = Source.right;
            side.Normalize();
            Vector3 binormal = Vector3.Cross(tangent, side).normalized;

            for (int i = 0; i <= segments; i++)
            {
                float u = (float)i / segments;
                float distance = length * u;

                float time = distance / Mathf.Max(0.2f, initialSpeed);
                Vector3 center = p0 + v0 * time + 0.5f * Physics.gravity * time * time;

                // Controlled fluid wobble instead of a perfectly rigid tube.
                float wob =
                    Mathf.Sin(Time.time * 7f + u * 11f) * wobble * u +
                    Mathf.Sin(Time.time * 4.1f - u * 17f) * wobble * 0.45f * u;

                center += side * wob;
                center += binormal * Mathf.Sin(Time.time * 5.2f + u * 8f) * wobble * 0.35f * u;

                // Slight narrowing in the middle gives a more natural falling stream.
                float radiusProfile = Mathf.Lerp(startRadius, endRadius, u);
                radiusProfile *= (0.88f + 0.12f * Mathf.Sin(u * Mathf.PI));
                radiusProfile *= Mathf.Lerp(0.55f, 1f, Strength);

                for (int j = 0; j < radialSegments; j++)
                {
                    float a = j * Mathf.PI * 2f / radialSegments;
                    Vector3 offset = (side * Mathf.Cos(a) + binormal * Mathf.Sin(a)) * radiusProfile;
                    vertices[i * radialSegments + j] = transform.InverseTransformPoint(center + offset);
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
