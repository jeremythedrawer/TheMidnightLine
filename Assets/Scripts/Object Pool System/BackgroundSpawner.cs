using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEditor.PlayerSettings;

public class BackgroundSpawner : Spawner
{
    public static BackgroundSpawner Instance { get; private set; }

    [Header("Pool Prefabs")]
    [SerializeField] private SpawnedBGPrefab spawnedBGPrefab;

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

            bgPrefab.transform.position = new Vector3(randomX, randomY, randomZ);
            if (bgPrefab.lodSprites.Count > 0)
            {
                SpawnedBGPrefab.LodSprites randomLodSprites = bgPrefab.lodSprites[UnityEngine.Random.Range(0, bgPrefab.lodSprites.Count)];
                bgPrefab.chosenLods = randomLodSprites;
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

