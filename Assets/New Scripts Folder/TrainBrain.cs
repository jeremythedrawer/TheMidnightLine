using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using System.Collections.Generic;
using UnityEngine;


public class TrainBrain : MonoBehaviour
{
    const float KM_TO_MPS = 0.27777777778f;

    [Serializable] public struct SOData
    {
        public TrainSettingsSO settings;
        public TrainStatsSO stats;
        public StationSO[] stations;
        public CarriageSO[] carriages;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnReset;
    }
    [SerializeField] GameEventData gameEventData;

    [Serializable] public struct WheelData
    {
        public Sprite wheelSprite;
        internal List<Transform> wheelTransforms;
        internal float degPerMeter;
    }
    [SerializeField] WheelData wheelData;

    [Serializable] public struct ComponentData
    {
        public SpriteRenderer frontCarriageSpriteRenderer;
    }
    [SerializeField] ComponentData componentData;

    private void Awake()
    {
        wheelData.wheelTransforms = new List<Transform>();
        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag("Wheel"))
            {
                wheelData.wheelTransforms.Add(child);
            }
        }
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
        wheelData.degPerMeter = 360.0f / ((wheelData.wheelSprite.rect.size.x / wheelData.wheelSprite.pixelsPerUnit) * Mathf.PI);
        soData.stats.halfXSize = (componentData.frontCarriageSpriteRenderer.bounds.max.x - transform.position.x) * 0.5f;
    }
    private void Update()
    {
        soData.stats.curKMPerHour = Mathf.Lerp(soData.stats.curKMPerHour, soData.stats.targetKMPerHour, soData.settings.accelerationSpeed * Time.deltaTime);
        soData.stats.curMPerSec = soData.stats.curKMPerHour * KM_TO_MPS;
        soData.stats.metersTravelled += soData.stats.curMPerSec * Time.deltaTime;
        soData.stats.curCenterXPos = transform.position.x + soData.stats.halfXSize;
        soData.stats.distanceToNextStation = soData.stations[soData.stats.curStationIndex].metersPosition - soData.stats.metersTravelled - (soData.stats.startXPos + soData.stats.halfXSize);

        float metersMoved = soData.stats.curKMPerHour * KM_TO_MPS * Time.deltaTime;
        float wheelRotation = -metersMoved * wheelData.degPerMeter;

        foreach (Transform wheel in wheelData.wheelTransforms)
        {
            wheel.transform.Rotate(0, 0, wheelRotation);
        }
    }
    private async UniTaskVoid MovingTrainToStart()
    {
        while (Mathf.Abs(soData.stats.curKMPerHour - soData.stats.targetKMPerHour) > 0.1f)
        {
            transform.position = new Vector3(soData.stats.startXPos + soData.stats.metersTravelled, 0, 0);
            await UniTask.Yield();
        }

        while (soData.stats.distanceToNextStation > 0.1f)
        {
            soData.stats.stoppingDistance = (soData.stats.curMPerSec * soData.stats.curMPerSec) / (2f * soData.settings.accelerationSpeed);

            if (soData.stats.distanceToNextStation <= soData.stats.stoppingDistance)
            {
                soData.stats.targetKMPerHour = 0f;
            }
            else
            {
                soData.stats.targetKMPerHour = soData.stats.curKMPerHour;
            }
            transform.position = new Vector3(soData.stats.startXPos + soData.stats.metersTravelled, 0, 0);

            await UniTask.Yield();
        }
    }

    private void ResetStats()
    { 
        soData.stats.curStationIndex = 0;
        soData.stats.curKMPerHour = 0;
        soData.stats.curMPerSec = 0;
        soData.stats.targetKMPerHour = 100;
        soData.stats.metersTravelled = 0.0f;
        soData.stats.arrivedAtStartPos = false;
        soData.stats.distanceToNextStation = Mathf.Infinity;    
        soData.stats.stoppingDistance = 0.0f;
        soData.stats.startXPos = transform.position.x;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector2(soData.stats.stoppingDistance, 0), new Vector2(soData.stats.stoppingDistance, 5));
    }
}
