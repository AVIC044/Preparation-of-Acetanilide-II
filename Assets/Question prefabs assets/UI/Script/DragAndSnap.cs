using UnityEngine;
using UnityEngine.Events;

public class DragAndSnap : MonoBehaviour
{
    private Camera cam;

    private bool isDragging = false;
    private bool isReturning = false;
    private bool isSnapping = false;
    private bool isPlaced = false;

    private Vector3 startPosition;
    private Vector3 offset;

    [Header("Target Settings")]
    public Collider targetCollider;

    [Header("Snap Settings")]
    public Transform snapPoint;
    public float snapSpeed = 8f;

    [Header("Return Settings")]
    public float returnSpeed = 5f;

    [Header("Scale Settings")]
    [Tooltip("Object size multiplier while dragging.")]
    public float dragScaleMultiplier = 1.2f;

    [Tooltip("Speed of scaling animation.")]
    public float scaleSpeed = 8f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    [Header("Events")]
    public UnityEvent onCorrectCollision;
    public UnityEvent onWrongCollision;

    [Header("Drag Hint Material")]
    public Material dragMaterial;

    private Renderer[] objectRenderers;
    private Material[][] originalMaterials;

    [Header("Target Highlight")]
    public Renderer targetRenderer;

    private Material[] targetOriginalMaterials;
    void Start()
    {
        cam = Camera.main;

        startPosition = transform.position;

        originalScale = transform.localScale;
        targetScale = originalScale;

        // Get all renderers in this object and its children
        objectRenderers = GetComponentsInChildren<Renderer>(true);

        originalMaterials = new Material[objectRenderers.Length][];

        for (int i = 0; i < objectRenderers.Length; i++)
        {
            originalMaterials[i] = objectRenderers[i].materials;

            if (dragMaterial != null)
            {
                Material[] mats = objectRenderers[i].materials;

                for (int j = 0; j < mats.Length; j++)
                {
                    mats[j] = dragMaterial;
                }

                objectRenderers[i].materials = mats;
            }
        }
        if (targetRenderer != null)
        {
            targetOriginalMaterials = targetRenderer.materials;
        }
    }

    void OnMouseDown()
    {
        // Don't allow dragging after placement
        if (isPlaced)
            return;

        if (objectRenderers != null)
        {
            for (int i = 0; i < objectRenderers.Length; i++)
            {
                objectRenderers[i].materials = originalMaterials[i];
            }
        }

        isDragging = true;
        isReturning = false;

        // Highlight target while dragging
        if (targetRenderer != null && dragMaterial != null)
        {
            Material[] mats = targetRenderer.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = dragMaterial;
            }

            targetRenderer.materials = mats;
        }

        // Increase size while dragging
        targetScale = originalScale * dragScaleMultiplier;

        Vector3 mousePos = GetMouseWorldPos();
        offset = transform.position - mousePos;
    }

    void Update()
    {
        // Dragging
        if (isDragging)
        {
            Vector3 mousePos = GetMouseWorldPos();
            transform.position = mousePos + offset;
        }
        // Restore target material
      
        // Return to original position
        if (isReturning)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                startPosition,
                returnSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, startPosition) < 0.01f)
            {
                transform.position = startPosition;
            isReturning = false;

                if (!isPlaced && objectRenderers != null)
                {
                    for (int i = 0; i < objectRenderers.Length; i++)
                    {
                        Material[] mats = objectRenderers[i].materials;

                        for (int j = 0; j < mats.Length; j++)
                        {
                            mats[j] = dragMaterial;
                        }

                        objectRenderers[i].materials = mats;
                    }
                }
            }
        }

        // Snap to target
        if (isSnapping)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                snapPoint.position,
                snapSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                snapPoint.rotation,
                snapSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, snapPoint.position) < 0.01f)
            {
                transform.position = snapPoint.position;
                transform.rotation = snapPoint.rotation;

                // Restore original scale after snapping
                transform.localScale = originalScale;
                targetScale = originalScale;

                isSnapping = false;
            }
        }

        // Smooth scaling animation
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            scaleSpeed * Time.deltaTime);
    }

    void OnMouseUp()
    {
        if (isPlaced)
            return;

        isDragging = false;

        // Return to original size
        targetScale = originalScale;

        // Return to original position if not snapped
        // Return target material if not snapped
        if (!isSnapping)
        {
            isReturning = true;

            if (targetRenderer != null && targetOriginalMaterials != null)
            {
                targetRenderer.materials = targetOriginalMaterials;
            }
        }
    }

    Vector3 GetMouseWorldPos()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(-cam.transform.forward, startPosition);

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isPlaced)
            return;

        if (other == targetCollider)
        {
            Debug.Log("Correct Object Hit!");

            isDragging = false;
            isReturning = false;
            isSnapping = true;
            isPlaced = true;

            // Return to original size while snapping
            targetScale = originalScale;
            // Restore target material
            if (targetRenderer != null && targetOriginalMaterials != null)
            {
                targetRenderer.materials = targetOriginalMaterials;
            }
            onCorrectCollision?.Invoke();
        }
        else
        {
            Debug.Log("Wrong Object Hit!");

            onWrongCollision?.Invoke();
        }
    }
}