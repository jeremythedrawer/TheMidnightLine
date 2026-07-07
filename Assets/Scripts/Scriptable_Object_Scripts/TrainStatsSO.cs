using System.Collections.Generic;
using UnityEngine;
using static Train;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{

    public LayerMask activeSlideDoorsMask;
    
    public Bounds totalBounds;
    
    public DepthSections depthSections;

    public float[] exteriorSlideDoorXBounds;    
    public float[] interiorSlideDoorXBounds;    

    public Vector2 curVelocity;
    public Vector2 targetVelocity;
    public Vector2 targetElevatePos;

    public float prevPeakVelocity;
    public float targetPosition;
    public float targetKMPH;
    public float targetNightValue;
    public float distToSpawnNextStation;
    public float trainToMaxSpawnDist;

    public int curStationIndex;
    public int totalNPCsBoarded;
    public int targetNPCsToBoard;
    public int slideDoorsAmountOpened;

}