using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    const float KM_TO_MPS = 0.27777777778f;
    
    internal int nextStationIndex = 0;
    internal float curKMPerHour = 0;
    internal float targetKMPerHour = 10;
    internal float metersTravelled;

    internal float distToNextStation;
    internal float stoppingDistance;
    internal float startMinXPos;
    internal float startCenterXPos;
    internal float trainLength;
    internal float trainMaxHeight;
    internal float trainHalfLength;

    internal bool closingDoors;
    internal float wheelCircumference;

    internal int curPassengerCount;
    internal int targetPassengerCount;

    internal List<Vector2> slideDoorPositions = new List<Vector2>();
    internal SlideDoors.Type slideDoorsToUnlock;
    public float GetMetersPerSecond()
    {
        return curKMPerHour * KM_TO_MPS;
    }
}
