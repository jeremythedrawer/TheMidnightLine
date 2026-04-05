using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
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
    public TrainStates curState;
    public int slideDoorsCountBase2;
    public bool closingSlideDoors;
    private void OnValidate()
    {
        SetCarriageDictionary();
        stats.totalBounds = driversPit.bounds;
        stats.totalBounds.Encapsulate(backSprite.bounds);

    }
    private void Awake() 
    {
        stats.totalBounds = driversPit.bounds;
        stats.totalBounds.Encapsulate(backSprite.bounds);

        Carriage carriage = carriages[0];
        AtlasRenderer grapPoleRenderer = carriage.grapPoleRenderers[0];
        StationSO stationData = trip.stationsDataArray[0];

        stats.targetPassengersBoarding = stationData.bystanderProfiles.Length + stationData.traitorProfiles.Length + 1; // +1 for spy himself
        stats.curVelocity = GetVelocity(stationData.targetKMPH);
        stats.targetVelocity = GetVelocity(stationData.targetKMPH);
        stats.metersTravelled = 0;
        stats.distToNextStation = trip.curStation.metersPosition - stats.metersTravelled;
        stats.brakePos = GetBrakeDistance(stats.targetVelocity, stats.distToNextStation);
        
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

        stats.metersTravelled = 0;
    }
    private void Start()
    {
        SetCarriageDictionary();
        MoveTrainToStartPosition().Forget();
    }
    private void Update()
    {
        SelectState();
        UpdateState();
    }
    private void FixedUpdate()
    {
        FixedUpdateState();
    }
    private void SelectState()
    {
        if (stats.distToNextStation < CLOSE_TO_STATION_DISTANCE)
        {
            SetState(TrainStates.Stopped);
        }
        else if (stats.metersTravelled > stats.brakePos)
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
                stats.curVelocity = 0;
                gameEventData.OnStationArrival.Raise();
            }
            break;
        }
    }
    private void UpdateState()
    {
        switch (curState)
        {
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
                        stats.distToNextStation = trip.curStation.metersPosition - stats.metersTravelled;
                        stats.targetVelocity = GetVelocity(trip.curStation.targetKMPH);
                        stats.brakePos = GetBrakeDistance(stats.targetVelocity, trip.curStation.metersPosition);
                 
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
                stats.curVelocity = Mathf.Lerp(stats.curVelocity, stats.targetVelocity, settings.accelerationSpeed * Time.fixedDeltaTime);
                stats.metersTravelled += stats.curVelocity * Time.fixedDeltaTime;
                stats.distToNextStation = trip.curStation.metersPosition - stats.metersTravelled;
            }
            break;

            case TrainStates.Decelerating:
            {
                stats.curVelocity = Mathf.Lerp(stats.curVelocity, stats.targetVelocity, settings.accelerationSpeed * Time.fixedDeltaTime);
                stats.metersTravelled += stats.curVelocity * Time.fixedDeltaTime;
                stats.distToNextStation = trip.curStation.metersPosition - stats.metersTravelled;
            }
            break;

            case TrainStates.AtMaxSpeed:
            {
                stats.metersTravelled += stats.curVelocity * Time.fixedDeltaTime;
                stats.distToNextStation = trip.curStation.metersPosition - stats.metersTravelled;
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
        while (stats.distToNextStation > CLOSE_TO_STATION_DISTANCE)
        {
            transform.position = new Vector3(stats.metersTravelled, transform.position.y, transform.position.z);
            await UniTask.Yield(cancellationToken: trainCTS.Token);
        }
        stats.totalBounds.center = transform.position;
        SetSlideDoorPositions();
        stats.trainToMaxSpawnDist = spawner.spawnMaxPos.x - stats.totalBounds.center.x;
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
        int slideDoorsPerCarriage = carriages[0].exteriorRenderers.Length;
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
    private float GetBrakeDistance(float targetVelocity, float nextStationDist)
    {
        float meters = nextStationDist;

        for (int i = 0; i < 1000; i++)
        {
            targetVelocity = Mathf.Lerp(targetVelocity, 0, settings.accelerationSpeed * Time.fixedDeltaTime);
            meters -= targetVelocity * Time.fixedDeltaTime;
            if (targetVelocity <= CLOSE_TO_STATION_DISTANCE) break;
        }
        return meters;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(stats.totalBounds.center, stats.totalBounds.size);
        Gizmos.DrawLine(new Vector2(stats.totalBounds.min.x, stats.totalBounds.center.y) , new Vector2(stats.totalBounds.max.x, stats.totalBounds.center.y));
        Gizmos.DrawLine(new Vector2(stats.totalBounds.center.x, stats.totalBounds.min.y), new Vector2(stats.totalBounds.center.x, stats.totalBounds.max.y));
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
