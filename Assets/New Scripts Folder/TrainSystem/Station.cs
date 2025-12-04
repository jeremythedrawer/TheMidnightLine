using Proselyte.Sigils;
using System;
using UnityEngine;

public class Station : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public StationSO station;
        public TrainStatsSO trainStats;
    }
    [SerializeField] public SOData soData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnStationLeave;
    }
    [SerializeField] GameEventData gameEvents;
    private void OnEnable()
    {
        gameEvents.OnStationLeave.RegisterListener(SpawnNPCs);
    }

    private void OnDisable()
    {
        gameEvents.OnStationLeave.UnregisterListener(SpawnNPCs);
    }
    private void SpawnNPCs()
    {

    }
}
