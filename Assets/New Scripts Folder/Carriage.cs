using Proselyte.Sigils;
using System;
using UnityEngine;

public class Carriage : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public TrainStatsSO trainStats;
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

}
