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
        public int index;
        public GameObject highlightObject;

        [Header("Rotation")]
        public bool snapRotation = false;
        public float snapRotationThreshold = 0.5f;

        [Header("Mode")]
        public bool triggerEventOnly = false;

        [Header("Navigation")]
        public bool unlockPages = true;

        [Header("Events")]
        public UnityEvent onDragStart;
        public UnityEvent onSnapCompleted;

        [HideInInspector] public Collider highlightCollider;
        [HideInInspector] public bool snapped;
    }

    [Header("Snap Elements")]
    [SerializeField] private List<SnapElement> elements = new List<SnapElement>();

    [Header("Movement Settings")]
    [SerializeField] private float snapSpeed = 8f;
    [SerializeField] private float returnSpeed = 6f;
    [SerializeField] private float snapDistance = 0.01f;

    private PageNavigationController pageNavigationController;
    private Camera mainCam;
    private Collider objectCollider;

    private bool isDragging;
    private bool snapping;
    private bool returning;
    private bool canDrag;
    private bool interactionLocked;

    private int activeElementIndex = -1;
    private int lastSnappedElementIndex = -1;

    private Vector3 offset;
    private float objectScreenZ;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Awake()
    {
        pageNavigationController = FindFirstObjectByType<PageNavigationController>();
        CacheCamera();

        objectCollider = GetComponent<Collider>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        foreach (var element in elements)
        {
            if (element.highlightObject != null)
            {
                element.highlightCollider = element.highlightObject.GetComponent<Collider>();
                element.highlightObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void Start()
    {
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

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

        canDrag = false;
        activeElementIndex = -1;
    }

    void ResetState()
    {
        isDragging = false;
        snapping = false;
        returning = false;

        if (activeElementIndex >= 0 && activeElementIndex < elements.Count)
        {
            var el = elements[activeElementIndex];
            if (el.highlightObject != null && !el.snapped)
                el.highlightObject.SetActive(false);
        }
    }

    void ActivateElement(int index)
    {
        activeElementIndex = index;
        interactionLocked = false;

        var element = elements[index];

        if (element.snapped)
        {
            canDrag = false;
            interactionLocked = true;
        }
        else
        {
            canDrag = true;
        }
    }

    void Update()
    {
        if (returning)
        {
            ReturnToLastValidPosition();
            return;
        }

        if (activeElementIndex >= 0 && !elements[activeElementIndex].triggerEventOnly && snapping)
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
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
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
        if (activeElementIndex < 0) return;

        var element = elements[activeElementIndex];
        if (element.snapped) return;

        if (mainCam == null) CacheCamera();
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(inputPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider == objectCollider)
            {
                isDragging = true;
                element.onDragStart?.Invoke();

                objectScreenZ = mainCam.WorldToScreenPoint(transform.position).z;
                offset = transform.position - GetWorldPosition(inputPos);

                if (element.highlightObject != null)
                    element.highlightObject.SetActive(true);
            }
        }
    }

    void Drag(Vector3 inputPos)
    {
        transform.position = GetWorldPosition(inputPos) + offset;
    }

    void Release()
    {
        if (!isDragging) return;
        isDragging = false;

        if (activeElementIndex < 0)
        {
            StartReturn();
            return;
        }

        var element = elements[activeElementIndex];

        if (element.highlightCollider == null)
        {
            StartReturn();
            return;
        }

        bool inside = objectCollider.bounds.Intersects(element.highlightCollider.bounds);

        if (element.triggerEventOnly)
        {
            if (inside && !element.snapped)
            {
                CompleteSnap(element);
            }
            else
            {
                StartReturn();
            }
            return;
        }

        if (inside && !element.snapped)
        {
            snapping = true;
        }
        else
        {
            StartReturn();
        }
    }

    void SnapToHighlight()
    {
        var element = elements[activeElementIndex];

        if (element.highlightObject == null)
        {
            StartReturn();
            return;
        }

        Transform target = element.highlightObject.transform;
        float decay = 1f - Mathf.Exp(-snapSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, target.position, decay);

        if (element.snapRotation)
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, decay);

        bool posReached = Vector3.Distance(transform.position, target.position) < snapDistance;
        bool rotReached = !element.snapRotation || Quaternion.Angle(transform.rotation, target.rotation) < element.snapRotationThreshold;

        if (posReached && rotReached)
        {
            transform.position = target.position;
            if (element.snapRotation)
                transform.rotation = target.rotation;

            snapping = false;
            CompleteSnap(element);
        }
    }

    void CompleteSnap(SnapElement element)
    {
        element.snapped = true;
        lastSnappedElementIndex = activeElementIndex;

        canDrag = false;
        interactionLocked = true;

        if (element.highlightObject != null)
            element.highlightObject.SetActive(false);

        element.onSnapCompleted?.Invoke();

        if (pageNavigationController != null && element.unlockPages)
            pageNavigationController.EnableNavigationButtons();
    }

    void StartReturn()
    {
        returning = true;

        if (activeElementIndex >= 0)
        {
            var element = elements[activeElementIndex];
            if (element.highlightObject != null)
                element.highlightObject.SetActive(false);
        }
    }

    void ReturnToLastValidPosition()
    {
        Vector3 targetPos = originalPosition;
        Quaternion targetRot = originalRotation;
        bool applyRotation = false;

        if (lastSnappedElementIndex >= 0 && elements[lastSnappedElementIndex].highlightObject != null)
        {
            var lastSnappedElement = elements[lastSnappedElementIndex];
            Transform lastSnappedTransform = lastSnappedElement.highlightObject.transform;
            targetPos = lastSnappedTransform.position;
            applyRotation = lastSnappedElement.snapRotation;
            if (applyRotation) targetRot = lastSnappedTransform.rotation;
        }

        float decay = 1f - Mathf.Exp(-returnSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, targetPos, decay);
        if (applyRotation)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, decay);

        bool posReached = Vector3.Distance(transform.position, targetPos) < snapDistance;
        bool rotReached = !applyRotation || Quaternion.Angle(transform.rotation, targetRot) < 0.5f;

        if (posReached && rotReached)
        {
            transform.position = targetPos;
            if (applyRotation) transform.rotation = targetRot;

            returning = false;
        }
    }

    void CacheCamera()
    {
        mainCam = Camera.main;
        if (mainCam == null)
            mainCam = FindFirstObjectByType<Camera>();
    }

    Vector3 GetWorldPosition(Vector3 inputPos)
    {
        inputPos.z = objectScreenZ;
        return mainCam.ScreenToWorldPoint(inputPos);
    }
}