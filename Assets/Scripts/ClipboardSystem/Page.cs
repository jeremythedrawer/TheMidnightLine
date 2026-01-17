using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Page : MonoBehaviour
{
    public enum Type
    { 
        Profile,
        Front,
    }
    [Serializable] public struct Stats
    {
        public Color borderColor;
        public int pageIndex;
        public float curNormAnimTime;
    }

    [SerializeField] Type type;

    public RectTransform rectTransform;
    [SerializeField] Image pageImage;
    [SerializeField] Image foldImage;
    [SerializeField] PageSO page;
    [SerializeField] SpyStatsSO spyStats;
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] ClipboardSettingsSO clipboardSettings;
    [SerializeField] TutorialSO tutorial;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] RectTransform flippedPageSpawnTransform;

    [Header("Profile Page")]
    [SerializeField] TMP_Text behavioursText;
    [SerializeField] TMP_Text appearenceText;
    [SerializeField] Image borderImage;

    [Header("Front Page")]
    [SerializeField] TMP_Text idText;
    [SerializeField] Image idBorderImage;

    CancellationTokenSource flipCTS;
    CancellationTokenSource ditherValueCTS;
    
    Image flipPageImage;
    bool prevHovered = true;
    public Stats stats;
    public bool flipped;
    private void Awake()
    {
        flipPageImage = Instantiate(clipboardSettings.flippedPagePrefab, flippedPageSpawnTransform);

        flipPageImage.material = Instantiate(flipPageImage.material);
        flipPageImage.enabled = false;
        foldImage.enabled = false;

        switch (type)
        {
            case Type.Profile:
            {
                behavioursText.fontMaterial = Instantiate(behavioursText.fontMaterial);
                appearenceText.fontMaterial = Instantiate(appearenceText.fontMaterial);
                behavioursText.fontMaterial.SetColor("_FaceColor", Color.black);
                appearenceText.fontMaterial.SetColor("_FaceColor", Color.black);
            }
            break;
            case Type.Front:
            {
                idText.material = Instantiate(idText.material);
                idText.text = spyStats.spyID;
            }
            break;
        }

        if (type == Type.Profile)
        {
        }
        else
        {

        }
        pageImage.sprite = page.unfoldedPageSprite;
        flipPageImage.material = Instantiate(flipPageImage.material);
        flipPageImage.enabled = false;
    }
    private void OnDisable()
    {
        flipCTS?.Cancel();
        flipCTS?.Dispose();
        flipCTS = null;

        ditherValueCTS?.Cancel();
        ditherValueCTS?.Dispose();
        ditherValueCTS = null;
    }
    private void Start()
    {
        switch (type)
        {
            case Type.Profile:
            {

            }
            break;
            case Type.Front:
            {
                idText.fontMaterial.SetColor("_FaceColor", Color.white);
                idBorderImage.material.SetFloat(materialIDs.ids.ditherValue, 1);
            }
            break;
        }
    }
    private void Update()
    {
        switch (type)
        {
            case Type.Profile:
            {

            }
            break;
            case Type.Front:
            {
                if (!clipboardStats.tempStats.canClickID) return;

                bool hovered = RectTransformUtility.RectangleContainsScreenPoint(idBorderImage.rectTransform, playerInputs.mouseScreenPos, Camera.main);
                bool clicked = hovered && playerInputs.mouseLeftDown;

                if (hovered && clicked)
                {
                    tutorial.curConvoIndex++;
                    HoveringFrontPageID(false, 1, ditherValueCTS.Token).Forget();

                    clipboardStats.tempStats.canClickID = false;
                }

                if (hovered != prevHovered)
                {
                    if (ditherValueCTS == null || ditherValueCTS.IsCancellationRequested)
                    {
                        ditherValueCTS?.Dispose();
                        ditherValueCTS = new CancellationTokenSource();
                    }
                    HoveringFrontPageID(hovered, 0.9f, ditherValueCTS.Token).Forget();
                    prevHovered = hovered;
                }
            }
            break;
        }
    }
    public void SetProfilePageParams(int pageIndex)
    {
        stats.pageIndex = pageIndex;
        NPCTraits.Behaviours rawBehave = clipboardStats.profilePageArray[pageIndex].behaviours;
        NPCTraits.Appearence rawAppear = clipboardStats.profilePageArray[pageIndex].appearence;

        int behaveCount = Bitwise.GetSetBitCount((long)rawBehave);
        int[] behavePositions = new int[behaveCount];
        int appearPosition = 0;

        int posIndex = 0;
        int flagIndex = 0;
        foreach (NPCTraits.Behaviours flag in Enum.GetValues(typeof(NPCTraits.Behaviours)))
        {
            if (flag == NPCTraits.Behaviours.Nothing) continue;

            if ((rawBehave & flag) != 0)
            {
                behavePositions[posIndex] = flagIndex;
                posIndex++;
            }
            flagIndex++;
        }
        flagIndex = 0;
        foreach (NPCTraits.Appearence flag in Enum.GetValues(typeof(NPCTraits.Appearence)))
        {
            if (flag == NPCTraits.Appearence.Nothing) continue;

            if ((rawAppear & flag) != 0)
            {
                appearPosition = flagIndex;
                break;
            }
            flagIndex++;
        }

        string behaveText = "";
        for (int i = 0; i < behavePositions.Length; i++)
        {
            behaveText += "- " + NPCTraits.behaviourDescriptions[behavePositions[i]];
            if (i < behavePositions.Length - 1) behaveText += "\n";
        }

        behavioursText.text = behaveText;
        appearenceText.text = "- " + NPCTraits.appearenceDescriptions[appearPosition];

        borderImage.color = clipboardStats.profilePageArray[pageIndex].color;

        rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, -(clipboardStats.profilePageArray.Length - 1 - pageIndex));
    }
    public void UpdateFlip()
    {
        flipPageImage.material.SetFloat(materialIDs.ids.normAnimTime, clipboardStats.tempStats.curDragMouseT);
    }
    public void ClickPage()
    {
        foldImage.enabled = true;
        pageImage.sprite = page.foldedPageSprite;
    }
    public void UnclickPage()
    {
        foldImage.enabled = false;
        pageImage.sprite = page.unfoldedPageSprite;
    }
    public void FlipUp()
    {
        switch (type)
        {
            case Type.Profile:
            {
                behavioursText.enabled = false;
                appearenceText.enabled = false;
                borderImage.enabled = false;
            }
            break;
            case Type.Front:
            {
                idText.enabled = false;
                idBorderImage.enabled = false;
            }
            break;
        }

        flipPageImage.enabled = true;
        foldImage.enabled = false;
        pageImage.sprite = page.unfoldedPageSprite;
        pageImage.enabled = false;
    }
    public void FlipDown()
    {
        switch (type)
        {
            case Type.Profile:
            {
                behavioursText.enabled = true;
                appearenceText.enabled = true;
                borderImage.enabled = true;
            }
            break;
            case Type.Front:
            {
                idText.enabled = true;
                idBorderImage.enabled = true;
            }
            break;
        }

        flipPageImage.enabled = false;
        pageImage.enabled = true;
        foldImage.enabled = false;
        pageImage.sprite = page.unfoldedPageSprite;
    }
    public void AutoFlip()
    {
        if (flipCTS == null || flipCTS.IsCancellationRequested)
        {
            flipCTS?.Dispose();
            flipCTS = new CancellationTokenSource();
        }
        AutoFlipTask().Forget();
    }
    private async UniTask AutoFlipTask()
    {
        /*
         * I cache the flip state, the anim time
         * I calculate the remaining elapsed time, the target anim time and the start anim time.
         * start anim time is one minused if the page is flipping down because the animation is reversed
         * The page will update its animation using its material shader's atlas.
         * If cancelled the task will return early so no events are raised
         *  If not cancelled, depending on whether the page is flipping up or down will determine the corelating events to be raised.
         */
        float startNormTime = flipPageImage.material.GetFloat(materialIDs.ids.normAnimTime);
        if (!clipboardStats.tempStats.flipUp) startNormTime = 1 - startNormTime;
        float elapsedTime = startNormTime * clipboardSettings.releasePageTime;
        float targetFlip = clipboardStats.tempStats.flipUp ? 1 : 0;
        float startFlip = 1 - targetFlip;

        try
        {
            while (elapsedTime < clipboardSettings.releasePageTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / clipboardSettings.releasePageTime;
                float curNormTime = Mathf.Lerp(startFlip, targetFlip, t);
                flipPageImage.material.SetFloat(materialIDs.ids.normAnimTime, curNormTime);
                await UniTask.Yield(cancellationToken: flipCTS.Token);
            }
        }
        catch(OperationCanceledException)
        {
            return;
        }

        if (clipboardStats.tempStats.flipUp)
        {
            gameEventData.OnFlipUpPage.Raise();
        }
        else
        {
            FlipDown();
            gameEventData.OnFlipDownPage.Raise();
        }

        flipPageImage.material.SetFloat(materialIDs.ids.normAnimTime, targetFlip);
        flipped = targetFlip == 1;
    }
    private async UniTask HoveringFrontPageID(bool hovering, float maxValue, CancellationToken token)
    {
        float elaspedTime = clipboardStats.cacheStats.ditherTransitionValue * clipboardSettings.ditherTransitionTime;
        try
        {
            while (hovering ? elaspedTime > 0f : elaspedTime < clipboardSettings.ditherTransitionTime)
            {
                elaspedTime += (hovering ? -Time.deltaTime : Time.deltaTime);

                float t = elaspedTime / clipboardSettings.ditherTransitionTime;
                t *= t;
                clipboardStats.cacheStats.ditherTransitionValue = t * maxValue;
                idBorderImage.material.SetFloat(materialIDs.ids.ditherValue, clipboardStats.cacheStats.ditherTransitionValue);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            idBorderImage.material.SetFloat(materialIDs.ids.ditherValue, hovering ? 0 : maxValue);
        }
    }
}
