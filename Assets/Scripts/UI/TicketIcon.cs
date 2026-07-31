using Cysharp.Threading.Tasks;
using UnityEngine;

using static AtlasUI;

public class TicketIcon : MonoBehaviour
{
    const float USE_TIME = 0.8f;

    public IconUIElement mainTicket;
    public IconUIElement stubTicket;
    public OptionsSO colors;

    private void Start()
    {
        mainTicket.startPos = mainTicket.renderer.transform.localPosition;
        stubTicket.startPos = stubTicket.renderer.transform.localPosition;

        stubTicket.renderer.custom.y = stubTicket.renderer.bounds.size.x;
    }
    public void InvertIcon(bool toggle)
    {
        float value = toggle ? 1f : 0f;
        mainTicket.renderer.custom.x = value;
        stubTicket.renderer.custom.x = value;
    }
    public void RipStubTicket()
    {
        RippingStubTicket().Forget();
    }
    public void Appear()
    {
        mainTicket.renderer.custom.x = 0;
        stubTicket.renderer.custom.x = 0;
        Appearing().Forget();
    }
    public void Disappear()
    {
        Disappearing().Forget();
    }
    public void Init()
    {
        mainTicket.renderer.custom.w = 1;
        stubTicket.renderer.custom.w = 1;
    }
    private async UniTask RippingStubTicket()
    {
        float elapsed = 0;

        while (elapsed < USE_TIME)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / USE_TIME;
            float easeOutT = Mathf.Pow(t, 0.25f);
            stubTicket.renderer.custom.w = easeOutT;
            mainTicket.renderer.custom.x = 1 - easeOutT;
            await UniTask.Yield();
        }

        stubTicket.renderer.custom.w = 1;
        mainTicket.renderer.custom.x = 0;
    }
    private async UniTask Appearing()
    {
        float elapsed = USE_TIME;

        while (elapsed > 0)
        {
            elapsed -= Time.deltaTime;
            float t = elapsed / USE_TIME;
            mainTicket.renderer.custom.w = t;
            stubTicket.renderer.custom.w = t;
            await UniTask.Yield();
        }
        mainTicket.renderer.custom.w = 0;
        stubTicket.renderer.custom.w = 0;
    }
    private async UniTask Disappearing()
    {
        float elapsed = 0;

        while (elapsed < USE_TIME)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / USE_TIME;

            mainTicket.renderer.custom.w = t;
            stubTicket.renderer.custom.w = t;
            
            await UniTask.Yield();
        }
        mainTicket.renderer.custom.w = 1;
        stubTicket.renderer.custom.w = 1;
    }
}
