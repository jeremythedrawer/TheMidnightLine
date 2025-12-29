using Proselyte.Sigils;
using UnityEngine;

[CreateAssetMenu(fileName = "GameEventDataSO", menuName = "Midnight Line SOs / Game Event Data SO")]
public class GameEventDataSO : ScriptableObject
{
    public GameEvent OnReset;
    public GameEvent OnInteract;
    public GameEvent OnBoardingSpy;
    public GameEvent OnCloseSlideDoors;
    public GameEvent OnStationArrival;
    public GameEvent OnStationLeave;
    public GameEvent OnTrainArrivedAtStartPosition;
}
