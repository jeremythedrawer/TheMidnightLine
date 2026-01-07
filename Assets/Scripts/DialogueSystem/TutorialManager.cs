using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] PlayerInputsSO playerInputs;
    [SerializeField] PhoneSO phone;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] TextAsset conversation;
    [SerializeField] RectTransform speechBox;
    [SerializeField] TMP_Text speech;

    CancellationTokenSource typingCTS;
    string[] lines;
    private int characterDelayMS = 30;
    private float speechBoxGrowTime = 0.25f;
    private float paddingX = 20f;
    private float paddingY = 20f;
    private int convIndex;
    private void Awake()
    {
        lines = conversation.text.Split('\n');
        typingCTS = new CancellationTokenSource();
    }

    private void Start()
    {
        TypeLine(typingCTS.Token).Forget();
    }
    private async UniTask TypeLine(CancellationToken token)
    {
        string line = lines[convIndex];
        speech.text = line;
        speech.ForceMeshUpdate();

        int lineCount = speech.textInfo.lineCount;
        Debug.Log(lineCount);
        TMP_LineInfo speechLineInfo = speech.textInfo.lineInfo[0];
        float totalHeight = lineCount * speechLineInfo.lineHeight;
        float totalWidth = speech.preferredWidth;
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
            convIndex++;
        }
    }
}
