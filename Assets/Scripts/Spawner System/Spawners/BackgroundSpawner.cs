using UnityEditor;
using UnityEngine;

public class BackgroundSpawner : ObjectPoolSpawner
{
    [Header("Pool Prefab")]
    [SerializeField] private SpawnedBGPrefab spawnedBgPrefab;

    [Header("Parameters")]
    [Range(0, 50)]

    private float randomX;
    private float randomY;
    private float randomZ;

    public override void OnValidate()
    {
        base.OnValidate();
    }
    private void OnDrawGizmosSelected()
    {
        DrawRandomXFactor();
        DrawLodRange();
    }

    public override void Start()
    {
        base.Start();
        CreatePool(spawnedBgPrefab);
    }

    public override void ActivatePrefab(SpawnedPrefab prefab)
    {
        base.ActivatePrefab(prefab);

        if (prefab is SpawnedBGPrefab bgPrefab)
        {
            randomX = Random.Range(transform.position.x, transform.position.x + randomXFactor);
            randomY = Random.Range(transform.position.y - randomYFactor, transform.position.y + randomYFactor);
            randomZ = Random.Range(minZPos, maxZPos);

            bgPrefab.transform.position = new Vector3(randomX, randomY, randomZ);

            if (bgPrefab.lodSprites.Count > 0)
            {
                SpawnedBGPrefab.LodSprites randomLodSprites = bgPrefab.lodSprites[UnityEngine.Random.Range(0, bgPrefab.lodSprites.Count)];
                bgPrefab.chosenLods = randomLodSprites;
            }

        }

    }
    
    public void IncreasePresetPrefabs()
    {
        float canvasMin = canvasBounds.left;
        float canvasMax = canvasBounds.right;

        randomX = Random.Range(canvasMin, canvasMax);
        randomY = Random.Range(transform.position.y - randomYFactor, transform.position.y + randomYFactor);
        randomZ = Random.Range(minZPos, maxZPos);
        Vector3 spawnPos = new Vector3(randomX, transform.position.y, randomZ);

        SpawnedBGPrefab prefab = Instantiate(spawnedBgPrefab, spawnPos, Quaternion.identity);
        prefab.transform.SetParent(transform);
        SpawnedBGPrefab.LodSprites randomLodSprites = prefab.lodSprites[UnityEngine.Random.Range(0, prefab.lodSprites.Count)];
        prefab.chosenLods = randomLodSprites;
        prefab.Initialize();
    }
    
    public void DecreasePresetPrefabs()
    {
        DestroyImmediate(transform.GetChild(transform.childCount - 1).gameObject);
    }

    private void DrawRandomXFactor()
    {
        Gizmos.color = Color.red;
        Vector3 point1 = new Vector3(spawnPos.x, spawnPos.y, minZPos);
        Vector3 point2 = new Vector3(spawnPos.x + randomXFactor, spawnPos.y, minZPos);
        Vector3 point3 = new Vector3(spawnPos.x + randomXFactor, spawnPos.y, maxZPos);
        Vector3 point4 = new Vector3(spawnPos.x, spawnPos.y, maxZPos);
        Gizmos.DrawLine(point1, point2);
        Gizmos.DrawLine(point2, point3);
        Gizmos.DrawLine(point3, point4);
    }
}

