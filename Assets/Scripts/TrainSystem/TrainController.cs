using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static Train;
public class TrainController : MonoBehaviour
{
    [SerializeField] TrainSettingsSO settings;
    [SerializeField] TrainStatsSO stats;
    [SerializeField] TripSO trip;
    [SerializeField] ZoneSpawnerSO spawner;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] AtlasRenderer backSprite;
    [SerializeField] AtlasRenderer driversPit;
    [Header("Generated")]
    [SerializeField] Carriage[] carriages;
    CancellationTokenSource trainCTS;
    public Station[] stations;
    public int stationSpawnFlag;
    public TrainStates curState;
    public int slideDoorsCountBase2;
    public bool closingSlideDoors;
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
        stats.totalTicketsChecked = 0;
        
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

        stats.totalTicketsChecked = 0;
    }
    private void Start()
    {
        SetBounds();
        InitStations();
        SpawnFirstStation();

        SetCarriageDictionary();
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
        if (stats.curVelocity == 0)
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
                stats.targetVelocity = 0;
            }
            break;

            case TrainStates.AtMaxSpeed:
            {

            }
            break;

            case TrainStates.Stopped:
            {
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
                stats.ticketsCheckedSinceLastStation = 0;
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
                if (stats.ticketsCheckedSinceLastStation >= trip.curStation.ticketsToCheckBeforeSpawn)
                {
                    if((stationSpawnFlag & 1 << trip.curStationIndex) == 0)
                    {
                        SpawnStation();    
                        stats.distToBreak = GetBrakeDistance(stats.curVelocity, settings.deceleration);
                    }
                    else if(stats.distanceToNextStation < stats.distToBreak)
                    {
                        stats.targetVelocity = 0;
                    }
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
                        trip.curStationIndex++;
                        trip.curStation = trip.stationsDataArray[trip.curStationIndex];

                        stats.targetVelocity = KMPHToVelocity(trip.curStation.targetKMPH);
                        stats.distToBreak = GetBrakeDistance(stats.targetVelocity, settings.deceleration);
                 
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
                stats.curVelocity = UpdateVelocity(stats.curVelocity, stats.targetVelocity, settings.acceleration);
            }
            break;

            case TrainStates.Decelerating:
            {
                stats.curVelocity = UpdateVelocity(stats.curVelocity, stats.targetVelocity, settings.deceleration);
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
    private async UniTask MoveTrainToStartPosition()
    {
        float initTargetVelocity = KMPHToVelocity(trip.stationsDataArray[0].targetKMPH);
        stats.targetVelocity = initTargetVelocity;
        float nextTargetVelocity = initTargetVelocity; 
        stats.curVelocity = stats.targetVelocity;

        float xPos = transform.position.x;
        
        while (true)
        {
            float distanceToFirstStation = FIRST_STATION_WORLD_POS - xPos;
            float brakeDistance = GetBrakeDistance(stats.curVelocity, settings.deceleration);

            if (distanceToFirstStation < brakeDistance)
            {
                stats.targetVelocity = 0;
                nextTargetVelocity = 0;
            }
            else
            {
                nextTargetVelocity = initTargetVelocity;
            }
            stats.curVelocity = UpdateVelocity(stats.curVelocity, nextTargetVelocity, settings.deceleration);

            xPos += stats.curVelocity * Time.fixedDeltaTime;
            transform.position = new Vector3(xPos, transform.position.y, transform.position.z);

            if (distanceToFirstStation <= 0.05f) break;

            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, trainCTS.Token);
        }

        stats.totalBounds.center = transform.position;
        SetSlideDoorPositions();
        stats.trainToMaxSpawnDist = spawner.bounds.max.x - stats.totalBounds.center.x;
        gameEventData.OnTrainArrivedAtStartPosition.Raise();
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
        Station firstStation = stations[trip.curStationIndex];
        float stationXPos = 300;
        firstStation.transform.position = new Vector3(stationXPos, 0, 0);

        stationSpawnFlag |= 1 << trip.curStationIndex;

        firstStation.gameObject.SetActive(true);
        firstStation.SpawnNPCs();
    }
    private void SpawnStation()
    {
        Station nextStation = stations[trip.curStationIndex];
        float stationXPos = spawner.bounds.max.x - nextStation.platformRenderer.transform.localPosition.x;
        nextStation.transform.position = new Vector3(stationXPos, 0, 0);

        stats.distanceToNextStation = stationXPos - transform.position.x;
        stationSpawnFlag |= 1 << trip.curStationIndex;

        nextStation.gameObject.SetActive(true);
        nextStation.SpawnNPCs();
    }
    private void InitStations()
    {
        trip.curStationIndex = 0;
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
