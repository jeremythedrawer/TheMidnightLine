using Proselyte.Sigils;
using UnityEngine;

[CreateAssetMenu(fileName = "GameEventDataSO", menuName = "Midnight Line SOs / Game Event Data SO")]
public class GameEventDataSO : ScriptableObject
{
    public GameEvent OnReset;
    public GameEvent OnInteract;
    public GameEvent OnStationArrival;
    public GameEvent OnTrainArrivedAtStartPosition;
    public GameEvent OnTrainDeceleration;
}
