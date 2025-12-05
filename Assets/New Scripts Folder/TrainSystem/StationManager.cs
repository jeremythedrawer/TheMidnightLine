using Proselyte.Sigils;
using System;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public StationsDataSO stationsData;
        public TrainStatsSO trainStats;
        public MaxBoundsStatsSO maxBounds;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnStationLeave;
    }
    [SerializeField] GameEventData gameEvents;

    private void OnEnable()
    {
        gameEvents.OnStationLeave.RegisterListener(InstaniateStation);
    }

    private void OnDisable()
    {
        gameEvents.OnStationLeave.UnregisterListener(InstaniateStation);
    }

    private void InstaniateStation()
    {
        Station nextStation = soData.stationsData.stations[soData.trainStats.nextStationIndex].stationPrefab;
        float stationXPos = soData.maxBounds.max.x + nextStation.components.platformCollider.bounds.extents.x;
        Vector3 stationPos = new Vector3(stationXPos, 0, 0);
        Instantiate(nextStation, stationPos, Quaternion.identity, null);
    }
}
