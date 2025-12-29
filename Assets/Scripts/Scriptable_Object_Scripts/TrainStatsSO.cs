using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    const float KM_TO_MPS = 0.27777777778f;
    
    internal int nextStationIndex;
    internal float curKMPerHour;
    internal float targetKMPerHour;
    internal float metersTravelled;
    internal float accellation2;
    internal float distToNextStation;
    internal float brakeDist;
    internal float startXPos;
    internal float trainLength;
    internal float trainMaxHeight;
    internal float trainHalfLength;

    internal bool closingDoors;
    internal float wheelCircumference;

    internal int curPassengerCount;
    internal int targetPassengerCount;

    internal List<Vector2> slideDoorPositions = new List<Vector2>();
    internal StationSO curStation;
    public float GetMetersPerSecond(float kmph)
    {
        return kmph * KM_TO_MPS;
    }
}
