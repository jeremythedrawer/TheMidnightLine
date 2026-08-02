using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static AtlasUI;
using static Scenes;
public class FadeBlack : MonoBehaviour
{
    public const float DEFAULT_DEPTH = 2;
    public const float NOTEPAD_DEPTH = 12.5f;
    [Flags] public enum State
    { 
        FadingIn,
        FadingOut,
        WritingText,
        FinishedFadeIn,
        FinishedFadeOut,
        ReadyToSceneChange,
    }

    public static event Action OnFinishFadeFromBlack;

    public SceneData sceneData;
    public Material fadeBlackMaterial;
    public PlayerInputsSO playerInputs;

    public AtlasTextRenderer textRenderer;
    public AtlasRenderer spacebarRenderer;

    public CancellationTokenSource ctsFadeBlack;

    [Header("Generated")]
    public SceneType curSceneType;
    public State curState;
    public int curSceneIndex;
    public void FadeInChangeScene(string text, SceneType sceneType, int sceneIndex)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();
        transform.localPosition = new Vector3(0, 0, DEFAULT_DEPTH);
        FadingToBlackChangeScene(text, sceneType, sceneIndex).Forget();
    }
    public void FadeInWithSpacebar(float value, float spacebarWaitTime, float fadeBlackDepth = DEFAULT_DEPTH)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        transform.localPosition = new Vector3(0, 0, fadeBlackDepth);
        FadingInWithSpacebar(value, spacebarWaitTime).Forget();
    }
    public void FadeIn(float value, float fadeBlackDepth = DEFAULT_DEPTH)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        transform.localPosition = new Vector3(0, 0, fadeBlackDepth);
        FadingIn(value).Forget();
    }
    public void CheckToFadeOutSceneChange()
    {
        if (curState == (State.FinishedFadeIn | State.ReadyToSceneChange))
        {
            if (playerInputs.mouseLeftUp || playerInputs.spacebarDown || playerInputs.move != 0)
            {
                FadeOutWithSceneChange();
            }
        }
        else if (curState == State.FadingIn || curState == State.WritingText)
        {
            if (playerInputs.spacebarDown)
            {
                textRenderer.ctsWrite?.Cancel();
            }
        }
    }
    public void FadeOut()
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        FadingOut().Forget();
    }
    public void FadeOutWithSceneChange()
    {
        Scenes.SetScene(sceneData, curSceneType, curSceneIndex);
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        FadingOut().Forget();
    }
    private async UniTask FadingToBlackChangeScene(string text, SceneType sceneType, int sceneIndex)
    {
        try
        {
            float elapsedTime = 0;
            curState = State.FadingIn;
            while (elapsedTime < FADE_BLACK_DURATION)
            {
                float t = elapsedTime / FADE_BLACK_DURATION;
                fadeBlackMaterial.SetFloat("_Alpha", t);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Alpha", 1);
            curSceneType = sceneType;
            curSceneIndex = sceneIndex;
            curState = State.WritingText;
            textRenderer.WriteText(text, WRITE_LETTER_TIME, FinishWritingWithSceneChange);

        }
        catch (OperationCanceledException) { }
    }

    private async UniTask FadingInWithSpacebar(float value, float spacebarWaitTime)
    {
        try
        {
            float elapsedTime = fadeBlackMaterial.GetFloat("_Alpha");
            curState = State.FadingIn;
            float totalTime = FADE_BLACK_DURATION * value;
            while (elapsedTime < totalTime)
            {
                float t = (elapsedTime / totalTime) * value;
                fadeBlackMaterial.SetFloat("_Alpha", t);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Alpha", value);
            curState = State.FinishedFadeIn;
            WaitAndSetSpacebar(spacebarWaitTime);

        }
        catch (OperationCanceledException) { }
    }
    private async UniTask FadingIn(float value)
    {
        try
        {
            float elapsedTime = fadeBlackMaterial.GetFloat("_Alpha");
            curState = State.FadingIn;
            float totalTime = FADE_BLACK_DURATION * value;
            while (elapsedTime < totalTime)
            {
                float t = (elapsedTime / totalTime) * value;
                fadeBlackMaterial.SetFloat("_Alpha", t);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Alpha", value);
            curState = State.FinishedFadeIn;
        }
        catch (OperationCanceledException) { }
    }
    private async UniTask FadingOut()
    {
        try
        {
            float elapsedTime = fadeBlackMaterial.GetFloat("_Alpha");
            spacebarRenderer.custom.w = 0;
            curState = State.FadingOut;
            while (elapsedTime > 0)
            {
                float t = elapsedTime / FADE_BLACK_DURATION;
                fadeBlackMaterial.SetFloat("_Alpha", t);
                textRenderer.SetAppearTextAlpha(t);
                elapsedTime -= Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Alpha", 0);
            textRenderer.SetAppearTextAlpha(1);
            curState = State.FinishedFadeOut;
            OnFinishFadeFromBlack?.Invoke();
        }
        catch (OperationCanceledException) { }
    }
    public void SetAlpha(float value)
    {
        fadeBlackMaterial.SetFloat("_Alpha", value);
    }
    private void FinishWritingWithSceneChange()
    {
        curState = (State.FinishedFadeIn | State.ReadyToSceneChange);
        WaitAndSetSpacebar(waitTime: 1);
    }
    public void WaitAndSetSpacebar(float waitTime)
    {
        WaitingAndSettingSpacebar(waitTime).Forget();
    }
    private async UniTask WaitingAndSettingSpacebar(float waitTime)
    {
        await UniTask.WaitForSeconds(waitTime, cancellationToken: ctsFadeBlack.Token);
        spacebarRenderer.custom.w = 1;
    }
}
