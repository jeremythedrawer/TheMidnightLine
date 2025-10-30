using Proselyte.Sigils;
using System;
using UnityEngine;

public class Carriage : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public TrainStatsSO trainStats;
        public TrainSettingsSO trainSettings;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct ComponentData
    {
        public Transform[] wheelTransforms;
    }
    [SerializeField] ComponentData componentData;
    [Serializable] public struct GameEventData
    {
        public GameEvent OnReset;
    }
    [SerializeField] GameEventData gameEventData;

    private void OnEnable()
    {
        ResetStats();
        gameEventData.OnReset.RegisterListener(ResetStats);

    }

    private void OnDisable()
    {
        gameEventData.OnReset.UnregisterListener(ResetStats);

    }

    private void Start()
    {

    }
    private void Update()
    {
        float metersMoved = soData.trainStats.curMPerSec * Time.deltaTime;
        float wheelRotation = -metersMoved * soData.trainStats.degPerMeter;

        foreach (Transform wheel in componentData.wheelTransforms)
        {
            wheel.transform.Rotate(0, 0, wheelRotation);
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
