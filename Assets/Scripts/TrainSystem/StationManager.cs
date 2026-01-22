using Proselyte.Sigils;
using System;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    public StationsDataSO stationsData;
    public TrainStatsSO trainStats;
    public SpawnerStatsSO spawnerStats;
    public GameEventDataSO gameEventData;

    private void OnEnable()
    {
        gameEventData.OnStationLeave.RegisterListener(InstaniateStation);
    }

    private void OnDisable()
    {
        gameEventData.OnStationLeave.UnregisterListener(InstaniateStation);
    }

    private void InstaniateStation()
    {
        Station nextStationPrefab = stationsData.stations[trainStats.nextStationIndex].stationPrefab;
        Station nextStation = Instantiate(nextStationPrefab, transform);
        float stationXPos = spawnerStats.bottomLeftFront.x + (nextStation.transform.position.x - nextStation.components.platformCollider.bounds.min.x);
        nextStation.transform.position = new Vector3(stationXPos, 0, 0);
    }
}
