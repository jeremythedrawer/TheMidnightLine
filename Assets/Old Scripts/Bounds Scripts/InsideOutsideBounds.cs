using System.Threading.Tasks;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public abstract class InsideOutsideBounds : OldBounds
{
    [Header ("References")]
    public CarriageBounds carriageBounds;
    public LayerMask gangwayTriggerLayer;

    public GangwayBounds leftGangwayBounds { get; private set; }
    public GangwayBounds rightGangwayBounds { get; private set; }
    protected BoxCollider2D Collider2D { get; private set; }
    public UnityEngine.Bounds objectBounds { get; private set; }

    public float leftEdge { get; private set; }
    public float rightEdge { get; private set; }
    public float boundsHeight { get; private set; }

    public bool isBackCarriage { get; private set; }
    public bool isFrontCarriage { get; private set; }
    public Vector2 leftGangwayPos { get; private set; }
    public Vector2 rightGangwayPos { get; private set; }

    private Component rightComponentBounds;
    private Component leftComponentBounds;

    private void Awake()
    {
        if (carriageBounds.carriageType == CarriageBounds.CarriageType.backCarriage) isBackCarriage = true;
        if (carriageBounds.carriageType == CarriageBounds.CarriageType.frontCarriage) isFrontCarriage = true;

    }

    public virtual void Start()
    {
        Collider2D = this.GetComponent<BoxCollider2D>();
    }

    public void SetUpCarriageBounds()
    {
        objectBounds = Collider2D.bounds;

        leftEdge = Collider2D.bounds.min.x;
        rightEdge = Collider2D.bounds.max.x;
        boundsHeight = Collider2D.bounds.size.y;

        SetNeighbouringBounds(Collider2D, 5, gangwayTriggerLayer, typeof(GangwayBounds), ref leftComponentBounds, ref rightComponentBounds);
        leftGangwayBounds = leftComponentBounds as GangwayBounds;
        rightGangwayBounds = rightComponentBounds as GangwayBounds;


        if (isBackCarriage)
        {
            rightGangwayPos = rightComponentBounds.transform.position;
        }
        else if (isFrontCarriage)
        {
            leftGangwayPos = leftComponentBounds.transform.position;
        }
        else
        {
            leftGangwayPos = leftComponentBounds.transform.position;
            rightGangwayPos = rightComponentBounds.transform.position;
        }
    }
}
