using System.Collections.Generic;
using UnityEngine;

public class TrainBounds : Bounds
{
    [Header("References")]
    public Collider2D backCarriageCollider;
    public Collider2D frontCarriageCollider;

    [Header("Parameters")]
    public float wheelLevel = 1.0f;
    public float roofLevel = 1.0f;

    public float boundsMinX => backCarriageCollider.bounds.min.x;
    public float boundsMaxX => frontCarriageCollider.bounds.max.x;
    private void OnDrawGizmos()
    {
        DebugLines(true);
    }

    private void Start()
    {
        DebugLines(false);
        wheelLevel = transform.position.y + wheelLevel;
        roofLevel = transform.position.y + roofLevel;    
    }

    private void DebugLines(bool usingGizmos)
    {
#if UNITY_EDITOR
        if (seeBoundsGizmos)
        {
            Vector2 wheelsMin = new Vector2 (boundsMinX, wheelLevel);
            Vector2 wheelsMax = new Vector2 (boundsMaxX, wheelLevel);
            Vector2 roofMin = new Vector2 (boundsMinX, roofLevel);
            Vector2 roofMax = new Vector2 (boundsMaxX, roofLevel);

            if (!usingGizmos)
            {
                Debug.DrawLine(wheelsMin, wheelsMax, Color.yellow);
                Debug.DrawLine(roofMin, roofMax, Color.yellow);
            }
            else
            {
                if (Application.isPlaying) return;

                float wheels = transform.position.y + this.wheelLevel;
                float roof = transform.position.y + this.roofLevel;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(wheelsMin, wheelsMax);
                Gizmos.DrawLine(roofMin, roofMax);
            }

        }
#endif
    }
}
