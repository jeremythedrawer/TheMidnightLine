using Cysharp.Threading.Tasks;
using UnityEngine;
using static Atlas;

public class GangwayDoor : MonoBehaviour
{
    private static float DOOR_MOVE_TIME = 0.3f;

    [SerializeField] BoxCollider2D wallCollider;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] AtlasRenderer atlasRenderer;
    [SerializeField] Carriage carriage;
    [SerializeField] SpyStatsSO spyStats;

    [Header("Generated")]
    public bool isOpen;
    public AtlasClip doorClip;


    private void Start()
    {
        doorClip = atlasRenderer.atlas.clipDict[(int)TrainMotion.TrainDoor];
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if ((layerSettings.spy & (1 << collision.gameObject.layer)) == 0 || !isOpen) return;

        ClosingDoor().Forget();

        if ((atlasRenderer.flipX && spyStats.curWorldPos.x < transform.position.x) || (!atlasRenderer.flipX && spyStats.curWorldPos.x > transform.position.x))
        {
            carriage.FadeIn();
        }
    }

    public void OpenDoor()
    {
        if (isOpen) return;

        OpeningDoor().Forget();
        carriage.FadeOut();
    }
    private async UniTask OpeningDoor()
    {
        float elapsedTime = 0;

        while (elapsedTime < DOOR_MOVE_TIME)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Pow((elapsedTime / DOOR_MOVE_TIME), 2);

            atlasRenderer.PlayManualClip(doorClip, t);

            await UniTask.Yield();
        }
        atlasRenderer.PlayManualClip(doorClip, 1);
        isOpen = true;
        wallCollider.enabled = false;
    }

    private async UniTask ClosingDoor()
    {
        float elapsedTime = DOOR_MOVE_TIME;

        while (elapsedTime > 0)
        {
            elapsedTime -= Time.deltaTime;

            float t = Mathf.Pow((elapsedTime / DOOR_MOVE_TIME), 2);

            atlasRenderer.PlayManualClip(doorClip, t);

            await UniTask.Yield();
        }
        atlasRenderer.PlayManualClip(doorClip, 0);
        isOpen = false;
        wallCollider.enabled = true;
    }
}
