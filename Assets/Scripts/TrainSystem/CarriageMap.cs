using Cysharp.Threading.Tasks;
using UnityEngine;

public class CarriageMap : MonoBehaviour
{
    const float EFFECT_TIME = 0.2f;
    public AtlasRenderer atlasRenderer;
    public void InteractEffect()
    {
        Interacting().Forget();
    }
    public void CancelEffect()
    {
        Cancelling().Forget();
    }
    private async UniTask Interacting()
    {
        float elapsedTime = 0;
        while (elapsedTime < EFFECT_TIME)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / EFFECT_TIME;
            atlasRenderer.custom.x = t;
            await UniTask.Yield();
        }

        atlasRenderer.custom.x = 1;
    }

    private async UniTask Cancelling()
    {
        float elapsedTime = EFFECT_TIME;

        while (elapsedTime > 0)
        {
            elapsedTime -= Time.deltaTime;
            float t = elapsedTime / EFFECT_TIME;
            atlasRenderer.custom.x = t;
            await UniTask.Yield();
        }
        atlasRenderer.custom.x = 0;
    }
}
