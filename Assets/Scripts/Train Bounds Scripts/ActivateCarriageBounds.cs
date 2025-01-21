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
    public UnityEngine.Bounds Bounds { get; private set; }
    public bool playerInActiveArea {  get; private set; }
    public int bystanderCount { get; private set; }
    public float leftEdge {  get; private set; }
    public float rightEdge { get; private set; }
    public float boundsHeight { get; private set; }

    private Component leftComponentBounds;
    private Component rightComponentBounds;

    public GangwayBounds leftGangwayBounds { get; private set; }
    public GangwayBounds rightGangwayBounds { get; private set; }
    public Vector2 leftGangwayPos { get; private set; }
    public Vector2 rightGangwayPos { get; private set; }


    private void Start()
    {
        Collider2D = this.GetComponent<BoxCollider2D>();
        Bounds = Collider2D.bounds;

        leftEdge = Collider2D.bounds.min.x;
        rightEdge = Collider2D.bounds.max.x;
        boundsHeight = Collider2D.bounds.size.y;
        bystanderCount = 0;

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

        if (collision.gameObject.CompareTag("Agent Collider"))
        {
            var pathData = collision.gameObject.GetComponentInParent<PathData>();
            if (pathData != null)
            {
                if (this.CompareTag("Inside Bounds"))
                {
                    pathData.currentInsideBounds = this;
                }
                else if (this.CompareTag("Outside Bounds"))
                {
                    pathData.currentOutsideBounds = this;
                }
            }
            else
            {
                Debug.LogWarning("no navsystem was found");
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
            var pathData = collision.gameObject.GetComponentInParent<PathData>();
            if (pathData != null)
            {
                pathData.currentInsideBounds = null;
                pathData.currentOutsideBounds = null;
            }
        }
        if (collision.gameObject.CompareTag("Bystander Collider"))
        {
            bystanderCount--;
        }
    }
}
