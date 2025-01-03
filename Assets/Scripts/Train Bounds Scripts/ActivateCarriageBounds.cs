using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ActivateCarriageBounds : Bounds
{
    public static ActivateCarriageBounds Instance { get; private set; }

    public LayerMask gangwayTriggerLayer;
    public bool isBackCarriage;
    public bool isFrontCarriage;

    private BoxCollider2D Collider2D;

    public bool playerInActiveArea {  get; private set; }
    public int bystanderCount { get; private set; }
    public float leftEdge {  get; private set; }
    public float rightEdge { get; private set; }
    public float boundsHeight { get; private set; }

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
        if (collision.gameObject.CompareTag("Player Collider"))
        {
            Instance = this;
            playerInActiveArea = true;
        }

        if (collision.gameObject.CompareTag("Agent Collider") && this.CompareTag("Inside Bounds"))
        {
            var navSystem = collision.gameObject.GetComponentInParent<NavigationSystem>();
            if (navSystem != null)
            {
                navSystem.currentInsideBounds = this;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider"))
        {
            Instance = null;
            playerInActiveArea = false;
        }
        if (collision.gameObject.CompareTag("Agent Collider"))
        {
            var navSystem = collision.gameObject.GetComponentInParent<NavigationSystem>();

            if(navSystem != null)
            {
                navSystem.currentInsideBounds = null;
            }
        }
        if (collision.gameObject.CompareTag("Bystander Collider"))
        {
            bystanderCount--;
        }
    }
}
