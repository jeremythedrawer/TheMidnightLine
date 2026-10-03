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
    public ActionData actionData;


    public AtlasTextRenderer textRenderer;
    public AtlasRenderer tailRenderer;

    [Header("Generated")]    
    public Vector3 worldPos;

    public Vector2 size;

    public float clock;

    public bool opened;
    public CancellationTokenSource ctsOpen;

    private void OnEnable()
    {
        actionData.onUnmaskPassenger += Open;
        actionData.onCloseDialogueBubble += Close;

        ctsOpen = new CancellationTokenSource();
    }
    private void OnDisable()
    {
        actionData.onUnmaskPassenger -= Open;
        actionData.onCloseDialogueBubble -= Close;

        ctsOpen?.Cancel();
    }
    private void Start()
    {
        worldPos.z = -1;
    }
    private void Update()
    {
        if (opened)
        {
            Bounds passengerBounds = PassengerBrain.ActivePassenger.atlasRenderer.bounds;
            worldPos.y = passengerBounds.max.y + uiData.keyBindIconWorldSize.y;
            worldPos.x = passengerBounds.center.x;
            transform.position = worldPos;
        }
    }
    private void Open()
    {
        textRenderer.enabled = true;
        tailRenderer.enabled = true;

        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();
        opened = true;
        Opening().Forget();
    }
    private void Close()
    {
        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();
        opened = false;
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

            while(textRenderer.textBoxData.charCount != 0) await UniTask.Yield();

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

