using System.Collections;
using UnityEngine;

public class BackgroundSpawner : Spawner
{
    public static BackgroundSpawner Instance { get; private set; }

    [Header("Pool Prefab")]
    [SerializeField] private SpawnedBGPrefab spawnedBgPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public override void Start()
    {
        base.Start();
        CreatePool(spawnedBgPrefab);
        StartCoroutine(SpawnPrefabs());
    }

    public override void ActivatePrefab(SpawnedPrefab prefab)
    {
        base.ActivatePrefab(prefab);

        if (prefab is SpawnedBGPrefab bgPrefab)
        {
            float randomX = UnityEngine.Random.Range(transform.position.x, transform.position.x + randomXFactor);
            float randomY = UnityEngine.Random.Range(transform.position.y - randomYFactor, transform.position.y + randomYFactor);
            float randomZ = UnityEngine.Random.Range(minXPos, maxXPos);

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

