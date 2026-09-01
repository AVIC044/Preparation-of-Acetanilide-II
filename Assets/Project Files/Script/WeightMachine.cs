using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeightMachine : MonoBehaviour
{
    [Header("Weight")]
    public float currentWeight = 0f;
    public float targetWeight = 0f;

    [Header("Speed")]
    public float increaseSpeed = 10f;

    [Header("Weight Display")]
    public TMP_Text weightText;

    private bool increasing = false;

    private void Update()
    {
        if (!increasing)
            return;

        // Slowly increase the current weight
        currentWeight = Mathf.MoveTowards(
            currentWeight,
            targetWeight,
            increaseSpeed * Time.deltaTime
        );

        UpdateWeightDisplay();

        // Stop when target is reached
        if (Mathf.Approximately(currentWeight, targetWeight))
        {
            currentWeight = targetWeight;
            increasing = false;

            Debug.Log("Target weight reached: " + currentWeight + " g");
        }
    }

    // =====================================================
    // CALL THIS FUNCTION WHEN SALT IS ADDED
    // =====================================================

    public void AddWeight(float grams)
    {
        targetWeight += grams;

        increasing = true;

        Debug.Log("New target weight: " + targetWeight + " g");
    }

    // =====================================================
    // UPDATE DISPLAY
    // =====================================================

    private void UpdateWeightDisplay()
    {
        if (weightText != null)
        {
            weightText.text = currentWeight.ToString("0") + " g";
        }
    }
}