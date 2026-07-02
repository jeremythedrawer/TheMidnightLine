using Cysharp.Threading.Tasks;
using UnityEngine;

public class TicketIcon : MonoBehaviour
{
    const float USE_TIME = 0.8f;

    public AtlasRenderer mainTicket;
    public AtlasRenderer stubTicket;
    public ColorsSO colors;

    private void Start()
    {
        stubTicket.custom.y = stubTicket.bounds.size.x;
    }
    public void InvertIcon(bool toggle)
    {
        float value = toggle ? 1f : 0f;
        mainTicket.custom.x = value;
        stubTicket.custom.x = value;
    }
    public void RipStubTicket()
    {
        RippingStubTicket().Forget();
    }
    public void Appear()
    {
        mainTicket.custom.x = 0;
        stubTicket.custom.x = 0;
        Appearing().Forget();
    }
    public void Disappear()
    {
        Disappearing().Forget();
    }
    public void Init()
    {
        mainTicket.custom.w = 1;
        stubTicket.custom.w = 1;
    }
    private async UniTask RippingStubTicket()
    {
        float elapsed = 0;

        while (elapsed < USE_TIME)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / USE_TIME;
            float easeOutT = Mathf.Pow(t, 0.25f);
            stubTicket.custom.w = easeOutT;
            await UniTask.Yield();
        }

        stubTicket.custom.w = 1;
    }
    private async UniTask Appearing()
    {
        float elapsed = USE_TIME;

        while (elapsed > 0)
        {
            elapsed -= Time.deltaTime;
            float t = elapsed / USE_TIME;
            mainTicket.custom.w = t;
            stubTicket.custom.w = t;
            await UniTask.Yield();
        }
        mainTicket.custom.w = 0;
        stubTicket.custom.w = 0;
    }
    private async UniTask Disappearing()
    {
        float elapsed = 0;

        while (elapsed < USE_TIME)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / USE_TIME;

            mainTicket.custom.w = t;
            stubTicket.custom.w = t;
            
            await UniTask.Yield();
        }
        mainTicket.custom.w = 1;
        stubTicket.custom.w = 1;
    }
}
