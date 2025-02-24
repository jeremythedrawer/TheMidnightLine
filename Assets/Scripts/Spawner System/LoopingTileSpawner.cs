using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopingTileSpawner : Spawner
{
    [Header("Game Objects")]
    public List<LoopingTiles> loopingTiles = new List<LoopingTiles>();

    private bool inSpawnRange = false;

    private void OnValidate()
    {
        SetLodParams();
    }
    private void OnDrawGizmosSelected()
    {
        DrawLodRange();
    }
    private void Awake()
    {
        
    }
    private void Start()
    {
        SetLodParams();
        foreach (LoopingTiles loopingTile in loopingTiles)
        {
            StartCoroutine(LoopTiles(loopingTile));
        }
    }

    private void Update()
    {
        float travelled = trainData.metersTravelled;
        inSpawnRange = travelled >= startSpawnDistance && travelled <= endSpawnDistance;
        
    }
    public IEnumerator LoopTiles(LoopingTiles loopingTile)
    {
        loopingTile.transform.position = transform.position;

        //initialize first index straight away
        yield return new WaitUntil(() => loopingTile.parallaxController != null);

        loopingTile.parallaxController.enabled = false;
        yield return new WaitUntil(() => inSpawnRange);

        int index = loopingTiles.IndexOf(loopingTile);
        float frameBuffer = 0.85f;

        if (index == 0)
        {
            loopingTile.parallaxController.enabled = true;
        }

        while (inSpawnRange)
        {
            if (index != 0)
            {
                while (loopingTiles[index - 1].endPosX == 0)
                {
                    yield return null;
                }

                yield return new WaitUntil(() => loopingTiles[index - 1].endPosX < transform.position.x + frameBuffer);
                if (loopingTile.parallaxController.enabled == false)
                {
                    loopingTile.parallaxController.enabled = true;
                }
            }
            else //index zero needs to follow the last index instead of the next
            {
                yield return new WaitUntil(() => loopingTiles[loopingTiles.Count - 1].endPosX < transform.position.x);
                if (loopingTile.parallaxController.enabled == false)
                {
                    loopingTile.parallaxController.enabled = true;
                }
            }
            loopingTile.parallaxController.Initialize();
            yield return new WaitUntil(() => loopingTile.endPosX < despawnPos.x + frameBuffer); //waiting to despawn
            loopingTile.transform.position = transform.position;
            loopingTile.parallaxController.Initialize();
            loopingTile.parallaxController.enabled = false;
        }
    }
}
