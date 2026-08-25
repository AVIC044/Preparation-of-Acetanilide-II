using System.Collections.Generic;
using UnityEngine;

public class UIPromptController : MonoBehaviour
{
    // =========================================================
    // PAGE UI
    // =========================================================

    [System.Serializable]
    public class PageUI
    {
        public string pageName = "Page 1";

        public bool noUIInThisPage;

        public List<GameObject> uiItems = new List<GameObject>();
    }

    // =========================================================
    // PAGES
    // =========================================================

    [Header("PAGES")]
    [SerializeField]
    private List<PageUI> pages = new List<PageUI>();

    // =========================================================
    // START
    // =========================================================

    [Header("START")]
    [SerializeField]
    private bool hidePageUIOnStart = true;

    // =========================================================
    // CURRENT PAGE
    // =========================================================

    private int currentPageIndex = -1;

    public int CurrentPageIndex
    {
        get { return currentPageIndex; }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (hidePageUIOnStart)
        {
            HideAllPageUI();
        }
    }

    // =========================================================
    // SET CURRENT PAGE
    // =========================================================

    public void SetCurrentPage(int pageIndex)
    {
        if (!IsValidPage(pageIndex))
        {
            Debug.LogWarning(
                $"UIPromptController: Invalid page index {pageIndex}.",
                this);

            return;
        }

        currentPageIndex = pageIndex;
    }

    // =========================================================
    // CHANGE PAGE
    // =========================================================

    public void ChangePage(int pageIndex)
    {
        if (!IsValidPage(pageIndex))
        {
            Debug.LogWarning(
                $"UIPromptController: Invalid page index {pageIndex}.",
                this);

            return;
        }

        HideAllPageUI();

        currentPageIndex = pageIndex;
    }

    // =========================================================
    // SHOW UI
    // =========================================================

    public bool ShowUI(int pageIndex, int uiIndex)
    {
        if (!IsValidPage(pageIndex))
        {
            Debug.LogWarning(
                $"UIPromptController: Invalid page index {pageIndex}.",
                this);

            return false;
        }

        if (!IsValidUIItem(pageIndex, uiIndex))
        {
            Debug.LogWarning(
                $"UIPromptController: Invalid UI index {uiIndex} in Page {pageIndex + 1}.",
                this);

            return false;
        }

        GameObject uiObject = pages[pageIndex].uiItems[uiIndex];

        uiObject.SetActive(true);

        return true;
    }

    // =========================================================
    // SHOW CURRENT PAGE UI
    // =========================================================

    public bool ShowCurrentUI(int uiIndex)
    {
        if (!IsValidPage(currentPageIndex))
        {
            Debug.LogWarning(
                "UIPromptController: Current page is not set.",
                this);

            return false;
        }

        return ShowUI(currentPageIndex, uiIndex);
    }

    // =========================================================
    // HIDE UI
    // =========================================================

    public bool HideUI(int pageIndex, int uiIndex)
    {
        if (!IsValidPage(pageIndex))
        {
            Debug.LogWarning(
                $"UIPromptController: Invalid page index {pageIndex}.",
                this);

            return false;
        }

        if (!IsValidUIItem(pageIndex, uiIndex))
        {
            Debug.LogWarning(
                $"UIPromptController: Invalid UI index {uiIndex} in Page {pageIndex + 1}.",
                this);

            return false;
        }

        GameObject uiObject = pages[pageIndex].uiItems[uiIndex];

        uiObject.SetActive(false);

        return true;
    }

    // =========================================================
    // HIDE CURRENT PAGE UI
    // =========================================================

    public bool HideCurrentUI(int uiIndex)
    {
        if (!IsValidPage(currentPageIndex))
        {
            Debug.LogWarning(
                "UIPromptController: Current page is not set.",
                this);

            return false;
        }

        return HideUI(currentPageIndex, uiIndex);
    }

    // =========================================================
    // SHOW ALL UI IN PAGE
    // =========================================================

    public void ShowAllUIInPage(int pageIndex)
    {
        if (!IsValidPage(pageIndex))
        {
            return;
        }

        PageUI page = pages[pageIndex];

        if (page.noUIInThisPage)
        {
            return;
        }

        if (page.uiItems == null)
        {
            return;
        }

        foreach (GameObject uiObject in page.uiItems)
        {
            if (uiObject == null)
            {
                continue;
            }

            uiObject.SetActive(true);
        }
    }

    // =========================================================
    // HIDE ALL UI IN PAGE
    // =========================================================

    public void HideAllUIInPage(int pageIndex)
    {
        if (!IsValidPage(pageIndex))
        {
            return;
        }

        PageUI page = pages[pageIndex];

        if (page.noUIInThisPage)
        {
            return;
        }

        if (page.uiItems == null)
        {
            return;
        }

        foreach (GameObject uiObject in page.uiItems)
        {
            if (uiObject == null)
            {
                continue;
            }

            uiObject.SetActive(false);
        }
    }

    // =========================================================
    // HIDE ALL PAGE UI
    // =========================================================

    public void HideAllPageUI()
    {
        if (pages == null)
        {
            return;
        }

        foreach (PageUI page in pages)
        {
            if (page == null || page.uiItems == null)
            {
                continue;
            }

            foreach (GameObject uiObject in page.uiItems)
            {
                if (uiObject == null)
                {
                    continue;
                }

                uiObject.SetActive(false);
            }
        }
    }

    // =========================================================
    // SHOW GAMEOBJECT DIRECTLY
    // =========================================================

    public void ShowUI(GameObject uiObject)
    {
        if (uiObject != null)
        {
            uiObject.SetActive(true);
        }
    }

    // =========================================================
    // HIDE GAMEOBJECT DIRECTLY
    // =========================================================

    public void HideUI(GameObject uiObject)
    {
        if (uiObject != null)
        {
            uiObject.SetActive(false);
        }
    }

    // =========================================================
    // GET UI OBJECT
    // =========================================================

    public GameObject GetUIObject(int pageIndex, int uiIndex)
    {
        if (!IsValidUIItem(pageIndex, uiIndex))
        {
            return null;
        }

        return pages[pageIndex].uiItems[uiIndex];
    }

    // =========================================================
    // PAGE COUNT
    // =========================================================

    public int GetPageCount()
    {
        if (pages == null)
        {
            return 0;
        }

        return pages.Count;
    }

    // =========================================================
    // UI COUNT
    // =========================================================

    public int GetUIItemCount(int pageIndex)
    {
        if (!IsValidPage(pageIndex))
        {
            return 0;
        }

        PageUI page = pages[pageIndex];

        if (page.noUIInThisPage || page.uiItems == null)
        {
            return 0;
        }

        return page.uiItems.Count;
    }

    // =========================================================
    // CHECK PAGE HAS UI
    // =========================================================

    public bool PageHasUI(int pageIndex)
    {
        if (!IsValidPage(pageIndex))
        {
            return false;
        }

        PageUI page = pages[pageIndex];

        if (page.noUIInThisPage)
        {
            return false;
        }

        return page.uiItems != null &&
               page.uiItems.Count > 0;
    }

    // =========================================================
    // VALIDATE PAGE
    // =========================================================

    private bool IsValidPage(int pageIndex)
    {
        return pages != null &&
               pageIndex >= 0 &&
               pageIndex < pages.Count &&
               pages[pageIndex] != null;
    }

    // =========================================================
    // VALIDATE UI
    // =========================================================

    private bool IsValidUIItem(int pageIndex, int uiIndex)
    {
        if (!IsValidPage(pageIndex))
        {
            return false;
        }

        PageUI page = pages[pageIndex];

        if (page.noUIInThisPage ||
            page.uiItems == null)
        {
            return false;
        }

        if (uiIndex < 0 ||
            uiIndex >= page.uiItems.Count)
        {
            return false;
        }

        return page.uiItems[uiIndex] != null;
    }
}