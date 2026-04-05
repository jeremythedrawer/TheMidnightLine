using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Train;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    public float metersTravelled;

    public float curVelocity;
    public float targetVelocity;
    
    public float distToNextStation;
    public float distToSpawnNextStation;
    public float trainToMaxSpawnDist;
    public float brakePos;

    public int curPassengersBoarded;
    public int targetPassengersBoarding;

    public int slideDoorsAmountOpened;

    public DepthSections depthSections;
    public Bounds totalBounds;

    public float[] slideDoorPositions;

    public Dictionary<Collider2D, Carriage> carriageDict;

}