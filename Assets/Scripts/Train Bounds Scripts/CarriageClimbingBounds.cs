using UnityEngine;

public class CarriageClimbingBounds : MonoBehaviour
{
    public static CarriageClimbingBounds Instance { get; private set; }
    private BoxCollider2D Collider2D;

    [Range(0f, 1f)]
    public float hangActivationThreshold = 0f;

    public bool isLeftEdge;
    private float hangThresholdLine;
    public bool hangActivated { get; set; }
    public bool activated { get; set; }

    public Vector2 newPos { get; private set; }

    public float boxHeight { get; private set; }

    private bool isLeftCollision;
    private bool isRightCollision;
    private bool isBottomCollision;

    private void OnDrawGizmos()
    {
        #if UNITY_EDITOR
        if (Application.isPlaying) return;

        Collider2D = this.GetComponent<BoxCollider2D>();

        //draw box collider
        Gizmos.color = activated ? Color.green : Color.red;
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
        boxHeight = Collider2D.size.y;
    }

    private void FixedUpdate()
    {
#if UNITY_EDITOR
        // draw box collider
        Vector2 bottomLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.min.y);
        Vector2 bottomRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.min.y);
        Vector2 topLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.max.y);
        Vector2 topRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.max.y);
        Debug.DrawLine(bottomLeft, bottomRight, activated ? Color.green : Color.red);
        Debug.DrawLine(bottomRight, topRight, activated ? Color.green : Color.red);
        Debug.DrawLine(topRight, topLeft, activated ? Color.green : Color.red);
        Debug.DrawLine(topLeft, bottomLeft, activated ? Color.green : Color.red);

        //draw hangThresholdLine
        Vector2 leftOrigin = new Vector2(Collider2D.bounds.min.x, hangThresholdLine);
        Vector2 rightOrigin = new Vector2(Collider2D.bounds.max.x, hangThresholdLine);
        Debug.DrawLine(rightOrigin, leftOrigin, hangActivated ? Color.green : Color.red);
#endif
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        newPos = new Vector2(Collider2D.bounds.min.x, hangThresholdLine - collision.bounds.size.y);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TriggerInstance(collision);
        HangActivationThresholdDetection(collision);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider") || collision.gameObject.CompareTag("Agent Collider"))
        {
            Instance = null;
            activated = false;
            hangActivated = false;

            isLeftCollision = false;
            isRightCollision = false;
            isBottomCollision = false;
        }
    }

    private void TriggerInstance(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider") || collision.gameObject.CompareTag("Agent Collider"))
        {
            float thisColliderTopBound = GetComponent<Collider2D>().bounds.max.y;
            float collisionBottomBound = collision.bounds.min.y;

            if (collisionBottomBound >= thisColliderTopBound)
            {
                return;
            }

            Instance = this;
            activated = true;
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
