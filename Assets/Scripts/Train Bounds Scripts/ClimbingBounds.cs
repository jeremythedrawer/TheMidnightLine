using UnityEngine;

public class ClimbingBounds : Bounds
{
    private BoxCollider2D Collider2D => this.GetComponent<BoxCollider2D>();

    [Range(0f, 1f)]
    public float hangActivationThreshold = 0f;

    public bool isLeftEdge;
    public float hangThresholdLine => (Collider2D.bounds.size.y) * (hangActivationThreshold - 0.5f) + transform.position.y;
    public bool hangActivated { get; set; }
    public bool activatedByAgent { get; set; }
    public bool activatedByPlayer { get; set; }

    public float newPosX => isLeftEdge ? Collider2D.bounds.min.x : Collider2D.bounds.max.x;
    public Vector2 newPos {  get; private set; }
    public float boundsMaxY => Collider2D.bounds.max.y;


    private StateCore activeCharacter;
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;

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
        newPos = new Vector2(newPosX, hangThresholdLine - collision.bounds.size.y);

        activatedByPlayer =  collision.gameObject.CompareTag("Player Collider");
        activatedByAgent = collision.gameObject.CompareTag("Agent Collider");

        if (activatedByPlayer || activatedByAgent)
        {
            activeCharacter = collision.gameObject.GetComponentInParent<StateCore>();
            if (activeCharacter.onTrain) activeCharacter.currentClimbBounds = this;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        hangActivated = collision.bounds.max.y <= hangThresholdLine;
        if (activatedByPlayer || activatedByAgent)
        {
            if (activeCharacter.movementInputs.crouchInput) activeCharacter.isDropping = true;
            if (activeCharacter.isDropping) activeCharacter.currentClimbBounds = null;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (activatedByPlayer || activatedByAgent)
        {
            activeCharacter.isDropping = false;
            activeCharacter.currentClimbBounds = null;
        }
    }
}
