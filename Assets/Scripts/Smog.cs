using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class Smog : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] EnvironmentShaderValuesSO shaderValues;
    [SerializeField] SpyStatsSO spyStats;
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] LayerSettingsSO layerSettings;

    CancellationTokenSource ctsFade;
    MaterialPropertyBlock mpb;

    private void Awake()
    {
        shaderValues.densityID = Shader.PropertyToID("_Density");
        shaderValues.scrollTimeID = Shader.PropertyToID("_ScrollTime");
        mpb = new MaterialPropertyBlock();
    }
    private void Update()
    {
        Fade();
        Scroll();
        spriteRenderer.SetPropertyBlock(mpb);
    }
    private void Scroll()
    {
        shaderValues.curScrollSpeed = spyStats.curGroundLayer == layerSettings.trainLayerStruct.ground ? trainStats.curKMPerHour * 0.01f : shaderValues.minScrollSpeed;
        shaderValues.curScrollSpeed = Mathf.Max(shaderValues.curScrollSpeed, shaderValues.minScrollSpeed);
        shaderValues.curScrollTime += Time.deltaTime * shaderValues.curScrollSpeed;
        mpb.SetFloat(shaderValues.scrollTimeID, shaderValues.curScrollTime);
    }
    private void Fade()
    {
        bool shouldFadeOut = spyStats.curGroundLayer == layerSettings.trainLayerStruct.ground && spyStats.curCarriageMinXPos != 0;
        float targetDensity = shouldFadeOut ? 0f : 1f;

        if (shaderValues.targetDensity == targetDensity) return;
        shaderValues.targetDensity = targetDensity;
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();
        FadeTo(ctsFade.Token).Forget();
    }
    private async UniTask FadeTo(CancellationToken token)
    {
        float startDensity = shaderValues.curDensity;
        float elapsed = 0f;
        try
        {
            while (elapsed < shaderValues.fadeDensityTime)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = elapsed / shaderValues.fadeDensityTime;

                shaderValues.curDensity = Mathf.Lerp(startDensity, shaderValues.targetDensity, t);
                mpb.SetFloat(shaderValues.densityID, shaderValues.curDensity);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            shaderValues.curDensity = shaderValues.targetDensity;
            mpb.SetFloat(shaderValues.densityID, shaderValues.curDensity);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
