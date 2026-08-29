using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ObjectSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class SelectableItem
    {
        [Tooltip("The 3D object in the scene to click.")]
        public GameObject targetObject;

        [Tooltip("Is this one of the correct objects to find?")]
        public bool isCorrect;

        [Tooltip("The UI Canvas or Panel to show when this specific object is clicked.")]
        public GameObject uiToEnable;

        [HideInInspector] public bool hasBeenClicked = false;
    }

    [System.Serializable]
    public class SlideSelectionConfig
    {
        [Header("Slide Settings")]
        public int slideIndex;

        [Header("Objects to Click on this Slide")]
        public List<SelectableItem> items = new List<SelectableItem>();

        [Header("Slide Completion Event")]
        [Tooltip("This event fires when ALL correct objects on this specific slide have been clicked.")]
        public UnityEvent OnAllCorrectSelected;

        // Hidden trackers for logic
        [HideInInspector] public int totalCorrectRequired;
        [HideInInspector] public int correctFound;
        [HideInInspector] public bool isCompleted = false;
        [HideInInspector] public bool hasBeenInitialized = false;
    }

    [Header("Slide Configurations")]
    public List<SlideSelectionConfig> slideConfigurations = new List<SlideSelectionConfig>();

    private SlideSelectionConfig activeConfig;
    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        if (mainCam == null)
            mainCam = FindFirstObjectByType<Camera>();
    }

    private void OnEnable()
    {
        SlideController.OnSlideChanged += HandleSlideChanged;
    }

    private void OnDisable()
    {
        SlideController.OnSlideChanged -= HandleSlideChanged;
    }

    // Called automatically when the slide changes
    private void HandleSlideChanged(int currentSlideIndex)
    {
        activeConfig = null;

        foreach (var config in slideConfigurations)
        {
            if (config.slideIndex == currentSlideIndex)
            {
                activeConfig = config;

                // 1. Only do the math setup the VERY FIRST time we visit this slide
                if (!config.hasBeenInitialized)
                {
                    InitializeSlideConfig(config);
                }

                // 2. Restore the visuals based on what they already did
                RestoreSlideState(config);
                return;
            }
        }
    }

    // Counts the total correct items only once
    private void InitializeSlideConfig(SlideSelectionConfig config)
    {
        config.totalCorrectRequired = 0;
        config.correctFound = 0;
        config.isCompleted = false;

        foreach (var item in config.items)
        {
            item.hasBeenClicked = false;

            if (item.isCorrect)
            {
                config.totalCorrectRequired++;
            }
        }

        config.hasBeenInitialized = true;
    }

    // Restores UIs so they don't have to click them again if they leave and come back
    private void RestoreSlideState(SlideSelectionConfig config)
    {
        foreach (var item in config.items)
        {
            // If they clicked a WRONG item before leaving, let's hide it so they can try again.
            if (!config.isCompleted && !item.isCorrect && item.hasBeenClicked)
            {
                item.hasBeenClicked = false;
            }

            // Set the UI active if it has been successfully clicked previously
            if (item.uiToEnable != null)
            {
                item.uiToEnable.SetActive(item.hasBeenClicked);
            }
        }

        if (config.isCompleted)
        {
            Debug.Log($"[SelectionManager] Returned to Slide {config.slideIndex}. Already completed! Showing final visuals.");
        }
        else
        {
            int remaining = config.totalCorrectRequired - config.correctFound;
            Debug.Log($"[SelectionManager] Slide {config.slideIndex} active. Need to find {remaining} more correct items.");
        }
    }

    void Update()
    {
        // Don't do anything if we aren't on a configured slide, or if we already finished it!
        if (activeConfig == null || activeConfig.isCompleted)
            return;

        // Prevent clicking through UI buttons
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Detect Mouse Clicks
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CheckClickedObject(hit.collider.gameObject);
            }
        }
    }

    public void CheckClickedObject(GameObject clickedObj)
    {
        foreach (var item in activeConfig.items)
        {
            // If the object matches and hasn't been clicked yet
            if (item.targetObject == clickedObj && !item.hasBeenClicked)
            {
                item.hasBeenClicked = true;

                // Enable its specific UI
                if (item.uiToEnable != null)
                {
                    item.uiToEnable.SetActive(true);
                }

                // If it was a correct object, increase our score
                if (item.isCorrect)
                {
                    activeConfig.correctFound++;
                    Debug.Log($"Correct item found on slide {activeConfig.slideIndex}! ({activeConfig.correctFound}/{activeConfig.totalCorrectRequired})");

                    // Check if we found all of them
                    if (activeConfig.correctFound >= activeConfig.totalCorrectRequired)
                    {
                        activeConfig.isCompleted = true;
                        Debug.Log($"All correct items found for slide {activeConfig.slideIndex}!");

                        // Trigger the event specifically for this slide
                        activeConfig.OnAllCorrectSelected?.Invoke();
                    }
                }

                // Stop searching the list since we found the match
                break;
            }
        }
    }
}