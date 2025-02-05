using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class LoopingTileSpawner : MonoBehaviour
{
    [Header("References")]
    public CanvasBounds canvasBounds;

    [Header("Game Objects")]
    public LoopingTiles loopingTile1;
    public LoopingTiles loopingTile2;

    private Vector3 tile1Start;
    private Vector3 tile2Start;

    private void Start()
    {
        loopingTile1.parallaxController.enabled = false;
        loopingTile2.parallaxController.enabled = false;

        tile1Start = new Vector3(canvasBounds.despawnPoint.x,loopingTile1.transform.position.y, loopingTile1.transform.position.z);
        tile2Start = new Vector3(canvasBounds.nearPlaneSpawnPoint.x, loopingTile2.transform.position.y, loopingTile2.transform.position.z);
        loopingTile1.transform.position = tile1Start;
        loopingTile2.transform.position = tile2Start;

        loopingTile1.parallaxController.enabled = true;
        loopingTile2.parallaxController.enabled = true;


        StartCoroutine(LoopTiles());
    }
    public IEnumerator LoopTiles()
    {
        while (true)
        {
            while (loopingTile1.endPosX > canvasBounds.despawnPoint.x + 0.5f)
            {
                yield return null;
            }
            loopingTile1.transform.position = tile2Start;
            loopingTile1.parallaxController.Initialize();

            while (loopingTile2.endPosX > canvasBounds.despawnPoint.x + 0.5f)
            {
                yield return null;
            }
            loopingTile2.transform.position = tile2Start;
            loopingTile2.parallaxController.Initialize();

        }
    }
}
