using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class GameFader : MonoBehaviour
{
    [SerializeField] GameFadeSO data;
    [SerializeField] Image image;
    [SerializeField] GameEventDataSO gameEventData;

    CancellationTokenSource ctsFade;

    private void Awake()
    {
        data.valueID = Shader.PropertyToID("_Value");
        data.value = 0;
        image.material.SetFloat(data.valueID, data.value);
    }
    private void OnEnable()
    {
        gameEventData.OnGameFadeIn.RegisterListener(FadeIn);
        gameEventData.OnGameFadeOut.RegisterListener(FadeOut);
    }
    private void OnDisable()
    {
        gameEventData.OnGameFadeIn.UnregisterListener(FadeIn);
        gameEventData.OnGameFadeOut.UnregisterListener(FadeOut);
        
    }
    private void FadeIn()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        Fade(fadeIn: true, ctsFade.Token).Forget();
    }
    private void FadeOut()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        Fade(fadeIn: false, ctsFade.Token).Forget();
    }
    private async UniTask Fade(bool fadeIn, CancellationToken token)
    {
        float elaspedTime = data.value * data.fadeTime;
        while (fadeIn ? elaspedTime < data.fadeTime : elaspedTime > 0f)
        {
            token.ThrowIfCancellationRequested();

            elaspedTime += (fadeIn ? Time.deltaTime : -Time.deltaTime);

            data.value = elaspedTime / data.fadeTime;
            image.material.SetFloat(data.valueID, data.value);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        data.value = fadeIn ? 1f : 0f;
        image.material.SetFloat(data.valueID, data.value);
    }
}
