using UnityEngine;

/// <summary>
/// Drives the visual "stream" of liquid between two BeakerLiquid instances and
/// starts/stops the actual volume transfer on the source beaker.
///
/// Uses a LineRenderer for the stream shape (recommended over a stretched mesh
/// because it can sag under gravity and taper width along its length — a
/// straight scaled cylinder can't do either convincingly).
///
/// Setup:
/// - Put this on an empty GameObject with a LineRenderer component.
/// - sourceBeaker / destinationBeaker: the two BeakerLiquid objects involved.
/// - spoutPoint: an empty Transform at the beaker's lip/spout.
/// - streamMaterial: your transparent liquid material (or the LiquidStream shader).
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PourStreamController : MonoBehaviour
{
    [Header("Beakers")]
    public LiquidBeaker sourceBeaker;
    public LiquidBeaker destinationBeaker;

    [Header("Spout")]
    public Transform spoutPoint;

    [Header("Stream shape")]
    [Range(2, 32)] public int arcSegments = 12;
    [Tooltip("How much the stream bows downward under 'gravity'")]
    public float gravitySag = 0.6f;
    public AnimationCurve widthOverLength = AnimationCurve.Linear(0, 0.05f, 1, 0.03f);

    [Header("Flow look")]
    [Tooltip("Your transparent liquid material, or the LiquidStream shader material. If it has a tiling texture, its offset is scrolled each frame to fake flow motion — no custom shader required for this part.")]
    public Material streamMaterial;
    public float flowScrollSpeed = 2f;

    [Header("Splash")]
    public ParticleSystem splashParticles;
    [Tooltip("Small continuous wobble fed into the destination while the stream is landing")]
    public float splashWobbleImpulse = 15f;

    [Header("Pour trigger")]
    [Tooltip("Local Z tilt (degrees) of the source beaker that starts pouring")]
    public float tiltAngleToStartPour = 45f;

    LineRenderer line;
    bool pouring;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = Mathf.Max(arcSegments, 2);
        line.useWorldSpace = true;
        if (streamMaterial != null)
            line.material = streamMaterial;
        line.enabled = false;
    }

    void Update()
    {
        bool shouldPour = ShouldPour();

        if (shouldPour && !pouring) StartPouring();
        else if (!shouldPour && pouring) StopPouring();

        if (pouring) UpdateStreamShape();
    }

    // ---------- Pour trigger condition ----------

    bool ShouldPour()
    {
        if (sourceBeaker == null || destinationBeaker == null) return false;
        if (sourceBeaker.currentVolume <= 0f) return false;

        float tilt = Mathf.Abs(Mathf.DeltaAngle(0, sourceBeaker.transform.localEulerAngles.z));
        return tilt >= tiltAngleToStartPour;
    }

    // ---------- Start / stop ----------

    void StartPouring()
    {
        pouring = true;
        line.enabled = true;
        sourceBeaker.BeginPour(destinationBeaker);
        if (splashParticles != null)
            splashParticles.Play();
    }

    void StopPouring()
    {
        pouring = false;
        line.enabled = false;
        sourceBeaker.EndPour();
        if (splashParticles != null)
            splashParticles.Stop();
    }

    // ---------- Per-frame stream shape + effects ----------

    void UpdateStreamShape()
    {
        Vector3 start = spoutPoint != null ? spoutPoint.position : sourceBeaker.transform.position;
        Vector3 end = destinationBeaker.GetLiquidSurfaceWorldPos();

        int count = line.positionCount;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector3 point = Vector3.Lerp(start, end, t);

            // Sag downward like gravity, peaking mid-arc (sin curve, zero at both ends)
            point.y -= Mathf.Sin(t * Mathf.PI) * gravitySag;

            line.SetPosition(i, point);
        }

        line.widthCurve = widthOverLength;

        // Fake flow motion by scrolling the material's texture offset —
        // works with any shader exposing _MainTex (Standard, URP Lit, or the
        // LiquidStream shader below). Safe no-op if the material has no texture.
        if (streamMaterial != null)
        {
            Vector2 offset = streamMaterial.mainTextureOffset;
            offset.y -= flowScrollSpeed * Time.deltaTime;
            streamMaterial.mainTextureOffset = offset;
        }

        if (splashParticles != null)
            splashParticles.transform.position = end;

        // Keep disturbing the destination's surface gently while contact persists
        destinationBeaker.AddWobbleImpulse(new Vector2(0f, splashWobbleImpulse * Time.deltaTime));
    }
}
