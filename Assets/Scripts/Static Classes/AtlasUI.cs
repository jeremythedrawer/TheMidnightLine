using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;
using static AtlasUI;
using static NPC;
public static class AtlasUI
{
    public const int COLOR_SQUARE_SPRITE_INDEX = 5;
    public const int ONE_NUMPAD_SPRITE_INDEX = 12;
    public const int TWO_NUMPAD_SPRITE_INDEX = 13;
    public const int THREE_NUMPAD_SPRITE_INDEX = 14;
    public const int LOCK_SPRITE_INDEX = 18;
    public const int TICK_SPRITE_INDEX = 22;
    public const int FOUR_NUMPAD_SPRITE_INDEX = 25;
    public const int HOLDING_PENCIL_SPRITE_INDEX = 16;

    public const float LEFTHAND_DAMPING = 7f;
    public const float PENCIL_DISTANCE_THRESHOLD = 0.05f;
    public const float PENCIL_VERTICAL_FREQUENCY = 7f;
    public const float PENCIL_VERTICAL_MAGNITUDE = 0.07f;
    public const float BORDER_PADDING = 0f;
    public const float LETTER_ADVANCE = 0.122f;
    public const float APPEAR_TEXT_TIME = 0.2f;
    public const float FADE_BLACK_DURATION = 1f;
    public const float NATURAL_RADIUS = 0.1f;
    public const float NATURAL_TICK_RATE = 2.5f;
    public const float TARGET_MARGIN = 0.01f;
    public const float MOVE_DAMP = 4;
    public const float OPEN_TIME_ROW_COL = 0.0625f;
    public const float GRID_GAP = 0.272f;
    public const float WRITE_LETTER_TIME = 0.1f;
    public const float NOTEPAD_INACTIVE_OFFSET = 0.39f;
    public const float UI_POSITION_BUFFER = 0.3f;
    public static float TransitionTime = -Mathf.Log(TARGET_MARGIN) / MOVE_DAMP;

    public enum NotepadKeyframeState
    {
        None,
        Start,
        PaperClip,
        TogglePageContentsBottomHalf,
        TogglePageContentsTopHalf,
        ChangeDepth,
    }

    public enum NotepadState
    {
        None,
        Stationary,
        Writing,
        Erasing,
        FlippingUp,
        FlippingDown,
        Revealing,
    }
    public enum PickerState
    {
        None,
        Opening,
        Opened,
        Adjusting,
        Closing,
        Closed,
    }
    public enum PickerFunctionType
    {
        TicketCheck,
        Color,
        RuleOut,
    }
    [Flags] public enum NotepadSubState
    {
        None = 0,
        IsFlippingUp = 1 << 0,
        IsFlippingDown = 1 << 1,
        WriteToggle = 1 << 2,
        EraseToggle = 1 << 3,
        RevealToggle = 1 << 4,
        WillFlipUp = 1 << 5,
        WillFlipDown = 1 << 6,
        CanFlipUp = 1 << 7,
        CanFlipDown = 1 << 8,
        CanWillFlipUp = 1 << 9,
        CanWillFlipDown = 1 << 10,
        OnScreen = 1 << 11,
        InUse = 1 << 12,
    }
    [Flags]public enum UnlockType
    { 
        None = 0,
        RuleOut = 1 << 0,
        Color = 1 << 1,
        MultiColor = 1 << 2,
    }

    [Flags] public enum ColorBits
    { 
        None = 0,
        Color1 = 1 << 0,
        Color2 = 1 << 1,
        Color3 = 1 << 2,
        Diagonal = 1 << 3,
        Meridia = 1 << 4,
        Invert = 1 << 5,
    }
    [Flags] public enum TutorialState
    {
        None = 0,
        Ticket = 1 << 0,
        Traitor = 1 << 1,
        Marker = 1 << 2,
    }
    public enum PageType
    { 
        None,
        Prompt,
        Profile,
        ColorKey,
    }
    public enum TripPrompt
    {
        None,
        Stations,
        Carriage_Numbers,
        Sports_Teams,
        Count,
    }
    public enum TripClue
    { 
        None,
        Behaviours,
        Appearence,
        CarriageNumber,
    }
    public enum UIState
    {
        None,
        StartMenu,
        OptionsMenu,
        Notepad,
        Ticket,
        CarriageMap,

    }

    public enum KeySpriteIndices
    { 
        Cursor = 2,
        UpArrow = 3,
        Q = 7,
        W = 8,
        A = 9,
        S = 10,
        D = 11,
        One = 12,
        Two = 13,
        Three = 14,
        Shift = 15,
        E = 16,
        Space = 26,
        Pointer = 27,

    }

    public enum ButtonState
    { 
        Unhovered,
        Hovered,
        Clicked,
    }
    public enum MoveButtonDirection
    { 
        Left,
        Right,
    }


    
    [Serializable] public struct TextUIElement
    {
        public delegate void OnClick();

        public AtlasTextRenderer renderer;

        [Header("Generated")]
        public Vector3 activePos;
        public OnClick OnClickCallback;
        public ButtonState curState;
        public CancellationTokenSource ctsMove;
        public void Init(OnClick onClickCallback)
        {
            OnClickCallback = onClickCallback;
            activePos = renderer.transform.localPosition;
        }
        public void UpdateButton(PlayerInputsSO playerInputs)
        {
            switch (curState)
            { 
                case ButtonState.Unhovered:
                {
                    if (CursorController.IsInsideBounds(renderer.background_renderer.bounds, isClickable: true))
                    {
                        renderer.SetColorText(Color.white);
                        renderer.background_renderer.customBit |= (int)ColorBits.Invert;
                        curState = ButtonState.Hovered;
                    }
                }
                break;
                case ButtonState.Hovered:
                {
                    if (!CursorController.IsInsideBounds(renderer.background_renderer.bounds, isClickable: true))
                    {
                        renderer.background_renderer.customBit &= (int)~ColorBits.Invert;
                        renderer.SetColorText(Color.black);
                        curState = ButtonState.Unhovered;
                    }
                    else if (playerInputs.mouseLeftDown)
                    {
                        OnClickCallback();
                        curState = ButtonState.Clicked;
                    }
                }
                break;
                case ButtonState.Clicked:
                {
                    if (!CursorController.IsInsideBounds(renderer.background_renderer.bounds, isClickable: true))
                    {
                        renderer.SetColorText(Color.black);
                        renderer.background_renderer.customBit &= (int)~ColorBits.Invert;
                        curState = ButtonState.Unhovered;
                    }
                    else if (playerInputs.mouseLeftUp)
                    {
                        renderer.SetColorText(Color.white);
                        renderer.background_renderer.customBit |= (int)ColorBits.Invert;
                        curState = ButtonState.Hovered;
                    }
                }
                break;
            }
        }
        public void MoveButtonAway(CameraStatsSO camStats, MoveButtonDirection dir)
        {
            ctsMove?.Cancel();
            ctsMove = new CancellationTokenSource();
            MovingButtonAway(camStats, dir).Forget();
        }
        public void SetButtonAway(CameraStatsSO camStats, MoveButtonDirection dir)
        {
            Transform buttonTransform = renderer.transform;
            Bounds buttonBounds = renderer.background_renderer.GetBounds();

            Vector3 buttonPos = renderer.transform.localPosition;
            Vector3 targetPos = buttonPos;

            switch (dir)
            {
                case MoveButtonDirection.Left:
                {
                    targetPos.x = -camStats.camBounds.extents.x - buttonBounds.size.x;
                }
                break;
                case MoveButtonDirection.Right:
                {
                    targetPos.x = camStats.camBounds.extents.x + buttonBounds.size.x;
                }
                break;
            }
            renderer.transform.localPosition = targetPos;
        }
        public void MoveButtonToRight()
        {
            ctsMove?.Cancel();
            ctsMove = new CancellationTokenSource();
            MovingButtonToOppositeX().Forget();
        }
        public void MoveButtonToActive()
        {
            ctsMove?.Cancel();
            ctsMove = new CancellationTokenSource();
            MovingButtonToActive().Forget();
        }
        private async UniTask MovingButtonAway(CameraStatsSO camStats, MoveButtonDirection dir)
        {
            Transform buttonTransform = renderer.transform;
            Bounds buttonBounds = renderer.background_renderer.GetBounds();

            Vector3 buttonPos = renderer.transform.localPosition;
            Vector3 targetPos = buttonPos;

            switch(dir)
            {
                case MoveButtonDirection.Left:
                {
                    targetPos.x = -camStats.camBounds.extents.x - buttonBounds.size.x;
                }
                break;
                case MoveButtonDirection.Right:
                {
                    targetPos.x = camStats.camBounds.extents.x + buttonBounds.size.x;
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
                buttonPos = targetPos;
                buttonTransform.localPosition = buttonPos;

            }
            catch (OperationCanceledException)
            {

            }
        }
        private async UniTask MovingButtonToOppositeX()
        {
            Transform buttonTransform = renderer.transform;
            Bounds buttonBounds = renderer.background_renderer.GetBounds();

            Vector3 buttonPos = buttonTransform.localPosition;

            float targetPosX = -activePos.x;
            try
            {
                while (Mathf.Abs(buttonPos.x - targetPosX) > 0.005f)
                {
                    buttonPos.x = Mathf.Lerp(buttonPos.x, targetPosX, Time.deltaTime * 2);
                    buttonTransform.localPosition = buttonPos;

                    await UniTask.Yield(ctsMove.Token);
                }
                buttonPos.x = targetPosX;
                buttonTransform.localPosition = buttonPos;
            }
            catch (OperationCanceledException)
            {
            }
        }
        private async UniTask MovingButtonToActive()
        {
            Transform buttonTransform = renderer.transform;
            Bounds buttonBounds = renderer.background_renderer.GetBounds();
            Vector3 buttonPos = buttonTransform.localPosition;
            try
            {
                while ((buttonPos - activePos).sqrMagnitude > 0.005f)
                {
                    buttonPos = Vector3.Lerp(buttonPos, activePos, Time.deltaTime * 2);
                    buttonTransform.localPosition = buttonPos;

                    await UniTask.Yield(ctsMove.Token);
                }
                buttonTransform.localPosition = activePos;
            }
            catch (OperationCanceledException)
            {

            }
        }
    }
    [Serializable] public struct IconUIElement
    {
        public delegate void OnClick(IconUIElement icon);
        public delegate void OnEnter(IconUIElement icon);
        public delegate void OnExit(IconUIElement icon);

        public AtlasRenderer renderer;

        public OnClick OnClickCallback;
        public OnEnter OnEnterCallback;
        public OnExit OnExitCallback;

        [Header("Generated")]
        public Vector3 startPos;
        public ButtonState curState;

        public CancellationTokenSource ctsMove;
        public void Init(OnClick onClickCallback, OnEnter onEnterCallback, OnExit onExitCallback)
        {
            OnClickCallback = onClickCallback;
            OnEnterCallback = onEnterCallback;
            OnExitCallback = onExitCallback;
        }
        public void UpdateButton(PlayerInputsSO playerInputs)
        {
            switch (curState)
            {
                case ButtonState.Unhovered:
                {
                    if (CursorController.IsInsideBounds(renderer.bounds, isClickable: true))
                    {
                        OnEnterCallback(this);
                        curState = ButtonState.Hovered;
                    }
                }
                break;
                case ButtonState.Hovered:
                {
                    if (!CursorController.IsInsideBounds(renderer.bounds, isClickable: true))
                    {
                        OnExitCallback(this);
                        curState = ButtonState.Unhovered;
                    }
                    else if (playerInputs.mouseLeftDown)
                    {
                        OnClickCallback(this);
                        curState = ButtonState.Clicked;
                    }
                }
                break;
                case ButtonState.Clicked:
                {
                    if (!CursorController.IsInsideBounds(renderer.bounds, isClickable: true))
                    {
                        OnExitCallback(this);
                        curState = ButtonState.Unhovered;
                    }
                    else if (playerInputs.mouseLeftUp)
                    {
                        OnEnterCallback(this);
                        curState = ButtonState.Hovered;
                    }
                }
                break;
            }
        }

        public void MoveTutorialUIElement(CameraStatsSO camStats, AtlasTextRenderer tutorialRenderer, string text)
        {
            ctsMove?.Cancel();
            ctsMove = new CancellationTokenSource();

            MovingTutorialUIElement(camStats, tutorialRenderer, text).Forget();
        }
        public void MoveBackTutorialUIElement(AtlasTextRenderer tutorialRenderer)
        {
            ctsMove?.Cancel();
            ctsMove = new CancellationTokenSource();

            MovingBackTutorialUIElement(tutorialRenderer).Forget();
        }
        private async UniTask MovingTutorialUIElement(CameraStatsSO camStats, AtlasTextRenderer tutorialRenderer, string text)
        {
            Transform iconTransform = renderer.transform;

            Vector2 targetPos = new Vector2();
            targetPos.x = 0;

            float localPivotPosY = renderer.bounds.size.y * renderer.sprite.uvPivot.y;
            targetPos.y = camStats.camBounds.extents.y - localPivotPosY - UI_POSITION_BUFFER;

            Vector2 curPos = new Vector2();
            curPos.x = iconTransform.localPosition.x;
            curPos.y = iconTransform.localPosition.y;

            try
            {
                while ((curPos - targetPos).sqrMagnitude > 0.05f)
                {
                    curPos = Vector2.Lerp(curPos, targetPos, Time.deltaTime * 2);
                    iconTransform.localPosition = new Vector3(curPos.x, curPos.y, 1);
                    await UniTask.Yield(ctsMove.Token);
                }

                float tutTextLocaPosY = iconTransform.localPosition.y - localPivotPosY - tutorialRenderer.background_renderer.worldPivotsAndSizes[8].w - 0.1f;
                tutorialRenderer.transform.localPosition = new Vector3(iconTransform.localPosition.x, tutTextLocaPosY, 1);
                tutorialRenderer.SetText(text);
            }
            catch (OperationCanceledException)
            {
                iconTransform.localPosition = new Vector3(targetPos.x, targetPos.y, 1);
                float tutTextLocaPosY = iconTransform.localPosition.y - localPivotPosY - tutorialRenderer.background_renderer.worldPivotsAndSizes[8].w - 0.1f;
                tutorialRenderer.transform.localPosition = new Vector3(iconTransform.localPosition.x, tutTextLocaPosY, 1);
                tutorialRenderer.SetText(text);
            }
        }
        private async UniTask MovingBackTutorialUIElement(AtlasTextRenderer tutorialRenderer)
        {
            Transform iconTransform = renderer.transform;

            Vector3 curPos = iconTransform.localPosition;

            try
            {
                tutorialRenderer.SetText("");
                while ((curPos - startPos).sqrMagnitude > 0.01f)
                {
                    curPos = Vector3.Lerp(curPos, startPos, Time.deltaTime * 2);

                    iconTransform.localPosition = curPos;
                    await UniTask.Yield(ctsMove.Token);
                }
                iconTransform.localPosition = startPos;
            }
            catch (OperationCanceledException)
            {

            }
        }

    }

    static float NaturalMoveClock;

    public static Dictionary<TripPrompt, string> PromptStringDict;
    public static void UpdateNaturalPos(Vector3 activePos,  ref Vector3 naturalMovePos)
    {
        NaturalMoveClock += Time.deltaTime;

        if (NaturalMoveClock > NATURAL_TICK_RATE)
        {
            Vector2 xyPos = UnityEngine.Random.insideUnitCircle * NATURAL_RADIUS;
            NaturalMoveClock = 0;
            naturalMovePos.x = activePos.x + (xyPos.x * 0.1f);
            naturalMovePos.y = activePos.y + xyPos.y;
            naturalMovePos.z = activePos.z;
        }
    }
    public static void MoveUIElement(Transform transform, Vector3 nextPos, ref CancellationTokenSource cts, UIState curState)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        MovingUIElement(transform, cts, nextPos, curState).Forget();
    }
    private static async UniTask MovingUIElement(Transform transform, CancellationTokenSource cts, Vector3 nextPos, UIState curState)
    {
        float elapsedTime = 0f;
        try
        {
            while (elapsedTime < TransitionTime)
            {
                transform.transform.localPosition = Vector3.Lerp(transform.transform.localPosition, nextPos, Time.deltaTime * MOVE_DAMP);
                elapsedTime += Time.deltaTime;

                await UniTask.Yield(cts.Token);
            }
            transform.transform.localPosition = nextPos;
        }
        catch (OperationCanceledException)
        {
        }
    }
    public static Behaviours GetBehaviourAtIndex(Behaviours behaviours, int index)
    {
        int count = 0;
        foreach (Behaviours flag in Enum.GetValues(typeof(Behaviours)))
        {
            if (flag == Behaviours.None || flag == Behaviours.Count) continue;

            if ((behaviours & flag) != 0)
            {
                if (count == index) return flag;
                count++;
            }
        }
        return Behaviours.None;
    }
    public static Dictionary<TEnum, string> InitEnumToStringDict<TEnum>() where TEnum : Enum
    {
        Dictionary<TEnum, string> dict = new Dictionary<TEnum, string>();

        Array values = Enum.GetValues(typeof(TEnum));

        foreach (TEnum value in values)
        {
            int int32 = Convert.ToInt32(value);
            if (int32 == 0) continue;
            dict.Add(value, value.ToString().Replace("_", " "));
        }
        return dict;
    }
}
