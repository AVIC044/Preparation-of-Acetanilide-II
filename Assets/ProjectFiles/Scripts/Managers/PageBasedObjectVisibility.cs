using System.Collections.Generic;
using UnityEngine;

public class PageBasedObjectVisibility : MonoBehaviour
{
    [Header("Target GameObject")]
    [SerializeField] private GameObject targetObject;

    [Header("Visible On These Page Indexes")]
    [SerializeField] private List<int> pageIndexes = new List<int>();

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += OnPageChanged;
    }

    private void Start()
    {
        UpdateVisibility(PageNavigationController.CurrentIndex);
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= OnPageChanged;
    }

    private void OnPageChanged(int currentPageIndex)
    {
        UpdateVisibility(currentPageIndex);
    }

    private void UpdateVisibility(int currentPageIndex)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target GameObject is not assigned.", this);
            return;
        }

        // Show only when current page index is in the list
        bool shouldBeVisible = pageIndexes.Contains(currentPageIndex);

        targetObject.SetActive(shouldBeVisible);
    }
}