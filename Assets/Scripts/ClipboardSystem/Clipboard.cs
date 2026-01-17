using System.Security.Cryptography;
using UnityEngine;

public class Clipboard : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    [SerializeField] RectTransform imagesRectTransform;
    [SerializeField] RectTransform pagesFlippedDownRectTransform;
    [SerializeField] RectTransform pagesFlippedUpRectTransform;
    [SerializeField] RectTransform clipRectTransform;
    [SerializeField] RectTransform clipboardTab;

    [SerializeField] ClipboardSettingsSO settings;
    [SerializeField] ClipboardStatsSO stats;
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] GameEventDataSO gameEvents;
    Page[] pages;
    Page curPage;
    private void Start()
    {
        stats.cacheStats.imagesStartYPos = imagesRectTransform.localPosition.y;
        stats.cacheStats.imagesTargetYPos = imagesRectTransform.localPosition.y;
        stats.cacheStats.tabStartYPos = clipboardTab.localPosition.y;
        stats.cacheStats.tabTargetYPos = clipboardTab.localPosition.y;
        stats.cacheStats.ditherTransitionValue = 1;
        stats.tempStats = default;

        clipRectTransform.localPosition = new Vector3(clipRectTransform.localPosition.x, clipRectTransform.localPosition.y, -stats.profilePageArray.Length); // send clip to front
        CreatePages();
    }

    private void OnEnable()
    {
        gameEvents.OnFlipDownPage.RegisterListener(FlippedDownPage);
        gameEvents.OnFlipUpPage.RegisterListener(FlippedUpPage);
    }

    private void OnDisable()
    {
        gameEvents.OnFlipDownPage.UnregisterListener(FlippedDownPage);
        gameEvents.OnFlipUpPage.UnregisterListener(FlippedUpPage);
        
    }
    private void Update()
    {
        HandlePageInputs();
        HandleClipboardMovement();
        ActivateClipboard();
    }
    private void HandlePageInputs()
    {
        /*
         * First checking the clipboard is active and skip the first frame using the mouse left down input so the input stats update before the clipbaord stats
         * mouse left up needs to be checked before anything else because the player may release the mouse outside the clipboard
         * Then check to make sure the player is holding the mouse button
         * Then checking to see if that start drag pos is within the x axis range of the clipboard
         * The raw height is the difference between the cur mouse pos and start drag pos
         * I dont update the page straight away because pages will flip by accident. I use a buffer to make sure the player wants to flip the page.
         * flipping up or down is dependant on the sign of the raw height
         * I use the button click type as a one frame initilizer to prepare to flip the page
         * depending on the direction of the flip, I either use the distance from start drag y pos to the top of the screen or bottom
         * When the player flips down I need to target the previous index of the page array
         * The progress of the drag is normalized and used as an offset for the final noramlized value to give to the page material for the flipping animation
         * When flipping up the page needs to update its rendering order immediately. Flippind down is the opposite where the rendering order updates on the last frame
         * Calling UpdateFlip manually sets the material of the current page for the flipping animation
         */
        if (!stats.tempStats.active || playerInputs.mouseLeftDown) return;

        if (playerInputs.mouseLeftUp && stats.tempStats.buttonTypeClicked == ClipboardStatsSO.ButtonTypeClicked.Page)
        {
            curPage.AutoFlip();
            stats.tempStats.startDragMouseT = 0;
            stats.tempStats.buttonTypeClicked = ClipboardStatsSO.ButtonTypeClicked.None;
            stats.tempStats.rawHeight = 0;
        }

        if (!playerInputs.mouseLeftPress || stats.tempStats.hoverTab) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(imagesRectTransform, playerInputs.startDragMouseScreenPos, Camera.main, out Vector2 startDragMousePosInPageRect);
        
        if (startDragMousePosInPageRect.x < imagesRectTransform.rect.xMin || startDragMousePosInPageRect.x > imagesRectTransform.rect.xMax) return;

        stats.tempStats.rawHeight = playerInputs.mouseScreenPos.y - playerInputs.startDragMouseScreenPos.y;

        if (Mathf.Abs(stats.tempStats.rawHeight) < Screen.height * settings.dragToFlipPageThreshold) return;

        stats.tempStats.flipUp = stats.tempStats.rawHeight > 0;

        if (stats.tempStats.buttonTypeClicked != ClipboardStatsSO.ButtonTypeClicked.Page)
        {
            if (stats.tempStats.flipUp)
            {
                stats.tempStats.flipDist = Screen.height - playerInputs.startDragMouseScreenPos.y;
            }
            else
            {
                if (stats.tempStats.curPageIndex != stats.profilePageArray.Length || !curPage.flipped)
                {
                    stats.tempStats.curPageIndex--;
                    stats.tempStats.curPageIndex = Mathf.Clamp(stats.tempStats.curPageIndex, 0, stats.profilePageArray.Length);
                    curPage = pages[stats.tempStats.curPageIndex];
                }
                stats.tempStats.startDragMouseT = 1;
                stats.tempStats.flipDist = playerInputs.startDragMouseScreenPos.y;
            }
            stats.tempStats.buttonTypeClicked = ClipboardStatsSO.ButtonTypeClicked.Page;
        }
        
        float normCurHeight = stats.tempStats.rawHeight / stats.tempStats.flipDist;

        stats.tempStats.curDragMouseT = Mathf.Clamp01(stats.tempStats.startDragMouseT + normCurHeight);


        if (stats.tempStats.flipUp)
        {
            if (curPage.transform.parent != pagesFlippedUpRectTransform)
            {
                curPage.FlipUp();
                curPage.transform.SetParent(pagesFlippedUpRectTransform, worldPositionStays: false);
            }
        }

        curPage.UpdateFlip();
    }
    private void ActivateClipboard()
    {
        stats.tempStats.hoverTab = RectTransformUtility.RectangleContainsScreenPoint(clipboardTab, playerInputs.mouseScreenPos, Camera.main);
        bool clickedTab = stats.tempStats.hoverTab && playerInputs.mouseLeftDown && stats.tempStats.curPageIndex == 0;

        if (clickedTab)
        {
            stats.tempStats.active = !stats.tempStats.active;
            stats.cacheStats.imagesTargetYPos = stats.tempStats.active ? settings.imagesOnScreenYPos : stats.cacheStats.imagesStartYPos;
            stats.cacheStats.tabTargetYPos = stats.cacheStats.tabStartYPos;
            stats.tempStats.buttonTypeClicked = ClipboardStatsSO.ButtonTypeClicked.Tab;
        }
        else if (stats.tempStats.hoverTab)
        {
            stats.cacheStats.tabTargetYPos = stats.cacheStats.tabStartYPos + settings.tabHoverYPos;
        }
        else
        {
            stats.cacheStats.tabTargetYPos = stats.cacheStats.tabStartYPos;
        }
    }
    private void HandleClipboardMovement()
    {
        float curRectYPos = Mathf.Lerp(imagesRectTransform.localPosition.y, stats.cacheStats.imagesTargetYPos, settings.moveTime * Time.deltaTime);
        imagesRectTransform.localPosition = new Vector3(imagesRectTransform.localPosition.x, curRectYPos, imagesRectTransform.localPosition.z);

        float curTabYPos = Mathf.Lerp(clipboardTab.localPosition.y, stats.cacheStats.tabTargetYPos, settings.moveTime * Time.deltaTime);
        clipboardTab.localPosition = new Vector3(clipboardTab.localPosition.x, curTabYPos, imagesRectTransform.localPosition.z);
    }
    private void CreatePages()
    {
        /*
         * The pages array is created with a length of the profilePageArray length + 1. 
         * The first page is the front page hence the + 1.
         * In the for loop, each page is set to be the first sibling so the rendering order is correct.
         * This is due to how the render order of Image types are dealt with by Unity.
         */
        pages = new Page[stats.profilePageArray.Length + 1];
        pages[0] = Instantiate(settings.frontPagePrefab, pagesFlippedDownRectTransform);
        curPage = pages[0];
        curPage.transform.SetAsFirstSibling();
        for (int i = 0; i < stats.profilePageArray.Length; i++)
        {
            int curPageIndex = i + 1;
            pages[curPageIndex] = Instantiate(settings.profilePagePrefab, pagesFlippedDownRectTransform);

            pages[curPageIndex].transform.SetAsFirstSibling(); // first index gets rendered last
            float offsetX = UnityEngine.Random.Range(-settings.randomPixelOffsetForPage, settings.randomPixelOffsetForPage);
            float offsetY = UnityEngine.Random.Range(-settings.randomPixelOffsetForPage, settings.randomPixelOffsetForPage);
            pages[curPageIndex].rectTransform.anchoredPosition += new Vector2(offsetX, offsetY);

            pages[curPageIndex].SetProfilePageParams(i);
        }
    }
    private void FlippedDownPage()
    {
        curPage.transform.SetParent(pagesFlippedDownRectTransform, worldPositionStays: false);
    }
    private void FlippedUpPage()
    {
        if (stats.tempStats.curPageIndex == pages.Length - 1) return;
        stats.tempStats.curPageIndex++;
        curPage = pages[stats.tempStats.curPageIndex];
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        DrawUISquare(imagesRectTransform, Color.red, settings.imagesOnScreenYPos);
        DrawUISquare(clipboardTab, Color.green, settings.tabHoverYPos);
    }
    private void DrawUISquare(RectTransform rectTransform, Color color, float height)
    {
        Gizmos.color = color;
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector3 worldOffset = rectTransform.TransformVector(new Vector3(0f, height, 0f));

        for (int i = 0; i < 4; i++) corners[i] += worldOffset;

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }
#endif
}
