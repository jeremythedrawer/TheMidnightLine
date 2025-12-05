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
    }
    [SerializeField] SOData soData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnReset;
        public GameEvent OnUnlockSlideDoors;
        public GameEvent OnCloseSlideDoors;
        public GameEvent OnStationArrival;
        public GameEvent OnStationLeave; //TODO raise when train leaves a station
        public GameEvent OnBoardingSpy;
    }
    [SerializeField] GameEventData gameEventData;

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
        gameEventData.OnBoardingSpy.RegisterListener(SpyHasBoarded);
        MovingTrainToStart().Forget();
    }
    private void OnDisable()
    {
        gameEventData.OnReset.UnregisterListener(ResetStats);     
        gameEventData.OnBoardingSpy.UnregisterListener(SpyHasBoarded);
    }
    private void Start()
    {
        soData.stats.trainLength = (componentData.frontCarriageSpriteRenderer.bounds.max.x - transform.position.x);
        soData.stats.trainHalfLength = soData.stats.trainLength * 0.5f;
        soData.stats.startMinXPos = transform.position.x;
        soData.stats.startCenterXPos = soData.stats.startMinXPos + soData.stats.trainHalfLength;

        Shader.SetGlobalVector(shaderData.entityDepthRangeID, (Vector2)soData.settings.entityDepthRange);
    }
    private void Update()
    {
        soData.stats.curKMPerHour = Mathf.Lerp(soData.stats.curKMPerHour, soData.stats.targetKMPerHour, soData.settings.accelerationSpeed * Time.deltaTime);
        soData.stats.metersTravelled += soData.stats.GetMetersPerSecond() * Time.deltaTime;
        soData.stats.distToNextStation = soData.settings.stations[soData.stats.nextStationIndex].metersPosition - soData.stats.metersTravelled - soData.stats.startCenterXPos;
    }
    private void OnApplicationQuit()
    {
        ResetStats();
    }
    private void SpyHasBoarded()
    {
        gameEventData.OnCloseSlideDoors.Raise();
    }
    private async UniTask MovingTrainToStart()
    {
        while (soData.stats.distToNextStation > 0.1f)
        {
            float curMPreSec = soData.stats.GetMetersPerSecond();

            soData.stats.stoppingDistance = (curMPreSec * curMPreSec) / (2f * soData.settings.accelerationSpeed); // v^2 / 2D

            if (soData.stats.distToNextStation <= soData.stats.stoppingDistance)
            {
                soData.stats.targetKMPerHour = 0f;
            }
            else
            {
                soData.stats.targetKMPerHour = soData.stats.curKMPerHour;
            }
            transform.position = new Vector3(soData.stats.startMinXPos + soData.stats.metersTravelled, transform.position.y, transform.position.z);

            await UniTask.Yield();
        }
        soData.stats.targetKMPerHour = 0f;
        soData.stats.curKMPerHour = 0f;
        soData.stats.slideDoorsToUnlock = SlideDoors.Type.Exterior;
        gameEventData.OnUnlockSlideDoors.Raise();
        gameEventData.OnStationArrival.Raise();
    }
    private void ResetStats()
    { 
        soData.stats.nextStationIndex = 0;
        soData.stats.curKMPerHour = 1000;
        soData.stats.targetKMPerHour = 1000;
        soData.stats.metersTravelled = 0.0f;
        soData.stats.arrivedAtStartPos = false;
        soData.stats.distToNextStation = Mathf.Infinity;    
        soData.stats.stoppingDistance = 0.0f;
        soData.stats.startMinXPos = transform.position.x;
        soData.stats.wheelCircumference = (soData.settings.wheelSprite.rect.size.x / soData.settings.wheelSprite.pixelsPerUnit) * Mathf.PI;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector2(soData.stats.stoppingDistance, 0), new Vector2(soData.stats.stoppingDistance, 5));
    }
}
