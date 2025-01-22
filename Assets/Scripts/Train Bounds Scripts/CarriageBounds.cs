using UnityEngine;

public abstract class CarriageBounds : Bounds
{
    public LayerMask gangwayTriggerLayer;
    public bool isBackCarriage;
    public bool isFrontCarriage;

    protected BoxCollider2D Collider2D;
    public UnityEngine.Bounds Bounds { get; private set; }

    public float leftEdge { get; private set; }
    public float rightEdge { get; private set; }
    public float boundsHeight { get; private set; }

    protected Component leftComponentBounds;
    protected Component rightComponentBounds;

    public GangwayBounds leftGangwayBounds { get; private set; }
    public GangwayBounds rightGangwayBounds { get; private set; }
    public Vector2 leftGangwayPos { get; private set; }
    public Vector2 rightGangwayPos { get; private set; }

    protected void SetUpCarriageBounds()
    {
        Collider2D = this.GetComponent<BoxCollider2D>();
        Bounds = Collider2D.bounds;

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
