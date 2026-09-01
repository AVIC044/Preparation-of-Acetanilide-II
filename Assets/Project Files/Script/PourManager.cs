using UnityEngine;
using System.Collections;

public class PourManager : MonoBehaviour
{
    [Header("Bottle")]
    [SerializeField] private Transform bottle;
    [SerializeField] private Transform pourPoint;
    [SerializeField] private Animator bottleAnimator;
    [SerializeField] private string pourTrigger = "Pour";

    [Header("Liquid Targets (in mL)")]
    [SerializeField] private IsolatedLiquidController bottleLiquid;
    [SerializeField] private IsolatedLiquidController cupLiquid;

    [Tooltip("What mL should the bottle drop down to? (Usually 0)")]
    [SerializeField] private float bottleTargetML = 0f;

    [Tooltip("What exact mL should the cup fill up to? (e.g., 3)")]
    [SerializeField] private float cupTargetML = 3f;

    [SerializeField] private float pourDuration = 3f;

    private bool poured = false;
    private bool isKeyframeReached = false; // NEW: Tracks if the animation reached the pour point

    [Header("Move To Pour Point")]
    [SerializeField] private float moveSpeed = 2f;

    public void StartPour()
    {
        if (poured) return;

        poured = true;
        isKeyframeReached = false; // Reset the flag
        StartCoroutine(PourSequence());
    }

    /// <summary>
    /// Call this function from your Animation Event at the exact keyframe!
    /// </summary>
    public void StartLiquidTransfer()
    {
        isKeyframeReached = true;
    }

    private IEnumerator MoveToPourPoint()
    {
        while (Vector3.Distance(bottle.position, pourPoint.position) > 0.01f)
        {
            bottle.position = Vector3.MoveTowards(
                bottle.position,
                pourPoint.position,
                moveSpeed * Time.deltaTime);

            bottle.rotation = Quaternion.RotateTowards(
                bottle.rotation,
                pourPoint.rotation,
                180f * Time.deltaTime);

            yield return null;
        }

        bottle.position = pourPoint.position;
        bottle.rotation = pourPoint.rotation;
    }

    private IEnumerator PourSequence()
    {
        // Move bottle to pour point
        yield return StartCoroutine(MoveToPourPoint());

        // Play pouring animation
        if (bottleAnimator != null)
            bottleAnimator.SetTrigger(pourTrigger);

        // --- WAIT FOR THE ANIMATION EVENT ---
        // Instead of waiting 0.5 seconds, we wait until the animation keyframe fires StartLiquidTransfer()
        yield return new WaitUntil(() => isKeyframeReached);

        // --- START POURING WITH EXACT mL ---
        bottleLiquid.StartLiquidFill(bottleTargetML, pourDuration);
        cupLiquid.StartLiquidFill(cupTargetML, pourDuration);

        // Wait until pouring is complete
        yield return new WaitForSeconds(pourDuration);
    }
}