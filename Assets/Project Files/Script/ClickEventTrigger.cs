
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ClickEventTrigger : MonoBehaviour
{
    [Header("Click Event")]
    public UnityEvent OnObjectClicked;

    private void OnMouseDown()
    {
        // Trigger the event when this object is clicked
        OnObjectClicked?.Invoke();
    }
}

