using UnityEngine;

namespace RealLiquidChanged
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RealLiquidSurface : MonoBehaviour
    {
        //     [Header("Mesh Settings")]
        //     [Tooltip("Assign your custom mesh asset here. If left unassigned, a procedural ring mesh will be generated as a fallback.")]
        //     public Mesh customMesh;

        //     [Header("Surface Dimensions")]
        //     [Range(8, 48)] public int radialSegments = 28;
        //     [Range(2, 12)] public int rings = 5;
        //     [Range(0.1f, 2f)] public float radius = 0.42f;
        //     [Range(0.05f, 2f)] public float height = 1f;

        //     [Range(0.05f, 0.95f)] public float Fill = 0.55f;
        //     public Vector3 Tilt;
        //     public float TimeValue;

        //     [Header("Slosh")]
        //     public float waveHeight = 0.035f;
        //     public float waveFrequency = 2.7f;
        //     public float waveSpeed = 1.8f;

        //     Mesh mesh;
        //     Vector3[] baseVertices;
        //     Vector3[] vertices;
        //     int[] triangles;

        //     void Awake() => Build();

        //     void OnValidate()
        //     {
        //         radialSegments = Mathf.Clamp(radialSegments, 8, 48);
        //         rings = Mathf.Clamp(rings, 2, 12);
        //         if (Application.isPlaying) Build();
        //     }

        //     void Build()
        //     {
        //         if (customMesh != null)
        //         {
        //             // Clone the assigned custom mesh asset to prevent modifying disk files
        //             mesh = Instantiate(customMesh);
        //             mesh.name = "RTLC_CustomLiquidSurface";
        //             mesh.MarkDynamic();

        //             baseVertices = customMesh.vertices;
        //             vertices = new Vector3[baseVertices.Length];
        //         }
        //         else
        //         {
        //             // Fallback: Generate procedural ring grid mesh
        //             radialSegments = Mathf.Clamp(radialSegments, 8, 48);
        //             rings = Mathf.Clamp(rings, 2, 12);

        //             int count = (rings + 1) * radialSegments;
        //             vertices = new Vector3[count];
        //             triangles = new int[rings * radialSegments * 6];

        //             int t = 0;
        //             for (int r = 0; r < rings; r++)
        //             {
        //                 for (int s = 0; s < radialSegments; s++)
        //                 {
        //                     int a = r * radialSegments + s;
        //                     int b = r * radialSegments + (s + 1) % radialSegments;
        //                     int c = (r + 1) * radialSegments + s;
        //                     int d = (r + 1) * radialSegments + (s + 1) % radialSegments;

        //                     triangles[t++] = a;
        //                     triangles[t++] = c;
        //                     triangles[t++] = b;
        //                     triangles[t++] = b;
        //                     triangles[t++] = c;
        //                     triangles[t++] = d;
        //                 }
        //             }

        //             mesh = new Mesh { name = "RTLC_SmoothLiquidSurface" };
        //             mesh.MarkDynamic();
        //             mesh.vertices = vertices;
        //             mesh.triangles = triangles;
        //         }

        //         GetComponent<MeshFilter>().sharedMesh = mesh;
        //         UpdateMesh();
        //     }

        //     void LateUpdate()
        //     {
        //         if (mesh) UpdateMesh();
        //     }

        //     void UpdateMesh()
        //     {
        //         float baseY = Mathf.Lerp(-height * 0.5f, height * 0.5f, Fill);

        //         if (customMesh != null && baseVertices != null)
        //         {
        //             // Displace custom mesh vertices relative to base geometry
        //             for (int i = 0; i < baseVertices.Length; i++)
        //             {
        //                 Vector3 p = baseVertices[i];
        //                 float a = Mathf.Atan2(p.z, p.x);
        //                 float normalizedDist = Mathf.Clamp01(new Vector2(p.x, p.z).magnitude / radius);

        //                 float wave =
        //                     Mathf.Sin(a * waveFrequency + TimeValue * waveSpeed) * waveHeight +
        //                     Mathf.Sin(a * 3.0f - TimeValue * 1.1f) * waveHeight * 0.35f;

        //                 p.y = baseY + p.y + (wave * normalizedDist);
        //                 p.y += (-Tilt.x * p.z - Tilt.z * p.x) * normalizedDist;

        //                 vertices[i] = p;
        //             }

        //             mesh.vertices = vertices;
        //         }
        //         else
        //         {
        //             // Displace procedural grid vertices
        //             for (int r = 0; r <= rings; r++)
        //             {
        //                 float rr = (float)r / rings;
        //                 float edge = Mathf.Lerp(0.72f, 1f, rr);

        //                 for (int s = 0; s < radialSegments; s++)
        //                 {
        //                     float a = s * Mathf.PI * 2f / radialSegments;
        //                     float ca = Mathf.Cos(a);
        //                     float sa = Mathf.Sin(a);

        //                     float wave =
        //                         Mathf.Sin(a * waveFrequency + TimeValue * waveSpeed) * waveHeight +
        //                         Mathf.Sin(a * 3.0f - TimeValue * 1.1f) * waveHeight * 0.35f;

        //                     float y = baseY + wave * rr;
        //                     Vector3 p = new Vector3(ca * radius * edge, y, sa * radius * edge);

        //                     // Slosh opposite to container tilt
        //                     p.y += (-Tilt.x * p.z - Tilt.z * p.x) * rr;

        //                     vertices[r * radialSegments + s] = p;
        //                 }
        //             }

        //             mesh.Clear();
        //             mesh.vertices = vertices;
        //             mesh.triangles = triangles;
        //         }

        //         mesh.RecalculateNormals();
        //         mesh.RecalculateBounds();
        //     }
        // }
        [Header("Source Object")]
        [Tooltip("Drag your target GameObject here (e.g., 'filling water'). Mesh and Material will be fetched automatically.")]
        public GameObject sourceMeshObject;

        [Header("Surface Dimensions")]
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
        Mesh customMesh;
        Vector3[] baseVertices;
        Vector3[] vertices;
        int[] triangles;

        MeshFilter myMeshFilter;
        MeshRenderer myMeshRenderer;

        void Awake()
        {
            myMeshFilter = GetComponent<MeshFilter>();
            myMeshRenderer = GetComponent<MeshRenderer>();
            Build();
        }

        void OnValidate()
        {
            radialSegments = Mathf.Clamp(radialSegments, 8, 48);
            rings = Mathf.Clamp(rings, 2, 12);

            if (myMeshRenderer == null) myMeshRenderer = GetComponent<MeshRenderer>();
            FetchAndApplyMaterial();

            if (Application.isPlaying) Build();
        }

        void FetchAndApplyMaterial()
        {
            if (sourceMeshObject != null && myMeshRenderer != null)
            {
                if (sourceMeshObject.TryGetComponent<MeshRenderer>(out var sourceRenderer))
                {
                    if (sourceRenderer.sharedMaterial != null)
                    {
                        myMeshRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
                    }
                }
            }
        }

        void Build()
        {
            if (myMeshFilter == null) myMeshFilter = GetComponent<MeshFilter>();
            if (myMeshRenderer == null) myMeshRenderer = GetComponent<MeshRenderer>();

            FetchAndApplyMaterial();

            if (sourceMeshObject != null && sourceMeshObject.TryGetComponent<MeshFilter>(out var sourceFilter) && sourceFilter.sharedMesh != null)
            {
                customMesh = sourceFilter.sharedMesh;

                // Clone the fetched mesh to avoid overwriting asset files on disk
                mesh = Instantiate(customMesh);
                mesh.name = "RTLC_SourceLiquidSurface";
                mesh.MarkDynamic();

                baseVertices = customMesh.vertices;
                vertices = new Vector3[baseVertices.Length];

                myMeshFilter.sharedMesh = mesh;
            }
            else
            {
                // Fallback: Procedurally generate ring surface grid
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
                mesh.vertices = vertices;
                mesh.triangles = triangles;

                myMeshFilter.sharedMesh = mesh;
            }

            UpdateMesh();
        }

        void LateUpdate()
        {
            if (mesh) UpdateMesh();
        }

        void UpdateMesh()
        {
            float baseY = Mathf.Lerp(-height * 0.5f, height * 0.5f, Fill);

            if (customMesh != null && baseVertices != null)
            {
                // Deform source object vertices relative to its original shape
                for (int i = 0; i < baseVertices.Length; i++)
                {
                    Vector3 p = baseVertices[i];
                    float a = Mathf.Atan2(p.z, p.x);
                    float normalizedDist = Mathf.Clamp01(new Vector2(p.x, p.z).magnitude / radius);

                    float wave =
                        Mathf.Sin(a * waveFrequency + TimeValue * waveSpeed) * waveHeight +
                        Mathf.Sin(a * 3.0f - TimeValue * 1.1f) * waveHeight * 0.35f;

                    p.y = baseY + p.y + (wave * normalizedDist);
                    p.y += (-Tilt.x * p.z - Tilt.z * p.x) * normalizedDist;

                    vertices[i] = p;
                }

                mesh.vertices = vertices;
            }
            else
            {
                // Deform procedural ring vertices
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

                        p.y += (-Tilt.x * p.z - Tilt.z * p.x) * rr;

                        vertices[r * radialSegments + s] = p;
                    }
                }

                mesh.Clear();
                mesh.vertices = vertices;
                mesh.triangles = triangles;
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}

