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
    const float FADE_BLACK_DURATION = 1f;
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
    }
    public enum TripPrompt
    {
        None,
        Stations,
        Carriage_Numbers,
        Sports_Teams
    }
    public enum TripClue
    { 
        None,
        Behaviours,
        Appearence
    }
    public static Dictionary<TripPrompt, string> PromptDict;
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
        }
        catch (OperationCanceledException) { }
    }
    public static Behaviours GetBehaviourAtIndex(Behaviours behaviours, int index)
    {
        int count = 0;
        foreach (Behaviours flag in Enum.GetValues(typeof(Behaviours)))
        {
            if (flag == Behaviours.None) continue;

            if ((behaviours & flag) != 0)
            {
                if (count == index) return flag;
                count++;
            }
        }
        return Behaviours.None;
    }
}
