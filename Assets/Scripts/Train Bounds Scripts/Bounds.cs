using System.Collections.Generic;
using UnityEngine;

public class Bounds : MonoBehaviour
{
    public bool seeBoundsGizmos;
    public void SetNeighbouringBounds(Collider2D Collider2D, float dectectionBuffer, LayerMask layerToFind, System.Type boundsComponentToFind, ref Component leftComponent, ref Component rightComponent)
    {

        Vector2 LeftPointA = new Vector2 (Collider2D.bounds.min.x, Collider2D.bounds.min.y - dectectionBuffer);
        Vector2 LeftPointB = new Vector2(LeftPointA.x - dectectionBuffer, Collider2D.bounds.max.y + dectectionBuffer);

        Vector2 RightPointA = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.max.y + dectectionBuffer);
        Vector2 RightPointB = new Vector2(RightPointA.x + dectectionBuffer, Collider2D.bounds.min.y - dectectionBuffer);

        Collider2D leftCollider = Physics2D.OverlapArea(LeftPointA, LeftPointB, layerToFind);
        Collider2D righCollider = Physics2D.OverlapArea(RightPointA, RightPointB, layerToFind);

        if (leftCollider != null)
        {
            leftComponent = leftCollider.GetComponent(boundsComponentToFind);
        }
        if (righCollider != null)
        {
            rightComponent = righCollider.GetComponent(boundsComponentToFind);
        }

#if UNITY_EDITOR
        if(seeBoundsGizmos)
        {
            Helpers.DrawBoxCastDebug(LeftPointA, LeftPointB, leftCollider != null ? Color.green : Color.red);
            Helpers.DrawBoxCastDebug(RightPointA, RightPointB, leftCollider != null ? Color.green : Color.red);
        }
#endif
    }
}