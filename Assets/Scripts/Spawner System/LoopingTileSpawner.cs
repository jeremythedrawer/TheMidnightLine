using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopingTileSpawner : Spawner
{
    [Header("Game Objects")]
    public List<LoopingTiles> loopingTiles = new List<LoopingTiles>();

    private void OnValidate()
    {
        SetLodParams();
    }
    private void OnDrawGizmosSelected()
    {
        DrawLodRange();
    }

    private void Start()
    {
        SetLodParams();

        foreach (LoopingTiles loopingtile in loopingTiles)
        {
            loopingtile.parallaxController.enabled = false;
            loopingtile.transform.position = transform.position;
            StartCoroutine(LoopTiles(loopingtile));
        }
    }
    public IEnumerator LoopTiles(LoopingTiles loopingTile)
    {
        int index = loopingTiles.IndexOf(loopingTile);
        float frameBuffer = 0.85f;

        //initialize first index straight away
        if (index == 0)
        {
            loopingTile.parallaxController.enabled = true;
        }

        
        while (true)
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
            }
            loopingTile.parallaxController.Initialize();
            yield return new WaitUntil(() => loopingTile.endPosX < despawnPos.x + frameBuffer); //waiting to despawn
            loopingTile.transform.position = transform.position;
            loopingTile.parallaxController.Initialize();
        }
    }
}
