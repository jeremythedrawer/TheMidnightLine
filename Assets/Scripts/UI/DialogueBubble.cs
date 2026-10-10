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

    public bool followPassenger;
    public CancellationTokenSource ctsOpen;

    private void OnEnable()
    {
        actionData.onUnmaskPassenger += Open;
        actionData.onHoverUnmaskedPassenger += TurnOn;
        actionData.onUnhoverUnmaskedPassenger += TurnOff;
        actionData.onCloseDialogueBubble += Close;

        ctsOpen = new CancellationTokenSource();
    }
    private void OnDisable()
    {
        actionData.onUnmaskPassenger -= Open;
        actionData.onHoverUnmaskedPassenger -= TurnOn;
        actionData.onUnhoverUnmaskedPassenger -= TurnOff;
        actionData.onCloseDialogueBubble -= Close;

        ctsOpen?.Cancel();
    }
    private void Start()
    {
        uiData.curDialogueWorldPosition.z = -1;
    }
    private void Update()
    {
        if (followPassenger)
        {
            Bounds passengerBounds = PassengerBrain.ActivePassenger.atlasRenderer.bounds;
            uiData.curDialogueWorldPosition.x = passengerBounds.center.x;
            uiData.curDialogueWorldPosition.y = passengerBounds.max.y + uiData.keyBindIconWorldSize.y;
            transform.position = uiData.curDialogueWorldPosition;
        }
    }
    private void Open()
    {
        textRenderer.enabled = true;
        tailRenderer.enabled = true;

        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();
        followPassenger = true;
        Opening().Forget();
    }
    private void TurnOn()
    {
        ctsOpen?.Cancel();

        textRenderer.enabled = true;
        tailRenderer.enabled = true;
        followPassenger = false;
        textRenderer.SetText(uiData.curDialogueText);
        size = textRenderer.textBoxData.size;
        transform.position = uiData.curDialogueWorldPosition;
        textRenderer.backgroundRenderer.SetNineSliceSizeFromWorldSpace(size);
    }
    private void TurnOff()
    {
        ctsOpen?.Cancel();
        textRenderer.enabled = false;
        tailRenderer.enabled = false;
    }
    private void Close()
    {
        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();
        followPassenger = false;
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
        }
    }
    private async UniTask Closing()
    {
        try
        {
            textRenderer.EraseText(WRITE_LETTER_TIME);

            while(textRenderer.textBoxData.charCount != 0) await UniTask.Yield(ctsOpen.Token);

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
        }
    }
}

