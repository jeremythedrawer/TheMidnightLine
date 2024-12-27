using UnityEngine;

public class CarriageClimbingBounds : MonoBehaviour
{
    public static CarriageClimbingBounds Instance { get; private set; }
    private BoxCollider2D Collider2D;

    public bool activated {  get; set; }

    private void OnDrawGizmos()
    {
        #if UNITY_EDITOR
        if (Application.isPlaying) return;

        Collider2D = this.GetComponent<BoxCollider2D>();

        Gizmos.color = activated ? Color.green : Color.red;
        Vector2 bottomLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.min.y);
        Vector2 bottomRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.min.y);
        Vector2 topLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.max.y);
        Vector2 topRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.max.y);
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        #endif

    }

    void Start()
    {
        Collider2D = this.GetComponent<BoxCollider2D>();



    }

    private void FixedUpdate()
    {
#if UNITY_EDITOR
        Vector2 bottomLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.min.y);
        Vector2 bottomRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.min.y);
        Vector2 topLeft = new Vector2(Collider2D.bounds.min.x, Collider2D.bounds.max.y);
        Vector2 topRight = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.max.y);
        Debug.DrawLine(bottomLeft, bottomRight, activated ? Color.green : Color.red);
        Debug.DrawLine(bottomRight, topRight, activated ? Color.green : Color.red);
        Debug.DrawLine(topRight, topLeft, activated ? Color.green : Color.red);
        Debug.DrawLine(topLeft, bottomLeft, activated ? Color.green : Color.red);
#endif
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider") || collision.gameObject.CompareTag("Agent Collider"))
        {
            Instance = this;
            activated = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider") || collision.gameObject.CompareTag("Agent Collider"))
        {
            Instance = null;
            activated = false;
        }
    }

    //TODO: adjust side origins to teleport the players x position towards for hanging on the left or right side
}
