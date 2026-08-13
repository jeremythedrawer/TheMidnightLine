using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class Graffiti : MonoBehaviour
{
    public const float LIFETIME = 60;

    public AtlasRenderer[] renderers;
    public AtlasSO atlas;
    public CancellationTokenSource ctsDissappear;
    private void OnDisable()
    {
        ctsDissappear?.Cancel();
    }
    public void SetSprites(int index)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            AtlasRenderer renderer = renderers[i];
            renderer.enabled = true;
            renderer.UpdateSpriteInputsByIndex(index);
            renderer.custom.x = 0;
        }
    }
    public void UpdateAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            AtlasRenderer renderer = renderers[i];
            renderer.custom.x = alpha;
        }
    }
    public void Dissappear()
    {
        ctsDissappear?.Cancel();
        ctsDissappear = new CancellationTokenSource();

        Dissappearing().Forget();
    }

    public async UniTask Dissappearing()
    {
        float clock = LIFETIME;

        try
        {
            while (clock >= 0)
            {
                clock -= Time.deltaTime;
                float t = clock / LIFETIME;
            
                for (int i = 0; i < renderers.Length; i++)
                {
                    AtlasRenderer renderer = renderers[i];
                    renderer.custom.x = t;
                }
                await UniTask.Yield(ctsDissappear.Token);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                AtlasRenderer renderer = renderers[i];
                renderer.enabled = false;
            }
            enabled = false;
            NPCManager.ReturnGraffiti(this);
        }
        catch(OperationCanceledException)
        {
        }
    }
}
