using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour
{
    [SerializeField] TrainSettingsSO settings;
    [SerializeField] TrainStatsSO stats;
    [SerializeField] StationsDataSO stationsData;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] ZoneSpawnerStatsSO spawnerStats;
    [SerializeField] BoxCollider2D backCollider;
    [SerializeField] BoxCollider2D frontCollider;
    [SerializeField] Carriage frontCarriage;

    [Header("Generated")]
    [SerializeField] Carriage[] carriages;
    CancellationTokenSource trainCTS;

    private void OnValidate()
    {
        SetCarriageDictionary();
    }
    private void Awake()
    {
        stats.startXPos = transform.position.x;
        stats.trainMaxHeight = frontCollider.bounds.max.y;
        stats.curStation = stationsData.stations[0];
        stats.targetPassengerCount = stats.curStation.bystanderSpawnAmount + stationsData.stations[0].agentSpawnAmount + 1; // +1 for spy himself
        stats.curKMPerHour = stats.curStation.targetTrainSpeed;
        stats.targetKMPerHour = stats.curStation.targetTrainSpeed;
        stats.nextStationIndex = 0;
        //stats.wheelCircumference = (settings.wheelSprite.rect.size.x / settings.wheelSprite.pixelsPerUnit) * Mathf.PI;
        stats.metersTravelled = 0;
        stats.curPassengerCount = 0;
        stats.closingDoors = false;
        stats.brakeDist = GetBrakeDistance();
        trainCTS = new CancellationTokenSource();

        stats.minDepth = frontCarriage.exteriorRenderers[0].depthOrder;
        stats.maxDepth = frontCarriage.interiorSlideDoors[0].rightSlideDoorRenderer.depthOrder;
        stats.depthSection_front_min = frontCarriage.grapPoleRenderers[0].depthOrder - 2;
        stats.depthSection_front_max = frontCarriage.grapPoleRenderers[0].depthOrder - 1;
        stats.depthSection_back_min = frontCarriage.grapPoleRenderers[0].depthOrder + 1;
        stats.depthSection_back_max = frontCarriage.grapPoleRenderers[0].depthOrder + 2;


    }
    private void OnDisable()
    {
        trainCTS?.Cancel();
        trainCTS?.Dispose();
        trainCTS = null;
    }
    private void Start()
    {
        SetCarriageDictionary();
        MoveTrainToStartPosition().Forget();
    }
    private void Update()
    {
        stats.curVelocity = stats.GetMetersPerSecond(stats.curKMPerHour) * Time.deltaTime;
        stats.metersTravelled += stats.curVelocity;

        stats.distToNextStation = stats.curStation.metersPosition - stats.metersTravelled;

        if (stats.distToNextStation <= stats.brakeDist)
        {
            stats.targetKMPerHour = 0;
        }
        else
        {
            stats.targetKMPerHour = stats.curStation.targetTrainSpeed;
        }

        if (stats.distToNextStation < 0.05f && stats.curKMPerHour != 0)
        {
            stats.curKMPerHour = 0;
            gameEventData.OnStationArrival.Raise();
        }
        else if (stats.distToNextStation > 0.05f)
        {
            stats.curKMPerHour = Mathf.Lerp(stats.curKMPerHour, stats.targetKMPerHour, settings.accelerationSpeed * Time.deltaTime);
        }

        if (stats.curPassengerCount == stats.targetPassengerCount && !stats.closingDoors)
        {
            LeavingStation().Forget();
        }
    }
    private async UniTask LeavingStation()
    {
        gameEventData.OnCloseSlideDoors.Raise();
        stats.closingDoors = true;
        await UniTask.WaitForSeconds(settings.doorMoveTime, cancellationToken: trainCTS.Token); // wait for doors to close

        stats.nextStationIndex++;
        stats.curStation = stationsData.stations[stats.nextStationIndex];
        stats.brakeDist = GetBrakeDistance();
        gameEventData.OnStationLeave.Raise();
    }
    private async UniTask MoveTrainToStartPosition()
    {
        stats.distToNextStation = (stationsData.stations[stats.nextStationIndex].metersPosition) - stats.metersTravelled;
        while (stats.distToNextStation > 0.05f)
        {
            transform.position = new Vector3(stats.metersTravelled, transform.position.y, transform.position.z);
            await UniTask.Yield(cancellationToken: trainCTS.Token);
        }

        gameEventData.OnTrainArrivedAtStartPosition.Raise();

        SetSlideDoorPositions();
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
        List<float> slideDoorPosList = new List<float>();

        for (int i = 0; i < carriages.Length; i++)
        {
            Carriage carriage = carriages[i];

            for (int j = 0; j < carriage.exteriorSlideDoors.Length; j++)
            {
                slideDoorPosList.Add(carriage.exteriorSlideDoors[j].transform.position.x);
            }
        }

        stats.slideDoorPositions = slideDoorPosList.ToArray();
    }
    private float GetBrakeDistance()
    {
        float kmph = stats.curStation.targetTrainSpeed;
        float meters = 0;
        for (int i = 0; i < 1000; i++)
        {
            kmph = Mathf.Lerp(kmph, 0, settings.accelerationSpeed * Time.fixedDeltaTime);
            float mps = stats.GetMetersPerSecond(kmph);
            meters += mps * Time.fixedDeltaTime;
            if (kmph <= 0.01f) break;
        }
        return meters;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector2(stats.brakeDist, 0), new Vector2(stats.brakeDist, 5));
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        string kmphString = stats.curKMPerHour.ToString("F2") + "  KM/PH";
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperCenter;
        style.normal.textColor = Color.white;
        Rect rect = new Rect(0, 20f, Screen.width - 20f, 25f); // full-width area, 20px margin
        GUI.Label(rect, kmphString, style);
    }
#endif
}
