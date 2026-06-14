using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static NPC;
public static class AtlasUI
{
    public const float BORDER_PADDING = 0.05f;
    public const float LETTER_ADVANCE = 0.122f;
    public const float APPEAR_TEXT_TIME = 0.2f;
    public const float FADE_BLACK_DURATION = 1f;
    public const float NATURAL_RADIUS = 0.1f;
    public const float NATURAL_TICK_RATE = 2.5f;
    public const float TARGET_MARGIN = 0.01f;
    public const float MOVE_DAMP = 4;

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
    public enum PageType
    { 
        None,
        Prompt,
        Profile,
        Confirm,
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
    public static Vector3 NotepadActivePos;
    public static Vector3 NotepadInactivePos;
    public static Vector3 NnotepadHoverPos;

    static float NaturalMoveClock;

    public static event Action OnFinishFadeFromBlack;
    public static Dictionary<TripPrompt, string> PromptStringDict;

    public static void WriteText(AtlasTextRenderer textRenderer, string text, CancellationTokenSource cts, float writeLetterTime)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        WritingText(text, textRenderer, cts, writeLetterTime).Forget();
    }
    public static void EraseText(string text, AtlasTextRenderer textRenderer, CancellationTokenSource cts, float writeLetterTime)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        ErasingText(text, textRenderer, cts, writeLetterTime).Forget();
    }
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
    public static void MoveUIElement(Behaviour behaviour, Vector3 nextPos, ref CancellationTokenSource cts, UIState curState)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        MovingUIElement(behaviour, cts, nextPos, curState).Forget();
    }
    private static async UniTask MovingUIElement(Behaviour behaviour, CancellationTokenSource cts, Vector3 nextPos, UIState curState)
    {
        float elapsedTime = 0f;

        behaviour.enabled = true;
        try
        {
            while (elapsedTime < TransitionTime)
            {
                behaviour.transform.localPosition = Vector3.Lerp(behaviour.transform.localPosition, nextPos, Time.deltaTime * MOVE_DAMP);
                elapsedTime += Time.deltaTime;

                await UniTask.Yield(cts.Token);

            }
            behaviour.transform.localPosition = nextPos;
            if (curState == UIState.None) behaviour.enabled = false;
        }
        catch (OperationCanceledException)
        {
        }
    }
    private static async UniTask WritingText(string text, AtlasTextRenderer textRenderer, CancellationTokenSource cts, float writeLetterTime)
    {
        int stationNameLetterCount = text.Length;
        int curLetterIndex = 0;

        string curStationString = "";
        textRenderer.SetText(curStationString);

        try
        {
            while (curLetterIndex < stationNameLetterCount)
            {
                curStationString += text[curLetterIndex];
                await UniTask.WaitForSeconds(writeLetterTime, cancellationToken: cts.Token);
                textRenderer.SetText(curStationString);
                curLetterIndex++;
            }
        }
        catch (OperationCanceledException) { }
    }
    private static async UniTask ErasingText(string text, AtlasTextRenderer textRenderer, CancellationTokenSource cts, float writeLetterTime)
    {
        string curStationString = text;
        try
        {
            while (curStationString.Length > 0)
            {
                await UniTask.WaitForSeconds(writeLetterTime, cancellationToken: cts.Token);
                curStationString = curStationString[..^1];
                textRenderer.SetText(curStationString);
            }
        }
        catch (OperationCanceledException) { }
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
