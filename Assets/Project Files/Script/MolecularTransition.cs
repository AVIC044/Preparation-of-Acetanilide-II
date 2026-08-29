using UnityEngine;
using System.Collections;

public class MolecularTransition : MonoBehaviour
{
    [Header("Slide Settings")]
    [Tooltip("The slide index that triggers the transition (e.g., 14).")]
    public int triggerSlideIndex = 14;

    [Tooltip("How many seconds to wait after reaching the slide before zooming.")]
    public float delayBeforeZoom = 2f;

    [Header("Transition Targets")]
    [Tooltip("Place an Empty GameObject right inside the flask liquid and assign it here.")]
    public Transform flaskZoomPoint;

    [Tooltip("The exact position and rotation the camera should snap to for the molecular view.")]
    public Transform molecularEntryPoint;

    [Header("Animation Settings")]
    [Tooltip("How many seconds it takes to zoom into the flask.")]
    public float zoomDuration = 2.5f;

    private Camera mainCam;
    private Coroutine transitionCoroutine;

    void Start()
    {
        // Automatically find the camera tagged as MainCamera
        mainCam = Camera.main;

        if (mainCam == null)
            mainCam = FindFirstObjectByType<Camera>();

        if (mainCam == null)
            Debug.LogError("No camera tagged 'MainCamera' was found in the scene!");
    }

    private void OnEnable()
    {
        // Listen for slide changes
        SlideController.OnSlideChanged += HandleSlideChanged;
    }

    private void OnDisable()
    {
        SlideController.OnSlideChanged -= HandleSlideChanged;
    }

    private void HandleSlideChanged(int currentSlideIndex)
    {
        // If the user reaches the specific slide, start the countdown and zoom
        if (currentSlideIndex == triggerSlideIndex)
        {
            // Stop any existing transition if there is one
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            transitionCoroutine = StartCoroutine(DiveIntoFlaskWithDelay());
        }
        else
        {
            // Optional: If the user quickly skips past slide 14 before the 2 seconds 
            // are up, this cancels the zoom so the camera doesn't randomly fly away.
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }
        }
    }

    private IEnumerator DiveIntoFlaskWithDelay()
    {
        // 1. Wait for 2 seconds
        yield return new WaitForSeconds(delayBeforeZoom);

        if (mainCam == null) yield break;

        // Record where the camera is starting from
        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        float elapsed = 0f;

        // 2. Smoothly fly the camera into the flask
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;

            // SmoothStep makes it start slow, speed up, and slow down at the end
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);

            mainCam.transform.position = Vector3.Lerp(startPos, flaskZoomPoint.position, t);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, flaskZoomPoint.rotation, t);

            yield return null; // Wait for the next frame
        }

        // 3. Instantly teleport to the molecular environment once we hit the liquid
        mainCam.transform.position = molecularEntryPoint.position;
        mainCam.transform.rotation = molecularEntryPoint.rotation;

        Debug.Log("Transitioned to the microscopic world!");

        // Clear the coroutine tracker once finished
        transitionCoroutine = null;
    }
}