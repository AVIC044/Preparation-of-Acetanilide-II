using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class DraggableObject : MonoBehaviour
{
    [System.Serializable]
    public class SnapElement
    {
        [Header("Slide Index")]
        public int index;

        public bool unlocknavigationOnSnap = true;

        [Header("Drop / Highlight Object")]
        public GameObject highlightObject;

        public bool restoreToSnapWhenConditionActive = true;

        public UnityEvent OnSnapCompleted;

        [Header("Display Options")]
        [Tooltip("If enabled, first time this index is reached, interaction will be ignored.")]
        public bool enableFirstIgnore = false;

        [HideInInspector] public bool hasVisitedOnce = false;
        [HideInInspector] public Collider highlightCollider;

        [Tooltip("True once snapping is completed. Dragging will be disabled.")]
        [HideInInspector] public bool snapped;
    }

    [Header("Snap Elements")]
    [SerializeField] private List<SnapElement> elements = new List<SnapElement>();

    [Header("Movement")]
    [SerializeField] private float snapSpeed = 8f;
    [SerializeField] private float returnSpeed = 6f;
    [SerializeField] private float snapDistance = 0.01f;

    [Header("Rotation")]
    [SerializeField] private bool snapRotation = false;

    [Header("Mode")]
    [SerializeField] private bool triggerEventOnly = false;

    [Header("Animator Control")]
    [SerializeField] private Animator animator;

    [Header("Materials")]
    [Tooltip("Material shown while waiting to be dragged.")]
    [SerializeField] private Material blinkMaterial;

    private Dictionary<Renderer, Material[]> originalMaterials =
        new Dictionary<Renderer, Material[]>();

    [Header("Drag Events")]
    [SerializeField] private UnityEvent OnDragStart;

    private Camera mainCam;
    private Collider objectCollider;

    private bool isDragging;
    private bool snapping;
    private bool returning;
    private bool canDrag;
    private bool interactionLocked;

    private int activeElementIndex = -1;

    private Vector3 offset;

    private float objectScreenZ;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Awake()
    {
        mainCam = Camera.main;

        if (mainCam == null)
            mainCam = FindFirstObjectByType<Camera>();

        objectCollider = GetComponent<Collider>();

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Automatically get all child renderers
        Renderer[] allRenderers =
            GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in allRenderers)
        {
            originalMaterials.Add(r, r.materials);
        }

        // Get collider from every drop/highlight object
        foreach (var element in elements)
        {
            if (element.highlightObject != null)
            {
                element.highlightCollider =
                    element.highlightObject.GetComponent<Collider>();

                // Hide drop area initially
                element.highlightObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        SlideController.OnSlideChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        SlideController.OnSlideChanged -= HandlePageChanged;
    }

    private void Start()
    {
        // Handle the currently active slide
        HandlePageChanged(SlideController.CurrentIndex);
    }

    // Called automatically when SlideController changes slide
    private void HandlePageChanged(int pageIndex)
    {
        ResetState();

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].index == pageIndex)
            {
                ActivateElement(i);
                return;
            }
        }

        // No matching slide
        canDrag = false;
        activeElementIndex = -1;

        SetBlinkState(false);
    }

    void ResetState()
    {
        isDragging = false;
        snapping = false;
        returning = false;

        // Hide all highlight objects when changing slide
        foreach (var element in elements)
        {
            if (element.highlightObject != null)
            {
                element.highlightObject.SetActive(false);
            }
        }
    }

    void ActivateElement(int index)
    {
        activeElementIndex = index;
        interactionLocked = false;

        var element = elements[index];

        if (element.enableFirstIgnore && !element.hasVisitedOnce)
        {
            element.hasVisitedOnce = true;
            canDrag = false;
            interactionLocked = true;

            SetBlinkState(false);
            return;
        }

        element.hasVisitedOnce = true;

        if (element.snapped)
        {
            canDrag = false;
            interactionLocked = true;

            SetBlinkState(false);
        }
        else
        {
            canDrag = true;

            // Show drag hint material
            SetBlinkState(true);
        }

        // If already snapped, restore position
        if (element.restoreToSnapWhenConditionActive &&
            element.snapped &&
            element.highlightObject != null)
        {
            Transform t = element.highlightObject.transform;

            transform.position = t.position;

            if (snapRotation)
                transform.rotation = t.rotation;
        }
    }

    void Update()
    {
        if (returning)
        {
            ReturnToLastValidPosition();
            return;
        }

        if (!triggerEventOnly && snapping)
        {
            SnapToHighlight();
            return;
        }

        if (!canDrag || interactionLocked)
            return;

        HandleInput();
    }

    void HandleInput()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
            TryStartDrag(Input.mousePosition);

        if (isDragging && Input.GetMouseButton(0))
            Drag(Input.mousePosition);

        if (isDragging && Input.GetMouseButtonUp(0))
            Release();
    }

    void TryStartDrag(Vector3 inputPos)
    {
        if (activeElementIndex < 0)
            return;

        var element = elements[activeElementIndex];

        if (element.snapped)
            return;

        Ray ray = mainCam.ScreenPointToRay(inputPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider == objectCollider)
            {
                isDragging = true;

                OnDragStart?.Invoke();

                // Restore original object materials
                SetBlinkState(false);

                if (animator != null && animator.enabled)
                    animator.enabled = false;

                objectScreenZ =
                    mainCam.WorldToScreenPoint(transform.position).z;

                offset =
                    transform.position -
                    GetWorldPosition(inputPos);

                // Show duplicate/drop area
                if (element.highlightObject != null)
                    element.highlightObject.SetActive(true);
            }
        }
    }

    void Drag(Vector3 inputPos)
    {
        transform.position =
            GetWorldPosition(inputPos) + offset;

        if (activeElementIndex >= 0)
        {
            var element =
                elements[activeElementIndex];

            if (element.highlightCollider != null &&
                element.highlightCollider.bounds.Intersects(
                    objectCollider.bounds))
            {
                // Stop dragging
                isDragging = false;

                // Hide duplicate/drop area
                if (element.highlightObject != null)
                    element.highlightObject.SetActive(false);

                if (triggerEventOnly)
                {
                    CompleteSnap(element);
                }
                else
                {
                    snapping = true;
                }
            }
        }
    }

    void Release()
    {
        if (!isDragging)
            return;

        isDragging = false;

        if (activeElementIndex < 0)
        {
            StartReturn();
            EnableAnimator();
            return;
        }

        var element =
            elements[activeElementIndex];

        // Hide drop area
        if (element.highlightObject != null)
            element.highlightObject.SetActive(false);

        // Check correct drop
        if (element.highlightCollider != null &&
            element.highlightCollider.bounds.Intersects(
                objectCollider.bounds))
        {
            if (triggerEventOnly)
            {
                CompleteSnap(element);
            }
            else
            {
                snapping = true;
            }
        }
        else
        {
            // Wrong drop
            StartReturn();
        }
    }

    void StartReturn()
    {
        returning = true;

        // Show drag hint again
        SetBlinkState(true);
    }

    void ReturnToLastValidPosition()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            originalPosition,
            Time.deltaTime * returnSpeed);

        if (snapRotation)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                originalRotation,
                Time.deltaTime * returnSpeed);
        }

        if (Vector3.Distance(
                transform.position,
                originalPosition) < snapDistance)
        {
            transform.position =
                originalPosition;

            if (snapRotation)
                transform.rotation =
                    originalRotation;

            returning = false;

            EnableAnimator();
        }
    }

    void SnapToHighlight()
    {
        var element =
            elements[activeElementIndex];

        Transform t =
            element.highlightObject.transform;

        transform.position = Vector3.Lerp(
            transform.position,
            t.position,
            Time.deltaTime * snapSpeed);

        if (snapRotation)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                t.rotation,
                Time.deltaTime * snapSpeed);
        }

        if (Vector3.Distance(
                transform.position,
                t.position) < snapDistance)
        {
            transform.position =
                t.position;

            if (snapRotation)
                transform.rotation =
                    t.rotation;

            CompleteSnap(element);
        }
    }

    void CompleteSnap(SnapElement element)
    {
        snapping = false;

        element.snapped = true;

        canDrag = false;

        // Restore original materials
        SetBlinkState(false);

        element.OnSnapCompleted?.Invoke();

        // Unlock navigation if enabled
        if (element.unlocknavigationOnSnap &&
            SlideController.Instance != null)
        {
            SlideController.Instance.MarkPageCompleted();
        }
    }

    Vector3 GetWorldPosition(Vector3 screenPos)
    {
        screenPos.z = objectScreenZ;

        return mainCam.ScreenToWorldPoint(screenPos);
    }

    void EnableAnimator()
    {
        if (animator != null)
            animator.enabled = true;
    }

    private void SetBlinkState(bool useBlink)
    {
        if (blinkMaterial == null)
            return;

        foreach (var kvp in originalMaterials)
        {
            Renderer r = kvp.Key;

            if (r == null)
                continue;

            if (useBlink)
            {
                Material[] blinkMats =
                    new Material[kvp.Value.Length];

                for (int i = 0;
                     i < blinkMats.Length;
                     i++)
                {
                    blinkMats[i] =
                        blinkMaterial;
                }

                r.materials = blinkMats;
            }
            else
            {
                r.materials =
                    kvp.Value;
            }
        }
    }
}