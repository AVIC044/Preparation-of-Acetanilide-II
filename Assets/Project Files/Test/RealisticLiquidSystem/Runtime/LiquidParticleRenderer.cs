using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticLiquidSystem
{
    public class LiquidParticleRenderer : MonoBehaviour
    {
        public LiquidSimulation simulation;
        public Mesh particleMesh;
        public Material material;

        [Min(0.005f)]
        public float particleScale = 0.09f;

        private Matrix4x4[] matrices;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            matrices = new Matrix4x4[1023];
            propertyBlock = new MaterialPropertyBlock();

            // Automatically use Unity's built-in sphere if none is assigned.
            if (particleMesh == null)
            {
                particleMesh = CreateSphereMesh();
            }

            if (material != null)
            {
                material.enableInstancing = true;
            }
        }

        private void LateUpdate()
        {
            if (simulation == null)
                return;

            if (material == null)
                return;

            if (particleMesh == null)
                return;

            int count = simulation.ActiveCount;

            if (count <= 0)
                return;

            FluidParticle[] particles = simulation.Particles;

            int rendered = 0;

            while (rendered < count)
            {
                int batchCount = Mathf.Min(1023, count - rendered);

                for (int i = 0; i < batchCount; i++)
                {
                    FluidParticle p = particles[rendered + i];

                    if (!p.active)
                        continue;

                    Vector3 worldPosition =
                        transform.TransformPoint(p.position);

                    matrices[i] = Matrix4x4.TRS(
                        worldPosition,
                        Quaternion.identity,
                        Vector3.one * particleScale
                    );
                }

                Graphics.DrawMeshInstanced(
                    particleMesh,
                    0,
                    material,
                    matrices,
                    batchCount,
                    propertyBlock,
                    ShadowCastingMode.Off,
                    false,
                    gameObject.layer
                );

                rendered += batchCount;
            }
        }

        private Mesh CreateSphereMesh()
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;

            temp.hideFlags = HideFlags.HideAndDontSave;

            DestroyImmediate(temp);

            return mesh;
        }
    }
}