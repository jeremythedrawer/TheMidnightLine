using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Train;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    public float curVelocity;
    public float targetVelocity;
    public float prevPeakVelocity;
    public float targetStopPosition;

    public float distToSpawnNextStation;

    public float trainToMaxSpawnDist;

    public int totalPassengersBoarded;
    public int targetPassengersBoarding;

    public int slideDoorsAmountOpened;

    public DepthSections depthSections;
    public Bounds totalBounds;

    public float[] slideDoorPositions;

    public Dictionary<Collider2D, Carriage> carriageDict;

}