using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Train;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    public int totalTicketsChecked;
    public int ticketsCheckedSinceLastStation;

    public float curVelocity;
    public float targetVelocity;
    
    public float distToSpawnNextStation;
    public float distToBreak;
    public float distanceToNextStation;

    public float trainToMaxSpawnDist;

    public int curPassengersBoarded;
    public int targetPassengersBoarding;

    public int slideDoorsAmountOpened;

    public DepthSections depthSections;
    public Bounds totalBounds;

    public float[] slideDoorPositions;

    public Dictionary<Collider2D, Carriage> carriageDict;

}