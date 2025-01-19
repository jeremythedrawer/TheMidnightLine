using System.Collections.Generic;
using UnityEngine;

public class PathData : MonoBehaviour
{
    [Header("References")]
    public TrainBounds trainBounds;

    public ActivateCarriageBounds currentInsideBounds { get; set; }
    public GangwayBounds currentGangwayBounds { get; set; }
    public ActivateCarriageBounds currentOutsideBounds { get; set; }

    public GangwayBounds chosenGangway { get; set; }
    public CarriageClimbingBounds chosenClimbingBounds { get; set; }
    public ActivateCarriageBounds chosenInsideBounds { get; set; }
    public List<NamedPosition> pathToTarget { get; set; } = new List<NamedPosition>();
    public enum PosType
    {
        InsideBound,
        OutsideBound,
        Gangway,
        ClimbingBound,
        RoofEdge,
        Target
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
}
