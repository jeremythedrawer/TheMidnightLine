using Proselyte.Sigils;
using UnityEngine;

[CreateAssetMenu(fileName = "GameEventDataSO", menuName = "Midnight Line SOs / Game Event Data SO")]
public class GameEventData : ScriptableObject
{
    public GameEvent OnStartTrip;
    public GameEvent OnResetTrip;

    public GameEvent OnStationArrival;
    public GameEvent OnStationLeave;
    public GameEvent OnTrainDeceleration;
    public GameEvent OnMetersAtSpawnBounds;
    public GameEvent OnStationSpawn;

    public GameEvent OnFinishTripScene;
}
