using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class CarriageMapProp : MonoBehaviour
{
    const float EFFECT_TIME = 0.2f;
    public AtlasRenderer atlasRenderer;

    [Header("Generated")]
    public CancellationTokenSource ctsInvert;
    public void Revert()
    {
        ctsInvert?.Cancel();
        ctsInvert = new CancellationTokenSource();
        Exiting().Forget();
    }
    public void Enter()
    {
        ctsInvert?.Cancel();
        atlasRenderer.custom.x = 1;
    }
    public void Exit()
    {
        ctsInvert?.Cancel();
        atlasRenderer.custom.x = 0;

    }
    public void Invert()
    {
        ctsInvert?.Cancel();
        ctsInvert = new CancellationTokenSource();
        Entering().Forget();
    }
    private async UniTask Entering()
    {
        float elapsedTime = atlasRenderer.custom.x * EFFECT_TIME;
        try
        {
            while (elapsedTime < EFFECT_TIME)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / EFFECT_TIME;
                atlasRenderer.custom.x = t;
                await UniTask.Yield(ctsInvert.Token);
            }

        }
        catch (OperationCanceledException)
        {

        }
    }

    private async UniTask Exiting()
    {
        float elapsedTime = atlasRenderer.custom.x * EFFECT_TIME;
        try
        {
            while (elapsedTime > 0)
            {
                elapsedTime -= Time.deltaTime;
                float t = elapsedTime / EFFECT_TIME;
                atlasRenderer.custom.x = t;
                await UniTask.Yield(ctsInvert.Token);
            }
            atlasRenderer.custom.x = 0;
        }
        catch(OperationCanceledException)
        {

        }
    }
}
