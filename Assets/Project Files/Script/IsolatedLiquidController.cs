using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class IsolatedLiquidController : MonoBehaviour
{
    [Header("Material Setup")]
    [Tooltip("Drag the specific mesh object that holds your 'liqudeshader' here.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("The exact Reference name from your Graph Blackboard panel (e.g., _fill or fill).")]
    [SerializeField] private string fillPropertyName = "_fill";

    [Header("UI & Capacity Settings")]
    [Tooltip("Drag your UI Slider here.")]
    [SerializeField] private Slider liquidSlider;

    [Tooltip("The maximum mL capacity of this container.")]
    [SerializeField] private float maxCapacityML = 10f;

    [Tooltip("How fast or slow the liquid drains (in seconds) during animations.")]
    [SerializeField] private float drainDuration = 1.5f;

    [Header("Task Logic")]
    [Tooltip("The exact mL amount the player needs to reach to lock this slider.")]
    [SerializeField] private float requiredML = 3f;

    [Tooltip("This event fires ONLY when this specific slider hits the requiredML.")]
    public UnityEvent onTargetReached;

    [Header("Callbacks & Events")]
    public UnityEvent onReductionComplete;

    private Material dynamicMaterial;
    private Coroutine activeDrainRoutine;
    private float currentShaderTarget = 0.0f;
    private bool isLocked = false;

    private void Awake()
    {
        if (targetRenderer != null)
        {
            dynamicMaterial = targetRenderer.material;
        }
        else
        {
            Debug.LogError($"[IsolatedLiquidController] No Renderer assigned on {gameObject.name}!");
        }
    }

    private void Start()
    {
        if (liquidSlider != null)
        {
            liquidSlider.minValue = 0;
            liquidSlider.maxValue = maxCapacityML;
            liquidSlider.wholeNumbers = true;
            liquidSlider.onValueChanged.AddListener(SetFillFromSlider);

            SetFillFromSlider(liquidSlider.value);
        }
    }

    public void SetFillFromSlider(float currentML)
    {
        if (dynamicMaterial == null) return;

        // If the slider is already locked, prevent any further changes
        if (isLocked)
        {
            if (liquidSlider != null)
            {
                liquidSlider.SetValueWithoutNotify(requiredML);
            }
            return;
        }

        if (activeDrainRoutine != null)
        {
            StopCoroutine(activeDrainRoutine);
        }

        // --- EXACT MATH FORMULA ---
        float finalShaderValue = 0.01f + (currentML * 0.02f);
        dynamicMaterial.SetFloat(fillPropertyName, finalShaderValue);

        // --- CHECK IF TARGET IS REACHED ---
        if (currentML == requiredML)
        {
            isLocked = true;

            if (liquidSlider != null)
            {
                liquidSlider.interactable = false;
            }

            if (onTargetReached != null)
            {
                onTargetReached.Invoke();
            }
        }
    }

    // --- Animation Methods (For Pouring Manager) ---

    public void StartLiquidFill(float targetML, float duration)
    {
        // Now directly uses the targetML passed from the PourManager
        currentShaderTarget = 0.01f + (targetML * 0.02f);

        drainDuration = duration;

        if (activeDrainRoutine != null)
            StopCoroutine(activeDrainRoutine);

        activeDrainRoutine = StartCoroutine(AnimateDrain());
    }

    private IEnumerator AnimateDrain()
    {
        float startValue = dynamicMaterial.GetFloat(fillPropertyName);
        float elapsed = 0f;

        while (elapsed < drainDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / drainDuration);

            float currentFillLevel = Mathf.Lerp(startValue, currentShaderTarget, t);
            dynamicMaterial.SetFloat(fillPropertyName, currentFillLevel);
            yield return null;
        }

        dynamicMaterial.SetFloat(fillPropertyName, currentShaderTarget);

        // Update slider visually
        if (liquidSlider != null && !isLocked)
        {
            float finalML = (currentShaderTarget - 0.01f) / 0.02f;
            liquidSlider.value = finalML;
        }

        if (onReductionComplete != null)
        {
            onReductionComplete.Invoke();
        }
    }
}