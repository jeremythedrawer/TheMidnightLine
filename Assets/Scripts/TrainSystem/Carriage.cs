using Proselyte.Sigils;
using System;
using UnityEngine;

public class Carriage : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public TrainStatsSO trainStats;
        public TrainSettingsSO trainSettings;
        public GameEventDataSO gameEventData;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct ComponentData
    {
        public Transform[] wheelTransforms;
    }
    [SerializeField] ComponentData componentData;

    private void OnEnable()
    {
        ResetStats();
        soData.gameEventData.OnReset.RegisterListener(ResetStats);

    }

    private void OnDisable()
    {
        soData.gameEventData.OnReset.UnregisterListener(ResetStats);

    }

    private void Update()
    {
        if (soData.trainStats.wheelCircumference <= 0f) return;

        float wheelRotation = (soData.trainStats.metersTravelled / soData.trainStats.wheelCircumference) * 360f;

        wheelRotation %= 360f;

        foreach (Transform wheel in componentData.wheelTransforms)
        {
            wheel.localRotation = Quaternion.Euler(0f, 0f, -wheelRotation);
        }
    }
    private void ResetStats()
    {
    }
    private void OnApplicationQuit()
    {
        ResetStats();
    }

    private void OnDrawGizmos()
    {
        if (soData.trainSettings == null) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y + 2, soData.trainSettings.entityDepthRange.x), new Vector3(transform.position.x, transform.position.y + 2, soData.trainSettings.entityDepthRange.y));
    }
}
