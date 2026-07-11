using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
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

    public static float TransitionTime = -Mathf.Log(TARGET_MARGIN) / MOVE_DAMP;

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
        Notepad,
        Ticket,
        CarriageMap,
    }
    public static Vector3 NotepadActiveLocalPos;
    public static Vector3 NotepadInactiveLocalPos;
    public static Vector3 NotepadHoverPos;

    static float NaturalMoveClock;

    public static event Action OnFinishFadeFromBlack;
    public static Dictionary<TripPrompt, string> PromptStringDict;
    public static void InvertButton(bool invert, AtlasRenderer renderer)
    {
        renderer.custom.x = invert ? 0 : 1;
    }
    public static void SetFadeBlack(Material fadeBlackMaterial, bool toFadeBlack)
    {
        fadeBlackMaterial.SetFloat("_Alpha", toFadeBlack ? 1 : 0);
    }
    public static void FadeBlack(Material fadeBlackMaterial, CancellationTokenSource cts, bool toFadeBlack)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();

        if (toFadeBlack)
        {
            FadingToBlack(fadeBlackMaterial, cts).Forget();
        }
        else
        {
            FadingFromBlack(fadeBlackMaterial, cts).Forget();
        }
    }
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
    private static async UniTask FadingToBlack(Material fadeBlackMaterial, CancellationTokenSource cts)
    {
        try
        {
            float elapsedTime = 0;

            while(elapsedTime < FADE_BLACK_DURATION)
            {
                float t = elapsedTime / FADE_BLACK_DURATION;
                fadeBlackMaterial.SetFloat("_Alpha", t);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(cts.Token);
            }
            fadeBlackMaterial.SetFloat("_Alpha", 1);
        }
        catch (OperationCanceledException) { }
    }
    private static async UniTask FadingFromBlack(Material fadeBlackMaterial, CancellationTokenSource cts)
    {
        try
        {
            float elapsedTime = FADE_BLACK_DURATION;

            while(elapsedTime > 0)
            {
                float t = elapsedTime / FADE_BLACK_DURATION;
                fadeBlackMaterial.SetFloat("_Alpha", t);
                elapsedTime -= Time.deltaTime;
                await UniTask.Yield(cts.Token);
            }
            fadeBlackMaterial.SetFloat("_Alpha", 0);
            OnFinishFadeFromBlack?.Invoke();
        }
        catch (OperationCanceledException) { }
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
