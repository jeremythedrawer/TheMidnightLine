using UnityEngine;
using static Train;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    public Vector2 curVelocity;
    public Vector2 targetVelocity;
    public float prevPeakVelocity;
    public float targetPosition;
    public float targetKMPH;

    public Vector2 targetElevatePos;

    public float distToSpawnNextStation;

    public float trainToMaxSpawnDist;

    public int totalNPCsBoarded;
    public int targetNPCsToBoard;

    public int slideDoorsAmountOpened;

    public DepthSections depthSections;
    public Bounds totalBounds;
    public LayerMask activeSlideDoorsMask;

    public float[] exteriorSlideDoorPositions;    
    public float[] interiorSlideDoorPositions;    
}