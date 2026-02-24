
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static Atlas;

public class GangwayDoor : MonoBehaviour
{
    [SerializeField] BoxCollider2D wallCollider;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] AtlasRenderer atlasRenderer;

    [Header("Generated")]
    public bool isOpen;
    public AtlasClip doorClip;

    private const float DOOR_MOVE_TIME = 1;

    private void Start()
    {
        doorClip = atlasRenderer.atlas.clipDict[(int)TrainMotion.TrainDoor];
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((layerSettings.spy & (1 << collision.gameObject.layer)) == 0 || isOpen) return;

        OpeningDoor().Forget();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if ((layerSettings.spy & (1 << collision.gameObject.layer)) == 0 || !isOpen) return;

        ClosingDoor().Forget();
    }
    private async UniTask OpeningDoor()
    {
        float elapsedTime = 0;

        while (elapsedTime < DOOR_MOVE_TIME)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Pow((elapsedTime / DOOR_MOVE_TIME), 2);

            atlasRenderer.sprite = doorClip.GetNextSpriteManual(t);

            await UniTask.Yield();
        }
        atlasRenderer.sprite = doorClip.GetNextSpriteManual(1);
        isOpen = true;
    }

    private async UniTask ClosingDoor()
    {
        float elapsedTime = DOOR_MOVE_TIME;

        while (elapsedTime > 0)
        {
            elapsedTime -= Time.deltaTime;

            float t = Mathf.Pow((elapsedTime / DOOR_MOVE_TIME), 2);

            atlasRenderer.sprite = doorClip.GetNextSpriteManual(t);

            await UniTask.Yield();
        }
        atlasRenderer.sprite = doorClip.GetNextSpriteManual(0);
        isOpen = false;
    }
}
