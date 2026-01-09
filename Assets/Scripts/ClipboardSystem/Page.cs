using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
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
        internal Color borderColor;
        internal int pageIndex;
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
    [SerializeField] Stats stats;

    [Header("Front Page")]
    [SerializeField] TMP_Text idText;
    [SerializeField] Image idBorderImage;
    CancellationTokenSource ditherValueCTS;
    CancellationTokenSource releasePageCTS;
    
    Image flipPageImage;
    bool prevHovered = true;
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
        releasePageCTS?.Cancel();
        releasePageCTS?.Dispose();
        releasePageCTS = null;
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
                    HoveringID(false, 1, ditherValueCTS.Token).Forget();

                    clipboardStats.tempStats.canClickID = false;
                }

                if (hovered != prevHovered)
                {
                    if (ditherValueCTS == null || ditherValueCTS.IsCancellationRequested)
                    {
                        ditherValueCTS?.Dispose();
                        ditherValueCTS = new CancellationTokenSource();
                    }
                    HoveringID(hovered, 0.9f, ditherValueCTS.Token).Forget();
                    prevHovered = hovered;
                }
            }
            break;
        }
    }
    public void SetPageParams(int pageIndex)
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
    public void ClickedPage(bool clicked)
    {
        pageImage.sprite = clicked ? page.foldedPageSprite : page.unfoldedPageSprite;
        foldImage.enabled = clicked;
    }
    public void UpdateFlip()
    {
        flipPageImage.material.SetFloat(materialIDs.ids.normAnimTime, clipboardStats.tempStats.curDragMouseT);
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
    }
    public void Unflip()
    {
        if (releasePageCTS == null || releasePageCTS.IsCancellationRequested)
        {
            releasePageCTS?.Dispose();
            releasePageCTS = new CancellationTokenSource();
        }
        ReleasingPage().Forget();
    }
    private async UniTask ReleasingPage()
    {
        float startNormTime = flipPageImage.material.GetFloat(materialIDs.ids.normAnimTime);
        float elapsedTime = 0f;

        try
        {
            while (elapsedTime < page.releasePageTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / page.releasePageTime;
                float curNormTime = Mathf.Lerp(startNormTime, 0, t);
                flipPageImage.material.SetFloat(materialIDs.ids.normAnimTime, curNormTime);
                await UniTask.Yield(PlayerLoopTiming.Update, releasePageCTS.Token);
            }

            FlipDown();
        }
        catch (OperationCanceledException)
        {
        }

    }
    private async UniTask HoveringID(bool hovering, float maxValue, CancellationToken token)
    {
        float elaspedTime = clipboardStats.tempStats.ditherTransitionValue * clipboardSettings.ditherTransitionTime;
        try
        {
            while (hovering ? elaspedTime > 0f : elaspedTime < clipboardSettings.ditherTransitionTime)
            {
                elaspedTime += (hovering ? -Time.deltaTime : Time.deltaTime);

                float t = elaspedTime / clipboardSettings.ditherTransitionTime;
                t *= t;
                clipboardStats.tempStats.ditherTransitionValue = t * maxValue;
                idBorderImage.material.SetFloat(materialIDs.ids.ditherValue, clipboardStats.tempStats.ditherTransitionValue);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            idBorderImage.material.SetFloat(materialIDs.ids.ditherValue, hovering ? 0 : maxValue);
        }
    }
}
