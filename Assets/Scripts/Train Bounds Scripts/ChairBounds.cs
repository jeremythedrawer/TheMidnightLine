using System.Collections.Generic;
using UnityEngine;

public class ChairBounds : Bounds
{
    [Header ("Parameters")]
    [Range(1,20)]
    public int seats;

    [Header ("References")]
    public BoxCollider2D boxCollider2D;

    public List<Vector2> seatPos = new List<Vector2>();

    private float seatWidth;
    private float seatOffset;
    private float seatPosX;

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (seeBoundsGizmos)
        {
            if (Application.isPlaying) return;
            if (boxCollider2D == null || seats <= 0) return;

            seatWidth = boxCollider2D.bounds.size.x / seats;
            seatOffset = seatWidth * 0.5f;

            for (int i = 0; i < seats; i++)
            {
                seatPosX = boxCollider2D.bounds.min.x + (i * seatWidth) + seatOffset;
                Gizmos.DrawLine( new Vector2(seatPosX, boxCollider2D.bounds.min.y), new Vector2(seatPosX, boxCollider2D.bounds.max.y));
            }
        }
#endif
    }
    private void Start()
    {
        if (boxCollider2D == null || seats <= 0) return;

        seatWidth = boxCollider2D.bounds.size.x / seats;
        seatOffset = seatWidth * 0.5f;

        for (int i = 0; i < seats; i++)
        {
            seatPosX = boxCollider2D.bounds.min.x + (i * seatWidth) + seatOffset;
            seatPos.Add(new Vector2(seatPosX, boxCollider2D.bounds.center.y));
        }
    }
}
