using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;


public class TrainBrain : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public TrainSettingsSO settings;
        public TrainStatsSO stats;
        public StationsDataSO stationsData;
        public GameEventDataSO gameEventData;
    }
    [SerializeField] SOData soData;

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
        soData.gameEventData.OnReset.RegisterListener(ResetStats);
    }
    private void OnDisable()
    {
        soData.gameEventData.OnReset.UnregisterListener(ResetStats);     
    }
    private void Start()
    {
        soData.stats.trainLength = (componentData.frontCarriageSpriteRenderer.bounds.max.x - transform.position.x);
        soData.stats.trainHalfLength = soData.stats.trainLength * 0.5f;
        soData.stats.startXPos = transform.position.x;
        soData.stats.trainMaxHeight = componentData.frontCarriageSpriteRenderer.bounds.max.y;
        soData.stats.targetPassengerCount = soData.stationsData.stations[0].bystanderSpawnAmount + soData.stationsData.stations[0].agentSpawnAmount + 1; // +1 for spy himself
        soData.stats.accellation2 = 2 * soData.settings.accelerationSpeed;
        soData.stats.brakeDist = GetBrakeDistance();

        Shader.SetGlobalVector(shaderData.entityDepthRangeID, (Vector2)soData.settings.entityDepthRange);

        MoveTrainToStartPosition().Forget();
    }
    private void Update()
    {
        soData.stats.metersTravelled += soData.stats.GetMetersPerSecond(soData.stats.curKMPerHour) * Time.deltaTime;
        soData.stats.distToNextStation = (soData.stationsData.stations[soData.stats.nextStationIndex].metersPosition + soData.stats.trainHalfLength) - soData.stats.metersTravelled;

        if (soData.stats.distToNextStation <= soData.stats.brakeDist)
        {
            soData.stats.targetKMPerHour = 0;
        }
        else
        {
            soData.stats.targetKMPerHour = soData.stationsData.stations[soData.stats.nextStationIndex].targetTrainSpeed;
        }

        if (soData.stats.distToNextStation < 0.05f && soData.stats.curKMPerHour != 0)
        {
            soData.stats.curKMPerHour = 0;
            soData.stats.slideDoorsToUnlock = soData.stationsData.stations[soData.stats.nextStationIndex].slideDoorsType;
            soData.gameEventData.OnUnlockSlideDoors.Raise();
            soData.gameEventData.OnStationArrival.Raise();
        }
        else if (soData.stats.distToNextStation > 0.05f)
        {
            soData.stats.curKMPerHour = Mathf.Lerp(soData.stats.curKMPerHour, soData.stats.targetKMPerHour, soData.settings.accelerationSpeed * Time.deltaTime);
        }

        if (soData.stats.curPassengerCount == soData.stats.targetPassengerCount && !soData.stats.closingDoors)
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
        soData.gameEventData.OnCloseSlideDoors.Raise();
        soData.stats.closingDoors = true;
        await UniTask.WaitForSeconds(soData.settings.doorMoveTime); // wait for doors to close

        soData.stats.nextStationIndex++;
        soData.stats.brakeDist = GetBrakeDistance();
        soData.gameEventData.OnStationLeave.Raise();
    }
    //private async UniTask ArrivingAtStation(SlideDoors.Type slideDoorsToUnlock)
    //{
    //    while (soData.stats.distToNextStation > 0.1f)
    //    {
    //        float curMPreSec = soData.stats.GetMetersPerSecond(soData.stats.curKMPerHour);

    //        soData.stats.brakeDist = (curMPreSec * curMPreSec) / soData.stats.accellation2; // v^2 / 2A

    //        if (soData.stats.distToNextStation <= soData.stats.brakeDist)
    //        {
    //            soData.stats.targetKMPerHour = 0f;
    //        }
    //        else
    //        {
    //            soData.stats.targetKMPerHour = soData.stats.curKMPerHour;
    //        }

    //        await UniTask.Yield();
    //    }
    //    soData.stats.targetKMPerHour = 0f;
    //    soData.stats.curKMPerHour = 0f;
    //    soData.stats.slideDoorsToUnlock = slideDoorsToUnlock;
    //    soData.gameEventData.OnUnlockSlideDoors.Raise();
    //    soData.gameEventData.OnStationArrival.Raise();
    //}

    private float GetBrakeDistance()
    {
        float kmph = soData.stationsData.stations[soData.stats.nextStationIndex].targetTrainSpeed;
        float meters = 0;
        for (int i = 0; i < 1000; i++)
        {
            kmph = Mathf.Lerp(kmph, 0, soData.settings.accelerationSpeed * Time.fixedDeltaTime);
            float mps = soData.stats.GetMetersPerSecond(kmph);
            meters += mps * Time.fixedDeltaTime;
            if (kmph <= 0.01f) break;
        }
        return meters;
    }
    private async UniTask MoveTrainToStartPosition()
    {
        while (soData.stats.distToNextStation > 0.05f)
        {
            transform.position = new Vector3(soData.stats.metersTravelled - soData.stats.trainLength, transform.position.y, transform.position.z);
            await UniTask.Yield();
        }
    }
    private void ResetStats()
    { 
        soData.stats.nextStationIndex = 0;
        soData.stats.curKMPerHour = soData.settings.startKMPerHour;
        soData.stats.targetKMPerHour = soData.settings.startKMPerHour;
        soData.stats.metersTravelled = 0.0f;
        soData.stats.distToNextStation = Mathf.Infinity;    
        soData.stats.brakeDist = 0.0f;
        soData.stats.wheelCircumference = (soData.settings.wheelSprite.rect.size.x / soData.settings.wheelSprite.pixelsPerUnit) * Mathf.PI;
        soData.stats.curPassengerCount = 0;
        soData.stats.targetPassengerCount = 0;
        soData.stats.closingDoors = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector2(soData.stats.brakeDist, 0), new Vector2(soData.stats.brakeDist, 5));
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        string kmphString = soData.stats.curKMPerHour.ToString("F2") + "  KM/PH";
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperCenter;
        style.normal.textColor = Color.white;
        Rect rect = new Rect(0, 20f, Screen.width - 20f, 25f); // full-width area, 20px margin
        GUI.Label(rect, kmphString, style);
    }
#endif
}
