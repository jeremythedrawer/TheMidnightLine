using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class Gangway : MonoBehaviour
{
    public TrainSettingsSO trainSettings;
    public AtlasRenderer exteriorRenderer;
    [Header("Generated")]
    public float alpha;
    public CancellationTokenSource ctsFade;
    public void MoveUp()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        MovingUp().Forget();

    }
    public void MoveDown()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        MovingDown().Forget();
    }

    private async UniTask MovingDown()
    {
        float elaspedTime = alpha * trainSettings.exteriorWallFadeTime;
        try
        {
            while (elaspedTime < trainSettings.exteriorWallFadeTime)
            {
                elaspedTime += Time.deltaTime;

                alpha = elaspedTime / trainSettings.exteriorWallFadeTime;
                alpha = alpha < 0.5 ? 16 * alpha * alpha * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 5) * 0.5f;
                exteriorRenderer.custom.x = alpha;

                await UniTask.Yield(cancellationToken: ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    private async UniTask MovingUp()
    {
        float elaspedTime = alpha * trainSettings.exteriorWallFadeTime;
        try
        {
            while (elaspedTime > 0)
            {
                elaspedTime -= Time.deltaTime;

                alpha = elaspedTime / trainSettings.exteriorWallFadeTime;
                alpha = alpha < 0.5 ? 16 * alpha * alpha * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 5) * 0.5f;
                exteriorRenderer.custom.x = alpha;

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
