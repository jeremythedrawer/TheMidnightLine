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
    public void FadeToBlack(string text, SceneType sceneType, int sceneIndex)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();
        FadingToBlack(text, sceneType, sceneIndex).Forget();
    }
    public void FadeFromBlack()
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();
        FadingFromBlack().Forget();
    }
    public void CheckToFadeFromBlack()
    {
        if (finishedFade && (playerInputs.mouseLeftUp || playerInputs.spacebarDown || playerInputs.move != 0))
        {
            FadeFromBlack();
        }
    }
    private async UniTask FadingToBlack(string text, SceneType sceneType, int sceneIndex)
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
    private async UniTask FadingFromBlack()
    {
        try
        {
            float elapsedTime = FADE_BLACK_DURATION;
            Scenes.SetScene(sceneData, curSceneType, curSceneIndex);
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
    private void SetFinishFade()
    {
        finishedFade = true;
    }
}
