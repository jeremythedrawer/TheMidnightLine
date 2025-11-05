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

    private BackgroundSpawner backgroundSpawner;

    private void Start()
    {
            backgroundSpawner = GetComponentInParent<BackgroundSpawner>();
        
    }

    public override IEnumerator SetLifeTime()
    {
        while (spawner == null)
        {
            yield return null;
        }
        yield return new WaitUntil(() => transform.position.x < spawner.despawnPos.x);

        if (gameObject != null && gameObject.activeSelf)
        {
            backgroundSpawner.prefabPool.Release(this);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        ChooseLOD();
    }

    private void ChooseLOD()
    {
        float zPos = transform.position.z;
        float[] thresholds = spawner.lodThresholdValues;

        switch(thresholds.Length)
        {
            case 0:
            {
                spriteRenderer.sprite = chosenLods.high;
            }
            break;

            case 1:
            {
                if (transform.position.z < spawner.lodThresholdValues[0])
                {
                    spriteRenderer.sprite = chosenLods.high;
                }
                else
                {
                    spriteRenderer.sprite = chosenLods.med;
                }
            }
            break;

            case 2:
            {
                if (transform.position.z < spawner.lodThresholdValues[0])
                {
                    spriteRenderer.sprite = chosenLods.high;
                }
                else if (transform.position.z > spawner.lodThresholdValues[0] && transform.position.z < spawner.lodThresholdValues[1])
                {
                    spriteRenderer.sprite = chosenLods.low;
                }
                else
                {
                    spriteRenderer.sprite = chosenLods.med;
                }                
            }
            break;
        }
    }
}
