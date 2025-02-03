using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BackgroundSpawner : Spawner
{
    public static BackgroundSpawner Instance { get; private set; }

    [Header("Pool Prefabs")]
    [SerializeField] private SpawnedBGPrefab spawnedBGPrefab;

    [SerializeField] private List<Sprite> bgSprites;

    ObjectPool<SpawnedBGPrefab> backgroundPool;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        transform.position = canvasBounds.spawnPoint;
        CreatePool(spawnedBGPrefab);
        StartCoroutine(SpawnPrefabs());
    }

    public override void ActivatePrefab(SpawnedPrefab prefab)
    {
        base.ActivatePrefab(prefab);

        if (prefab is SpawnedBGPrefab bgPrefab)
        {
            float randomX = UnityEngine.Random.Range(spawnOrigin.position.x, spawnOrigin.position.x + randomXFactor);
            float randomY = UnityEngine.Random.Range(spawnOrigin.position.y - randomYFactor, spawnOrigin.position.y + randomYFactor);
            float randomZ = UnityEngine.Random.Range(canvasBounds.minDepthNormalized, canvasBounds.maxDepthNormalized);
            Vector3 newPos = new Vector3(randomX, randomY, randomZ);
            prefab.transform.position = newPos;

            if (bgSprites.Count > 0 )
            {
                Sprite randonSprite = bgSprites[UnityEngine.Random.Range(0, bgSprites.Count)];
                bgPrefab.spriteRenderer.sprite = randonSprite;
            }

        }

    }

    public override IEnumerator SpawnPrefabs()
    {
        while (true)
        {
            if (prefabPool.CountActive < maxSpawns)
            {
                SpawnPrefab();
                yield return new WaitForSeconds(1f / spawnRate);
            }
            yield return null;
         }
    }
}

