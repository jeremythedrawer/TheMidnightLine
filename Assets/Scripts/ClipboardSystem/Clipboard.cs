using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using static ClipboardStatsSO;

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
    [SerializeField] PlayerInputsSO inputs;


    ProfilePage[] profilePages;
    FrontPage frontPage;
    private void Awake()
    {
        stats.materialIDs.normAnimTime = Shader.PropertyToID("_NormAnimTime");
    }
    private void Start()
    {
        stats.tempStats.imagesStartYPos = imagesRectTransform.localPosition.y;
        stats.tempStats.imagesTargetYPos = imagesRectTransform.localPosition.y;
        stats.tempStats.tabStartYPos = clipboardTab.localPosition.y;
        stats.tempStats.tabTargetYPos = clipboardTab.localPosition.y;
        stats.tempStats.active = false;
        stats.tempStats.flippingPage = false;
        stats.tempStats.curPageIndex = -1;
        clipRectTransform.localPosition = new Vector3(clipRectTransform.localPosition.x, clipRectTransform.localPosition.y, -stats.profilePageArray.Length); // send clip to front
        CreateProfilePages();
    }
    private void Update()
    {
        HandleInputs();
        HandleMovement();
    }
    private void HandleInputs()
    {
        bool hovered = RectTransformUtility.RectangleContainsScreenPoint(clipboardTab,inputs.mouseScreenPos,Camera.main);
        bool clicked = hovered && inputs.mouseLeftDown;

        if (clicked)
        {
            stats.tempStats.active = !stats.tempStats.active;

            stats.tempStats.imagesTargetYPos = stats.tempStats.active
                ? settings.imagesOnScreenYPos
                : stats.tempStats.imagesStartYPos;

            stats.tempStats.tabTargetYPos = stats.tempStats.tabStartYPos;
        }
        else if (hovered)
        {
            stats.tempStats.tabTargetYPos =
                stats.tempStats.tabStartYPos + settings.tabHoverYPos;
        }
        else
        {
            stats.tempStats.tabTargetYPos = stats.tempStats.tabStartYPos;
        }

        if (!stats.tempStats.active || stats.tempStats.flippingPage || inputs.mouseScroll == 0)return;

        if (inputs.mouseScroll > 0)
        {
            if (stats.tempStats.curPageIndex == -1)
            {
                FlippingUpFrontPage().Forget();
            }
            else if (stats.tempStats.curPageIndex < profilePages.Length)
            {
                FlippingUpPage().Forget();
            }

            stats.tempStats.flippingPage = true;
        }
        else if (inputs.mouseScroll < 0 && stats.tempStats.curPageIndex > -1)
        {
            if (stats.tempStats.curPageIndex == 0)
            {
                FlippingDownFrontPage().Forget();
            }
            else
            {
                FlippingDownPage().Forget();
            }

            stats.tempStats.flippingPage = true;
        }
    }
    private void HandleMovement()
    {
        float curRectYPos = Mathf.Lerp(imagesRectTransform.localPosition.y, stats.tempStats.imagesTargetYPos, settings.moveTime * Time.deltaTime);
        imagesRectTransform.localPosition = new Vector3(imagesRectTransform.localPosition.x, curRectYPos, imagesRectTransform.localPosition.z);

        float curTabYPos = Mathf.Lerp(clipboardTab.localPosition.y, stats.tempStats.tabTargetYPos, settings.moveTime * Time.deltaTime);
        clipboardTab.localPosition = new Vector3(clipboardTab.localPosition.x, curTabYPos, imagesRectTransform.localPosition.z);
    }
    private void CreateProfilePages()
    {
        profilePages = new ProfilePage[stats.profilePageArray.Length];
        frontPage = Instantiate(settings.frontPagePrefab, profilePagesNotFlippedRectTransform);
        frontPage.transform.SetAsFirstSibling();

        for (int i = 0; i < stats.profilePageArray.Length; i++)
        {
            profilePages[i] = Instantiate(settings.profilePagePrefab, profilePagesNotFlippedRectTransform);

            profilePages[i].transform.SetAsFirstSibling(); // first index gets rendered last
            float offsetX = UnityEngine.Random.Range(-settings.randomPixelOffsetForPage, settings.randomPixelOffsetForPage);
            float offsetY = UnityEngine.Random.Range(-settings.randomPixelOffsetForPage, settings.randomPixelOffsetForPage);
            profilePages[i].rectTransform.anchoredPosition += new Vector2(offsetX, offsetY);

            profilePages[i].SetPageParams(i);
        }
    }
    private async UniTask FlippingUpPage()
    {
        int curIndex = stats.tempStats.curPageIndex;
        profilePages[curIndex].Flipped(flippedDown: false);
        profilePages[curIndex].transform.SetParent(profilePagesFlippedRectTransform, worldPositionStays: false);
        float elapsedTime = 0;
        while (elapsedTime < settings.flipPageTime)
        {
            elapsedTime += Time.deltaTime;

            profilePages[curIndex].flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, elapsedTime / settings.flipPageTime);
            await UniTask.Yield();
        }
        profilePages[curIndex].flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, 1);
        stats.tempStats.curPageIndex++;
        stats.tempStats.flippingPage = false;
    }
    private async UniTask FlippingDownPage()
    {
        stats.tempStats.curPageIndex--;
        int curIndex = stats.tempStats.curPageIndex;
        float elapsedTime = settings.flipPageTime;
        while (elapsedTime > 0)
        {
            elapsedTime -= Time.deltaTime;
            profilePages[curIndex].flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, elapsedTime / settings.flipPageTime);
            await UniTask.Yield();
        }
        profilePages[curIndex].Flipped(flippedDown: true);
        profilePages[curIndex].flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, 0);
        profilePages[curIndex].transform.SetParent(profilePagesNotFlippedRectTransform, worldPositionStays: false);
        profilePages[curIndex].transform.SetAsLastSibling();
        stats.tempStats.flippingPage = false;
    }
    private async UniTask FlippingUpFrontPage()
    {
        frontPage.Flipped(flippedDown: false);
        frontPage.transform.SetParent(profilePagesFlippedRectTransform, worldPositionStays: false);
        float elapsedTime = 0;
        while (elapsedTime < settings.flipPageTime)
        {
            elapsedTime += Time.deltaTime;
            frontPage.flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, elapsedTime / settings.flipPageTime);
            await UniTask.Yield();
        }
        frontPage.flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, 1);
        stats.tempStats.curPageIndex++;
        stats.tempStats.flippingPage = false;
    }
    private async UniTask FlippingDownFrontPage()
    {
        stats.tempStats.curPageIndex--;
        float elapsedTime = settings.flipPageTime;
        while (elapsedTime > 0)
        {
            elapsedTime -= Time.deltaTime;
            frontPage.flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, elapsedTime / settings.flipPageTime);
            await UniTask.Yield();
        }
        frontPage.Flipped(flippedDown: true);
        frontPage.flipPageImage.material.SetFloat(stats.materialIDs.normAnimTime, 0);
        frontPage.transform.SetParent(profilePagesNotFlippedRectTransform, worldPositionStays: false);
        frontPage.transform.SetAsLastSibling();
        stats.tempStats.flippingPage = false;
    }
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
