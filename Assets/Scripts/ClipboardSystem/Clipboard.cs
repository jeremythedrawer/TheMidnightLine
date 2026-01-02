using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Clipboard : MonoBehaviour
{
    [Serializable] public struct ComponentData
    {
        public RectTransform rectTransform;
        public RectTransform imagesRectTransform;
        public RectTransform profilePagesNotFlippedRectTransform;
        public RectTransform profilePagesFlippedRectTransform;
        public RectTransform clipRectTransform;
    }
    [SerializeField] ComponentData components;
    [Serializable] public struct SOData
    { 
        public ClipboardSettingsSO settings;
        public ClipboardStatsSO stats;
        public PlayerInputsSO inputs;
    }
    [SerializeField] SOData soData;

    ProfilePage[] profilePages;
    private void Awake()
    {
       soData.stats.materialIDs.normAnimTime = Shader.PropertyToID("_NormAnimTime");
    }
    private void Start()
    {
        soData.stats.startYPos = components.imagesRectTransform.localPosition.y;
        soData.stats.hoverYPos = soData.stats.startYPos + ((soData.settings.onScreenYPos - soData.stats.startYPos) * 0.25f);
        soData.stats.active = false;
        soData.stats.flippingPage = false;
        soData.stats.activePageIndex = 0;
        components.clipRectTransform.localPosition = new Vector3(components.clipRectTransform.localPosition.x, components.clipRectTransform.localPosition.y, -soData.stats.profilePageArray.Length); // send clip to front
        CreateProfilePages();
    }
    private void Update()
    {
        HandleInputs();
        HandleMovement();
    }

    private void HandleInputs()
    {
        if (!soData.stats.active && soData.inputs.mouseScroll == 1)
        {
            soData.stats.active = true;
        }
        else if (soData.stats.activePageIndex == 0 && soData.inputs.mouseScroll == -1)
        {
            soData.stats.active = false;
        }
        else if (soData.stats.active && !soData.stats.flippingPage && soData.inputs.mouseScroll != 0)
        {
            if (soData.inputs.mouseScroll > 0 && soData.stats.activePageIndex < profilePages.Length)
            {
                soData.stats.flippingPage = true;
                FlippingUpPage().Forget();
            }
            else if (soData.inputs.mouseScroll < 0 && soData.stats.activePageIndex > 0)
            {
                soData.stats.flippingPage = true;
                FlippingDownPage().Forget();
            }
        }
    }
    private void HandleMovement()
    {
        if (soData.stats.active)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(components.rectTransform, soData.inputs.mouseScreenPos, Camera.main, out Vector2 localPoint);

            if (localPoint.x > components.imagesRectTransform.rect.xMin && localPoint.y > components.rectTransform.localPosition.y)
            {
                soData.stats.targetYPos = soData.stats.hoverYPos;
            }
            else
            {
                soData.stats.targetYPos = soData.settings.onScreenYPos;
            }
        }
        else
        {
            soData.stats.targetYPos = soData.stats.startYPos;
        }
        float curRectYPos = Mathf.Lerp(components.imagesRectTransform.localPosition.y, soData.stats.targetYPos, soData.settings.moveTime * Time.deltaTime);
        components.imagesRectTransform.localPosition = new Vector3(components.imagesRectTransform.localPosition.x, curRectYPos, components.imagesRectTransform.localPosition.z);
    }
    private void CreateProfilePages()
    {
        profilePages = new ProfilePage[soData.stats.profilePageArray.Length];
        for (int i = 0; i < soData.stats.profilePageArray.Length; i++)
        {
            ProfilePage page = Instantiate(soData.settings.profilePagePrefab, components.profilePagesNotFlippedRectTransform);
            profilePages[i] = page;

            page.transform.SetAsFirstSibling(); // first index gets rendered last


            float offsetX = UnityEngine.Random.Range(-soData.settings.randomPixelOffsetForPage, soData.settings.randomPixelOffsetForPage);
            float offsetY = UnityEngine.Random.Range(-soData.settings.randomPixelOffsetForPage, soData.settings.randomPixelOffsetForPage);


            page.components.rectTransform.anchoredPosition += new Vector2(offsetX, offsetY);

            page.SetPageParams(i);
        }
    }

    private async UniTask FlippingUpPage()
    {
        profilePages[soData.stats.activePageIndex].Flipped(flippedDown: false);
        profilePages[soData.stats.activePageIndex].transform.SetParent(components.profilePagesFlippedRectTransform, worldPositionStays: false);
        float elapsedTime = 0;
        while (elapsedTime < soData.settings.flipPageTime)
        {
            elapsedTime += Time.deltaTime;

            profilePages[soData.stats.activePageIndex].components.pageImage.material.SetFloat(soData.stats.materialIDs.normAnimTime, elapsedTime / soData.settings.flipPageTime);
            await UniTask.Yield();
        }
        profilePages[soData.stats.activePageIndex].components.pageImage.material.SetFloat(soData.stats.materialIDs.normAnimTime, 1);
        soData.stats.activePageIndex = Mathf.Min(soData.stats.activePageIndex + 1, profilePages.Length);
        soData.stats.flippingPage = false;
    }

    private async UniTask FlippingDownPage()
    {
        soData.stats.activePageIndex = Mathf.Max(soData.stats.activePageIndex - 1, 0);

        float elapsedTime = soData.settings.flipPageTime;
        while (elapsedTime > 0)
        {
            elapsedTime -= Time.deltaTime;
            profilePages[soData.stats.activePageIndex].components.pageImage.material.SetFloat(soData.stats.materialIDs.normAnimTime, elapsedTime / soData.settings.flipPageTime);
            await UniTask.Yield();
        }
        profilePages[soData.stats.activePageIndex].Flipped(flippedDown: true);

        profilePages[soData.stats.activePageIndex].components.pageImage.material.SetFloat(soData.stats.materialIDs.normAnimTime, 0);
        profilePages[soData.stats.activePageIndex].transform.SetParent(components.profilePagesNotFlippedRectTransform, worldPositionStays: false);
        profilePages[soData.stats.activePageIndex].transform.SetAsLastSibling();

        soData.stats.flippingPage = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector3[] corners = new Vector3[4];
        components.imagesRectTransform.GetWorldCorners(corners);

        float yOffset = soData.settings.onScreenYPos;
        Vector3 worldOffset = components.imagesRectTransform.TransformVector(new Vector3(0f, soData.settings.onScreenYPos, 0f));

        for (int i = 0; i < 4; i++) corners[i] += worldOffset;

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }
}
