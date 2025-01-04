using System.Collections.Generic;
using UnityEngine;

public class GangwayBounds : Bounds
{
    public LayerMask outsideBoundsLayer;
    public LayerMask insideBoundsLayer;
    public LayerMask climbingBoundsLayer;
    public bool seeGizmos;

    public Collider2D Collider2D {  get; private set; }
    public UnityEngine.Bounds Bounds { get; private set; }

    private Component leftOutsideBounds;
    private Component rightOutsideBounds;
    public ActivateCarriageBounds foundLeftOutsideBounds { get; private set; }
    public ActivateCarriageBounds foundRightOutsideBounds { get; private set; }

    private Component leftInsideBounds;
    private Component rightInsideBounds;
    public ActivateCarriageBounds foundLeftInsideBounds { get; private set; }
    public ActivateCarriageBounds foundRightInsideBounds { get; private set; }

    private Component leftClimbingBounds;
    private Component rightClimbingBounds;
    public CarriageClimbingBounds foundsLeftClimbBounds { get; private set; }
    public CarriageClimbingBounds foundsRightClimbBounds { get; private set; }

    public bool playerOnLeftRoof {  get; private set; }
    public bool playerOnRightRoof { get; private set; }
    public bool playerInLeftCarriage { get; private set; }
    public bool playerInRightCarriage { get; private set; }
    public bool leftClimbActivated { get; private set; }
    public bool righhtClimbActivated { get; private set; }

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

            Collider2D leftCollider = Physics2D.OverlapArea(LeftPointA, LeftPointB, outsideBoundsLayer);
            Collider2D righCollider = Physics2D.OverlapArea(RightPointA, RightPointB, outsideBoundsLayer);
            Helpers.DrawBoxCastDebug(LeftPointA, LeftPointB, leftCollider != null ? Color.green : Color.red);
            Helpers.DrawBoxCastDebug(RightPointA, RightPointB, righCollider != null ? Color.green : Color.red);
        }
#endif
        
    }
    private void Start()
    {
        Collider2D = GetComponent<Collider2D>();
        Bounds = Collider2D.bounds;

        float detectionSizeBuffer = 5;
        SetNeighbouringBounds(Collider2D, detectionSizeBuffer, outsideBoundsLayer, typeof(ActivateCarriageBounds), ref leftOutsideBounds, ref rightOutsideBounds);
        SetNeighbouringBounds(Collider2D, detectionSizeBuffer, insideBoundsLayer, typeof(ActivateCarriageBounds), ref leftInsideBounds, ref rightInsideBounds);
        SetNeighbouringBounds(Collider2D, detectionSizeBuffer, climbingBoundsLayer, typeof(CarriageClimbingBounds), ref leftClimbingBounds, ref rightClimbingBounds);

        foundLeftOutsideBounds = leftOutsideBounds as ActivateCarriageBounds;
        foundRightOutsideBounds = rightOutsideBounds as ActivateCarriageBounds;
        foundLeftInsideBounds = leftInsideBounds as ActivateCarriageBounds;
        foundRightInsideBounds = rightInsideBounds as ActivateCarriageBounds;

        foundsLeftClimbBounds = leftClimbingBounds as CarriageClimbingBounds;
        foundsRightClimbBounds = rightClimbingBounds as CarriageClimbingBounds; 
    }

    private void Update()
    {
        playerOnLeftRoof = foundLeftOutsideBounds.playerInActiveArea;
        playerOnRightRoof = foundRightOutsideBounds.playerInActiveArea;
        playerInLeftCarriage = foundLeftInsideBounds.playerInActiveArea;
        playerInRightCarriage = foundRightInsideBounds.playerInActiveArea;

        leftClimbActivated = foundsLeftClimbBounds.activated;
        righhtClimbActivated = foundsRightClimbBounds.activated;
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
            var foundLeftOutsideBounds = leftOutsideBounds as ActivateCarriageBounds;
            var foundRightOutsideBounds = rightOutsideBounds as ActivateCarriageBounds;

            var foundLeftInsideBounds = leftInsideBounds as ActivateCarriageBounds;
            var foundRightInsideBounds = rightInsideBounds as ActivateCarriageBounds;

            var foundLeftClimbBounds = leftClimbingBounds as CarriageClimbingBounds;
            var foundRightClimbBounds = rightClimbingBounds as CarriageClimbingBounds;
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
