using UnityEngine;


[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    internal float easeOutTime;
    internal float easeInTime;

    internal int curStationIndex = 0;
    internal float curKMPerHour = 0;
    internal float targetKMPerHour = 10;
    internal float metersTravelled;
    internal bool arrivedAtStartPos;
    internal float distanceToNextStation;
}
