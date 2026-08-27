using UnityEngine;

using Cysharp.Threading.Tasks;

using System;
using System.Threading;
using System.Collections.Generic;

using static Train;
using static Spy;

#if UNITY_EDITOR
using UnityEditor;
#endif
public class TrainController : MonoBehaviour
{
    const float VELOCITY_BUFFER = 0.5f;
    const float DEFAULT_TARGET_KMPH = 40;
    const float OFF_TRAIN_TIME_BUFFER = 5;

    public static Station NextStationInstance;

    public static SlideDoors[] ExteriorSlideDoors;
    public static SlideDoors[] InteriorSlideDoors;

    public static event Action OnTrainAtStartPosition;
    public static event Action OnFinishTripScene;
    public static event Action OnMetersAtSpawnBounds;
    public static event Action OnStationSpawn;
    public static event Action OnStationLeave;
    public static event Action OnStationArrival;
    public static event Action OnTrainDeceleration;

    public TrainSettingsSO settings;
    public TrainData trainData;
    public LayerData layerSettings;
    public TripData trip;
    public SpawnData spawnData;
    public SpyData spyStats;
    public CameraData camStats;

    public AtlasRenderer backSprite;
    public AtlasRenderer driversPit; 

    [Header("Generated")]
    public Carriage[] carriages;
    public Gangway[] gangways;
    public Station[] stations;

    public Station nextStation;

    public CancellationTokenSource trainCTS;
    
    public TrainStates curState;

    public float metersTravelled;
    public float metersTravelledOnBezier;
    public float renderTextureScale;
    public float offTrainClock;

    public bool skipMoveToStart;
    public bool closingSlideDoors;
    public bool atStartPosition;
    private void OnValidate()
    {
        SetBounds();
    }
    private void OnEnable()
    {
        SpyBrain.OnTicketInspect += UpdateTicketInspectParams;
        Scenes.OnLoadTrip1 += Init;
    }
    private void OnDisable()
    {
        SpyBrain.OnTicketInspect -= UpdateTicketInspectParams;
        Scenes.OnLoadTrip1 -= Init;
        
        trainCTS?.Cancel();
        trainCTS?.Dispose();
        trainCTS = null;

        trainData.curVelocity = Vector2.zero;
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
#if UNITY_EDITOR
        nextStation = NextStationInstance;
#endif
    }
    public void Init()
    {
        trip.traitorsSpawned = 0;
        trainData.curStationIndex = 0;

        trainData.targetKMPH = trip.kmValues[0];

        trainData.curVelocity.x = KMPHToVelocity(trainData.targetKMPH);
        trainData.targetVelocity = Vector2.zero;

        trainData.totalNPCsBoarded = 0;
        trainData.slideDoorsAmountOpened = 0;

        trainData.targetElevatePos = Vector2.zero;

        trainCTS = new CancellationTokenSource();

        SetDepthSections();
        SetBounds();
        InitStations();
        SpawnFirstStation();

        trainData.targetPosition = transform.position.x;

        float offset = TRAIN_WORLD_POS_X - transform.position.x;

        for (int i = 0; i < carriages.Length; i++)
        {
            Carriage carriage = carriages[i];
            carriage.SetSeatData(offset);
            carriage.SetSmokerRoomData(offset);
            carriage.SetTotalBounds(offset);
            carriage.SetSignToNextStation(trip.stationAhead.stationName);
        }

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
    private void ChooseState()
    {
        if (trainData.curVelocity.x < CLOSE_TO_STOP_VELOCITY && trainData.targetVelocity.x == 0 && atStartPosition)
        {
            SetState(TrainStates.Stopped);
        }
        else if ((trainData.curVelocity.x - trainData.targetVelocity.x) > VELOCITY_BUFFER)
        {
            SetState(TrainStates.Decelerating);
        }
        else if ((trainData.curVelocity.x - trainData.targetVelocity.x) < -VELOCITY_BUFFER)
        {
            SetState(TrainStates.Accelerating);
        }
        else if (trainData.targetVelocity.x != 0)
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
                    carriages[i].SetSignToNextStation(trip.stationAhead.stationName);
                }
            }
            break;

            case TrainStates.Decelerating:
            {
                trainData.prevPeakVelocity = trainData.curVelocity.x;
                OnTrainDeceleration.Invoke();
            }
            break;

            case TrainStates.AtMaxSpeed:
            {

            }
            break;

            case TrainStates.Stopped:
            {
                trainData.targetNPCsToBoard = trip.stationAhead.bystanderSpawnCount + trip.stationAhead.traitorSpawnCount + trip.stationAhead.accompliceSpawnCount;
                trainData.curVelocity = Vector2.zero;

                if (trip.stationAhead.isFrontOfTrain)
                {
                    for (int i = 0; i < carriages.Length; i++)
                    {
                        carriages[i].UnlockExteriorSlideDoors();
                        carriages[i].SetSignToCurrentStation(trip.stationAhead.stationName);
                    }
                }
                else
                {
                    for (int i = 0; i < carriages.Length; i++)
                    {
                        carriages[i].UnlockInteriorDoors();
                        carriages[i].SetSignToCurrentStation(trip.stationAhead.stationName);
                    }
                }
                OnStationArrival.Invoke();
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
                trainData.curVelocity.x = ChangeVelocity(trainData.curVelocity.x, trainData.targetVelocity.x, trainData.acceleration);
                HandleTrainMeters();
            }
            break;
            case TrainStates.Decelerating:
            {
                if (atStartPosition)
                {
                    if (NextStationInstance != null)
                    {
                        trainData.targetPosition = NextStationInstance.transform.position.x;
                        trainData.curVelocity.x = DecreaseVelocityToTarget(trainData.curVelocity.x, trainData.targetVelocity.x, trainData.prevPeakVelocity, trainData.deceleration, trainData.targetPosition);

                    }
                    else
                    {
                        trainData.curVelocity.x = ChangeVelocity(trainData.curVelocity.x, trainData.targetVelocity.x, trainData.deceleration);
                    }
                }
                HandleTrainMeters();
            }
            break;
            case TrainStates.AtMaxSpeed:
            {
                if (trip.ticketsCheckedSinceLastStation == trip.stationAhead.ticketsToCheckBeforeSpawn)
                {
                    SpawnStation();
                    trainData.targetVelocity = Vector2.zero;
                    trip.ticketsCheckedSinceLastStation = 0;
                }
                HandleTrainMeters();
            }
            break;
            case TrainStates.Stopped:
            {
                switch (camStats.curLocationState)
                {
                    case LocationState.Carriage:
                    case LocationState.Gangway:
                    {
                        offTrainClock = 0;

                        if (trainData.curStationIndex < trip.stationsDataArray.Length - 1)
                        {
                            if (trainData.totalNPCsBoarded == trainData.targetNPCsToBoard)
                            {
                                if (!closingSlideDoors)
                                {
                                    CloseAllSlideDoors();
                                    closingSlideDoors = true;
                                }

                                if (trainData.slideDoorsAmountOpened == 0)
                                {
                                    trainData.curStationIndex++;
                                    trip.stationAhead = trip.stationsDataArray[trainData.curStationIndex];
                                    NextStationInstance = null;
                                    trainData.targetVelocity.x = KMPHToVelocity(DEFAULT_TARGET_KMPH);
                                }
                            }
                        }
                    }
                    break;

                    case LocationState.Station:
                    {
                        if (trainData.curStationIndex > 0)
                        {
                            offTrainClock += Time.deltaTime;
                         
                            if (offTrainClock < OFF_TRAIN_TIME_BUFFER) return;
                            
                            if (!closingSlideDoors)
                            {
                                CloseAllSlideDoors();
                                closingSlideDoors = true;
                            }

                            if (trainData.slideDoorsAmountOpened == 0)
                            {
                                SpyBrain.CurCarriage.MoveUp();
                                trainCTS?.Cancel();
                                trainCTS = new CancellationTokenSource();
                                MoveTrainAwayFromCamera().Forget();
                                trainData.targetVelocity.x = KMPHToVelocity(DEFAULT_TARGET_KMPH);
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
                trainData.totalNPCsBoarded = 0;
                trainData.distToSpawnNextStation = trainData.trainToMaxSpawnDist - trip.stationAhead.station_prefab.platformRenderer.transform.localPosition.x;
                closingSlideDoors = false;
                OnStationLeave.Invoke();
            }
            break;
        }
    }
    private void SpawnStation()
    {
        NextStationInstance = stations[trainData.curStationIndex];
        float stationXPos = spawnData.bounds.max.x + (NextStationInstance.transform.position.x - NextStationInstance.platformRenderer.bounds.min.x);
        NextStationInstance.transform.position = new Vector3(stationXPos, 0, 0);
        NextStationInstance.gameObject.SetActive(true);
        NextStationInstance.SpawnNPCs();
        OnStationSpawn.Invoke();
    }
    private void CloseAllSlideDoors()
    {
        if (trip.stationAhead.isFrontOfTrain)
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
    private void SetSlideDoorPositions()
    {
        int slideDoorsPerCarriage = carriages[0].exteriorSlideDoors.Length;
        int totalSlideDoors = carriages.Length * slideDoorsPerCarriage;

        ExteriorSlideDoors = new SlideDoors[totalSlideDoors];
        InteriorSlideDoors = new SlideDoors[totalSlideDoors];

        for (int i = 0; i < carriages.Length; i++)
        {
            Carriage carriage = carriages[i];

            for (int j = 0; j < carriage.exteriorSlideDoors.Length; j++)
            {

                SlideDoors exteriorDoors = carriage.exteriorSlideDoors[j];
                SlideDoors interiorDoors = carriage.interiorSlideDoors[j];

                int curIndex = i * slideDoorsPerCarriage + j;
                ExteriorSlideDoors[curIndex] = exteriorDoors;
                InteriorSlideDoors[curIndex] = interiorDoors;
            }
        }

        int xBoundsCount = totalSlideDoors - 1;
        trainData.exteriorSlideDoorXBounds = new float[xBoundsCount];
        trainData.interiorSlideDoorXBounds = new float[xBoundsCount];
        for (int i = 0; i < xBoundsCount; i++)
        {
            float curExteriorDoorPosX = ExteriorSlideDoors[i].transform.position.x;
            float nextExteriorDoorPosX = ExteriorSlideDoors[i + 1].transform.position.x;

            trainData.exteriorSlideDoorXBounds[i] = curExteriorDoorPosX + ((nextExteriorDoorPosX - curExteriorDoorPosX) * 0.5f);

            float curInteriorDoorPosX = InteriorSlideDoors[i].transform.position.x;
            float nextInteriorDoorPosX = InteriorSlideDoors[i + 1].transform.position.x;

            trainData.interiorSlideDoorXBounds[i] = curInteriorDoorPosX + ((nextInteriorDoorPosX - curInteriorDoorPosX) * 0.5f);
        }
    }
    private void SpawnFirstStation()
    {
        Station firstStation = stations[trainData.curStationIndex];
        NextStationInstance = firstStation;
        firstStation.transform.position = new Vector3(TRAIN_WORLD_POS_X, 0, 0);

        firstStation.gameObject.SetActive(true);
        firstStation.SpawnNPCs();
    }
    private void InitStations()
    {
        trip.stationAhead = trip.stationsDataArray[0];

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
        trainData.totalBounds = backSprite.GetBounds();
        trainData.totalBounds.Encapsulate(driversPit.GetBounds());
        Shader.SetGlobalVector("_TrainBoundsMin", trainData.totalBounds.min);
        Shader.SetGlobalVector("_TrainBoundsSize", trainData.totalBounds.size);
    }
    private void SetDepthSections()
    {
        Carriage sampleCarriage = carriages[0];
        AtlasRenderer grapPoleRenderer = sampleCarriage.grapPoleRenderers[0];
        trainData.depthSections.back = (int)sampleCarriage.interiorSlideDoors[0].rightSlideDoorRenderer.transform.position.z;
        trainData.depthSections.carriageSeat = (int)sampleCarriage.seatRenderers[0].transform.position.z - 1;
        trainData.depthSections.backStandingBack = trainData.depthSections.carriageSeat - 1;
        trainData.depthSections.backStandingFront = (int)grapPoleRenderer.transform.position.z + 1;
        trainData.depthSections.frontStandingBack = (int)grapPoleRenderer.transform.position.z - 1;
        trainData.depthSections.frontStandingFront = (int)sampleCarriage.exteriorSlideDoors[0].rightSlideDoorRenderer.transform.position.z + 1;
        trainData.depthSections.front = (int)sampleCarriage.exteriorRenderers[0].transform.position.z;
    }
    private void HandleTrainMeters()
    {
        metersTravelled += trainData.curVelocity.x * Time.deltaTime;
        if (metersTravelled > spawnData.bounds.size.x)
        {
            OnMetersAtSpawnBounds.Invoke();
            metersTravelled = 0;
        }
        Shader.SetGlobalFloat("_MetersTravelled", metersTravelled);
    }
    private void UpdateTicketInspectParams()
    {
        int ticketParamsIndex = trip.ticketsCheckedTotal - 1;
        trainData.targetElevatePos = trip.elevationValues[ticketParamsIndex];
        trainData.targetKMPH = trip.kmValues[ticketParamsIndex];
        trainData.targetNightValue = trip.dayNightValues[ticketParamsIndex];
        trainData.targetVelocity.x = KMPHToVelocity(trainData.targetKMPH);
        metersTravelledOnBezier = 0;
        metersTravelled = 0;
        MoveOnBezier().Forget();
    }
    private void InitAtStartPosition()
    {
        transform.position = new Vector3(TRAIN_WORLD_POS_X, transform.position.y, transform.position.z);
        SetBounds();
        SetSlideDoorPositions();
        trainData.trainToMaxSpawnDist = spawnData.bounds.max.x - trainData.totalBounds.center.x;
        OnTrainAtStartPosition?.Invoke();
        atStartPosition = true;
    }
    private async UniTask MoveOnBezier()
    {
        while(metersTravelledOnBezier < trainData.targetElevatePos.x)
        {
            trainData.curVelocity.y = GetElevateVelocityBezier(trainData.curVelocity, trainData.targetElevatePos, ref metersTravelledOnBezier).y;
            await UniTask.Yield();
        }
    }
    private async UniTask MoveTrainToStartPosition()
    {
        while (trainData.curVelocity.x > 0)
        {
            trainData.targetPosition += trainData.curVelocity.x * Time.deltaTime;
            trainData.curVelocity.x = DecreaseVelocityToTarget(trainData.curVelocity.x, trainData.targetVelocity.x, trainData.prevPeakVelocity, trainData.deceleration, trainData.targetPosition);
            transform.position = new Vector3(trainData.targetPosition, transform.position.y, transform.position.z);
            await UniTask.Yield(trainCTS.Token);
        }
        InitAtStartPosition();
    }
    private async UniTask MoveTrainAwayFromCamera()
    {
        trainData.targetPosition = transform.position.x;
        float moveAwayPos = spawnData.bounds.max.x + trainData.totalBounds.extents.x;
        while (transform.position.x < moveAwayPos)
        {
            trainData.targetPosition += trainData.curVelocity.x * Time.deltaTime;
            transform.position = new Vector3(trainData.targetPosition, transform.position.y, transform.position.z);
            await UniTask.Yield(trainCTS.Token);
        }
        OnFinishTripScene.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(trainData.totalBounds.center, trainData.totalBounds.size);
        Gizmos.DrawWireSphere(trainData.totalBounds.center, 0.1f);

        if(nextStation != null)
        {
            Gizmos.color = Color.blue;
            if (nextStation.station.isFrontOfTrain)
            {
                for (int i = 0; i < trainData.exteriorSlideDoorXBounds.Length; i++)
                {
                    Gizmos.DrawLine(new Vector2(trainData.exteriorSlideDoorXBounds[i], trainData.totalBounds.min.y), new Vector2(trainData.exteriorSlideDoorXBounds[i], trainData.totalBounds.max.y));
                }
            }
            else
            {
                for (int i = 0; i < trainData.interiorSlideDoorXBounds.Length; i++)
                {
                    Gizmos.DrawLine(new Vector2(trainData.interiorSlideDoorXBounds[i], trainData.totalBounds.min.y), new Vector2(trainData.interiorSlideDoorXBounds[i], trainData.totalBounds.max.y));
                }
            }
        }
    }
    public void SkipMoveTrainToStartPosition()
    {
        trainCTS?.Cancel();
        trainData.curVelocity = Vector2.zero;
        InitAtStartPosition();
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
            EditorUtility.SetDirty(trainController);
            AssetDatabase.SaveAssetIfDirty(trainController);
        }
    }
}
#endif
