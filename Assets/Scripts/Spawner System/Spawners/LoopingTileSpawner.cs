using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopingTileSpawner : Spawner
{
    private List<LoopingTiles> loopingTiles = new List<LoopingTiles>();

    private void OnDrawGizmosSelected()
    {
        DrawLodRange();
    }


    public void PresetTilePositions()
    {
        SetLodParams();
        if (startSpawnDistance == 0)
        {
            for(int i = 0; i < loopingTiles.Count; i++)
            {
                if (i == 0)
                {
                    loopingTiles[i].transform.position = new Vector3(canvasBounds.left, transform.position.y, transform.position.z);
                }
                else
                {
                    LoopingTiles prevTile = loopingTiles[i - 1];
                    loopingTiles[i].transform.position = new Vector3(prevTile.spriteRenderer.bounds.max.x, transform.position.y, transform.position.z);
                }
            }
        }
        else
        {
            for (int i = 0; i < loopingTiles.Count; i++)
            {
                loopingTiles[i].transform.position = transform.position;
            }
        }
    }
    private void SetLoopingTilesList()
    {
        loopingTiles.Clear(); // Clear the list before adding new tiles

        // Get all LoopingTiles children and add them to the list
        LoopingTiles[] allTiles = GetComponentsInChildren<LoopingTiles>();
        loopingTiles.AddRange(allTiles);
    }
}
