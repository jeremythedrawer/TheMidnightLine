using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Carriage;

public class FrontPage : MonoBehaviour
{
    [SerializeField] TMP_Text idText;
    [SerializeField] Image pageImage;
    [SerializeField] SpyStatsSO spyStats;
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] ClipboardSettingsSO clipboardSettings;
    [SerializeField] TutorialSO tutorial;
    public RectTransform rectTransform;
    public Image flipPageImage;
    public Image idBorderImage;

    CancellationTokenSource ditherValueCTS;
    bool prevHovered;
    private void Awake()
    {
        flipPageImage.material = Instantiate(flipPageImage.material);
        flipPageImage.enabled = false;
        idText.material = Instantiate(idText.material);
        idText.text = spyStats.spyID;
    }

    private void Start()
    {
        idText.fontMaterial.SetColor("_FaceColor", Color.white);
    }
    private void Update()
    {
        //if (!clipboardStats.tempStats.canClickID) return;
        bool hovered = RectTransformUtility.RectangleContainsScreenPoint(idBorderImage.rectTransform, playerInputs.mouseScreenPos, Camera.main);
        bool clicked = hovered && playerInputs.mouseLeftDown;

        if (hovered && clicked)
        {
            clipboardStats.tempStats.canClickID = false;
            tutorial.curConvoIndex++;
        }

        if (hovered != prevHovered)
        {
            if (ditherValueCTS == null || ditherValueCTS.IsCancellationRequested)
            {
                ditherValueCTS?.Dispose();
                ditherValueCTS = new CancellationTokenSource();
            }
            HoveringID(hovered, ditherValueCTS.Token).Forget();
            prevHovered = hovered;
        }
    }

    private async UniTask HoveringID(bool hovering, CancellationToken token)
    {
        float elaspedTime = clipboardStats.tempStats.ditherTransitionValue * clipboardSettings.ditherTransitionTime;
        try
        {
            while (hovering ? elaspedTime > 0f : elaspedTime < clipboardSettings.ditherTransitionTime)
            {

                elaspedTime += (hovering ? -Time.deltaTime : Time.deltaTime);

                float t = elaspedTime / clipboardSettings.ditherTransitionTime;
                t = Mathf.Pow(t, 3);
                clipboardStats.tempStats.ditherTransitionValue = t;
                idBorderImage.material.SetFloat(clipboardStats.materialIDs.ditherValue, clipboardStats.tempStats.ditherTransitionValue);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        idBorderImage.material.SetFloat(clipboardStats.materialIDs.ditherValue, hovering ? 0 : 1);
    }
    public void Flipped(bool flippedDown)
    {
        flipPageImage.enabled = !flippedDown;
        pageImage.enabled = flippedDown;
        idText.enabled = flippedDown;
        idBorderImage.enabled = flippedDown;
    }
}
