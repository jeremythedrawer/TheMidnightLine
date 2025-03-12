using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BystanderSpawner : NPCSpawner
{
    private int trainGroundLayer => GlobalReferenceManager.Instance.trainGroundLayer;
    private int stationGroundLayer => GlobalReferenceManager.Instance.stationGroundLayer;

    private int bystandersToGet;
    private StationData station;
    private BystanderPool pool;
    private TrainData trainData;

    private List<NPCCore> bystanders = new List<NPCCore>();

    private void Start()
    {
        station = transform.parent.transform.parent.GetComponent<StationData>();
        pool = GlobalReferenceManager.Instance.bystanderPool;
        trainData = GlobalReferenceManager.Instance.trainData;

        foreach (StationData.SpawnArea spawnArea in station.spawnAreas)
        {
            bystandersToGet += spawnArea.bystanderCount;
        }
    }

    private void Update()
    {
        if ((trainData.metersTravelled > station.parallaxController.spawnPosition || station == GlobalReferenceManager.Instance.stations[0]) && bystanders.Count != bystandersToGet && pool.bystanders.Count > 0)
        {
            ActivateBystanders();
        }
    }

    private void ActivateBystanders()
    {
        List<NPCCore> pooledBystanders = pool.bystanders.Where(bystander => bystander.transform.parent == pool.transform).Take(bystandersToGet).ToList();

        for(int i = 0; i < bystandersToGet; i++)
        {
            NPCCore bystander = pooledBystanders[i];
            if (bystander.transform.parent == pool.transform)
            {
                bystander.transform.SetParent(transform);

                bystander.spriteRenderer.sortingOrder = 1;

                bystander.boxCollider2D.excludeLayers |= 1 << trainGroundLayer;
                bystander.boxCollider2D.excludeLayers &= ~(1 << stationGroundLayer);

                bystander.collisionChecker.activeGroundLayer = 1 << stationGroundLayer;


                float xPos = SpawnAreaXPosition();

                bystander.transform.position = new Vector2(xPos, 2);

                BystanderPool.CharacterVisuals characterVisuals = RandomCharacterVisuals();
                bystander.material = characterVisuals.material;
                bystander.animatorOverride = characterVisuals.overrideController;

                bystander.startingStation = station;

                bystander.gameObject.SetActive(true);

                bystanders.Add(bystander);
                station.charactersList.Add(bystander);
            }
        }
    }

    private float SpawnAreaXPosition()
    {
        float xPos = 0;

        for (int i = 0; i < station.spawnAreas.Length; i++)
        {
            ref StationData.SpawnArea spawnArea = ref station.spawnAreas[i];
            if (spawnArea.bystanderCount > 0)
            {
                xPos = UnityEngine.Random.Range(spawnArea.min, spawnArea.max);
                spawnArea.bystanderCount--;
                break;
            }
        }
        return xPos;
    }

    private BystanderPool.CharacterVisuals RandomCharacterVisuals()
    {
        int randomIndex = Random.Range(0, pool.characterVisualsList.Count - 1);
        return pool.characterVisualsList[randomIndex];
    }
}
