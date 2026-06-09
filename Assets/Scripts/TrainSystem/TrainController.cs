using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using static Train;
using static Spy;
using UnityEditor;
using System.Collections.Generic;
public class TrainController : MonoBehaviour
{
    const float VELOCITY_BUFFER = 0.5f;
    const float DEFAULT_TARGET_KMPH = 40;
    public TrainSettingsSO settings;
    public TrainStatsSO stats;
    public LayerSettingsSO layerSettings;
    public TripSO trip;
    public SpawnData spawnData;
    public GameEventDataSO gameEventData;
    public SpyStatsSO spyStats;
    public AtlasRenderer backSprite;
    public AtlasRenderer driversPit;

    [Header("Generated")]
    public Carriage[] carriages;
    public Gangway[] gangways;
    CancellationTokenSource trainCTS;
    public Station[] stations;
    public static Station NextStationInstance;
    public TrainStates curState;
    public bool closingSlideDoors;
    public int curStationIndex;
    public float metersTravelled;
    public bool skipMoveToStart;

    public static Dictionary<Collider2D, Carriage> carriageDict;

    public float metersTravelledOnBezier;
    private void OnValidate()
    {
        SetBounds();
    }
    private void OnEnable()
    {
        gameEventData.OnTicketInspect.RegisterListener(UpdateTicketInspectParams);
    }
    private void OnDisable()
    {
        gameEventData.OnTicketInspect.UnregisterListener(UpdateTicketInspectParams);
        trainCTS?.Cancel();
        trainCTS?.Dispose();
        trainCTS = null;

        stats.curVelocity = Vector2.zero;
    }
    private void Start()
    {
        Init();
#if UNITY_EDITOR
        if (skipMoveToStart)
        {
            SkipMoveTrainToStartPosition();
        }
        else
        {
            MoveTrainToStartPosition().Forget();
        }
#else
        MoveTrainToStartPosition().Forget();
#endif
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
    }
    public void Init()
    {
        stats.targetKMPH = trip.kmValues[0];

        stats.curVelocity.x = KMPHToVelocity(stats.targetKMPH);
        stats.targetVelocity = Vector2.zero;

        stats.totalNPCsBoarded = 0;
        stats.slideDoorsAmountOpened = 0;

        stats.targetElevatePos = Vector2.zero;

        trainCTS = new CancellationTokenSource();

        SetDepthSections();
        SetBounds();
        InitStations();
        SpawnFirstStation();

        stats.targetPosition = transform.position.x;

        float offset = TRAIN_WORLD_POS_X - transform.position.x;

        carriageDict = new Dictionary<Collider2D, Carriage>();
        for (int i = 0; i < carriages.Length; i++)
        {
            Carriage carriage = carriages[i];
            carriage.SetSeatData(offset);
            carriage.SetSmokerRoomData(offset);
            carriage.SetTotalBounds(offset);
            carriage.SetSignToNextStation(trip.nextStation.stationName);

            carriageDict.Add(carriage.insideBoundsCollider, carriage);
        }
        SetSlideDoorPositions(offset);
    }
    private void ChooseState()
    {
        if (stats.curVelocity.x < CLOSE_TO_STOP_VELOCITY && stats.targetVelocity.x == 0)
        {
            SetState(TrainStates.Stopped);
        }
        else if ((stats.curVelocity.x - stats.targetVelocity.x) > VELOCITY_BUFFER)
        {
            SetState(TrainStates.Decelerating);
        }
        else if ((stats.curVelocity.x - stats.targetVelocity.x) < -VELOCITY_BUFFER)
        {
            SetState(TrainStates.Accelerating);
        }
        else if (stats.targetVelocity.x != 0)
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
                stats.prevPeakVelocity = stats.curVelocity.x;
                gameEventData.OnTrainDeceleration.Raise();
            }
            break;

            case TrainStates.AtMaxSpeed:
            {

            }
            break;

            case TrainStates.Stopped:
            {
                stats.targetNPCsToBoard = trip.nextStation.bystanderProfiles.Length + trip.nextStation.traitorProfiles.Length;
                stats.curVelocity = Vector2.zero;

                if (trip.nextStation.isFrontOfTrain)
                {
                    for (int i = 0; i < carriages.Length; i++)
                    {
                        carriages[i].UnlockExteriorSlideDoors();
                        carriages[i].SetSignToCurrentStation(trip.nextStation.stationName);
                    }
                    stats.activeSlideDoorsMask = layerSettings.trainLayers.exteriorSlideDoors;
                }
                else
                {
                    for (int i = 0; i < carriages.Length; i++)
                    {
                        carriages[i].UnlockInteriorDoors();
                        carriages[i].SetSignToCurrentStation(trip.nextStation.stationName);
                    }
                    stats.activeSlideDoorsMask = layerSettings.trainLayers.interiorSlideDoors;
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
            case TrainStates.Accelerating:
            {
                stats.curVelocity.x = IncreaseVelocity(stats.curVelocity.x, stats.targetVelocity.x, settings.acceleration);
                HandleTrainMeters();
            }
            break;
            case TrainStates.Decelerating:
            {
                if (NextStationInstance != null)
                {
                    stats.targetPosition = NextStationInstance.transform.position.x;
                }
                stats.curVelocity.x = DecreaseVelocity(stats.curVelocity.x, stats.targetVelocity.x, stats.prevPeakVelocity, settings.deceleration, stats.targetPosition);
                HandleTrainMeters();
            }
            break;
            case TrainStates.AtMaxSpeed:
            {
                if (trip.ticketsCheckedSinceLastStation == trip.nextStation.ticketsToCheckBeforeSpawn || spyStats.signedNotepad)
                {
                    SpawnStation();
                    stats.targetVelocity = Vector2.zero;
                }
                HandleTrainMeters();
            }
            break;
            case TrainStates.Stopped:
            {

                switch (spyStats.curLocationState)
                {
                    case LocationState.Carriage:
                    case LocationState.Gangway:
                    {
                        if (stats.totalNPCsBoarded == stats.targetNPCsToBoard && !spyStats.signedNotepad)
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
                                stats.targetVelocity.x = KMPHToVelocity(DEFAULT_TARGET_KMPH);
                            }
                        }
                    }
                    break;

                    case LocationState.Station:
                    {
                        if (spyStats.signedNotepad)
                        {
                            if (!closingSlideDoors)
                            {
                                CloseAllSlideDoors();
                                closingSlideDoors = true;
                            }

                            if (stats.slideDoorsAmountOpened == 0)
                            {
                                trainCTS?.Cancel();
                                trainCTS = new CancellationTokenSource();
                                MoveTrainAwayFromCamera().Forget();
                                stats.targetVelocity.x = KMPHToVelocity(DEFAULT_TARGET_KMPH);


                            }
                        }
                    }
                    break;
                }


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
                stats.totalNPCsBoarded = 0;
                stats.distToSpawnNextStation = stats.trainToMaxSpawnDist - trip.nextStation.station_prefab.frontPlatformRenderer.transform.localPosition.x;
                closingSlideDoors = false;
                gameEventData.OnStationLeave.Raise();
            }
            break;
        }
    }
    private void SpawnStation()
    {
        NextStationInstance = stations[curStationIndex];
        float stationXPos = GetBrakeDistance(stats.curVelocity.x, settings.deceleration, NextStationInstance.parallaxController.parallaxFactor) + TRAIN_WORLD_POS_X;
        NextStationInstance.transform.position = new Vector3(stationXPos, 0, 0);
        NextStationInstance.gameObject.SetActive(true);
        NextStationInstance.SpawnNPCs();
        NextStationInstance.SetFrontParallaxPosition();
        gameEventData.OnStationSpawn.Raise();
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

        stats.exteriorSlideDoorPositions = new float[totalSlideDoors];
        stats.interiorSlideDoorPositions = new float[totalSlideDoors];

        for (int i = 0; i < carriages.Length; i++)
        {
            Carriage carriage = carriages[i];

            for (int j = 0; j < carriage.exteriorSlideDoors.Length; j++)
            {
                int curIndex = i * slideDoorsPerCarriage + j;
                stats.exteriorSlideDoorPositions[curIndex] = carriage.exteriorSlideDoors[j].transform.position.x + offset;
                stats.interiorSlideDoorPositions[curIndex] = carriage.interiorSlideDoors[j].transform.position.x + offset;
            }
        }
    }
    private void SpawnFirstStation()
    {
        Station firstStation = stations[curStationIndex];
        firstStation.transform.position = new Vector3(TRAIN_WORLD_POS_X, 0, 0);

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
        stats.depthSections.min = (int)sampleCarriage.exteriorRenderers[0].transform.position.z;
        stats.depthSections.max = (int)sampleCarriage.interiorSlideDoors[0].rightSlideDoorRenderer.transform.position.z;
        stats.depthSections.frontMin = (int)grapPoleRenderer.transform.position.z;
        stats.depthSections.frontMax = (int)grapPoleRenderer.transform.position.z;
        stats.depthSections.backMin = (int)grapPoleRenderer.transform.position.z;
        stats.depthSections.backMax = (int)grapPoleRenderer.transform.position.z;
        stats.depthSections.carriageSeat = (int)sampleCarriage.seatRenderers[0].transform.position.z;
    }
    private void HandleTrainMeters()
    {
        metersTravelled += stats.curVelocity.x * Time.deltaTime;
        if (metersTravelled > spawnData.bounds.size.x)
        {
            gameEventData.OnMetersAtSpawnBounds.Raise();
            metersTravelled = 0;
        }
        Shader.SetGlobalFloat("_MetersTravelled", metersTravelled);
    }
    private void UpdateTicketInspectParams()
    {
        stats.targetElevatePos = trip.elevationValues[spyStats.ticketsCheckedTotal];
        stats.targetKMPH = trip.kmValues[spyStats.ticketsCheckedTotal];
        stats.targetVelocity.x = KMPHToVelocity(stats.targetKMPH);
        metersTravelledOnBezier = 0;
        MoveOnBezier().Forget();
    }
    private async UniTask MoveOnBezier()
    {
        while(metersTravelledOnBezier < stats.targetElevatePos.x)
        {
            stats.curVelocity.y = GetElevateVelocityBezier(stats.curVelocity, stats.targetElevatePos, ref metersTravelledOnBezier).y;
            await UniTask.Yield();
        }
    }
    private async UniTask MoveTrainToStartPosition()
    {        
        while (stats.curVelocity.x != 0)
        {
            stats.targetPosition += stats.curVelocity.x * Time.deltaTime;
            transform.position = new Vector3(stats.targetPosition, transform.position.y, transform.position.z);
            await UniTask.Yield(trainCTS.Token);
        }
        
        transform.position = new Vector3(TRAIN_WORLD_POS_X, transform.position.y, transform.position.z);
        stats.totalBounds.center = transform.position;
        stats.trainToMaxSpawnDist = spawnData.bounds.max.x - stats.totalBounds.center.x;
        Shader.SetGlobalVector("_TrainBoundsMin", stats.totalBounds.min);
    }
    private async UniTask MoveTrainAwayFromCamera()
    {
        stats.targetPosition = transform.position.x;
        float moveAwayPos = spawnData.bounds.max.x + stats.totalBounds.extents.x;
        while (transform.position.x < moveAwayPos)
        {
            stats.targetPosition += stats.curVelocity.x * Time.deltaTime;
            transform.position = new Vector3(stats.targetPosition, transform.position.y, transform.position.z);
            await UniTask.Yield(trainCTS.Token);
        }
        gameEventData.OnChangeToScoreScene.Raise();
    }
    public static Carriage GetCarriage(Collider2D collider)
    {
        return carriageDict[collider];
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(stats.totalBounds.center, stats.totalBounds.size);
        Gizmos.DrawLine(new Vector3(stats.totalBounds.min.x, stats.totalBounds.center.y, stats.totalBounds.min.z) , new Vector3(stats.totalBounds.max.x, stats.totalBounds.center.y, stats.totalBounds.min.z));
        Gizmos.DrawLine(new Vector3(stats.totalBounds.center.x, stats.totalBounds.min.y, stats.totalBounds.min.z), new Vector3(stats.totalBounds.center.x, stats.totalBounds.max.y, stats.totalBounds.min.z));
    }
#if UNITY_EDITOR
    public void SkipMoveTrainToStartPosition()
    {
        trainCTS?.Cancel();

        stats.curVelocity = Vector2.zero;

        transform.position = new Vector3(TRAIN_WORLD_POS_X, transform.position.y, transform.position.z);

        stats.totalBounds.center = transform.position;
        stats.trainToMaxSpawnDist = spawnData.bounds.max.x - stats.totalBounds.center.x;

        Shader.SetGlobalVector("_TrainBoundsMin", stats.totalBounds.min);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TrainController))]
public class TrainControllerEditor : Editor
{
    private const string SKIP_MOVING_KEY = "TrainControllerEditor_SkipMoving";
    private bool skipMoving;

    private void OnEnable()
    {
        skipMoving = EditorPrefs.GetBool(SKIP_MOVING_KEY, false);

        if (skipMoving)
        {
            TrainController trainController = (TrainController)target;
            trainController.skipMoveToStart = true;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Rect rect = EditorGUILayout.GetControlRect();

        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUI.LabelField(rect, "Editor", style);

        bool newValue = EditorGUILayout.Toggle("Skip Moving", skipMoving);

        if (newValue != skipMoving)
        {
            skipMoving = newValue;

            EditorPrefs.SetBool(SKIP_MOVING_KEY, skipMoving);
            TrainController trainController = (TrainController)target;
            trainController.skipMoveToStart = skipMoving;
        }
    }
}
#endif
