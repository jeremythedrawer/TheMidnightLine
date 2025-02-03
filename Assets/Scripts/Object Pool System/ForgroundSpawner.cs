using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class ForgroundSpawner : Spawner
{
    public static ForgroundSpawner Instance { get; private set; }

    [Header("Pool Prefabs")]
    [SerializeField] private SpawnedFGPrefab spawnedFgPrefab;

    private SpawnedFGPrefab lastFgPrefab;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        transform.position = canvasBounds.nearPlaneSpawnPoint;
        CreatePool(spawnedFgPrefab);
        StartCoroutine(SpawnPrefabs());
    }

    public override void ActivatePrefab(SpawnedPrefab prefab)
    {
        base.ActivatePrefab(prefab);
        if (prefab is SpawnedFGPrefab fgPrefab)
        {
            prefab.transform.position = spawnOrigin.position;
            lastFgPrefab = fgPrefab;
        }
    }
    public override IEnumerator SpawnPrefabs()
    {
        while (true)
        {
            if (prefabPool.CountActive < maxSpawns)
            {
                SpawnPrefab();
                yield return new WaitUntil(() => lastFgPrefab.transform.position.x < canvasBounds.despawnPoint.x);
            }
            yield return null;
        }
    }
}
