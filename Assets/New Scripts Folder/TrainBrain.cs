using Proselyte.Sigils;
using System;
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

    [Serializable] public struct ComponentData
    {
        public Sprite wheelSprite;
        public Transform[] wheelTransforms;
        public float degPerMeter;
    }
    [SerializeField] ComponentData componentData;

    private void Start()
    {
        componentData.degPerMeter = 360.0f / ((componentData.wheelSprite.rect.size.x / componentData.wheelSprite.pixelsPerUnit) * Mathf.PI);
    }
    private void Update()
    {
        soData.stats.metersTravelled += soData.stats.curKMPerHour * KM_TO_MPS * Time.deltaTime;
        soData.stats.distanceToNextStation = soData.stations[soData.stats.curStationIndex].metersPosition - soData.stats.metersTravelled;

        foreach (Transform wheel in componentData.wheelTransforms)
        {
            float deltaX = soData.stats.curKMPerHour * KM_TO_MPS * Time.deltaTime;
            float rotation = -deltaX * componentData.degPerMeter;
            wheel.transform.Rotate(0, 0, rotation);
        }
    }
}
