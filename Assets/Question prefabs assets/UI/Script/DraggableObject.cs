using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class DraggableObject : MonoBehaviour
{
    // =========================================================
    // SNAP ELEMENT
    // =========================================================

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

        [HideInInspector]
        public bool hasVisitedOnce = false;

        [HideInInspector]
        public Collider highlightCollider;

        [Tooltip("True once snapping is completed. Dragging will be disabled.")]
        [HideInInspector]
        public bool snapped;
    }


    // =========================================================
    // INSPECTOR
    // =========================================================

    [Header("Snap Elements")]
    [SerializeField]
    private List<SnapElement> elements = new List<SnapElement>();


    [Header("Movement")]
    [SerializeField]
    private float snapSpeed = 8f;

    [SerializeField]
    private float returnSpeed = 6f;

    [SerializeField]
    private float snapDistance = 0.01f;


    [Header("Rotation")]
    [SerializeField]
    private bool snapRotation = false;


    [Header("Mode")]
    [SerializeField]
    private bool triggerEventOnly = false;


    [Header("Animator Control")]
    [Tooltip("Animator attached to this object. The Animator Controller will NEVER be removed.")]
    [SerializeField]
    private Animator animator;


    [Header("Materials")]
    [Tooltip("Material shown while waiting to be dragged.")]
    [SerializeField]
    private Material blinkMaterial;


    [Header("Drag Events")]
    [SerializeField]
    private UnityEvent OnDragStart;


    // =========================================================
    // PRIVATE VARIABLES
    // =========================================================

    private Dictionary<Renderer, Material[]> originalMaterials =
        new Dictionary<Renderer, Material[]>();

    private Camera mainCam;

    private Collider objectCollider;


    private bool isDragging;

    private bool snapping;

    private bool returning;

    private bool canDrag;

    private bool interactionLocked;


    // NEW:
    // True after the object has successfully snapped.
    // Prevents the Animator from moving it away from the target.
    private bool keepSnappedPosition;


    private int activeElementIndex = -1;


    private Vector3 offset;

    private float objectScreenZ;


    private Vector3 originalPosition;

    private Quaternion originalRotation;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // Find camera
        mainCam = Camera.main;

        if (mainCam == null)
        {
            mainCam = FindFirstObjectByType<Camera>();
        }


        // Get collider
        objectCollider = GetComponent<Collider>();


        // =====================================================
        // AUTO-DETECT ANIMATOR
        // =====================================================

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }


        // =====================================================
        // STORE ORIGINAL POSITION
        // =====================================================

        originalPosition = transform.position;

        originalRotation = transform.rotation;


        // =====================================================
        // STORE ORIGINAL MATERIALS
        // =====================================================

        Renderer[] allRenderers =
            GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in allRenderers)
        {
            if (r != null && !originalMaterials.ContainsKey(r))
            {
                originalMaterials.Add(r, r.materials);
            }
        }


        // =====================================================
        // GET HIGHLIGHT COLLIDERS
        // =====================================================

        foreach (SnapElement element in elements)
        {
            if (element.highlightObject != null)
            {
                element.highlightCollider =
                    element.highlightObject.GetComponent<Collider>();


                // Hide target initially
                element.highlightObject.SetActive(false);
            }
        }
    }


    // =========================================================
    // ENABLE
    // =========================================================

    private void OnEnable()
    {
        SlideController.OnSlideChanged += HandlePageChanged;
    }


    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        SlideController.OnSlideChanged -= HandlePageChanged;
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Handle current slide
        HandlePageChanged(SlideController.CurrentIndex);
    }


    // =========================================================
    // SLIDE CHANGED
    // =========================================================

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

        SetBlinkState(false);
    }


    // =========================================================
    // RESET STATE
    // =========================================================

    private void ResetState()
    {
        isDragging = false;

        snapping = false;

        returning = false;

        // Important:
        // The object is no longer being position locked
        // until a successful snap happens.
        keepSnappedPosition = false;


        foreach (SnapElement element in elements)
        {
            if (element.highlightObject != null)
            {
                element.highlightObject.SetActive(false);
            }
        }
    }


    // =========================================================
    // ACTIVATE ELEMENT
    // =========================================================

    private void ActivateElement(int index)
    {
        activeElementIndex = index;

        interactionLocked = false;


        SnapElement element = elements[index];


        // =====================================================
        // FIRST IGNORE
        // =====================================================

        if (element.enableFirstIgnore &&
            !element.hasVisitedOnce)
        {
            element.hasVisitedOnce = true;

            canDrag = false;

            interactionLocked = true;

            SetBlinkState(false);

            return;
        }


        element.hasVisitedOnce = true;


        // =====================================================
        // ALREADY SNAPPED
        // =====================================================

        if (element.snapped)
        {
            canDrag = false;

            interactionLocked = true;

            SetBlinkState(false);
        }
        else
        {
            canDrag = true;

            SetBlinkState(true);
        }


        // =====================================================
        // RESTORE OBJECT TO SNAP POSITION
        // =====================================================

        if (element.restoreToSnapWhenConditionActive &&
            element.snapped &&
            element.highlightObject != null)
        {
            Transform target =
                element.highlightObject.transform;


            transform.position = target.position;


            if (snapRotation)
            {
                transform.rotation = target.rotation;
            }


            // Keep the object locked to target
            keepSnappedPosition = true;
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // SUCCESSFULLY SNAPPED
        // =====================================================

        if (keepSnappedPosition &&
            activeElementIndex >= 0)
        {
            MaintainSnappedPosition();

            return;
        }


        // =====================================================
        // RETURNING
        // =====================================================

        if (returning)
        {
            ReturnToOriginalPosition();

            return;
        }


        // =====================================================
        // SNAPPING
        // =====================================================

        if (!triggerEventOnly && snapping)
        {
            SnapToHighlight();

            return;
        }


        // =====================================================
        // NOT ALLOWED TO DRAG
        // =====================================================

        if (!canDrag || interactionLocked)
        {
            return;
        }


        HandleInput();
    }


    // =========================================================
    // HANDLE INPUT
    // =========================================================

    private void HandleInput()
    {
        // Ignore clicks over UI
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }


        // Mouse down
        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag(Input.mousePosition);
        }


        // Mouse drag
        if (isDragging &&
            Input.GetMouseButton(0))
        {
            Drag(Input.mousePosition);
        }


        // Mouse release
        if (isDragging &&
            Input.GetMouseButtonUp(0))
        {
            Release();
        }
    }


    // =========================================================
    // START DRAG
    // =========================================================

    private void TryStartDrag(Vector3 inputPos)
    {
        if (activeElementIndex < 0)
        {
            return;
        }


        SnapElement element =
            elements[activeElementIndex];


        // Don't allow dragging after snap
        if (element.snapped)
        {
            return;
        }


        Ray ray =
            mainCam.ScreenPointToRay(inputPos);


        RaycastHit hit;


        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider == objectCollider)
            {
                // Start dragging
                isDragging = true;


                // Trigger event
                OnDragStart?.Invoke();


                // Remove blink material
                SetBlinkState(false);


                // =================================================
                // DISABLE ANIMATOR DURING DRAG
                // =================================================

                if (animator != null &&
                    animator.enabled)
                {
                    animator.enabled = false;
                }


                // =================================================
                // CALCULATE SCREEN DEPTH
                // =================================================

                objectScreenZ =
                    mainCam.WorldToScreenPoint(
                        transform.position
                    ).z;


                // =================================================
                // CALCULATE DRAG OFFSET
                // =================================================

                offset =
                    transform.position -
                    GetWorldPosition(inputPos);


                // =================================================
                // SHOW TARGET
                // =================================================

                if (element.highlightObject != null)
                {
                    element.highlightObject.SetActive(true);
                }
            }
        }
    }


    // =========================================================
    // DRAG
    // =========================================================

    private void Drag(Vector3 inputPos)
    {
        transform.position =
            GetWorldPosition(inputPos) + offset;


        if (activeElementIndex < 0)
        {
            return;
        }


        SnapElement element =
            elements[activeElementIndex];


        // =====================================================
        // CHECK TARGET COLLISION
        // =====================================================

        if (element.highlightCollider != null &&
            element.highlightCollider.bounds.Intersects(
                objectCollider.bounds))
        {
            isDragging = false;


            if (element.highlightObject != null)
            {
                element.highlightObject.SetActive(false);
            }


            // =================================================
            // EVENT ONLY MODE
            // =================================================

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


    // =========================================================
    // RELEASE
    // =========================================================

    private void Release()
    {
        if (!isDragging)
        {
            return;
        }


        isDragging = false;


        if (activeElementIndex < 0)
        {
            StartReturn();

            return;
        }


        SnapElement element =
            elements[activeElementIndex];


        if (element.highlightObject != null)
        {
            element.highlightObject.SetActive(false);
        }


        // =====================================================
        // CORRECT TARGET
        // =====================================================

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
            // =================================================
            // WRONG DROP
            // =================================================

            StartReturn();
        }
    }


    // =========================================================
    // START RETURN
    // =========================================================

    private void StartReturn()
    {
        returning = true;

        keepSnappedPosition = false;

        SetBlinkState(true);
    }


    // =========================================================
    // RETURN TO ORIGINAL POSITION
    // =========================================================

    private void ReturnToOriginalPosition()
    {
        transform.position =
            Vector3.Lerp(
                transform.position,
                originalPosition,
                Time.deltaTime * returnSpeed
            );


        if (snapRotation)
        {
            transform.rotation =
                Quaternion.Lerp(
                    transform.rotation,
                    originalRotation,
                    Time.deltaTime * returnSpeed
                );
        }


        // =====================================================
        // RETURN COMPLETE
        // =====================================================

        if (Vector3.Distance(
                transform.position,
                originalPosition) < snapDistance)
        {
            transform.position =
                originalPosition;


            if (snapRotation)
            {
                transform.rotation =
                    originalRotation;
            }


            returning = false;


            // Re-enable Animator
            EnableAnimator();
        }
    }


    // =========================================================
    // SNAP TO HIGHLIGHT
    // =========================================================

    private void SnapToHighlight()
    {
        if (activeElementIndex < 0)
        {
            snapping = false;

            return;
        }


        SnapElement element =
            elements[activeElementIndex];


        if (element.highlightObject == null)
        {
            snapping = false;

            StartReturn();

            return;
        }


        Transform target =
            element.highlightObject.transform;


        // =====================================================
        // MOVE TO TARGET
        // =====================================================

        transform.position =
            Vector3.Lerp(
                transform.position,
                target.position,
                Time.deltaTime * snapSpeed
            );


        // =====================================================
        // ROTATE TO TARGET
        // =====================================================

        if (snapRotation)
        {
            transform.rotation =
                Quaternion.Lerp(
                    transform.rotation,
                    target.rotation,
                    Time.deltaTime * snapSpeed
                );
        }


        // =====================================================
        // SNAP COMPLETE
        // =====================================================

        if (Vector3.Distance(
                transform.position,
                target.position) < snapDistance)
        {
            transform.position =
                target.position;


            if (snapRotation)
            {
                transform.rotation =
                    target.rotation;
            }


            CompleteSnap(element);
        }
    }


    // =========================================================
    // COMPLETE SNAP
    // =========================================================

    private void CompleteSnap(SnapElement element)
    {
        snapping = false;

        isDragging = false;

        returning = false;


        // =====================================================
        // MARK AS SNAPPED
        // =====================================================

        element.snapped = true;

        canDrag = false;

        interactionLocked = true;


        // =====================================================
        // SET FINAL TARGET POSITION
        // =====================================================

        if (element.highlightObject != null)
        {
            Transform target =
                element.highlightObject.transform;


            transform.position =
                target.position;


            if (snapRotation)
            {
                transform.rotation =
                    target.rotation;
            }
        }


        // =====================================================
        // IMPORTANT
        // =====================================================
        // Once snapped, continuously maintain the target
        // position so the Animator cannot move the object away.
        // =====================================================

        keepSnappedPosition = true;


        // Remove blink material
        SetBlinkState(false);


        // =====================================================
        // ENABLE ANIMATOR
        // =====================================================
        //
        // This does NOT remove the Animator Controller.
        //
        // animator.enabled = true
        //
        // Controller remains assigned.
        // =====================================================

        EnableAnimator();


        // =====================================================
        // SNAP EVENT
        // =====================================================

        element.OnSnapCompleted?.Invoke();


        // =====================================================
        // UNLOCK NAVIGATION
        // =====================================================

        if (element.unlocknavigationOnSnap &&
            SlideController.Instance != null)
        {
            SlideController.Instance.MarkPageCompleted();
        }
    }


    // =========================================================
    // MAINTAIN SNAPPED POSITION
    // =========================================================

    private void MaintainSnappedPosition()
    {
        if (activeElementIndex < 0)
        {
            keepSnappedPosition = false;

            return;
        }


        SnapElement element =
            elements[activeElementIndex];


        if (!element.snapped)
        {
            keepSnappedPosition = false;

            return;
        }


        if (element.highlightObject == null)
        {
            return;
        }


        Transform target =
            element.highlightObject.transform;


        // =====================================================
        // FORCE POSITION TO TARGET
        // =====================================================

        transform.position =
            target.position;


        // =====================================================
        // FORCE ROTATION TO TARGET
        // =====================================================

        if (snapRotation)
        {
            transform.rotation =
                target.rotation;
        }
    }


    // =========================================================
    // WORLD POSITION
    // =========================================================

    private Vector3 GetWorldPosition(Vector3 screenPos)
    {
        screenPos.z = objectScreenZ;

        return mainCam.ScreenToWorldPoint(screenPos);
    }


    // =========================================================
    // ENABLE ANIMATOR
    // =========================================================

    private void EnableAnimator()
    {
        if (animator != null)
        {
            // IMPORTANT:
            // We ONLY enable the Animator.
            //
            // We NEVER do:
            //
            // animator.runtimeAnimatorController = null;
            //
            // Therefore the controller remains assigned.

            animator.enabled = true;
        }
    }


    // =========================================================
    // BLINK MATERIAL
    // =========================================================

    private void SetBlinkState(bool useBlink)
    {
        if (blinkMaterial == null)
        {
            return;
        }


        foreach (var kvp in originalMaterials)
        {
            Renderer r = kvp.Key;


            if (r == null)
            {
                continue;
            }


            if (useBlink)
            {
                Material[] blinkMats =
                    new Material[kvp.Value.Length];


                for (int i = 0;
                     i < blinkMats.Length;
                     i++)
                {
                    blinkMats[i] = blinkMaterial;
                }


                r.materials = blinkMats;
            }
            else
            {
                r.materials = kvp.Value;
            }
        }
    }
}

