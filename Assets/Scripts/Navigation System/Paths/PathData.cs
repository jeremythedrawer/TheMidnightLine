using System.Collections.Generic;
using UnityEngine;

public class PathData : MonoBehaviour
{
    [Header("References")]
    public TrainBounds trainBounds;

    public InsideBounds currentInsideBounds { get; set; }
    public OutsideBounds currentOutsideBounds { get; set; }
    public GangwayBounds currentGangwayBounds { get; set; }

    public InsideBounds chosenInsideBounds { get; set; }
    public OutsideBounds chosenOutsideBounds { get; set; }
    public GangwayBounds chosenGangwayBounds { get; set; }
    public ClimbingBounds chosenClimbingBounds { get; set; }
    public SeatBounds chosenSeatBounds { get; set; }
    public enum PosType
    {
        InsideBound,
        OutsideBound,
        GangwayBound,
        ClimbingBound,
        RoofEdge,
        Seat,
        Stand,
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
}
