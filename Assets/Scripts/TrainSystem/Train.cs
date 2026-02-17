using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using System.Threading;
using UnityEngine;


public class Train : MonoBehaviour
{
    [SerializeField] TrainSettingsSO settings;
    [SerializeField] TrainStatsSO stats;
    [SerializeField] StationsDataSO stationsData;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] AtlasSpawnerStatsSO spawnerStats;
    CancellationTokenSource trainCTS;
    [Serializable] public struct ComponentData
    {
        public SpriteRenderer frontCarriageSpriteRenderer;
    }
    [SerializeField] ComponentData componentData;

    private void Awake()
    {
        stats.trainLength = (componentData.frontCarriageSpriteRenderer.bounds.max.x - transform.position.x);
        stats.trainHalfLength = stats.trainLength * 0.5f;
        stats.startXPos = transform.position.x;
        stats.trainMaxHeight = componentData.frontCarriageSpriteRenderer.bounds.max.y;
        stats.curStation = stationsData.stations[0];
        stats.curStation.isSpawned = true;
        stats.targetPassengerCount = stats.curStation.bystanderSpawnAmount + stationsData.stations[0].agentSpawnAmount + 1; // +1 for spy himself
        stats.curKMPerHour = stats.curStation.targetTrainSpeed;
        stats.targetKMPerHour = stats.curStation.targetTrainSpeed;
        stats.nextStationIndex = 0;
        stats.wheelCircumference = (settings.wheelSprite.rect.size.x / settings.wheelSprite.pixelsPerUnit) * Mathf.PI;
        stats.metersTravelled = 0;
        stats.curPassengerCount = 0;
        stats.closingDoors = false;
        stats.brakeDist = GetBrakeDistance();
        trainCTS = new CancellationTokenSource();

    }
    private void OnDisable()
    {
        trainCTS?.Cancel();
        trainCTS?.Dispose();
        trainCTS = null;
    }
    private void Start()
    {
        MoveTrainToStartPosition().Forget();
        Vector2 entityDepthData = new Vector2(settings.maxMinWorldZPos.postion, settings.maxMinWorldZPos.size);
        Shader.SetGlobalVector(materialIDs.ids.entityDepthRange, entityDepthData);
    }
    private void Update()
    {
        stats.curVelocity = stats.GetMetersPerSecond(stats.curKMPerHour) * Time.deltaTime;
        stats.metersTravelled += stats.curVelocity;
        Shader.SetGlobalFloat(materialIDs.ids.trainMetersTravelled, stats.metersTravelled);
        stats.distToNextStation = (stats.curStation.metersPosition + stats.trainHalfLength) - stats.metersTravelled;
        stats.distToSpawnTrain = (stats.curStation.metersPosition - stats.trainHalfLength) - stats.metersTravelled;
        if (stats.distToSpawnTrain <= 0 && !stats.curStation.isSpawned)
        {
            gameEventData.OnSpawnStation.Raise();
            stats.curStation.isSpawned = true;
        }
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
        stats.distToNextStation = (stationsData.stations[stats.nextStationIndex].metersPosition + stats.trainHalfLength) - stats.metersTravelled;
        while (stats.distToNextStation > 0.05f)
        {
            transform.position = new Vector3(stats.metersTravelled - stats.trainLength, transform.position.y, transform.position.z);
            await UniTask.Yield(cancellationToken: trainCTS.Token);
        }
        gameEventData.OnTrainArrivedAtStartPosition.Raise();
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
        Gizmos.DrawSphere(transform.position + new Vector3(stats.trainHalfLength, 0, 0), 1);
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
