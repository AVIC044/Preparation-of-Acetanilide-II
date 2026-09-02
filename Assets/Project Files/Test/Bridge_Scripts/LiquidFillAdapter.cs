using UnityEngine;
using UnityEngine.UI;
using RealisticLiquidSystem;
using RealLiquidChanged;

public class LiquidFillAdapter : MonoBehaviour
{
    [Header("UI Source")]
    [SerializeField] private Slider fillSlider;

    [Header("Target Components")]
    [Tooltip("The Renderer attached to your container (e.g., 'filling water' or 'measuring cylinder').")]
    [SerializeField] private Renderer containerRenderer;

    [Tooltip("The surface mesh script managing waves and slosh.")]
    [SerializeField] private RealLiquidSurface surfaceMeshScript;

    [Tooltip("The particle simulation script managing SPH physics.")]
    [SerializeField] private LiquidSimulation particleSimulation;

    private Material instanceMaterial;
    private static readonly int FillLevelID = Shader.PropertyToID("_FillLevel");

    private void Awake()
    {
        if (containerRenderer != null)
        {
            // Cache an instance material to avoid modifying project asset files
            instanceMaterial = containerRenderer.material;
        }

        if (fillSlider != null)
        {
            fillSlider.onValueChanged.AddListener(SetFillAmount);
            SetFillAmount(fillSlider.value);
        }
    }

    /// <summary>
    /// Event listener for UI Slider (expects normalized value from 0.0 to 1.0).
    /// </summary>
    public void SetFillAmount(float normalizedAmount)
    {
        float clampedFill = Mathf.Clamp01(normalizedAmount);

        // 1. Sync Shader Cutoff Height (_FillLevel)
        if (instanceMaterial != null && instanceMaterial.HasProperty(FillLevelID))
        {
            instanceMaterial.SetFloat(FillLevelID, clampedFill);
        }

        // 2. Sync Surface Mesh Deformer (RealLiquidSurface.Fill)
        if (surfaceMeshScript != null)
        {
            surfaceMeshScript.Fill = clampedFill;
        }

        // 3. Sync Particle Physics Solver (LiquidSimulation.Fill)
        if (particleSimulation != null)
        {
            particleSimulation.Fill(clampedFill);
        }
    }

    private void OnDestroy()
    {
        if (fillSlider != null)
        {
            fillSlider.onValueChanged.RemoveListener(SetFillAmount);
        }
    }
}