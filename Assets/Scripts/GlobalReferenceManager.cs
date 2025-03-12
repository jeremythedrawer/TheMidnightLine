using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GlobalReferenceManager : MonoBehaviour
{
    public static GlobalReferenceManager Instance { get; private set; }
    public TrainController trainController {  get; private set; }
    public TrainData trainData { get; private set; }
    public CanvasBounds canvasBounds { get; private set; }
    public Camera mainCam { get; private set; }
    public BystanderPool bystanderPool { get; private set; }
    public AgentPool agentPool { get; private set; }
    public List<StationData> stations { get; private set; } = new List<StationData>();

    public int stationGroundLayer { get; private set; }
    public int trainGroundLayer { get; private set; }
    public int exitGroundLayer { get; private set; }

    public int totalBystanders { get; private set; }
    public int totalAgents { get; private set; }

    private void OnValidate() => CacheReferences();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CacheReferences();
    }

    private void Start()
    {
        CountNpcs();
    }
    private void CacheReferences()
    {
        InstantiateManager();
        if (trainController == null) trainController = FindFirstObjectByType<TrainController>();
        if (trainData == null) trainData = FindFirstObjectByType<TrainData>();
        if (canvasBounds == null) canvasBounds = FindFirstObjectByType<CanvasBounds>();
        if (mainCam == null) mainCam = Camera.main;
        if (bystanderPool == null) bystanderPool = FindFirstObjectByType<BystanderPool>();
        if (agentPool == null) agentPool = FindFirstObjectByType<AgentPool>();
        SetStationDataList();
        stationGroundLayer = LayerMask.NameToLayer("Station Ground");
        trainGroundLayer = LayerMask.NameToLayer("Train Ground");
        exitGroundLayer = LayerMask.NameToLayer("Exit Ground");
    }

    private void InstantiateManager()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (Instance == null) Instance = this;
    }

    private void SetStationDataList()
    {
        StationData[] stationsArray = FindObjectsByType<StationData>(FindObjectsSortMode.None);
        stations.AddRange(stationsArray);
        stations = stationsArray.OrderBy(station => station.GetComponent<ParallaxController>().spawnPosition).ToList();
    }

    private void CountNpcs()
    {
        foreach (StationData station in stations)
        {
            totalBystanders += station.bystanderSpawnCount;
            totalAgents += station.agentSpawnCount;
        }
    }
}
