using System.Collections.Generic;
using UnityEngine;

public class BystanderController : NPCController
{
    [Header("Paths")]
    public CalmPath calmPath;

    private List<PathData.NamedPosition> calmPathToTarget => calmPath.pathData.pathToTarget;

    private void Update()
    {
        targetPos = calmPath.chosenSeat.pos;
    }
    public void CalmInputs()
    {
        float distanceToChair = Mathf.Abs(transform.position.x - calmPath.chosenSeat.pos.x);
        if (distanceToChair < 0.1 && calmPath.pathData.chosenSeatBounds != null)
        {
            movementInputs.walkInput = 0;

            var seatData = calmPath.pathData.chosenSeatBounds.seats[calmPath.chosenSeatIndex];
            seatData.filled = true;
            calmPath.pathData.chosenSeatBounds.seats[calmPath.chosenSeatIndex] = seatData;
        }
        else
        {
            FollowCalmPath();
        }
    }
    private void FollowCalmPath()
    {
        colliderCenter = npcCore.boxCollider2D.size.y / 2f;
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, colliderCenter);

        calmPath.SetPath(currentPos, targetPos, colliderCenter);

        //handle direction
        if (currentPos.x < calmPathToTarget[0].value.x)
        {
            movementInputs.walkInput = 1;
        }
        else
        {
            movementInputs.walkInput = -1;
        }
    }
}
