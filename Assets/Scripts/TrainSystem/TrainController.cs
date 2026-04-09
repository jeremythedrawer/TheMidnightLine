using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static Train;
public class TrainController : MonoBehaviour
{
    public TrainSettingsSO settings;
    public TrainStatsSO stats;
    public TripSO trip;
    public ZoneSpawnerSO spawner;
    public GameEventDataSO gameEventData;
    public AtlasRenderer backSprite;
    public AtlasRenderer driversPit;
    [Header("Generated")]
    public Carriage[] carriages;
    CancellationTokenSource trainCTS;
    public Station[] stations;
    public Station nextStation;
    public TrainStates curState;
    public bool closingSlideDoors;
    public int curStationIndex;
    private void OnValidate()
    {
        SetCarriageDictionary();
        SetBounds();

    }
    private void Awake() 
    {
        stats.totalBounds = driversPit.bounds;
        stats.totalBounds.Encapsulate(backSprite.bounds);

        Carriage carriage = carriages[0];
        AtlasRenderer grapPoleRenderer = carriage.grapPoleRenderers[0];
        StationSO stationData = trip.stationsDataArray[0];

        stats.targetPassengersBoarding = stationData.bystanderProfiles.Length + stationData.traitorProfiles.Length + 1; // +1 for spy himself
        stats.curVelocity = KMPHToVelocity(stationData.targetKMPH);
        stats.targetVelocity = KMPHToVelocity(stationData.targetKMPH);
        trip.ticketsCheckedSinceStart = 0;
        
        stats.curPassengersBoarded = 0;

        stats.depthSections.min = carriage.exteriorRenderers[0].batchKey.depthOrder;
        stats.depthSections.max = carriage.interiorSlideDoors[0].rightSlideDoorRenderer.batchKey.depthOrder;
        stats.depthSections.frontMin = grapPoleRenderer.batchKey.depthOrder - 2;
        stats.depthSections.frontMax = grapPoleRenderer.batchKey.depthOrder - 1;
        stats.depthSections.backMin = grapPoleRenderer.batchKey.depthOrder + 1;
        stats.depthSections.backMax = grapPoleRenderer.batchKey.depthOrder + 2;
        stats.depthSections.carriageSeat = carriage.seatRenderers[0].batchKey.depthOrder - 1;

        stats.slideDoorsAmountOpened = 0;

        trainCTS = new CancellationTokenSource();
    }
    private void OnDisable()
    {
        trainCTS?.Cancel();
        trainCTS?.Dispose();
        trainCTS = null;

        trip.ticketsCheckedSinceStart = 0;
    }
    private void Start()
    {
        SetBounds();
        InitStations();
        SpawnFirstStation();
        SetCarriageDictionary();

        stats.targetVelocity = 0;
        stats.curVelocity = KMPHToVelocity(trip.stationsDataArray[0].targetKMPH);
        stats.targetStopPosition = transform.position.x;
        MoveTrainToStartPosition().Forget();
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
    }
    private void FixedUpdate()
    {
        FixedUpdateState();
    }
    private void ChooseState()
    {
        if (stats.curVelocity < CLOSE_TO_STOP_VELOCITY && stats.targetVelocity == 0)
        {
            SetState(TrainStates.Stopped);
        }
        else if (stats.targetVelocity == 0)
        {
            SetState(TrainStates.Decelerating);
        }
        else if (stats.curVelocity < stats.targetVelocity)
        {
            SetState(TrainStates.Accelerating);
        }
        else
        {
            SetState(TrainStates.AtMaxSpeed);
        }
    }
    private void SetState(TrainStates newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch (curState)
        { 
            case TrainStates.Accelerating:
            {
                for (int i = 0; i < carriages.Length; i++)
                {
                    carriages[i].SetSignToNextStation(trip.curStation.stationName);
                }
            }
            break;

            case TrainStates.Decelerating:
            {
                stats.prevPeakVelocity = stats.curVelocity;
            }
            break;

            case TrainStates.AtMaxSpeed:
            {

            }
            break;

            case TrainStates.Stopped:
            {
                stats.curVelocity = 0;
                if (trip.curStation.isFrontOfTrain)
                {
                    for (int i = 0; i < carriages.Length; i++)
                    {
                        carriages[i].UnlockExteriorSlideDoors();
                        carriages[i].SetSignToCurrentStation(trip.curStation.stationName);
                    }
                }
                else
                {
                    for (int i = 0; i < carriages.Length; i++)
                    {
                        carriages[i].UnlockInteriorDoors();
                        carriages[i].SetSignToCurrentStation(trip.curStation.stationName);
                    }
                }
                trip.ticketsCheckedSinceLastStation = 0;
                gameEventData.OnStationArrival.Raise();
            }
            break;
        }
    }
    private void UpdateState()
    {
        switch (curState)
        {
            case TrainStates.AtMaxSpeed:
            {
                if (trip.ticketsCheckedSinceLastStation >= trip.curStation.ticketsToCheckBeforeSpawn)
                {
                    nextStation = stations[curStationIndex];
                    float stationXPos = GetBrakeDistance(stats.curVelocity, settings.deceleration) + TRAIN_WORLD_POS;
                    nextStation.transform.position = new Vector3(stationXPos, 0, 0);
                    nextStation.gameObject.SetActive(true);
                    nextStation.SpawnNPCs();

                    stats.targetVelocity = 0;
                }
            }
            break;
            case TrainStates.Stopped:
            {
                if (stats.curPassengersBoarded == stats.targetPassengersBoarding)
                {
                    if (!closingSlideDoors)
                    {
                        CloseAllSlideDoors();
                        closingSlideDoors = true;
                    }

                    if (stats.slideDoorsAmountOpened == 0)
                    {
                        curStationIndex++;
                        trip.curStation = trip.stationsDataArray[curStationIndex];

                        stats.targetVelocity = KMPHToVelocity(trip.curStation.targetKMPH);
                 
                        stats.curPassengersBoarded = 0;
                        stats.targetPassengersBoarding = trip.curStation.bystanderProfiles.Length + trip.curStation.traitorProfiles.Length;
                        stats.distToSpawnNextStation = stats.trainToMaxSpawnDist - trip.curStation.station_prefab.platformRenderer.transform.localPosition.x;
                        closingSlideDoors = false;
                    }
                }
            }
            break;
        }
    }
    private void FixedUpdateState()
    {
        switch (curState)
        {
            case TrainStates.Accelerating:
            {
                stats.curVelocity = IncreaseVelocity(stats.curVelocity, stats.targetVelocity, settings.acceleration);
            }
            break;

            case TrainStates.Decelerating:
            {
                if (nextStation != null)
                {
                    stats.targetStopPosition = nextStation.transform.position.x;
                }
                stats.curVelocity = DecreaseVelocity(stats.curVelocity, stats.targetVelocity, stats.prevPeakVelocity, settings.deceleration, stats.targetStopPosition);
            }
            break;

            case TrainStates.AtMaxSpeed:
            {
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case TrainStates.Accelerating:
            {

            }
            break;

            case TrainStates.Decelerating:
            {

            }
            break;

            case TrainStates.AtMaxSpeed:
            {

            }
            break;

            case TrainStates.Stopped:
            {

            }
            break;
        }
    }
    private void CloseAllSlideDoors()
    {
        if (trip.curStation.isFrontOfTrain)
        {
            for (int i = 0; i < carriages.Length; i++)
            {
                carriages[i].CloseExteriorSlideDoors();
            }
        }
        else
        {
            for (int i = 0; i < carriages.Length; i++)
            {
                carriages[i].CloseInteriorSlideDoors();
            }
        }
    }
    private void SetCarriageDictionary()
    {
        carriages = GetComponentsInChildren<Carriage>();

        stats.carriageDict = new Dictionary<Collider2D, Carriage>();

        for (int i = 0; i < carriages.Length; i++)
        {
            Carriage curCarriage = carriages[i];
            stats.carriageDict.Add(curCarriage.insideBoundsCollider, curCarriage);
        }
    }
    private void SetSlideDoorPositions()
    {
        int slideDoorsPerCarriage = carriages[0].exteriorSlideDoors.Length;
        int totalSlideDoors = carriages.Length * slideDoorsPerCarriage;

        stats.slideDoorPositions = new float[totalSlideDoors];

        for (int i = 0; i < carriages.Length; i++)
        {
            Carriage carriage = carriages[i];

            for (int j = 0; j < carriage.exteriorSlideDoors.Length; j++)
            {
                int curIndex = i * slideDoorsPerCarriage + j;
                stats.slideDoorPositions[curIndex] = carriage.exteriorSlideDoors[j].transform.position.x;
            }
        }
    }
    private void SpawnFirstStation()
    {
        Station firstStation = stations[curStationIndex];
        float stationXPos = 300;
        firstStation.transform.position = new Vector3(stationXPos, 0, 0);

        firstStation.gameObject.SetActive(true);
        firstStation.SpawnNPCs();
    }
    private void InitStations()
    {
        trip.curStation = trip.stationsDataArray[0];

        stations = new Station[trip.stationsDataArray.Length];

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO stationData = trip.stationsDataArray[i];
            stationData.stationName = stationData.station_prefab.name;

            Station station = Instantiate(stationData.station_prefab, null);
            stations[i] = station;
            station.gameObject.SetActive(false);
        }
    }
    private void SetBounds()
    {
        stats.totalBounds = driversPit.bounds;
        stats.totalBounds.Encapsulate(backSprite.bounds);
    }
    private async UniTask MoveTrainToStartPosition()
    {        
        while (stats.curVelocity != 0)
        {
            stats.targetStopPosition += stats.curVelocity * Time.fixedDeltaTime;
            transform.position = new Vector3(stats.targetStopPosition, transform.position.y, transform.position.z);

            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, trainCTS.Token);
        }
        transform.position = new Vector3(TRAIN_WORLD_POS, transform.position.y, transform.position.z);
        stats.totalBounds.center = transform.position;
        SetSlideDoorPositions();
        stats.trainToMaxSpawnDist = spawner.bounds.max.x - stats.totalBounds.center.x;
        gameEventData.OnTrainArrivedAtStartPosition.Raise();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(stats.totalBounds.center, stats.totalBounds.size);
        Gizmos.DrawLine(new Vector3(stats.totalBounds.min.x, stats.totalBounds.center.y, stats.totalBounds.min.z) , new Vector3(stats.totalBounds.max.x, stats.totalBounds.center.y, stats.totalBounds.min.z));
        Gizmos.DrawLine(new Vector3(stats.totalBounds.center.x, stats.totalBounds.min.y, stats.totalBounds.min.z), new Vector3(stats.totalBounds.center.x, stats.totalBounds.max.y, stats.totalBounds.min.z));
    }
#if UNITY_EDITOR
    private void OnGUI()
    {
        string kmphString = stats.curVelocity.ToString("F2");
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperCenter;
        style.normal.textColor = Color.white;
        Rect rect = new Rect(0, 20f, Screen.width - 20f, 25f); // full-width area, 20px margin
        GUI.Label(rect, kmphString, style);
    }
#endif
}
