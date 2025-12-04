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
        public StationSO[] stations;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnReset;
        public GameEvent OnUnlockSlideDoors;
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
        ResetStats();
        gameEventData.OnReset.RegisterListener(ResetStats);
        MovingTrainToStart().Forget();
    }

    private void OnDisable()
    {
        gameEventData.OnReset.UnregisterListener(ResetStats);     
    }
    private void Start()
    {
        soData.stats.halfXSize = (componentData.frontCarriageSpriteRenderer.bounds.max.x - transform.position.x) * 0.5f;
        Shader.SetGlobalVector(shaderData.entityDepthRangeID, (Vector2)soData.settings.entityDepthRange);
    }
    private void Update()
    {
        soData.stats.curKMPerHour = Mathf.Lerp(soData.stats.curKMPerHour, soData.stats.targetKMPerHour, soData.settings.accelerationSpeed * Time.deltaTime);
        soData.stats.metersTravelled += soData.stats.GetMetersPerSecond() * Time.deltaTime;
        soData.stats.curCenterXPos = transform.position.x + soData.stats.halfXSize;
        soData.stats.distanceToNextStation = soData.stations[soData.stats.curStationIndex].metersPosition - soData.stats.metersTravelled - (soData.stats.startXPos + soData.stats.halfXSize);
    }
    private async UniTaskVoid MovingTrainToStart()
    {
        //while (Mathf.Abs(soData.stats.curKMPerHour - soData.stats.targetKMPerHour) > 0.1f)
        //{
        //    transform.position = new Vector3(soData.stats.startXPos + soData.stats.metersTravelled, transform.position.y, transform.position.z);
        //    await UniTask.Yield();
        //}

        while (soData.stats.distanceToNextStation > 0.1f)
        {
            float curMPreSec = soData.stats.GetMetersPerSecond();

            soData.stats.stoppingDistance = (curMPreSec * curMPreSec) / (2f * soData.settings.accelerationSpeed); // v^2 / 2D

            if (soData.stats.distanceToNextStation <= soData.stats.stoppingDistance)
            {
                soData.stats.targetKMPerHour = 0f;
            }
            else
            {
                soData.stats.targetKMPerHour = soData.stats.curKMPerHour;
            }
            transform.position = new Vector3(soData.stats.startXPos + soData.stats.metersTravelled, transform.position.y, transform.position.z);

            await UniTask.Yield();
        }
        soData.stats.targetKMPerHour = 0f;
        soData.stats.curKMPerHour = 0f;
        soData.stats.slideDoorsToUnlock = SlideDoors.Type.Exterior;
        gameEventData.OnUnlockSlideDoors.Raise();
    }
    private void ResetStats()
    { 
        soData.stats.curStationIndex = 0;
        soData.stats.curKMPerHour = 1000;
        soData.stats.targetKMPerHour = 1000;
        soData.stats.metersTravelled = 0.0f;
        soData.stats.arrivedAtStartPos = false;
        soData.stats.distanceToNextStation = Mathf.Infinity;    
        soData.stats.stoppingDistance = 0.0f;
        soData.stats.startXPos = transform.position.x;
        soData.stats.wheelCircumference = (soData.settings.wheelSprite.rect.size.x / soData.settings.wheelSprite.pixelsPerUnit) * Mathf.PI;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector2(soData.stats.stoppingDistance, 0), new Vector2(soData.stats.stoppingDistance, 5));
    }
}
