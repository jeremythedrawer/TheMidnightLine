using System.Collections;
using UnityEngine;
using UnityEngine.Pool;


public class Spawner : MonoBehaviour
{
    [Header("References")]
    public CanvasBounds canvasBounds;
    public TrainController trainController;

    [Header("Parameters")]
    [Tooltip("Spawns per second")]
    public int spawnRate = 10;
    public int maxSpawns = 50;
    public float randomXFactor = 15f;
    public float randomYFactor = 0.2f;

    public Color spawnRangeColor = Color.red;
    [Tooltip("In meters")]
    public float startSpawnDistance = 0;
    [Tooltip("In meters")]
    public float endSpawnDistance = 0;

    [Range(0f, 1f)]
    public float minDepth = 0;
    [Range(0f, 1f)]
    public float maxDepth = 1;


    // For BackgroundSpawner and LoopingTileSpawner
    public float oneThirdPlane { get; private set; }
    public float oneHalfPlane { get; private set; }
    public float twoThirdsPlane { get; private set; }

    public float minXPos { get; private set; }
    public float maxXPos { get; private set; }

    public Vector3 spawnPos { get; private set; }
    public Vector3 despawnPos { get; private set; }

    protected void SetLodParams()
    {
        if (canvasBounds == null) return;
        minDepth = Mathf.Clamp(minDepth, 0f, maxDepth);
        maxDepth = Mathf.Clamp(maxDepth, minDepth, 1f);

        minXPos = Mathf.Lerp(canvasBounds.nearClipPlanePos, canvasBounds.farClipPlanePos, minDepth);
        maxXPos = Mathf.Lerp(canvasBounds.nearClipPlanePos, canvasBounds.farClipPlanePos, maxDepth);


        oneThirdPlane = minXPos + ((maxXPos - minXPos) * 0.333f);
        oneHalfPlane = minXPos + ((maxXPos - minXPos) * 0.5f);
        twoThirdsPlane = minXPos + ((maxXPos - minXPos) * 0.667f);

        spawnPos = new Vector3(canvasBounds.right, transform.position.y, canvasBounds.nearClipPlanePos);
        despawnPos = new Vector3(canvasBounds.left, transform.position.y, canvasBounds.nearClipPlanePos);

        transform.position = new Vector3(spawnPos.x, transform.position.y, oneHalfPlane);
    }

    protected void DrawLodRange()
    {
        Gizmos.color = Color.red;
        //length
        Gizmos.DrawLine(new Vector3(spawnPos.x, spawnPos.y, minXPos), new Vector3(despawnPos.x, despawnPos.y, minXPos));
        Gizmos.DrawLine(new Vector3(spawnPos.x, spawnPos.y, maxXPos), new Vector3(despawnPos.x, despawnPos.y, maxXPos));

        Gizmos.DrawLine(new Vector3(spawnPos.x, spawnPos.y, oneThirdPlane), new Vector3(despawnPos.x, despawnPos.y, oneThirdPlane));
        Gizmos.DrawLine(new Vector3(spawnPos.x, spawnPos.y, twoThirdsPlane), new Vector3(despawnPos.x, despawnPos.y, twoThirdsPlane));

        //depth
        Gizmos.DrawLine(new Vector3(spawnPos.x, spawnPos.y, minXPos), new Vector3(spawnPos.x, spawnPos.y, maxXPos));
        Gizmos.DrawLine(new Vector3(despawnPos.x, despawnPos.y, minXPos), new Vector3(despawnPos.x, despawnPos.y, maxXPos));
    }





}
