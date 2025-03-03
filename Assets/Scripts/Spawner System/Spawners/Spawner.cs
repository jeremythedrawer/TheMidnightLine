using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;


public class Spawner : MonoBehaviour
{
    protected CanvasBounds canvasBounds => GlobalReferenceManager.Instance.canvasBounds;
    protected TrainData trainData => GlobalReferenceManager.Instance.trainData;

    [Header("Parameters")]
    [Range(0f, 1f)]
    public float minDepth = 0;
    [Range(0f, 1f)]
    public float maxDepth = 1;
    [Tooltip("In meters")]
    public float startSpawnDistance = 0;
    [Tooltip("In meters")]
    public float endSpawnDistance = 0;

    // For BackgroundSpawner and LoopingTileSpawner
    [Range (1f, 3f)]
    public int lodSpriteCount = 1;
    public float[] lodThresholdValues {  get; private set; }
    private int maxIndex;

    public float minZPos { get; private set; }
    public float maxZPos { get; private set; }

    public Vector3 spawnPos { get; private set; }
    public Vector3 despawnPos { get; private set; }

    private void Awake()
    {
        maxIndex = Mathf.Max(lodSpriteCount - 1, 0);
        lodThresholdValues = new float[maxIndex];
    }
    public virtual void OnValidate()
    {
        maxIndex = Mathf.Max(lodSpriteCount - 1, 0);
        if(lodThresholdValues == null || lodThresholdValues.Length != maxIndex)
        {
            lodThresholdValues = new float[maxIndex];
        }
    }

    public void SetSpawnerPos()
    {
        if (GlobalReferenceManager.Instance == null) return;

        canvasBounds.SetCanvasData();
        minDepth = Mathf.Clamp(minDepth, 0f, maxDepth);
        maxDepth = Mathf.Clamp(maxDepth, minDepth, 1f);

        minZPos = Mathf.Lerp(canvasBounds.nearClipPlanePos, canvasBounds.farClipPlanePos, minDepth);
        maxZPos = Mathf.Lerp(canvasBounds.nearClipPlanePos, canvasBounds.farClipPlanePos, maxDepth);
        lodSpriteCount = Mathf.Max(1, lodSpriteCount);

        for (int i = 0; i < lodThresholdValues.Length; i++)
        {
            float decimalThreshold = (1f / lodSpriteCount) * (i + 1);
            lodThresholdValues[i] = Mathf.Lerp(minZPos, maxZPos, decimalThreshold);
        }

        spawnPos = new Vector3(canvasBounds.right + 1, transform.position.y, transform.position.z);
        despawnPos = new Vector3(canvasBounds.left, transform.position.y, transform.position.z);
        transform.position = spawnPos;
    }

    protected void DrawLodRange()
    {
        Gizmos.color = Color.red;
        //length
        Gizmos.DrawLine(new Vector3(spawnPos.x, spawnPos.y, minZPos), new Vector3(despawnPos.x, despawnPos.y, minZPos));
        Gizmos.DrawLine(new Vector3(spawnPos.x, spawnPos.y, maxZPos), new Vector3(despawnPos.x, despawnPos.y, maxZPos));

        for (int i = 0; i < maxIndex; i++)
        {
            Gizmos.DrawLine(new Vector3(spawnPos.x, spawnPos.y, lodThresholdValues[i]), new Vector3(despawnPos.x, despawnPos.y, lodThresholdValues[i]));
        }

        //depth
        Gizmos.DrawLine(new Vector3(spawnPos.x, spawnPos.y, minZPos), new Vector3(spawnPos.x, spawnPos.y, maxZPos));
        Gizmos.DrawLine(new Vector3(despawnPos.x, despawnPos.y, minZPos), new Vector3(despawnPos.x, despawnPos.y, maxZPos));
    }
}
