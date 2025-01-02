using UnityEngine;

public class Bounds : MonoBehaviour
{
    public void SetNeighbouringBounds(Collider2D Collider2D, float dectectionBuffer, LayerMask layerToFind, System.Type boundsComponentToFind, ref Component leftComponent, ref Component rightComponent)
    {

        Vector2 LeftPointA = Collider2D.bounds.min;
        Vector2 LeftPointB = new Vector2(LeftPointA.x - dectectionBuffer, Collider2D.bounds.max.y + dectectionBuffer);

        Vector2 RightPointA = new Vector2(Collider2D.bounds.max.x, Collider2D.bounds.max.y + dectectionBuffer);
        Vector2 RightPointB = new Vector2(RightPointA.x + dectectionBuffer, Collider2D.bounds.min.y);

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

        if (leftCollider == null || righCollider == null)
        {
            Debug.LogError("Did not find 'Outside Bounds'"); // TODO: need front part of train
        }
    }
}
