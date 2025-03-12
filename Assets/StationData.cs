using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class StationData : MonoBehaviour
{
    public BystanderSpawner bystanderSpawner {  get; private set; }
    public AgentSpawner agentSpawner {  get; private set; }

    public ParallaxController parallaxController { get; private set; }
    [Header("Parameters")]
    public float accelerationThresholds;
    public float trainExitSpeed;
    public bool drawDebugLines;

    public float decelThreshold => transform.position.x - accelerationThresholds;
    public float accelThreshold => transform.position.x + accelerationThresholds;

    public DisableBounds disableBounds { get; private set; }

    public List<ExitBounds> exitBoundsList {  get; private set; }
    public List<StateCore> charactersList { get; set; } = new List<StateCore>();

    [System.Serializable]
    public struct SpawnArea
    {
        [HideInInspector] public float min;
        [HideInInspector] public float max;
        public int bystanderCount;
        public int agentCount;

        public SpawnArea(float min, float max, int npcCount, int agentCount)
        {
            this.min = min;
            this.max = max;
            this.bystanderCount = npcCount;
            this.agentCount = agentCount;
        }
    }

    [SerializeField]
    public SpawnArea[] spawnAreas;

    private InsideBounds[] trainDataInsideBounds;


    public int bystanderSpawnCount { get; private set; }
    public int agentSpawnCount { get; private set; }
    private void OnDrawGizmos()
    {
        if (drawDebugLines)
        {
            DrawAccelThresholds(true);
            DrawSpawnAreas();
        }
    }

    private void Start()
    {
        exitBoundsList = new List<ExitBounds>(GetComponentsInChildren<ExitBounds>());
        disableBounds = GetComponentInChildren<DisableBounds>();
        DrawAccelThresholds(false);

        bystanderSpawner = GetComponentInChildren<BystanderSpawner>();
        agentSpawner = GetComponentInChildren<AgentSpawner>();
        parallaxController = GetComponent<ParallaxController>();

        CountBystandersToSpawn();
    }

    private void CountBystandersToSpawn()
    {
        foreach (StationData.SpawnArea spawnArea in spawnAreas)
        {
            bystanderSpawnCount += spawnArea.bystanderCount;
            agentSpawnCount += spawnArea.agentCount;
        }
    }
    private void DrawAccelThresholds(bool usingGizmos)
    {
#if UNITY_EDITOR
        float decelX = transform.position.x - accelerationThresholds;
        float accelX = transform.position.x + accelerationThresholds;
        float height = 10;
        Vector2 decelUpperOrigin = new Vector2(decelX , transform.position.y + height);
        Vector2 decelLowerOrigin = new Vector2(decelX , transform.position.y);

        Vector2 accelUpperOrigin = new Vector2(accelX, transform.position.y + height);
        Vector2 accelLowerOrigin = new Vector2(accelX, transform.position.y);

        if (usingGizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(decelUpperOrigin, decelLowerOrigin);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine (accelUpperOrigin, accelLowerOrigin);
        }
        else
        {
            Debug.DrawLine(decelUpperOrigin , decelLowerOrigin, Color.red);
            Debug.DrawLine(accelLowerOrigin , accelUpperOrigin, Color.blue);
        }
#endif
    }
    public void SetSpawnAreas()
    {
        GetInsideBounds();
        if (spawnAreas == null)
        {
            spawnAreas = new SpawnArea[trainDataInsideBounds.Length];
        }
        Transform trainTransform = FindFirstObjectByType<TrainController>().transform;

        float trainWorldDistanceToStation = transform.position.x - trainTransform.position.x;
        float trainHalfLength = (trainDataInsideBounds[0].gameObject.GetComponent<BoxCollider2D>().bounds.max.x - trainDataInsideBounds[^1].gameObject.GetComponent<BoxCollider2D>().bounds.min.x) / 2;

        for(int i = 0; i < spawnAreas.Length; i++)
        {
            BoxCollider2D insideBoundsCollider = trainDataInsideBounds[i].gameObject.GetComponent<BoxCollider2D>();
            float start = insideBoundsCollider.bounds.min.x + trainWorldDistanceToStation + trainHalfLength - 25;
            float end = insideBoundsCollider.bounds.max.x + trainWorldDistanceToStation + trainHalfLength - 25;

            spawnAreas[i] = new SpawnArea(start, end, spawnAreas[i].bystanderCount, spawnAreas[i].agentCount);
        }
    }
    private void GetInsideBounds()
    {
        trainDataInsideBounds = FindObjectsByType<InsideBounds>(FindObjectsSortMode.InstanceID);
    }
    private void DrawSpawnAreas()
    {
        if (spawnAreas.Length == 0) return;
        Gizmos.color = Color.magenta;

        foreach (SpawnArea spawnArea in spawnAreas)
        {
            Vector2 bottomLeft = new Vector2(spawnArea.min, 0);
            Vector2 bottomRight = new Vector2(spawnArea.max, 0);
            Vector2 topLeft = new Vector2(spawnArea.min, 10);
            Vector2 topRight = new Vector2(spawnArea.max, 10);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}
