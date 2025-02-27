using UnityEngine;

public class GlobalReferenceManager : MonoBehaviour
{
    public static GlobalReferenceManager Instance { get; private set; }
    public TrainController trainController {  get; private set; }
    public TrainData trainData { get; private set; }
    public CanvasBounds canvasBounds { get; private set; }
    public Camera mainCam { get; private set; }

    public int stationGroundLayer { get; private set; }
    public int trainGroundLayer { get; private set; }

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
    private void OnValidate() => CacheReferences();

    private void CacheReferences()
    {
        InstantiateManager();
        if (trainController == null) trainController = FindFirstObjectByType<TrainController>();
        if (trainData == null) trainData = FindFirstObjectByType<TrainData>();
        if (canvasBounds == null) canvasBounds = FindFirstObjectByType<CanvasBounds>();
        if (mainCam == null) mainCam = Camera.main;

        stationGroundLayer = LayerMask.NameToLayer("Station Ground");
        trainGroundLayer = LayerMask.NameToLayer("Train Ground");
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
}
