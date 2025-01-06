using UnityEngine;

public class CarriageClimbingBounds : Bounds
{
    public static CarriageClimbingBounds Instance { get; private set; }
    private BoxCollider2D Collider2D;

    [Range(0f, 1f)]
    public float hangActivationThreshold = 0f;

    public bool isLeftEdge;
    public float hangThresholdLine { get; private set; }
    public bool hangActivated { get; set; }
    public bool activatedByAgent { get; set; }
    public bool activatedByPlayer { get; set; }
    public Vector2 newPos { get; private set; }
    public float boundsMaxY { get; private set; }



    private void OnDrawGizmos()
    {
        #if UNITY_EDITOR
        if (Application.isPlaying) return;

        Collider2D = this.GetComponent<BoxCollider2D>();

        //draw box collider
        Gizmos.color = activatedByAgent ? Color.green : Color.red;
        Vector2 bottomLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.min.y);
        Vector2 bottomRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.min.y);
        Vector2 topLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.max.y);
        Vector2 topRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.max.y);
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        //draw hangThresholdLine
        Gizmos.color = hangActivated ? Color.green : Color.red;
        Vector2 leftOrigin = new Vector2(Collider2D.bounds.min.x, (Collider2D.bounds.size.y) * (hangActivationThreshold - 0.5f) + transform.position.y);
        Vector2 rightOrigin = new Vector2(Collider2D.bounds.max.x, (Collider2D.bounds.size.y) * (hangActivationThreshold - 0.5f) + transform.position.y);
        Gizmos.DrawLine(rightOrigin, leftOrigin);
        #endif

    }

    void Start()
    {
        Collider2D = this.GetComponent<BoxCollider2D>();
        hangThresholdLine = (Collider2D.bounds.size.y) * (hangActivationThreshold - 0.5f) + transform.position.y;
        boundsMaxY = Collider2D.bounds.max.y;
    }

    private void FixedUpdate()
    {
#if UNITY_EDITOR
        // draw box collider
        Vector2 bottomLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.min.y);
        Vector2 bottomRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.min.y);
        Vector2 topLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.max.y);
        Vector2 topRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.max.y);
        Debug.DrawLine(bottomLeft, bottomRight, activatedByAgent || activatedByPlayer ? Color.green : Color.red);
        Debug.DrawLine(bottomRight, topRight, activatedByAgent || activatedByPlayer ? Color.green : Color.red);
        Debug.DrawLine(topRight, topLeft, activatedByAgent || activatedByPlayer ? Color.green : Color.red);
        Debug.DrawLine(topLeft, bottomLeft, activatedByAgent || activatedByPlayer ? Color.green : Color.red);

        //draw hangThresholdLine
        Vector2 leftOrigin = new Vector2(Collider2D.bounds.min.x, hangThresholdLine);
        Vector2 rightOrigin = new Vector2(Collider2D.bounds.max.x, hangThresholdLine);
        Debug.DrawLine(rightOrigin, leftOrigin, hangActivated ? Color.green : Color.red);
#endif
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isLeftEdge)
        {
            newPos = new Vector2(Collider2D.bounds.min.x, hangThresholdLine - collision.bounds.size.y);  
        }
        else
        {
            newPos = new Vector2(Collider2D.bounds.max.x, hangThresholdLine - collision.bounds.size.y);
        }
        activatedByPlayer =  collision.gameObject.CompareTag("Player Collider");
        activatedByAgent = collision.gameObject.CompareTag("Agent Collider");

        if (activatedByPlayer || activatedByAgent)
        {
            var core = collision.gameObject.GetComponentInParent<StateCore>();
            bool cancelInit = core.stateList.wallState.isDropping || core.stateList.wallState.hasClimbed;

            if (!cancelInit)
            {
                core.currentClimbBounds = this;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        HangActivationThresholdDetection(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        activatedByPlayer = collision.gameObject.CompareTag("Player Collider");
        activatedByAgent = collision.gameObject.CompareTag("Agent Collider");

        if (activatedByPlayer || activatedByAgent)
        {
            var core = collision.gameObject.GetComponentInParent<StateCore>();
            if (core == null) return;

            core.currentClimbBounds = null;
        }

    }

    private void HangActivationThresholdDetection(Collider2D collision)
    {
        if (collision.bounds.max.y <= hangThresholdLine)
        {
            hangActivated = true;
        }
    }
}
