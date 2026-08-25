using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasUI;
public class TextButton : MonoBehaviour
{
    public delegate void OnClick(TextButton text);
    public delegate void OnEnter(TextButton text);
    public delegate void OnExit(TextButton text);

    public InputData inputData;
    public CursorData cursorData;
    public CameraData camData;

    public AtlasTextRenderer textRenderer;

    public bool holdToClick;

    [Header("Generated")]
    public Vector3 activePos;

    public ButtonState curState;

    public float holdClock;

    public OnClick OnClickCallback;
    public OnEnter OnEnterCallback;
    public OnExit OnExitCallback;

    public CancellationTokenSource ctsMove;
    public void InitButton(OnClick onClickCallback, OnEnter onEnterCallback, OnExit onExitCallback, bool isHold = false)
    {
        OnClickCallback = onClickCallback;
        OnEnterCallback = onEnterCallback;
        OnExitCallback = onExitCallback;
        holdToClick = isHold;
        InitPos();
    }
    public void InitPos()
    {
        activePos = textRenderer.transform.localPosition;
    }
    public void UpdateButton()
    {
        switch (curState)
        {
            case ButtonState.Unhovered:
            {
                if (cursorData.IsInsideBounds(textRenderer.bounds, isClickable: true))
                {
                    OnEnterCallback(this);
                    curState = ButtonState.Hovered;
                }
            }
            break;
            case ButtonState.Hovered:
            {
                if (!cursorData.IsInsideBounds(textRenderer.bounds, isClickable: true))
                {
                    OnExitCallback(this);
                    curState = ButtonState.Unhovered;
                }
                else if (inputData.mouseLeftDown)
                {
                    curState = ButtonState.Clicked;
                    if (!holdToClick)
                    {
                        OnClickCallback(this);
                    }
                    else
                    {
                        holdClock = 0;
                    }
                }
            }
            break;
            case ButtonState.Clicked:
            {
                if (!cursorData.IsInsideBounds(textRenderer.bounds, isClickable: true))
                {
                    OnExitCallback(this);
                    curState = ButtonState.Unhovered;
                }
                else if (inputData.mouseLeftUp)
                {
                    OnEnterCallback(this);
                    curState = ButtonState.Hovered;
                }

                if (holdToClick)
                {
                    holdClock += Time.deltaTime;

                    if (holdClock > 3)
                    {
                        OnClickCallback(this);
                    }
                }
            }
            break;
        }
    }
    public void MoveAway(Direction dir)
    {
        ctsMove?.Cancel();
        ctsMove = new CancellationTokenSource();
        MovingAway(dir).Forget();
    }
    public void SetAway(Direction dir)
    {
        Bounds buttonBounds = textRenderer.GetBoundsCurrentText();

        Vector3 buttonPos = textRenderer.transform.localPosition;
        Vector3 targetPos = buttonPos;

        switch (dir)
        {
            case Direction.Left:
            {
                targetPos.x = -camData.camBounds.extents.x - buttonBounds.size.x;
            }
            break;
            case Direction.Right:
            {
                targetPos.x = camData.camBounds.extents.x + buttonBounds.size.x;
            }
            break;
        }
        textRenderer.transform.localPosition = targetPos;
    }
    public void MoveToRight()
    {
        ctsMove?.Cancel();
        ctsMove = new CancellationTokenSource();
        MovingRight().Forget();
    }
    public void MoveToActive()
    {
        ctsMove?.Cancel();
        ctsMove = new CancellationTokenSource();
        MovingToActive().Forget();
    }
    private async UniTask MovingAway(Direction dir)
    {
        Transform buttonTransform = textRenderer.transform;
        Bounds buttonBounds = textRenderer.GetBoundsCurrentText();

        Vector3 buttonPos = textRenderer.transform.localPosition;
        Vector3 targetPos = buttonPos;

        switch (dir)
        {
            case Direction.Left:
            {
                targetPos.x = -camData.camBounds.extents.x - buttonBounds.size.x;
            }
            break;
            case Direction.Right:
            {
                targetPos.x = camData.camBounds.extents.x + buttonBounds.size.x;
            }
            break;
        }
        try
        {
            while ((buttonPos - targetPos).sqrMagnitude > 0.005f)
            {
                buttonPos = Vector3.Lerp(buttonPos, targetPos, Time.deltaTime * 2);

                buttonTransform.localPosition = buttonPos;

                await UniTask.Yield(ctsMove.Token);
            }
            buttonTransform.localPosition = targetPos;

        }
        catch (OperationCanceledException)
        {

        }
    }
    private async UniTask MovingRight()
    {
        Transform buttonTransform = textRenderer.transform;
        Bounds bounds = textRenderer.GetBoundsCurrentText();

        SimpleSprite sprite = textRenderer.backgroundRenderer.sprite;

        Vector3 curPos = buttonTransform.localPosition;

        float ndcPivot = sprite.uvPivot.x * 2 - 1;
        float targetPosX = -activePos.x + (sprite.worldSize.x * ndcPivot);
        try
        {
            while (Mathf.Abs(curPos.x - targetPosX) > 0.005f)
            {
                curPos.x = Mathf.Lerp(curPos.x, targetPosX, Time.deltaTime * 2);
                buttonTransform.localPosition = curPos;

                await UniTask.Yield(ctsMove.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    private async UniTask MovingToActive()
    {
        Transform buttonTransform = textRenderer.transform;
        Bounds buttonBounds = textRenderer.GetBoundsCurrentText();
        Vector3 buttonPos = buttonTransform.localPosition;
        try
        {
            while ((buttonPos - activePos).sqrMagnitude > 0.005f)
            {
                buttonPos = Vector3.Lerp(buttonPos, activePos, Time.deltaTime * 2);
                buttonTransform.localPosition = buttonPos;

                await UniTask.Yield(ctsMove.Token);
            }
        }
        catch (OperationCanceledException)
        {

        }
    }
}
