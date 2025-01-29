using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public class BackgroundSpawner : MonoBehaviour
{
    public static BackgroundSpawner Instance { get; private set; }

    [Header("References")]
    public CanvasBounds canvasBounds;
    public Transform spawnOrigin;

    [Header("Parameters")]
    [Tooltip("Spawns per second")]
    public int spawnRate = 10;
    public int maxSpawns = 50;
    public float randomXFactor = 15f;
    public float randomYFactor = 0.2f;


    [Serializable]
    public class BuildingPrefab
    {
        public string buildingName;
        public SpawnedBuilding building;
    }
    [Header ("Pool Prefabs")]
    [SerializeField] private List<BuildingPrefab> buildingPrefabs;

    public Dictionary<string, ObjectPool<SpawnedBuilding>> buildingPools = new Dictionary<string, ObjectPool<SpawnedBuilding>>();


    private void OnValidate()
    {
        foreach (BuildingPrefab buildingPrefab in buildingPrefabs)
        {
            if (buildingPrefab.building != null && string.IsNullOrEmpty(buildingPrefab.buildingName))
            {
                buildingPrefab.buildingName = buildingPrefab.building.name;
            }

        }
    }

    private void Awake()
    {
        Instance = this;
    }


    private void Start()
    {
        transform.position = canvasBounds.spawnPoint;
        foreach (BuildingPrefab buildingPrefab in buildingPrefabs)
        {
            StartCoroutine(SpawnBuildings(buildingPrefab.buildingName));
            CreatePools(buildingPrefab);
        }
    }
    private void CreatePools(BuildingPrefab buildingPrefab)
    {
        buildingPools[buildingPrefab.buildingName] = new ObjectPool<SpawnedBuilding>(
            () => CreateBuilding(buildingPrefab.building),
            ActivateBuilding,
            DeactivateBuilding,
            DestroyBuilding,
            false,
            0, maxSpawns);
    }

    private SpawnedBuilding CreateBuilding(SpawnedBuilding buildingPrefab)
    {
        return Instantiate(buildingPrefab, spawnOrigin);
    }

    private void ActivateBuilding(SpawnedBuilding buildingPrefab)
    {
        buildingPrefab.gameObject.SetActive(true);

        float randomX = UnityEngine.Random.Range(spawnOrigin.position.x, spawnOrigin.position.x + randomXFactor);
        float randomY = UnityEngine.Random.Range(spawnOrigin.position.y - randomYFactor, spawnOrigin.position.y + randomYFactor);
        float randomZ = UnityEngine.Random.Range(canvasBounds.minDepthNormalized, canvasBounds.maxDepthNormalized);
        Vector3 newPos = new Vector3(randomX, randomY, randomZ);
        buildingPrefab.transform.position = newPos;
    }

    private void DeactivateBuilding(SpawnedBuilding buildingPrefab)
    {
        buildingPrefab.gameObject.SetActive(false);
    }

    private void DestroyBuilding(SpawnedBuilding buildingPrefab)
    {
        Destroy(buildingPrefab.gameObject);
    }

    private IEnumerator SpawnBuildings(string buildingName)
    {
        while (true)
        {
            if (buildingPools.ContainsKey(buildingName))
            {
                if (buildingPools[buildingName].CountActive < maxSpawns)
                {
                    SpawnBuilding(buildingName);
                    yield return new WaitForSeconds(1/spawnRate);
                }

            }
                yield return null;
        }
    }
    private void SpawnBuilding(string buildingName)
    {
        if (buildingPools.TryGetValue(buildingName, out var pool))
        {
            SpawnedBuilding newBuilding = pool.Get();
            newBuilding.Initialize();
        }
    }
}
