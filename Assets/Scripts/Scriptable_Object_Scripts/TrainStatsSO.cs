using UnityEngine;
using static Train;

[CreateAssetMenu(fileName = "TrainStats_SO", menuName = "Midnight Line SOs / Train Stats SO")]
public class TrainStatsSO : ScriptableObject
{
    public Vector2 curVelocity;
    public Vector2 targetVelocity;
    public float prevPeakVelocity;
    public float targetStopPosition;
    public float targetKMPH;

    public Vector2 targetElevatePos;

    public float distToSpawnNextStation;

    public float trainToMaxSpawnDist;

    public int totalPassengersBoarded;
    public int targetPassengersBoarding;

    public int slideDoorsAmountOpened;

    public DepthSections depthSections;
    public Bounds totalBounds;

    public float[] exteriorSlideDoorPositions;    
    public float[] interiorSlideDoorPositions;    
}