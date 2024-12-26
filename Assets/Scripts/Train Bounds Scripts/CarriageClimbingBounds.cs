using UnityEngine;

public class CarriageClimbingBounds : MonoBehaviour
{
    public static CarriageClimbingBounds Instance { get; private set; }
    private BoxCollider2D Collider2D;

    public bool activated {  get; private set; }

    void Start()
    {
        Collider2D = this.GetComponent<BoxCollider2D>();
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
