using UnityEngine;

public class GangwayBounds : Bounds
{
    public static GangwayBounds Instance { get; private set; }

    public LayerMask OutsideBoundsLayer;
    public bool seeGizmos;
    private Collider2D Collider2D;
    private Component leftOutsideBounds;
    private Component rightOutsideBounds;

    public bool playerOnLeftRoof {  get; private set; }
    public bool playerOnRightRoof { get; private set; }

    private void Start()
    {
        Collider2D = GetComponent<Collider2D>();
        SetNeighbouringBounds(Collider2D, 5, OutsideBoundsLayer, typeof(ActivateCarriageBounds), ref leftOutsideBounds, ref rightOutsideBounds);
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

    private void OnTriggerStay2D (Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Agent Collider"))
        {
            Instance = this;
            var foundLeftBounds = leftOutsideBounds as ActivateCarriageBounds;
            playerOnLeftRoof = foundLeftBounds.instanceActivated;

            var foundRightBounds = rightOutsideBounds as ActivateCarriageBounds;
            playerOnRightRoof = foundRightBounds.instanceActivated;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Agent Collider"))
        {
            Instance = null;
        }
    }
}
