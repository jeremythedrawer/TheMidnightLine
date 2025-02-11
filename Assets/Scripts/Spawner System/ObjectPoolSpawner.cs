using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolSpawner : Spawner
{
    public ObjectPool<SpawnedPrefab> prefabPool { get; private set; }

    public virtual void Start()
    {
        StartCoroutine(SpawnPrefabs());
    }
    protected void CreatePool(SpawnedPrefab prefab)
    {
        prefabPool = new ObjectPool<SpawnedPrefab>(
            () => CreatePrefab(prefab),
            ActivatePrefab,
            DeactivatePrefab,
            DestroyPrefab,
            false,
            maxSpawns,
            maxSpawns);

        for (int i = 0; i < maxSpawns; i++)
        {
            prefabPool.Release(CreatePrefab(prefab));
        }
    }

    private SpawnedPrefab CreatePrefab(SpawnedPrefab prefab)
    {
        return Instantiate(prefab, transform);
    }

    public virtual void ActivatePrefab(SpawnedPrefab prefab)
    {
        prefab.gameObject.SetActive(true);
    }

    private void DeactivatePrefab(SpawnedPrefab prefab)
    {
        prefab.gameObject.SetActive(false);
    }

    private void DestroyPrefab(SpawnedPrefab prefab)
    {
        Debug.Log(maxSpawns);
        Destroy(prefab.gameObject);
    }

    private IEnumerator SpawnPrefabs()
    {
        while (true)
        {
            float travelled = trainController.metersTravelled;

            if (travelled >= startSpawnDistance && travelled <= endSpawnDistance)
            {
                if (prefabPool.CountActive < maxSpawns)
                {
                    SpawnPrefab();
                }
            }

            yield return new WaitForSeconds(1f / spawnRate);
        }
    }
    protected void SpawnPrefab()
    {
        SpawnedPrefab newPrefab = prefabPool.Get();

        newPrefab.Initialize();
    }
}
