using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] PhoneSO phone;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] TutorialSO tutorial;
    [SerializeField] DialogueSettingsSO dialogueSettings;
    [SerializeField] RectTransform speechBox;
    [SerializeField] TMP_Text speech;

    CancellationTokenSource typingCTS;
    bool isTyping;
    //TODO: Put in dialogue so
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
        if (playerInputs.mouseLeftDown && isTyping)
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
            TypeLine().Forget();
            tutorial.prevConvoIndex = tutorial.curConvoIndex;
        }
        UpdateCurrentLine();
    }
    private async UniTask TypeLine()
    {
        isTyping = true;
        string line = tutorial.lines[tutorial.curConvoIndex];
        speech.text = line;
        speech.ForceMeshUpdate();

        int lineCount = speech.textInfo.lineCount;
        TMP_LineInfo speechLineInfo = speech.textInfo.lineInfo[0];
        float totalHeight = lineCount * speechLineInfo.lineHeight;
        float totalWidth = speech.renderedWidth;
        speechBox.anchoredPosition = speech.rectTransform.anchoredPosition + new Vector2(-dialogueSettings.paddingX, dialogueSettings.paddingY);
        speech.text = "";

        Vector2 targetSize = new Vector2(totalWidth + (dialogueSettings.paddingX * 2), totalHeight + (dialogueSettings.paddingY * 2));

        try
        {
            float curGrowTime = 0;
            while(curGrowTime < dialogueSettings.speechBoxGrowTime)
            {
                curGrowTime += Time.deltaTime;
                float t = curGrowTime / dialogueSettings.speechBoxGrowTime;
                t = 1 - Mathf.Pow(1 - t, 3);
                speechBox.sizeDelta = new Vector2(targetSize.x * t, targetSize.y);
                await UniTask.Yield(PlayerLoopTiming.Update, typingCTS.Token);
            }
            foreach (char c in line)
            {
                speech.text += c;
                await UniTask.Delay(dialogueSettings.characterDelayMS, cancellationToken: typingCTS.Token);
            }
        }
        finally
        {
            speech.text = line;
            speechBox.sizeDelta = targetSize;
            isTyping = false;
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
                if (playerInputs.mouseLeftDown && !isTyping)
                {
                    tutorial.curConvoIndex++;
                }
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
