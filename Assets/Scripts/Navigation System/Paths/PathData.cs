using System.Collections.Generic;
using UnityEngine;

public class PathData : MonoBehaviour
{
    public TrainData trainData {  get; private set; }
    public CollisionChecker collisionChecker { get; private set; }
    public InsideBounds currentInsideBounds { get; set; }
    public OutsideBounds currentOutsideBounds { get; set; }
    public GangwayBounds currentGangwayBounds { get; set; }

    public InsideBounds chosenInsideBounds { get; set; }
    public OutsideBounds chosenOutsideBounds { get; set; }
    public GangwayBounds chosenGangwayBounds { get; set; }
    public ClimbingBounds chosenClimbingBounds { get; set; }
    public SeatBounds chosenSeatBounds { get; set; }
    public SlideDoorBounds chosenSlideDoorBounds { get; set; }
    public ExitBounds chosenExitBounds { get; set; }
    public enum PosType
    {
        InsideBound,
        OutsideBound,
        GangwayBound,
        ClimbingBound,
        RoofEdge,
        Seat,
        Stand,
        SlidingDoors,
        ExitBound,
        DisableBound,
        Player
    }

    [System.Serializable]
    public struct NamedPosition
    {
        public Vector2 value;
        public PosType type;

        public NamedPosition(Vector2 value, PosType type)
        {
            this.value = value;
            this.type = type;
        }
    }
    public List<NamedPosition> pathToTarget { get; set; } = new List<NamedPosition>();
    public bool pathIsSet { get; set; }

    private void Awake()
    {
        trainData = GameObject.FindGameObjectWithTag("Train Object").GetComponent<TrainData>();
        collisionChecker = gameObject.GetComponent<CollisionChecker>();
    }
}
