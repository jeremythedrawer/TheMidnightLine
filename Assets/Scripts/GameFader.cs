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
    [SerializeField] MaterialIDSO materialIDs;
    CancellationTokenSource ctsFade;

    private void Awake()
    {

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

    private void Start()
    {
        data.brightness = 0;
        image.material.SetFloat(materialIDs.ids.brightness, data.brightness);
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
        float elaspedTime = data.brightness * data.fadeTime;
        while (fadeIn ? elaspedTime < data.fadeTime : elaspedTime > 0f)
        {
            elaspedTime += (fadeIn ? Time.deltaTime : -Time.deltaTime);

            data.brightness = elaspedTime / data.fadeTime;
            image.material.SetFloat(materialIDs.ids.brightness, data.brightness);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        data.brightness = fadeIn ? 1f : 0f;
        image.material.SetFloat(materialIDs.ids.brightness, data.brightness);
    }
}
