using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using static Train;
using static Spy;
public class TrainController : MonoBehaviour
{
    public TrainSettingsSO settings;
    public TrainStatsSO stats;
    public TripSO trip;
    public ZoneSpawnerSO spawner;
    public GameEventDataSO gameEventData;
    public SpyStatsSO spyStats;
    public AtlasRenderer backSprite;
    public AtlasRenderer driversPit;

    [Header("Generated")]
    public Carriage[] carriages;
    CancellationTokenSource trainCTS;
    public Station[] stations;
    public static Station nextStation;
    public TrainStates curState;
    public bool closingSlideDoors;
    public int curStationIndex;


    public static Carriage[] allCarriages;
    
    private void OnValidate()
    {
        SetBounds();
    }
    private void OnDisable()
    {
        trainCTS?.Cancel();
        trainCTS?.Dispose();
        trainCTS = null;
    }
    private void Start()
    {
        StationSO stationData = trip.stationsDataArray[0];
        
        stats.curVelocity = KMPHToVelocity(stationData.targetKMPH);
        stats.targetVelocity = 0;

        stats.totalPassengersBoarded = 0;
        stats.slideDoorsAmountOpened = 0;

        trainCTS = new CancellationTokenSource();

        SetDepthSections();
        SetBounds();
        InitStations();
        SpawnFirstStation();

        stats.targetStopPosition = transform.position.x;

        float offset = TRAIN_WORLD_POS - transform.position.x;
        for (int i = 0; i < carriages.Length; i++)
        {
            Carriage carriage = carriages[i];
            carriage.SetSeatData(offset);
            carriage.SetSmokerRoomData(offset);
            carriage.SetTotalBounds(offset);
            carriage.SetSignToNextStation(trip.nextStation.stationName);
        }
        SetSlideDoorPositions(offset);
        allCarriages = carriages;

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
                    carriages[i].SetSignToNextStation(trip.nextStation.stationName);
                }
            }
            break;

            case TrainStates.Decelerating:
            {
                stats.prevPeakVelocity = stats.curVelocity;
                gameEventData.OnTrainDeceleration.Raise();
            }
            break;

            case TrainStates.AtMaxSpeed:
            {

            }
            break;

            case TrainStates.Stopped:
            {
                stats.targetPassengersBoarding = trip.nextStation.bystanderProfiles.Length + trip.nextStation.traitorProfiles.Length;
                stats.curVelocity = 0;
                if (trip.nextStation.isFrontOfTrain)
                {
                    for (int i = 0; i < carriages.Length; i++)
                    {
                        carriages[i].UnlockExteriorSlideDoors();
                        carriages[i].SetSignToCurrentStation(trip.nextStation.stationName);
                    }
                }
                else
                {
                    for (int i = 0; i < carriages.Length; i++)
                    {
                        carriages[i].UnlockInteriorDoors();
                        carriages[i].SetSignToCurrentStation(trip.nextStation.stationName);
                    }
                }
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
                if (trip.ticketsCheckedSinceLastStation >= trip.nextStation.ticketsToCheckBeforeSpawn)
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
                if (stats.totalPassengersBoarded == stats.targetPassengersBoarding && spyStats.curLocationState != LocationState.Station)
                {
                    if (!closingSlideDoors)
                    {
                        CloseAllSlideDoors();
                        closingSlideDoors = true;
                    }

                    if (stats.slideDoorsAmountOpened == 0)
                    {
                        curStationIndex++;
                        trip.nextStation = trip.stationsDataArray[curStationIndex];

                        stats.targetVelocity = KMPHToVelocity(trip.nextStation.targetKMPH);
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
                stats.metersTravelled += stats.curVelocity * Time.fixedDeltaTime;
                Shader.SetGlobalFloat("_MetersTravelled", stats.metersTravelled);
            }
            break;

            case TrainStates.Decelerating:
            {
                if (nextStation != null)
                {
                    stats.targetStopPosition = nextStation.transform.position.x;
                }
                stats.curVelocity = DecreaseVelocity(stats.curVelocity, stats.targetVelocity, stats.prevPeakVelocity, settings.deceleration, stats.targetStopPosition);
                stats.metersTravelled += stats.curVelocity * Time.fixedDeltaTime;
                Shader.SetGlobalFloat("_MetersTravelled", stats.metersTravelled);
            }
            break;

            case TrainStates.AtMaxSpeed:
            {
                stats.metersTravelled = (stats.metersTravelled + stats.curVelocity * Time.fixedDeltaTime) % 1000f;
                Shader.SetGlobalFloat("_MetersTravelled", stats.metersTravelled);
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
                stats.totalPassengersBoarded = 0;
                stats.distToSpawnNextStation = stats.trainToMaxSpawnDist - trip.nextStation.station_prefab.frontPlatformRenderer.transform.localPosition.x;
                closingSlideDoors = false;
                gameEventData.OnStationLeave.Raise();
            }
            break;
        }
    }
    private void CloseAllSlideDoors()
    {
        if (trip.nextStation.isFrontOfTrain)
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
    private void SetSlideDoorPositions(float offset)
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
                stats.slideDoorPositions[curIndex] = carriage.exteriorSlideDoors[j].transform.position.x + offset;
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
        trip.nextStation = trip.stationsDataArray[0];

        stations = new Station[trip.stationsDataArray.Length];

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO stationData = trip.stationsDataArray[i];
            stationData.stationName = stationData.station_prefab.name;
            stationData.stationIndex = i;
            Station station = Instantiate(stationData.station_prefab, null);
            stations[i] = station;
            station.gameObject.SetActive(false);
        }
    }
    private void SetBounds()
    {
        stats.totalBounds = driversPit.bounds;
        stats.totalBounds.Encapsulate(backSprite.bounds);
        stats.totalBounds.size = new Vector3(stats.totalBounds.size.x, stats.totalBounds.size.y, stats.depthSections.min);
        stats.totalBounds.center = new Vector3(stats.totalBounds.center.x, stats.totalBounds.center.y, stats.totalBounds.size.z * 0.5f);
    }
    private void SetDepthSections()
    {
        Carriage sampleCarriage = carriages[0];
        AtlasRenderer grapPoleRenderer = sampleCarriage.grapPoleRenderers[0];
        stats.depthSections.min = sampleCarriage.exteriorRenderers[0].batchKey.depthOrder;
        stats.depthSections.max = sampleCarriage.interiorSlideDoors[0].rightSlideDoorRenderer.batchKey.depthOrder;
        stats.depthSections.frontMin = grapPoleRenderer.batchKey.depthOrder - 2;
        stats.depthSections.frontMax = grapPoleRenderer.batchKey.depthOrder - 1;
        stats.depthSections.backMin = grapPoleRenderer.batchKey.depthOrder + 1;
        stats.depthSections.backMax = grapPoleRenderer.batchKey.depthOrder + 2;
        stats.depthSections.carriageSeat = sampleCarriage.seatRenderers[0].batchKey.depthOrder - 1;
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
        stats.trainToMaxSpawnDist = spawner.bounds.max.x - stats.totalBounds.center.x;
        Shader.SetGlobalVector("_TrainBoundsMin", stats.totalBounds.min);
    }
    public static Carriage GetCarriage(float xPos)
    {
        for (int i = 0; i < allCarriages.Length; i++)
        {
            Carriage carriage = allCarriages[i];

            if (xPos > carriage.carriageWallRenderer.bounds.min.x && xPos < carriage.carriageWallRenderer.bounds.max.x)
            {
                return carriage;
            }
        }
        return null;
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
