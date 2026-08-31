using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasUI;

public class IconButton : MonoBehaviour
{
    public delegate void Callback(IconButton icon);

    public ButtonFunctionType button;

    public InputData inputData;
    public CursorData cursorData;
    public CameraData camData;

    public AtlasRenderer atlasRenderer;

    public bool alphaTest;
    [Header("Generated")]
    public Vector3 activePos;
    public ButtonState curState;

    public Callback OnMouseUpCallback;
    public Callback OnMouseDownCallback;
    public Callback OnEnterCallback;
    public Callback OnExitCallback;

    public CancellationTokenSource ctsMove;
    public void InitButton(Callback onMouseUp, Callback onMouseDown, Callback onEnter, Callback onExit)
    {
        OnMouseUpCallback = onMouseUp;
        OnMouseDownCallback = onMouseDown;
        OnEnterCallback = onEnter;
        OnExitCallback = onExit;
        atlasRenderer.SetBounds();
        activePos = atlasRenderer.transform.localPosition;
    }
    public void UpdateButton()
    {
        switch (curState)
        {
            case ButtonState.Unhovered:
            {
                if (alphaTest)
                {
                    if(cursorData.IsInsideSprite(atlasRenderer, isClickable: true))
                    {
                        OnEnterCallback(this);
                        curState = ButtonState.Hovered;
                    }
                }
                else
                {
                    if (cursorData.IsInsideBounds(atlasRenderer.bounds, isClickable: true))
                    {
                        OnEnterCallback(this);
                        curState = ButtonState.Hovered;
                    }
                }
            }
            break;
            case ButtonState.Hovered:
            {
                if (alphaTest)
                {
                    if (!cursorData.IsInsideSprite(atlasRenderer, isClickable: true))
                    {
                        OnExitCallback(this);
                        curState = ButtonState.Unhovered;
                    }
                    else if (inputData.mouseLeftDown)
                    {
                        OnMouseDownCallback(this);
                        curState = ButtonState.Clicked;
                    }
                }
                else
                {
                    if (!cursorData.IsInsideBounds(atlasRenderer.bounds, isClickable: true))
                    {
                        OnExitCallback(this);
                        curState = ButtonState.Unhovered;
                    }
                    else if (inputData.mouseLeftDown)
                    {
                        OnMouseDownCallback(this);
                        curState = ButtonState.Clicked;
                    }
                }
            }
            break;
            case ButtonState.Clicked:
            {
                if (inputData.mouseLeftUp)
                {
                    OnMouseUpCallback(this);
                    OnEnterCallback(this);
                    curState = ButtonState.Hovered;
                }

                if (alphaTest)
                {
                    if (!cursorData.IsInsideSprite(atlasRenderer, isClickable: true) && !inputData.mouseLeftHold)
                    {
                        OnExitCallback(this);
                        curState = ButtonState.Unhovered;
                    }
                }
                else
                {
                    if (!cursorData.IsInsideBounds(atlasRenderer.bounds, isClickable: true) && !inputData.mouseLeftHold)
                    {
                        OnExitCallback(this);
                        curState = ButtonState.Unhovered;
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
        Bounds buttonBounds = atlasRenderer.GetBounds();

        Vector3 buttonPos = atlasRenderer.transform.localPosition;
        Vector3 targetPos = buttonPos;

        switch (dir)
        {
            case Direction.Left:
            {
                targetPos.x = -camData.bounds.extents.x - buttonBounds.size.x;
            }
            break;
            case Direction.Right:
            {
                targetPos.x = camData.bounds.extents.x + buttonBounds.size.x;
            }
            break;
        }
        atlasRenderer.transform.localPosition = targetPos;
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
        Transform buttonTransform = atlasRenderer.transform;
        Bounds buttonBounds = atlasRenderer.GetBounds();

        Vector3 buttonPos = atlasRenderer.transform.localPosition;
        Vector3 targetPos = buttonPos;

        switch (dir)
        {
            case Direction.Left:
            {
                targetPos.x = -camData.bounds.extents.x - buttonBounds.size.x;
            }
            break;
            case Direction.Right:
            {
                targetPos.x = camData.bounds.extents.x + buttonBounds.size.x;
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
        Transform buttonTransform = atlasRenderer.transform;
        Bounds bounds = atlasRenderer.GetBounds();

        SimpleSprite sprite = atlasRenderer.sprite;

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
        Transform buttonTransform = atlasRenderer.transform;
        Bounds buttonBounds = atlasRenderer.GetBounds();
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