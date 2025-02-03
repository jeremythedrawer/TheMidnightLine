using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnedBGPrefab : SpawnedPrefab
{
    [System.Serializable]
    public struct LodSprites
    {
        public Sprite low;
        public Sprite med;
        public Sprite high;

        public LodSprites(Sprite low, Sprite med, Sprite high)
        {
            this.low = low;
            this.med = med;
            this.high = high;
        }
    }
    public List<LodSprites> lodSprites;

    public LodSprites chosenLods { get; set; }

    private void OnEnable()
    {
        StartCoroutine(SetLifeTime());
    }


    private void OnDisable()
    {
        StopCoroutine(SetLifeTime());
    }

    private IEnumerator SetLifeTime()
    {
        while (canvasBounds == null)
        {
            yield return null;
        }
        yield return new WaitUntil(() => transform.position.x < canvasBounds.despawnPoint.x);

        if (gameObject != null && gameObject.activeSelf)
        {
            BackgroundSpawner.Instance.prefabPool.Release(this);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        ChooseLOD();
    }

    private void ChooseLOD()
    {
        if (transform.position.z < canvasBounds.oneThirdClipPlane)
        {
            spriteRenderer.sprite = chosenLods.low;
        }
        else if (transform.position.z > canvasBounds.twoThirdsClipPlane)
        {
            spriteRenderer.sprite = chosenLods.high;
        }
        else
        {
            spriteRenderer.sprite = chosenLods.med;
        }
    }
}
