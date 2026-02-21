
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static Atlas;

public class GangwayDoor : MonoBehaviour
{
    [SerializeField] BoxCollider2D boxCollider;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((layerSettings.spy & (1 << collision.gameObject.layer)) == 0) return;

        if (isOpen)
        {

        }
        else
        {
        }
    }

    private async UniTask OpeningDoor()
    {
        float elapsedTime = 0;

        while (elapsedTime < DOOR_MOVE_TIME)
        {
            elapsedTime += Time.deltaTime;

            float t = (elapsedTime / DOOR_MOVE_TIME) * doorClip.time;

            //atlasRenderer.sprite = doorClip.GetNextSprite(ref t, )

            await UniTask.Yield();
        }
    }
}
