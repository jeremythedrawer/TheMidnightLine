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

    public override IEnumerator SetLifeTime()
    {
        while (spawner == null)
        {
            yield return null;
        }
        yield return new WaitUntil(() => transform.position.x < spawner.despawnPos.x);

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
        if (transform.position.z < spawner.oneThirdPlane)
        {
            spriteRenderer.sprite = chosenLods.high;
        }
        else if (transform.position.z > spawner.twoThirdsPlane)
        {
            spriteRenderer.sprite = chosenLods.low;
        }
        else
        {
            spriteRenderer.sprite = chosenLods.med;
        }
    }
}
