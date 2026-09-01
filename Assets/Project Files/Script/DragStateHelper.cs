using UnityEngine;

public class DragStateHelper : MonoBehaviour
{
    [Header("References")]
    public Transform snapPoint;
    public MonoBehaviour dragScript;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;

    // Has the user already gone back to this page?
    private bool allowAutoSnap = false;

    void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;
    }

    public void ResetForBack()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        transform.localScale = startScale;

        // Now AutoSnap is allowed
        allowAutoSnap = true;

        if (dragScript != null)
            dragScript.enabled = false;
    }

    public void AutoSnapAfterBack()
    {
        // Ignore the first time the page is completed
        if (!allowAutoSnap)
            return;

        transform.position = snapPoint.position;
        transform.rotation = snapPoint.rotation;
        transform.localScale = startScale;

        if (dragScript != null)
            dragScript.enabled = false;
    }
}