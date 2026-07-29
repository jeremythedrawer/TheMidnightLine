using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static AtlasUI;
using static Scenes;
public class FadeBlack : MonoBehaviour
{
    public static event Action OnFinishFadeFromBlack;

    public SceneData sceneData;
    public Material fadeBlackMaterial;
    public PlayerInputsSO playerInputs;

    public AtlasTextRenderer textRenderer;

    public CancellationTokenSource ctsFadeBlack;

    [Header("Generated")]
    public SceneType curSceneType;
    public int curSceneIndex;
    public bool finishedFade;
    public void FadeToBlackChangeScene(string text, SceneType sceneType, int sceneIndex)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();
        FadingToBlackChangeScene(text, sceneType, sceneIndex).Forget();
    }
    public void FadeToBlack(float value)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();
        FadingToBlack(value).Forget();
    }
    public void CheckToFadeFromBlack()
    {
        if (finishedFade && (playerInputs.mouseLeftUp || playerInputs.spacebarDown || playerInputs.move != 0))
        {
            FadeFromBlackWithSceneChange();
        }
    }
    public void FadeFromBlack()
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        FadingFromBlack().Forget();
    }
    public void FadeFromBlackWithSceneChange()
    {
        Scenes.SetScene(sceneData, curSceneType, curSceneIndex);
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        FadingFromBlack().Forget();
    }
    private async UniTask FadingToBlackChangeScene(string text, SceneType sceneType, int sceneIndex)
    {
        try
        {
            float elapsedTime = 0;
            finishedFade = false;
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

            textRenderer.WriteText(text, WRITE_LETTER_TIME, SetFinishFade);

        }
        catch (OperationCanceledException) { }
    }

    private async UniTask FadingToBlack(float value)
    {
        try
        {
            float elapsedTime = fadeBlackMaterial.GetFloat("_Alpha");
            finishedFade = false;
            float totalTime = FADE_BLACK_DURATION * value;
            while (elapsedTime < totalTime)
            {
                float t = elapsedTime / totalTime;
                fadeBlackMaterial.SetFloat("_Alpha", t);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Alpha", value);

        }
        catch (OperationCanceledException) { }
    }
    private async UniTask FadingFromBlack()
    {
        try
        {
            float elapsedTime = fadeBlackMaterial.GetFloat("_Alpha");
            while (elapsedTime > 0)
            {
                float t = elapsedTime / FADE_BLACK_DURATION;
                fadeBlackMaterial.SetFloat("_Alpha", t);
                textRenderer.SetAppearTextAlpha(1 - t);
                elapsedTime -= Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Alpha", 0);
            textRenderer.SetAppearTextAlpha(1);
            OnFinishFadeFromBlack?.Invoke();
        }
        catch (OperationCanceledException) { }
    }
    public void SetValue(float value)
    {
        fadeBlackMaterial.SetFloat("_Alpha", value);
    }
    private void SetFinishFade()
    {
        finishedFade = true;
    }
}
