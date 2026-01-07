using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using static UnityEditor.Rendering.ShadowCascadeGUI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] PhoneSO phone;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] TutorialSO tutorial;

    [SerializeField] RectTransform speechBox;
    [SerializeField] TMP_Text speech;

    CancellationTokenSource typingCTS;

    //TODO: Put in dialogue so
    private int characterDelayMS = 30;
    private float speechBoxGrowTime = 0.25f;
    private float paddingX = 20f;
    private float paddingY = 20f;
    private void Awake()
    {
        tutorial.lines = tutorial.conversation.text.Split('\n');
        speech.fontMaterial = Instantiate(speech.fontMaterial);
        speech.fontMaterial.SetColor("_FaceColor", Color.aquamarine);
        tutorial.curConvoIndex = 0;
        tutorial.prevConvoIndex = -1;
    }
    private void OnDisable()
    {
        typingCTS?.Cancel();
        typingCTS?.Dispose();
        typingCTS = null;
    }
    private void Update()
    {
        if (playerInputs.mouseLeftDown)
        {
            typingCTS?.Cancel();
        }

        if (tutorial.curConvoIndex != tutorial.prevConvoIndex)
        {
            if (typingCTS == null || typingCTS.IsCancellationRequested)
            {
                typingCTS?.Dispose();
                typingCTS = new CancellationTokenSource();
            }
            TypeLine(typingCTS.Token).Forget();
            tutorial.prevConvoIndex = tutorial.curConvoIndex;
        }
        UpdateCurrentLine();
    }
    private async UniTask TypeLine(CancellationToken token)
    {
        string line = tutorial.lines[tutorial.curConvoIndex];
        speech.text = line;
        speech.ForceMeshUpdate();

        int lineCount = speech.textInfo.lineCount;
        TMP_LineInfo speechLineInfo = speech.textInfo.lineInfo[0];
        float totalHeight = lineCount * speechLineInfo.lineHeight;
        float totalWidth = speech.renderedWidth;
        speechBox.anchoredPosition = speech.rectTransform.anchoredPosition + new Vector2(-paddingX, paddingY);
        speech.text = "";

        Vector2 targetSize = new Vector2(totalWidth + (paddingX * 2), totalHeight + (paddingY * 2));

        try
        {
            float curGrowTime = 0;
            while(curGrowTime < speechBoxGrowTime)
            {
                curGrowTime += Time.deltaTime;
                float t = curGrowTime / speechBoxGrowTime;
                t = 1 - Mathf.Pow(1 - t, 3);
                speechBox.sizeDelta = new Vector2(targetSize.x * t, targetSize.y);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            foreach (char c in line)
            {
                speech.text += c;
                await UniTask.Delay(characterDelayMS, cancellationToken: token);
            }
        }
        finally
        {
            speech.text = line;
            speechBox.sizeDelta = targetSize;
            EnterCurrentLine();
        }
    }

    private void EnterCurrentLine()
    {
        switch (tutorial.curConvoIndex)
        {
            case 0:
            {
                clipboardStats.tempStats.canClickID = true;
            }
            break;
            case 1:
            {

            }
            break;
            case 2:
            {

            }
            break;
            case 3:
            {

            }
            break;
            case 4:
            {

            }
            break;
            case 5:
            {

            }
            break;
            case 6:
            {

            }
            break;
        }
    }
    private void UpdateCurrentLine()
    {
        switch (tutorial.curConvoIndex)
        {
            case 0:
            {

            }
            break;
            case 1:
            {

            }
            break;
            case 2:
            {

            }
            break;
            case 3:
            {

            }
            break;
            case 4:
            {

            }
            break;
            case 5:
            {

            }
            break;
            case 6:
            {

            }
            break;
        }
    }
}
