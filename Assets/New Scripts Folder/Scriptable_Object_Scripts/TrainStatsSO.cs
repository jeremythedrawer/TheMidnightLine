using UnityEngine;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    internal int curStationIndex = 0;
    internal float curKMPerHour = 0;
    internal float curMPerSec = 0;
    public float targetKMPerHour = 10;
    internal float metersTravelled;
    internal bool arrivedAtStartPos;
    internal float distanceToNextStation;
    internal float stoppingDistance;
    internal float startXPos;
    internal float curCenterXPos;
    internal float halfXSize;

    internal bool boardPassengers;

    public float doorMovingTime = 2.0f;
    internal float wheelCircumference;
}
