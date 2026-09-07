using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using System.Threading;
using UnityEngine;

using static AtlasUI;
public class DialogueBubble : MonoBehaviour
{
    const float WRITE_LETTER_TIME = 0.05f;
    const float OPEN_HEIGHT_TIME = 0.25f;

    public UIData uiData;

    public AtlasTextRenderer textRenderer;
    public AtlasRenderer tailRenderer;

    public GameEvent onOpenDialogueBubble;
    public GameEvent onCloseDialogueBubble;
    [Header("Generated")]    
    public Vector3 worldPos;

    public Vector2 size;

    public float clock;
    public CancellationTokenSource ctsOpen;

    private void OnEnable()
    {
        onOpenDialogueBubble.RegisterListener(Open);
        onCloseDialogueBubble.RegisterListener(Close);

        ctsOpen = new CancellationTokenSource();
    }
    private void OnDisable()
    {
        onOpenDialogueBubble.UnregisterListener(Open);
        onCloseDialogueBubble.UnregisterListener(Close);

        ctsOpen?.Cancel();
    }
    private void Open()
    {
        worldPos.x = uiData.curDialogueBubbleBounds.center.x;
        worldPos.y = uiData.curDialogueBubbleBounds.max.y + uiData.keyBindIconWorldSize.y;
        worldPos.z = uiData.curDialogueBubbleBounds.min.z;
        transform.position = worldPos;

        textRenderer.enabled = true;
        tailRenderer.enabled = true;

        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();
        Opening().Forget();
    }
    private void Close()
    {
        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();
        Closing().Forget();
    }
    private async UniTask Opening()
    {
        try
        {
            while (clock <= OPEN_HEIGHT_TIME)
            {
                float t = clock / OPEN_HEIGHT_TIME;
                t = Curves.EaseOutT(t, 2);
                size.y = textRenderer.textAtlas.typeWorldHeight * t;
                textRenderer.backgroundRenderer.SetNineSliceSizeFromWorldSpace(size);

                clock += Time.deltaTime;

                await UniTask.Yield(ctsOpen.Token);

            }
            size.y = textRenderer.textAtlas.typeWorldHeight;
            textRenderer.backgroundRenderer.SetNineSliceSizeFromWorldSpace(size);

            textRenderer.WriteText(uiData.curDialogueText, WRITE_LETTER_TIME);
        }
        catch (OperationCanceledException)
        {
            textRenderer.SetText(uiData.curDialogueText);
            size = textRenderer.textBoxData.size;
        }
    }
    private async UniTask Closing()
    {
        try
        {
            textRenderer.EraseText(WRITE_LETTER_TIME);

            while(textRenderer.hasText) await UniTask.Yield();

            while (clock >= 0)
            {
                float t = clock / OPEN_HEIGHT_TIME;
                t = Curves.EaseOutT(t, 2);
                size.y = textRenderer.textAtlas.typeWorldHeight * t;
                textRenderer.backgroundRenderer.SetNineSliceSizeFromWorldSpace(size);

                clock -= Time.deltaTime;

                await UniTask.Yield(ctsOpen.Token);

            }

            textRenderer.enabled = false;
            tailRenderer.enabled = false;

        }
        catch (OperationCanceledException)
        {
            textRenderer.SetText(uiData.curDialogueText);
            size = textRenderer.textBoxData.size;
        }
    }
}

