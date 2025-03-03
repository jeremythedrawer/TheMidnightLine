using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopingTileSpawner : Spawner
{
    private List<LoopingTiles> loopingTiles = new List<LoopingTiles>();

    public override void OnValidate()
    {
        base.OnValidate();
        SetSpawnerPos();
    }
    private void OnDrawGizmosSelected()
    {
        DrawLodRange();
    }

    private void Start()
    {
        SetLoopingTilesList();
        LoopTiles();
    }

    private void LoopTiles()
    {
        foreach (LoopingTiles tile in loopingTiles)
        {
            StartCoroutine(LoopingTiles(tile));
        }
    }
    private IEnumerator LoopingTiles(LoopingTiles tile)
    {
        while (true)
        {
            int index = loopingTiles.IndexOf(tile);
            int lastIndex = loopingTiles.Count - 1;
            int prevIndex = index - 1;
            int indexToCheck = index == 0 ? lastIndex : prevIndex;
            yield return new WaitUntil(() => tile.spriteBoundsMaxX < canvasBounds.left);
            tile.transform.position = new Vector3(loopingTiles[indexToCheck].spriteBoundsMaxX, transform.position.y, transform.position.z);
            tile.parallaxController.Initialize();
        }

    }

    public void PresetTilePositions()
    {
        SetSpawnerPos();
        SetLoopingTilesList();
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
