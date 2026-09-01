using UnityEngine;
using UnityEngine.UI; // Required for Slider and Text components
using UnityEngine.Events;

[System.Serializable]
public class FlaskSetup
{
    [Header("UI & Liquid Objects")]
    public Slider flaskSlider;
    public Transform liquidTransform; // The object with your water material

    [Header("Volume Display (ml)")]
    public Text volumeText;           // The UI Text to show the ML value
    public float maxMilliliters = 500f; // The maximum ML when the flask is 100% full

    [Header("Fill Settings")]
    public float emptyYScale = 0f;
    public float fullYScale = 1f;

    [Header("Target Settings")]
    [Range(0f, 1f)]
    public float targetValue = 0.5f;
    public float tolerance = 0.02f;

    [Header("Events")]
    public UnityEvent onTargetReached;

    [HideInInspector] public bool isLocked = false;
}

public class LiquidFillerManager : MonoBehaviour
{
    public FlaskSetup[] flasks = new FlaskSetup[3];

    void Start()
    {
        foreach (var flask in flasks)
        {
            if (flask.flaskSlider != null)
            {
                flask.flaskSlider.minValue = 0f;
                flask.flaskSlider.maxValue = 1f;

                // Initialize the text at start based on the slider's starting value
                UpdateVolumeText(flask, flask.flaskSlider.value);

                FlaskSetup currentFlask = flask;
                currentFlask.flaskSlider.onValueChanged.AddListener((val) => OnSliderChanged(currentFlask, val));
            }
        }
    }

    void OnSliderChanged(FlaskSetup flask, float currentValue)
    {
        if (flask.isLocked) return;

        // 1. Increase the liquid scale
        Vector3 newScale = flask.liquidTransform.localScale;
        newScale.y = Mathf.Lerp(flask.emptyYScale, flask.fullYScale, currentValue);
        flask.liquidTransform.localScale = newScale;

        // 2. Update the UI Text to show the current ml
        UpdateVolumeText(flask, currentValue);

        // 3. Check if target is reached
        if (Mathf.Abs(currentValue - flask.targetValue) <= flask.tolerance)
        {
            LockFlask(flask);
        }
    }

    void UpdateVolumeText(FlaskSetup flask, float sliderValue)
    {
        if (flask.volumeText != null)
        {
            // Calculate the current ml based on the slider's percentage
            float currentML = Mathf.Lerp(0f, flask.maxMilliliters, sliderValue);

            // Round it to a whole number and add " ml" to the end
            flask.volumeText.text = Mathf.RoundToInt(currentML).ToString() + " ml";
        }
    }

    void LockFlask(FlaskSetup flask)
    {
        flask.isLocked = true;

        // Snap slider and disable it
        flask.flaskSlider.value = flask.targetValue;
        flask.flaskSlider.interactable = false;

        // Ensure text shows the exact target amount when locked
        UpdateVolumeText(flask, flask.targetValue);

        // Trigger events
        flask.onTargetReached.Invoke();
    }
}