using UnityEngine;
using UnityEngine.UI;

public class LiquidBeaker : MonoBehaviour
{

    /// <summary>
    /// Drives a scaled-mesh liquid inside a beaker: volume-based fill height,
    /// a spring-damper "slosh" wobble reacting to container motion, and
    /// simple volume-conserving transfer between two beakers for pouring.
    ///
    /// Setup:
    /// - liquidVisual: child mesh (e.g. a cylinder) using your transparent liquid material.
    /// - beakerBottomAnchor: an empty Transform placed at the inner floor of the beaker,
    ///   used so the liquid grows upward instead of scaling from its center.
    /// </summary>
    [System.Serializable]
    public struct LiquidData
    {
        public Material material;   // optional — leave null to keep the current material and just re-tint
        public Color color;
        public float wobbleStiffness;
        public float wobbleDamping;
        public float wobbleAmplitudeScale;
    }

    [Header("Volume")]
    public float maxVolume = 250f;       // mL, matches beaker capacity
    public float currentVolume = 100f;   // starting fill
    [Tooltip("Local scale.y of liquidVisual when at 100% full")]
    public float maxFillHeight = 1f;
    public float fillLerpSpeed = 4f;

    [Header("References")]
    public Transform liquidVisual;
    public Transform beakerBottomAnchor;

    [Header("UI (optional)")]
    [Tooltip("If assigned, this slider's range/value are kept in sync with currentVolume automatically.")]
    public Slider volumeSlider;

    [Header("Wobble (spring-damper slosh, no shader needed)")]
    public float wobbleStiffness = 120f;
    public float wobbleDamping = 8f;
    [Tooltip("Overall intensity multiplier — expose this for per-chemical customization")]
    public float wobbleAmplitudeScale = 1f;
    public float maxWobbleAngle = 12f;

    [Header("Pouring")]
    public float pourRatePerSecond = 60f; // mL/s at full pour

    float targetFillPct;
    float currentFillPct;

    Vector2 wobbleVelocity;
    Vector2 wobbleOffset;
    Vector3 lastEulerAngles;
    Vector3 lastPosition;

    bool isPouring;
    LiquidBeaker pourTarget;

    void Start()
    {
        lastEulerAngles = transform.eulerAngles;
        lastPosition = transform.position;
        targetFillPct = currentFillPct = Mathf.Clamp01(currentVolume / maxVolume);
        ApplyFillVisual(currentFillPct);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = maxVolume;
            volumeSlider.SetValueWithoutNotify(currentVolume); // position the handle without firing the event
            volumeSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    void Update()
    {
        HandleFillLerp();
        HandleWobble();
        if (isPouring && pourTarget != null)
            HandlePourTransfer();
    }

    // ---------- Public API for your UI buttons ----------

    public void OnPlusButton(float step = 10f) => SetVolume(currentVolume + step);
    public void OnMinusButton(float step = 10f) => SetVolume(currentVolume - step);

    /// <summary>Set fill as a normalized 0–1 amount instead of raw mL.</summary>
    public void SetFillAmount(float normalized01) => SetVolume(Mathf.Clamp01(normalized01) * maxVolume);

    // Wired to the Slider's OnValueChanged(float) event in Start() above.
    // You can also drag this onto the Slider's event list in the Inspector instead.
    public void OnSliderChanged(float value) => SetVolume(value);

    public void SetVolume(float newVolume)
    {
        float prevPct = targetFillPct;
        currentVolume = Mathf.Clamp(newVolume, 0f, maxVolume);
        targetFillPct = currentVolume / maxVolume;

        // Small bump impulse on volume change so it doesn't feel static/robotic
        float delta = targetFillPct - prevPct;
        AddWobbleImpulse(new Vector2(0f, delta * 40f));

        // Keep the slider's handle in sync no matter what changed the volume
        // (buttons, pouring, script). SetValueWithoutNotify is essential here —
        // a plain volumeSlider.value = ... would re-fire onValueChanged ->
        // OnSliderChanged -> SetVolume -> ... (infinite loop).
        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(currentVolume);
    }

    /// <summary>
    /// Everything about which liquid this is: what it looks like and how it
    /// moves. Call this once when a chemical is poured/selected — the caller
    /// (UI, a chemical-selection system, whatever) doesn't need to know
    /// anything about the wobble spring or the renderer underneath.
    /// </summary>
    public void SetLiquidData(LiquidData data)
    {
        wobbleStiffness = data.wobbleStiffness;
        wobbleDamping = data.wobbleDamping;
        wobbleAmplitudeScale = data.wobbleAmplitudeScale;

        if (liquidVisual == null) return;
        Renderer r = liquidVisual.GetComponent<Renderer>();
        if (r == null) return;

        if (data.material != null)
            r.material = data.material; // swaps the whole material (fine for a handful of beakers on screen)

        // Tint via MaterialPropertyBlock instead of r.material.color so beakers
        // sharing one material don't each spawn a separate material instance.
        var block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor("_Color", data.color);
        r.SetPropertyBlock(block);
    }

    // ---------- Public API for pouring ----------

    public void BeginPour(LiquidBeaker target)
    {
        pourTarget = target;
        isPouring = true;
    }

    public void EndPour()
    {
        isPouring = false;
        pourTarget = null;
    }

    public bool IsPouring => isPouring;

    /// <summary>
    /// World-space point at the current top surface of the liquid — this is
    /// what a pour stream should aim at (and where splash effects should spawn).
    /// </summary>
    public Vector3 GetLiquidSurfaceWorldPos()
    {
        if (liquidVisual == null) return transform.position;
        Renderer r = liquidVisual.GetComponent<Renderer>();
        if (r == null) return liquidVisual.position;

        Bounds b = r.bounds;
        return new Vector3(b.center.x, b.max.y, b.center.z);
    }

    // ---------- Fill visual (scale-based, no shader) ----------

    void HandleFillLerp()
    {
        currentFillPct = Mathf.Lerp(currentFillPct, targetFillPct, Time.deltaTime * fillLerpSpeed);
        ApplyFillVisual(currentFillPct);
    }

    void ApplyFillVisual(float pct)
    {
        if (liquidVisual == null) return;

        float height = Mathf.Max(pct * maxFillHeight, 0.0001f); // avoid zero-scale flicker
        Vector3 scale = liquidVisual.localScale;
        scale.y = height;
        liquidVisual.localScale = scale;

        // Keep the liquid's bottom anchored to the beaker floor as it scales upward
        if (beakerBottomAnchor != null)
        {
            Vector3 pos = liquidVisual.localPosition;
            pos.y = beakerBottomAnchor.localPosition.y + height * 0.5f;
            liquidVisual.localPosition = pos;
        }
    }

    // ---------- Wobble: spring-damper tilt reacting to container motion ----------

    void HandleWobble()
    {
        Vector3 angleDelta = transform.eulerAngles - lastEulerAngles;
        Vector3 posDelta = transform.position - lastPosition;
        lastEulerAngles = transform.eulerAngles;
        lastPosition = transform.position;

        Vector2 motionImpulse = new Vector2(
            Mathf.DeltaAngle(0, angleDelta.z) + posDelta.x * 20f,
            Mathf.DeltaAngle(0, angleDelta.x) + posDelta.z * 20f
        );
        if (motionImpulse.sqrMagnitude > 0.0001f)
            AddWobbleImpulse(motionImpulse * 0.5f);

        Vector2 springForce = -wobbleOffset * wobbleStiffness;
        Vector2 dampingForce = -wobbleVelocity * wobbleDamping;
        Vector2 acceleration = (springForce + dampingForce) * wobbleAmplitudeScale;

        wobbleVelocity += acceleration * Time.deltaTime;
        wobbleOffset += wobbleVelocity * Time.deltaTime;
        wobbleOffset.x = Mathf.Clamp(wobbleOffset.x, -maxWobbleAngle, maxWobbleAngle);
        wobbleOffset.y = Mathf.Clamp(wobbleOffset.y, -maxWobbleAngle, maxWobbleAngle);

        if (liquidVisual != null)
        {
            float keepY = liquidVisual.localEulerAngles.y;
            liquidVisual.localRotation = Quaternion.Euler(wobbleOffset.y, keepY, wobbleOffset.x);
        }
    }

    public void AddWobbleImpulse(Vector2 impulse)
    {
        wobbleVelocity += impulse;
    }

    // ---------- Pouring: volume-conserving transfer ----------

    void HandlePourTransfer()
    {
        float amount = pourRatePerSecond * Time.deltaTime;
        amount = Mathf.Min(amount, currentVolume);
        float room = pourTarget.maxVolume - pourTarget.currentVolume;
        amount = Mathf.Min(amount, room);

        if (amount <= 0f)
        {
            EndPour();
            return;
        }

        SetVolume(currentVolume - amount);
        pourTarget.SetVolume(pourTarget.currentVolume + amount);
    }

}