using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;


public class TrainBrain : MonoBehaviour
{
    [SerializeField] TrainSettingsSO settings;
    [SerializeField] TrainStatsSO stats;
    [SerializeField] StationsDataSO stationsData;
    [SerializeField] GameEventDataSO gameEventData;

    [Serializable] public struct ComponentData
    {
        public SpriteRenderer frontCarriageSpriteRenderer;
    }
    [SerializeField] ComponentData componentData;

    [Serializable] public struct ShaderData
    {
        internal int entityDepthRangeID;
    }
    [SerializeField] ShaderData shaderData;

    private void Awake()
    {
        shaderData.entityDepthRangeID = Shader.PropertyToID("_EntityDepthRange");
    }
    private void OnEnable()
    {
        gameEventData.OnReset.RegisterListener(ResetStats);
    }
    private void OnDisable()
    {
        gameEventData.OnReset.UnregisterListener(ResetStats);     
    }
    private void Start()
    {
        stats.trainLength = (componentData.frontCarriageSpriteRenderer.bounds.max.x - transform.position.x);
        stats.trainHalfLength = stats.trainLength * 0.5f;
        stats.startXPos = transform.position.x;
        stats.trainMaxHeight = componentData.frontCarriageSpriteRenderer.bounds.max.y;
        stats.targetPassengerCount = stationsData.stations[0].bystanderSpawnAmount + stationsData.stations[0].agentSpawnAmount + 1; // +1 for spy himself
        stats.accellation2 = 2 * settings.accelerationSpeed;
        stats.brakeDist = GetBrakeDistance();
        stats.curStation = stationsData.stations[0];

        Vector2 entityDepthData = new Vector2(settings.maxMinWorldZPos.min, settings.maxMinWorldZPos.max - settings.maxMinWorldZPos.min);
        Shader.SetGlobalVector(shaderData.entityDepthRangeID, entityDepthData);

        MoveTrainToStartPosition().Forget();
    }
    private void Update()
    {
        stats.metersTravelled += stats.GetMetersPerSecond(stats.curKMPerHour) * Time.deltaTime;
        stats.distToNextStation = (stationsData.stations[stats.nextStationIndex].metersPosition + stats.trainHalfLength) - stats.metersTravelled;

        if (stats.distToNextStation <= stats.brakeDist)
        {
            stats.targetKMPerHour = 0;
        }
        else
        {
            stats.targetKMPerHour = stationsData.stations[stats.nextStationIndex].targetTrainSpeed;
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
    private void OnApplicationQuit()
    {
        ResetStats();
    }
    private async UniTask LeavingStation()
    {
        gameEventData.OnCloseSlideDoors.Raise();
        stats.closingDoors = true;
        await UniTask.WaitForSeconds(settings.doorMoveTime); // wait for doors to close

        stats.nextStationIndex++;
        stats.curStation = stationsData.stations[stats.nextStationIndex];
        stats.brakeDist = GetBrakeDistance();
        gameEventData.OnStationLeave.Raise();
    }

    private float GetBrakeDistance()
    {
        float kmph = stationsData.stations[stats.nextStationIndex].targetTrainSpeed;
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
    private async UniTask MoveTrainToStartPosition()
    {
        while (stats.distToNextStation > 0.05f)
        {
            transform.position = new Vector3(stats.metersTravelled - stats.trainLength, transform.position.y, transform.position.z);
            await UniTask.Yield();
        }
        gameEventData.OnTrainArrivedAtStartPosition.Raise();
    }
    private void ResetStats()
    { 
        stats.nextStationIndex = 0;
        stats.curKMPerHour = settings.startKMPerHour;
        stats.targetKMPerHour = settings.startKMPerHour;
        stats.metersTravelled = 0.0f;
        stats.distToNextStation = Mathf.Infinity;    
        stats.brakeDist = 0.0f;
        stats.wheelCircumference = (settings.wheelSprite.rect.size.x / settings.wheelSprite.pixelsPerUnit) * Mathf.PI;
        stats.curPassengerCount = 0;
        stats.targetPassengerCount = 0;
        stats.closingDoors = false;
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
