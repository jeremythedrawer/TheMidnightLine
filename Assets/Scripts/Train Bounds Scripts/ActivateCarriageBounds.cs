using UnityEngine;
using System;

public class ActivateCarriageBounds : Bounds
{
    public static ActivateCarriageBounds Instance { get; private set; }

    public LayerMask gangwayTriggerLayer;
    public bool isBackCarriage;
    public bool isFrontCarriage;
    private BoxCollider2D Collider2D;

    public int bystanderCount { get; private set; }
    public float leftEdge {  get; private set; }
    public float rightEdge { get; private set; }
    public float boundsHeight { get; private set; }

    public bool instanceActivated { get; private set; } = false;
    private Component leftGangwayBounds;
    private Component rightGangwayBounds;

    public Vector2 leftGangwayPos { get; private set; }
    public Vector2 rightGangwayPos { get; private set; }


    private void Start()
    {
        Collider2D = this.GetComponent<BoxCollider2D>();

        leftEdge = Collider2D.bounds.min.x;
        rightEdge = Collider2D.bounds.max.x;
        boundsHeight = Collider2D.bounds.size.y;
        bystanderCount = 0;

        if (this.gameObject.CompareTag("Inside Bounds")) 
        {
            SetNeighbouringBounds(Collider2D, 5, gangwayTriggerLayer, typeof(GangwayBounds), ref leftGangwayBounds, ref rightGangwayBounds);
            if (isBackCarriage)
            {
                rightGangwayPos = rightGangwayBounds.transform.position;
            }
            else if (isFrontCarriage)
            {
                leftGangwayPos = leftGangwayBounds.transform.position;
            }
            else
            {
                leftGangwayPos = leftGangwayBounds.transform.position;
                rightGangwayPos = rightGangwayBounds.transform.position;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.gameObject.CompareTag("Bystander Collider"))
        {
            bystanderCount++;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider") || collision.gameObject.CompareTag("Agent Collider"))
        {
            Instance = this;
            instanceActivated = true;
        }
        
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider") || collision.gameObject.CompareTag("Agent Collider"))
        {
            Instance = null;
            instanceActivated = false;
        }
        if (collision.gameObject.CompareTag("Bystander Collider"))
        {
            bystanderCount--;
        }
    }
}
