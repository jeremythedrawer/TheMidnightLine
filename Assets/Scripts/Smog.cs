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
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] AtlasSpawnerStatsSO spawnStats;
    CancellationTokenSource ctsFade;
    MaterialPropertyBlock mpb;

    private void OnValidate()
    {
        //SetSize();
    }
    private void Awake()
    {
        mpb = new MaterialPropertyBlock();

        shaderValues.curDensity = 1f;
        shaderValues.targetDensity = 1f;

    }
    private void Update()
    {
        Fade();
        spriteRenderer.SetPropertyBlock(mpb);
    }
    private void SetSize()
    {
        transform.localScale = spawnStats.spawnSize;
        transform.position = new Vector3(spawnStats.spawnCenter.x, spawnStats.spawnCenter.y, transform.position.z);
    }
    private void Fade()
    {
        bool shouldFadeOut = spyStats.curGroundLayer == layerSettings.trainLayerStruct.ground && spyStats.curCarriageMinXPos != 0;
        float targetDensity = shouldFadeOut ? 0f : shaderValues.maxDensity;

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
                mpb.SetFloat(materialIDs.ids.density, shaderValues.curDensity);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            shaderValues.curDensity = shaderValues.targetDensity;
            mpb.SetFloat(materialIDs.ids.density, shaderValues.curDensity);
        }
    }
}
