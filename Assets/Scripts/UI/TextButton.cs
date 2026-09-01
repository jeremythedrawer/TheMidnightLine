using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasUI;
public class TextButton : MonoBehaviour
{
    public delegate void Callback();

    public ButtonFunctionType buttonFunctionType;

    public InputData inputData;
    public CursorData cursorData;
    public CameraData camData;

    public AtlasTextRenderer textRenderer;
    public AtlasRenderer backgroundRenderer;

    public bool holdToClick;

    [Header("Generated")]
    public Vector3 activePos;

    public ButtonState curState;

    public Callback OnMouseUpCallback;
    public Callback OnMouseDownCallback;
    public Callback OnEnterCallback;
    public Callback OnExitCallback;

    public CancellationTokenSource ctsMove;
    public void InitButton(Callback onMouseUp = null, Callback onMouseDown = null, Callback onEnter = null, Callback onExit = null)
    {
        OnMouseUpCallback = onMouseUp ?? MouseUpText;
        OnMouseDownCallback = onMouseDown ?? MouseDownText;
        OnEnterCallback = onEnter ?? EnterButtonText;
        OnExitCallback = onExit ?? ExitButtonText;

        backgroundRenderer.SetBounds();
        activePos = backgroundRenderer.transform.localPosition;
    }
    public void UpdateButton()
    {
        switch (curState)
        {
            case ButtonState.Unhovered:
            {
                if (cursorData.IsInsideBounds(backgroundRenderer.bounds, isClickable: true))
                {
                    OnEnterCallback();
                    curState = ButtonState.Hovered;
                }
            }
            break;
            case ButtonState.Hovered:
            {
                if (!cursorData.IsInsideBounds(backgroundRenderer.bounds, isClickable: true))
                {
                    OnExitCallback();
                    curState = ButtonState.Unhovered;
                }
                else if (inputData.mouseLeftDown)
                {
                    OnMouseDownCallback();
                    curState = ButtonState.Clicked;
                }
            }
            break;
            case ButtonState.Clicked:
            {
                if (!cursorData.IsInsideBounds(backgroundRenderer.bounds, isClickable: true))
                {
                    OnExitCallback();
                    curState = ButtonState.Unhovered;
                }
                else if (inputData.mouseLeftUp)
                {
                    OnMouseUpCallback();
                    OnEnterCallback();
                    curState = ButtonState.Hovered;
                }
            }
            break;
        }
    }
    private void MouseDownText()
    {
        backgroundRenderer.customBit ^= (int)ColorBits.Invert;
        textRenderer.customBit ^= (int)ColorBits.Invert;
    }
    private void EnterButtonText()
    {
        backgroundRenderer.customBit |= (int)ColorBits.GreenChannel;
    }
    private void ExitButtonText()
    {
        backgroundRenderer.customBit &= ~(int)ColorBits.GreenChannel;
        backgroundRenderer.customBit &= ~(int)ColorBits.Invert;
        textRenderer.customBit |= (int)ColorBits.Invert;
    }
    public void MouseUpText()
    {
        backgroundRenderer.customBit &= ~(int)ColorBits.Invert;
        textRenderer.customBit |= (int)ColorBits.Invert;
    }
}
