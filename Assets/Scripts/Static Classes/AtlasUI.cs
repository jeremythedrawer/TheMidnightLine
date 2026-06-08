using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using static NPC;

public static class AtlasUI
{
    public const float LETTER_ADVANCE = 0.122f;
    public const float APPEAR_TEXT_TIME = 0.2f;

    public enum NotepadState
    {
        None,
        Stationary,
        Writing,
        Erasing,
        FlippingUp,
        FlippingDown,
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


    public static Dictionary<TripPrompt, string> promptDict;
    
    public static void WriteText(AtlasTextRenderer textRenderer, string text, CancellationTokenSource cts, float writeLetterTime)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        WritingText(text, textRenderer, cts, writeLetterTime).Forget();
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
    public static void EraseText(string text, AtlasTextRenderer textRenderer, CancellationTokenSource cts, float writeLetterTime)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        ErasingText(text, textRenderer, cts, writeLetterTime).Forget();
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

    public static void InvertButton(bool invert, AtlasRenderer renderer)
    {
        renderer.custom.x = invert ? 0 : 1;
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
