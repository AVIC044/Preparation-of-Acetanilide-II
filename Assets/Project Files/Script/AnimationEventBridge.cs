using UnityEngine;

public class AnimationEventBridge : MonoBehaviour
{
    [Tooltip("Drag your PourManager here")]
    public PourManager pourManager;

    // Call this from the Animation Event
    public void TriggerPour()
    {
        if (pourManager != null)
        {
            pourManager.StartLiquidTransfer();
        }
    }
}