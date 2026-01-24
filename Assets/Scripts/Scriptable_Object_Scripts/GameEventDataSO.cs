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
    public GameEvent OnStartTutorial;
    public GameEvent OnGameFadeOut;
    public GameEvent OnGameFadeIn;
    public GameEvent OnStopSpyMovement;
    public GameEvent OnStartSpyMovement;
    public GameEvent OnFlipDownPage;
    public GameEvent OnFlipUpPage;
    public GameEvent OnBackgroundChange;
    public GameEvent OnSpawnStation;
}
