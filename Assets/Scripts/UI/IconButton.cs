using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasUI;

public class IconButton : MonoBehaviour
{
    public delegate void Callback();

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
    public void InitButton(Callback onMouseUp = null, Callback onMouseDown = null, Callback onEnter = null, Callback onExit = null)
    {
        OnMouseUpCallback = onMouseUp ?? MouseUp;
        OnMouseDownCallback = onMouseDown ?? MouseDown;
        OnEnterCallback = onEnter ?? EnterButton;
        OnExitCallback = onExit ?? ExitButton;
        atlasRenderer.SetBounds();
        activePos = atlasRenderer.transform.localPosition;

        OnExitCallback();
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
                        OnEnterCallback();
                        curState = ButtonState.Hovered;
                    }
                }
                else
                {
                    if (cursorData.IsInsideBounds(atlasRenderer.bounds, isClickable: true))
                    {
                        OnEnterCallback();
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
                        OnExitCallback();
                        curState = ButtonState.Unhovered;
                    }
                    else if (inputData.mouseLeftDown)
                    {
                        OnMouseDownCallback();
                        curState = ButtonState.Clicked;
                    }
                }
                else
                {
                    if (!cursorData.IsInsideBounds(atlasRenderer.bounds, isClickable: true))
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
            }
            break;
            case ButtonState.Clicked:
            {
                if (inputData.mouseLeftUp)
                {
                    OnMouseUpCallback();
                    OnEnterCallback();
                    curState = ButtonState.Hovered;
                }

                if (alphaTest)
                {
                    if (!cursorData.IsInsideSprite(atlasRenderer, isClickable: true) && !inputData.mouseLeftHold)
                    {
                        OnExitCallback();
                        curState = ButtonState.Unhovered;
                    }
                }
                else
                {
                    if (!cursorData.IsInsideBounds(atlasRenderer.bounds, isClickable: true) && !inputData.mouseLeftHold)
                    {
                        OnExitCallback();
                        curState = ButtonState.Unhovered;
                    }
                }
            }
            break;
        }
    }
    public void MouseDown()
    {
        atlasRenderer.customBit ^= (int)ColorBits.Invert;
    }
    public void EnterButton()
    {
        atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
        atlasRenderer.customBit &= ~(int)ColorBits.BlueChannel;
    }
    public void ExitButton()
    {
        atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
        atlasRenderer.customBit |= (int)ColorBits.BlueChannel;
        atlasRenderer.customBit &= ~(int)ColorBits.Invert;
    }
    public void MouseUp()
    {
        atlasRenderer.customBit &= ~(int)ColorBits.Invert;
    }
}