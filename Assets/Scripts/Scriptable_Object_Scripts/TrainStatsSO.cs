using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    const float KM_TO_MPS = 0.27777777778f;
    
    public int nextStationIndex;
    public float curKMPerHour;
    public float targetKMPerHour;
    public float metersTravelled;
    public float curVelocity;
    public float distToNextStation;
    public float distToSpawnTrain;
    public float brakeDist;
    public float startXPos;
    public float trainLength;
    public float trainMaxHeight;
    public float trainHalfLength;

    public bool closingDoors;
    public float wheelCircumference;

    public int curPassengerCount;
    public int targetPassengerCount;

    public StationSO curStation;
    public float GetMetersPerSecond(float kmph)
    {
        return kmph * KM_TO_MPS;
    }
}
