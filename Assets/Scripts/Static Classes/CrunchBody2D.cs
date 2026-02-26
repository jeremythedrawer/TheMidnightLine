using System;
using UnityEngine;

public static class CrunchBody2D
{
    [Serializable] public class Body
    {
        public Vector2 moveVelocity;
        public Vector2 targetVelocity;
        public Collider2D collider;
        public ContactFilter2D collisionFilter;
        public RaycastHit2D[] collisionHits = new RaycastHit2D[5];
        public float gravityScale;
        public float acceleration;

        public void VerticalVelocity()
        {
            float gravity = gravityScale * Time.fixedDeltaTime;
            moveVelocity.y -= gravity;
        }

        public void HorizontalVelocity()
        {
            Vector2 direction = moveVelocity.normalized;
            float distance = moveVelocity.magnitude * Time.fixedDeltaTime;

            int collisionCount = collider.Cast(direction, collisionFilter, collisionHits, distance);

            if (collisionCount > 0)
            {
                RaycastHit2D hit = collisionHits[0];
                distance = hit.distance;
                moveVelocity -= Vector2.Dot(moveVelocity, hit.normal) * hit.normal;
            }
            else
            {
                moveVelocity.x = Mathf.Lerp(moveVelocity.x, targetVelocity.x, acceleration * Time.fixedDeltaTime);
            }
        }
    }

}
