using Cysharp.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


public class Clipboard : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    [SerializeField] RectTransform imagesRectTransform;
    [SerializeField] RectTransform profilePagesNotFlippedRectTransform;
    [SerializeField] RectTransform profilePagesFlippedRectTransform;
    [SerializeField] RectTransform clipRectTransform;
    [SerializeField] RectTransform clipboardTab;
    [SerializeField] ClipboardSettingsSO settings;
    [SerializeField] ClipboardStatsSO stats;
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] MaterialIDSO materialIDs;

    Page[] pages;
    private void Start()
    {
        stats.tempStats.imagesStartYPos = imagesRectTransform.localPosition.y;
        stats.tempStats.imagesTargetYPos = imagesRectTransform.localPosition.y;
        stats.tempStats.tabStartYPos = clipboardTab.localPosition.y;
        stats.tempStats.tabTargetYPos = clipboardTab.localPosition.y;
        stats.tempStats.active = false;
        stats.tempStats.curPageIndex = 0;
        stats.tempStats.canClickID = false;
        stats.tempStats.ditherTransitionValue = 1;
        stats.tempStats.curDragMouseT = 0;
        Vector2 flippedPageAtlasSize = settings.flippedPagePrefab.material.GetVector(materialIDs.ids.atlasSize);
        stats.tempStats.flipPageAtlasUnitSize = 1 / (flippedPageAtlasSize.x * flippedPageAtlasSize.y);
        clipRectTransform.localPosition = new Vector3(clipRectTransform.localPosition.x, clipRectTransform.localPosition.y, -stats.profilePageArray.Length); // send clip to front
        CreatePages();
    }
    private void Update()
    {
        HandleInputs();
        HandleMovement();
        ActivateClipboard();
    }
    private void HandleInputs()
    {
        if (playerInputs.mouseLeftUp) stats.tempStats.prevCurDragMouseT = stats.tempStats.curDragMouseT;
        if (!stats.tempStats.active || !playerInputs.mouseLeftPress) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(imagesRectTransform, playerInputs.startDragMouseScreenPos, Camera.main, out Vector2 mousePosInImageRect);
        
        if (mousePosInImageRect.x < imagesRectTransform.rect.xMin || mousePosInImageRect.x > imagesRectTransform.rect.xMax) return; // Only test x position

        float normCurHeight = (playerInputs.mouseScreenPos.y - playerInputs.startDragMouseScreenPos.y) / imagesRectTransform.rect.height;
        stats.tempStats.curDragMouseT = Mathf.Clamp01(stats.tempStats.prevCurDragMouseT + normCurHeight);

        stats.tempStats.flipUp = normCurHeight > 0;


        if (stats.tempStats.flipUp)
        {
            if (pages[stats.tempStats.curPageIndex].transform.parent != profilePagesFlippedRectTransform)
            {
                pages[stats.tempStats.curPageIndex].FlipUp();
                pages[stats.tempStats.curPageIndex].transform.SetParent(profilePagesFlippedRectTransform, worldPositionStays: false);
            }
        }
        else
        {
            if (pages[stats.tempStats.curPageIndex].transform.parent != profilePagesNotFlippedRectTransform && stats.tempStats.curDragMouseT < stats.tempStats.flipPageAtlasUnitSize)
            {
                pages[stats.tempStats.curPageIndex].FlipDown();
                pages[stats.tempStats.curPageIndex].transform.SetParent(profilePagesNotFlippedRectTransform, worldPositionStays: false);
            }
        }

        pages[stats.tempStats.curPageIndex].UpdateFlip();

    }

    private void ActivateClipboard()
    {
        bool hoveredTab = RectTransformUtility.RectangleContainsScreenPoint(clipboardTab, playerInputs.mouseScreenPos, Camera.main);
        bool clickedTab = hoveredTab && playerInputs.mouseLeftDown;

        if (clickedTab)
        {
            stats.tempStats.active = !stats.tempStats.active;
            stats.tempStats.imagesTargetYPos = stats.tempStats.active ? settings.imagesOnScreenYPos : stats.tempStats.imagesStartYPos;
            stats.tempStats.tabTargetYPos = stats.tempStats.tabStartYPos;
        }
        else if (hoveredTab)
        {
            stats.tempStats.tabTargetYPos = stats.tempStats.tabStartYPos + settings.tabHoverYPos;
        }
        else
        {
            stats.tempStats.tabTargetYPos = stats.tempStats.tabStartYPos;
        }
    }
    private void HandleMovement()
    {
        float curRectYPos = Mathf.Lerp(imagesRectTransform.localPosition.y, stats.tempStats.imagesTargetYPos, settings.moveTime * Time.deltaTime);
        imagesRectTransform.localPosition = new Vector3(imagesRectTransform.localPosition.x, curRectYPos, imagesRectTransform.localPosition.z);

        float curTabYPos = Mathf.Lerp(clipboardTab.localPosition.y, stats.tempStats.tabTargetYPos, settings.moveTime * Time.deltaTime);
        clipboardTab.localPosition = new Vector3(clipboardTab.localPosition.x, curTabYPos, imagesRectTransform.localPosition.z);
    }
    private void CreatePages()
    {
        pages = new Page[stats.profilePageArray.Length + 1]; // +1 for front page
        pages[0] = Instantiate(settings.frontPagePrefab, profilePagesNotFlippedRectTransform);
        pages[0].transform.SetAsFirstSibling();

        for (int i = 1; i <= stats.profilePageArray.Length; i++)
        {
            pages[i] = Instantiate(settings.profilePagePrefab, profilePagesNotFlippedRectTransform);

            pages[i].transform.SetAsFirstSibling(); // first index gets rendered last
            float offsetX = UnityEngine.Random.Range(-settings.randomPixelOffsetForPage, settings.randomPixelOffsetForPage);
            float offsetY = UnityEngine.Random.Range(-settings.randomPixelOffsetForPage, settings.randomPixelOffsetForPage);
            pages[i].rectTransform.anchoredPosition += new Vector2(offsetX, offsetY);

            pages[i].SetPageParams(i - 1);
        }
    }
    //private async UniTask FlippingUpPage()
    //{
    //    int curIndex = stats.tempStats.curPageIndex;
    //    profilePages[curIndex].Flipped(flippedDown: false);
    //    profilePages[curIndex].transform.SetParent(profilePagesFlippedRectTransform, worldPositionStays: false);
    //    float elapsedTime = 0;
    //    while (elapsedTime < settings.flipPageTime)
    //    {
    //        elapsedTime += Time.deltaTime;

    //        profilePages[curIndex].flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, elapsedTime / settings.flipPageTime);
    //        await UniTask.Yield();
    //    }
    //    profilePages[curIndex].flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, 1);
    //    stats.tempStats.curPageIndex++;
    //    stats.tempStats.flippingPage = false;
    //}
    //private async UniTask FlippingDownPage()
    //{
    //    stats.tempStats.curPageIndex--;
    //    int curIndex = stats.tempStats.curPageIndex;
    //    float elapsedTime = settings.flipPageTime;
    //    while (elapsedTime > 0)
    //    {
    //        elapsedTime -= Time.deltaTime;
    //        profilePages[curIndex].flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, elapsedTime / settings.flipPageTime);
    //        await UniTask.Yield();
    //    }
    //    profilePages[curIndex].Flipped(flippedDown: true);
    //    profilePages[curIndex].flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, 0);
    //    profilePages[curIndex].transform.SetParent(profilePagesNotFlippedRectTransform, worldPositionStays: false);
    //    profilePages[curIndex].transform.SetAsLastSibling();
    //}
    //private async UniTask FlippingUpFrontPage()
    //{
    //    frontPage.Flipped(flippedDown: false);
    //    frontPage.transform.SetParent(profilePagesFlippedRectTransform, worldPositionStays: false);
    //    float elapsedTime = 0;
    //    while (elapsedTime < settings.flipPageTime)
    //    {
    //        elapsedTime += Time.deltaTime;
    //        frontPage.flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, elapsedTime / settings.flipPageTime);
    //        await UniTask.Yield();
    //    }
    //    frontPage.flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, 1);
    //    stats.tempStats.curPageIndex++;
    //}
    //private async UniTask FlippingDownFrontPage()
    //{
    //    stats.tempStats.curPageIndex--;
    //    float elapsedTime = settings.flipPageTime;
    //    while (elapsedTime > 0)
    //    {
    //        elapsedTime -= Time.deltaTime;
    //        frontPage.flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, elapsedTime / settings.flipPageTime);
    //        await UniTask.Yield();
    //    }
    //    frontPage.Flipped(flippedDown: true);
    //    frontPage.flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, 0);
    //    frontPage.transform.SetParent(profilePagesNotFlippedRectTransform, worldPositionStays: false);
    //    frontPage.transform.SetAsLastSibling();
    //    stats.tempStats.flippingPage = false;
    //}
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
}
