using System.Collections.Generic;
using UnityEngine;

public class SeatBounds : OldBounds
{
    [Header ("Parameters")]
    [Range(1,20)]
    public int seatsCount;

    [Header ("References")]
    public BoxCollider2D boxCollider2D;

    public bool isFull {  get; private set; }


    [System.Serializable]
    public struct SeatData
    {
        public Vector2 pos;
        public bool filled;

        public SeatData(Vector2 pos,  bool filled)
        {
            this.pos = pos;
            this.filled = filled;
        }
    }
    public List<SeatData> seats { get; set; } = new List<SeatData>();

    private float seatWidth;
    private float seatOffset;
    private float seatPosX;

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (seeBoundsGizmos)
        {
            if (Application.isPlaying) return;
            if (boxCollider2D == null || seatsCount <= 0) return;

            seatWidth = boxCollider2D.bounds.size.x / seatsCount;
            seatOffset = seatWidth * 0.5f;

            for (int i = 0; i < seatsCount; i++)
            {
                seatPosX = boxCollider2D.bounds.min.x + (i * seatWidth) + seatOffset;
                Gizmos.DrawLine( new Vector2(seatPosX, boxCollider2D.bounds.min.y), new Vector2(seatPosX, boxCollider2D.bounds.max.y));
            }
        }
#endif
    }

    public void SetSeatData()
    {
        if (boxCollider2D == null || seatsCount <= 0) return;

        seatWidth = boxCollider2D.bounds.size.x / seatsCount;
        seatOffset = seatWidth * 0.5f;

        for (int i = 0; i < seatsCount; i++)
        {
            seatPosX = boxCollider2D.bounds.min.x + (i * seatWidth) + seatOffset;
            AddToSeatData(new Vector2(seatPosX, boxCollider2D.bounds.center.y), false);
        }
    }
    private void AddToSeatData(Vector2 position, bool filled)
    {
        seats.Add(new SeatData(position, filled));
    }
}
