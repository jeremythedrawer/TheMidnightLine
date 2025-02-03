using System.Collections;
using UnityEngine;
using UnityEngine.Pool;


public class Spawner : MonoBehaviour
{
    [Header("References")]
    public CanvasBounds canvasBounds;
    public Transform spawnOrigin;

    [Header("Parameters")]
    [Tooltip("Spawns per second")]
    public int spawnRate = 10;
    public int maxSpawns = 50;
    public float randomXFactor = 15f;
    public float randomYFactor = 0.2f;

    public ObjectPool<SpawnedPrefab> prefabPool {  get; private set; }

    protected void CreatePool(SpawnedPrefab prefab)
    {
        prefabPool = new ObjectPool<SpawnedPrefab>(
            () => CreatepPrefab(prefab),
            ActivatePrefab,
            DeactivateBuilding,
            DestroyBuilding,
            false,
            0, maxSpawns);
    }

    private SpawnedPrefab CreatepPrefab(SpawnedPrefab prefab)
    {
        return Instantiate(prefab, spawnOrigin);
    }

    public virtual void ActivatePrefab(SpawnedPrefab prefab)
    {
        prefab.gameObject.SetActive(true);
    }

    private void DeactivateBuilding(SpawnedPrefab prefab)
    {
        prefab.gameObject.SetActive(false);
    }

    private void DestroyBuilding(SpawnedPrefab prefab)
    {
        Destroy(prefab.gameObject);
    }

    public virtual IEnumerator SpawnPrefabs()
    {
        yield return null;
    }
    protected void SpawnPrefab()
    {
        SpawnedPrefab newPrefab = prefabPool.Get();
        newPrefab.Initialize();
    }
}
