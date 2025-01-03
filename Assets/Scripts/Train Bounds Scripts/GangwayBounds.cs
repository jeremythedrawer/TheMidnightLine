using System.Collections.Generic;
using UnityEngine;

public class GangwayBounds : Bounds
{
    public LayerMask OutsideBoundsLayer;
    public LayerMask InsideBoundsLayer;
    public bool seeGizmos;

    private Collider2D Collider2D;

    private Component leftOutsideBounds;
    private Component rightOutsideBounds;
    private Component leftInsideBounds;
    private Component rightInsideBounds;

    public bool playerOnLeftRoof {  get; private set; }
    public bool playerOnRightRoof { get; private set; }
    public bool playerInLeftCarriage { get; private set; }
    public bool playerInRightCarriage { get; private set; }

    private void Start()
    {
        Collider2D = GetComponent<Collider2D>();

        float detectionSizeBuffer = 5;
        SetNeighbouringBounds(Collider2D, detectionSizeBuffer, OutsideBoundsLayer, typeof(ActivateCarriageBounds), ref leftOutsideBounds, ref rightOutsideBounds);
        SetNeighbouringBounds(Collider2D, detectionSizeBuffer, InsideBoundsLayer, typeof(ActivateCarriageBounds), ref leftInsideBounds, ref rightInsideBounds);
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (seeGizmos)
        {
            Collider2D = GetComponent<Collider2D>();
            float dectectionBuffer = 5f;

            Vector2 LeftPointA = Collider2D.bounds.min;
            Vector2 LeftPointB = new Vector2(LeftPointA.x - dectectionBuffer, Collider2D.bounds.max.y + dectectionBuffer);

            Vector2 RightPointA = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.max.y + dectectionBuffer);
            Vector2 RightPointB = new Vector2(RightPointA.x + dectectionBuffer, Collider2D.bounds.min.y);

            Collider2D leftCollider = Physics2D.OverlapArea(LeftPointA, LeftPointB, OutsideBoundsLayer);
            Collider2D righCollider = Physics2D.OverlapArea(RightPointA, RightPointB, OutsideBoundsLayer);
            Helpers.DrawBoxCastDebug(LeftPointA, LeftPointB, leftCollider != null ? Color.green : Color.red);
            Helpers.DrawBoxCastDebug(RightPointA, RightPointB, righCollider != null ? Color.green : Color.red);
        }
#endif
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Agent Collider"))
        {
            var navSystem = collision.gameObject.GetComponentInParent<NavigationSystem>();
            if (navSystem != null)
            {
                navSystem.currentGangwayBounds = this;
            }
        }
    }

    private void OnTriggerStay2D (Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Agent Collider"))
        {
            var foundRightOutsideBounds = rightOutsideBounds as ActivateCarriageBounds;
            playerOnRightRoof = foundRightOutsideBounds.playerInActiveArea;

            var foundLeftOutsideBounds = leftOutsideBounds as ActivateCarriageBounds;
            playerOnLeftRoof = foundLeftOutsideBounds.playerInActiveArea;

            var foundRightInsideBounds = rightInsideBounds as ActivateCarriageBounds;
            playerInRightCarriage = foundRightInsideBounds.playerInActiveArea;

            var foundLeftInsideBounds = leftInsideBounds as ActivateCarriageBounds;
            playerInLeftCarriage = foundLeftInsideBounds.playerInActiveArea;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Agent Collider"))
        {
            var navSystem = collision.gameObject.GetComponentInParent<NavigationSystem>();

            if (navSystem != null)
            {
                navSystem.currentGangwayBounds = null;
            }
        }
    }
}
