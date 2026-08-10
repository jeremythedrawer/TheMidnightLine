using Cysharp.Threading.Tasks;
using UnityEngine;

public class CarriageMapProp : MonoBehaviour
{
    const float EFFECT_TIME = 0.2f;
    public AtlasRenderer atlasRenderer;
    public void Use()
    {
        Using().Forget();
    }
    public void StopUsing()
    {
        StoppingUsing().Forget();
    }
    private async UniTask Using()
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

    private async UniTask StoppingUsing()
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
