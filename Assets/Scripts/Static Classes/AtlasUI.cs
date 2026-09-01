using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static Atlas;
using static Passenger;
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

    public const float PENCIL_DISTANCE_THRESHOLD = 0.05f;
    public const float PENCIL_VERTICAL_FREQUENCY = 7f;
    public const float PENCIL_VERTICAL_MAGNITUDE = 0.07f;

    public const float LETTER_ADVANCE = 0.122f;
    public const float APPEAR_TEXT_TIME = 0.2f;
    public const float FADE_BLACK_DURATION = 1f;
    public const float NATURAL_RADIUS = 0.1f;
    public const float NATURAL_TICK_RATE = 2.5f;
    public const float TARGET_MARGIN = 0.01f;
    public const float MOVE_DAMP = 4;
    public const float GRID_GAP = 0.272f;
    public const float NOTEPAD_INACTIVE_OFFSET = 0.39f;
    public const float UI_POSITION_BUFFER = 0.3f;
    public const float PRINT_LETTER_TIME = 0.05f;

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
        FlippingUp,
        FlippingDown,
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
        Outline = 1 << 6,
        Texture = 1 << 7,
        RedChannel = 1 << 8,
        GreenChannel = 1 << 9,
        BlueChannel = 1 << 10,
    }
    [Flags] public enum TutorialState
    {
        None = 0,
        Traitor = 1 << 0,
    }
    [Flags] public enum KeyIconActions
    {
        None = 0,
        ExitNotepad = 1 << 0,
        FlipUpNotepad = 1 << 1,
        FlipDownNotepad = 1 << 2,
        WriteNotepad = 1 << 3,
        EraseNotepad = 1 << 4,
        CarouselNotepadLeft = 1 << 5,
        CarouselNotepadRight = 1 << 6,
        EnterTrain = 1 << 6,
        ExitTrain = 1 << 7,
    }
    public enum PageType
    { 
        None,
        Prompt,
        Profile,
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
        MapMenu,
        Notepad,
        Ticket,
        CarriageMap,
        Outcome,

    }
    public enum ButtonState
    {
        Unhovered,
        Hovered,
        Clicked,
    }
    public enum ButtonFunctionType
    {
        None,
        Begin,
        Options,
        Quit,
        Back,
    }
    public enum KeyboardBindingIconIndices
    { 
        Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine, 
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Spacebar,
    }
    [Flags] public enum Direction
    { 
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3,
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
    public static void ShowKeyIcon(AtlasRenderer renderer, Vector2 position, KeyboardBindingIconIndices keySpriteIndex, Direction direction)
    {
        renderer.enabled = true;
        renderer.custom.w = 1;
        renderer.transform.SetParent(null);
        renderer.UpdateSpriteInputsByIndex((int)keySpriteIndex);
        
        Vector2 boundsSize = renderer.bounds.size;
        Vector3 newPos = position;
        if ((direction & Direction.Left) != 0)
        {
            newPos.x -= boundsSize.x;
        }
        
        if ((direction & Direction.Right) != 0)
        {
            newPos.x += boundsSize.x;
        }

        if ((direction & Direction.Down) != 0)
        {
            newPos.y -= boundsSize.y;
        }
        
        if ((direction & Direction.Up) != 0)
        {
            newPos.y += boundsSize.y;
        }
        newPos.z = renderer.transform.position.z;
        renderer.transform.position = newPos;
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
    public static Habits GetBehaviourAtIndex(Habits behaviours, int index)
    {
        int count = 0;
        foreach (Habits flag in Enum.GetValues(typeof(Habits)))
        {
            if (flag == Habits.None) continue;

            if ((behaviours & flag) != 0)
            {
                if (count == index) return flag;
                count++;
            }
        }
        return Habits.None;
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
