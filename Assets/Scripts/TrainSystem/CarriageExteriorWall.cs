using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using System.Threading;
using UnityEngine;

public class CarriageExteriorWall : MonoBehaviour
{
    [SerializeField] SpriteRenderer[] spriteRenderers;

    [Serializable] public struct MaterialIDs
    {
        internal int alphaID;
    }
    [SerializeField] MaterialIDs materialIDs;
    [Serializable] public struct Settings
    {
        public float fadeTime;

    }
    [SerializeField] Settings settings;
    MaterialPropertyBlock mpb;
    float alpha;

    CancellationTokenSource ctsFade;
    private void Awake()
    {
        materialIDs.alphaID = Shader.PropertyToID("_Alpha");
        mpb = new MaterialPropertyBlock();
        ctsFade = new CancellationTokenSource();
        alpha = 1;
    }

    public void StartFade(bool fadeIn)
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        Fade(fadeIn, ctsFade.Token).Forget();

    }
    private async UniTask Fade(bool fadeIn, CancellationToken token)
    {
        float elaspedTime = alpha * settings.fadeTime; 
        try
        {
            while (fadeIn ? elaspedTime < settings.fadeTime : elaspedTime > 0f)
            {
                token.ThrowIfCancellationRequested();

                elaspedTime += (fadeIn ? Time.deltaTime : - Time.deltaTime);
                
                alpha = elaspedTime / settings.fadeTime;
                mpb.SetFloat(materialIDs.alphaID, alpha);
                for(int i = 0; i < spriteRenderers.Length; i++)
                {
                    spriteRenderers[i].SetPropertyBlock(mpb);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
