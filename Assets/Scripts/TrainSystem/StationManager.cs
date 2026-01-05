using Proselyte.Sigils;
using System;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public StationsDataSO stationsData;
        public TrainStatsSO trainStats;
        public MaxBoundsSO maxBounds;
        public GameEventDataSO gameEventData;
    }
    [SerializeField] SOData soData;

    private void OnEnable()
    {
        soData.gameEventData.OnStationLeave.RegisterListener(InstaniateStation);
    }

    private void OnDisable()
    {
        soData.gameEventData.OnStationLeave.UnregisterListener(InstaniateStation);
    }

    private void InstaniateStation()
    {
        Station nextStationPrefab = soData.stationsData.stations[soData.trainStats.nextStationIndex].stationPrefab;
        Station nextStation = Instantiate(nextStationPrefab, transform);
        float stationXPos = soData.maxBounds.max.x + (nextStation.transform.position.x - nextStation.components.platformCollider.bounds.min.x);
        nextStation.transform.position = new Vector3(stationXPos, 0, 0);
    }
}
