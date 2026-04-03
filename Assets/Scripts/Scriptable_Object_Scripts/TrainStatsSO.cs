using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    const float KM_TO_MPS = 0.27777777778f;

    public float curKMPerHour;
    public float targetKMPerHour;
    public float metersTravelled;
    public float curVelocity;
    public float distToNextStation;
    public float brakeDist;
    public float startXPos;
    public float trainMaxHeight;
    public float trainWorldWidth;
    public float trainBackXPos;
    public bool closingDoors;

    public int curPassengerCount;
    public int targetPassengerCount;

    public int minDepth;
    public int maxDepth;

    public int depthSection_front_min;
    public int depthSection_front_max;
    public int depthSection_back_min;
    public int depthSection_back_max;
    public int depthSection_carriageSeat;

    public float[] slideDoorPositions;

    public Dictionary<Collider2D, Carriage> carriageDict;
    public float GetMetersPerSecond(float kmph)
    {
        return kmph * KM_TO_MPS;
    }
}
