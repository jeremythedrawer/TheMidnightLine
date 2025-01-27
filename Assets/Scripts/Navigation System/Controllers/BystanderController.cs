using System.Collections.Generic;
using UnityEngine;
using static SeatBounds;

public class BystanderController : NPCController
{
    [Header("Paths")]
    public CalmPath calmPath;

    private List<PathData.NamedPosition> calmPathToTarget => calmPath.pathData.pathToTarget;


    public void CalmInputs()
    {
        colliderCenter = npcCore.boxCollider2D.size.y / 2f;
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, colliderCenter);
        if (calmPath.pathData.chosenSeatBounds != null)
        {
            lastPos.value = calmPath.chosenSeat.pos;
            float distanceToSeat = Mathf.Abs(transform.position.x - calmPath.chosenSeat.pos.x);
            if (distanceToSeat < 0.1)
            {
                //set sitting parameters
                movementInputs.walkInput = 0;
                npcCore.isSitting = true;
                npcCore.characterMaterial.SendCharToSeatLayer();

                //set chosen seat to filled
                SeatData seatData = calmPath.pathData.chosenSeatBounds.seats[calmPath.chosenSeatIndex];
                seatData.filled = true;
                calmPath.pathData.chosenSeatBounds.seats[calmPath.chosenSeatIndex] = seatData;
            }
            else
            {
                FollowCalmPath(currentPos, lastPos, colliderCenter);
            }
        }
        else
        {
            lastPos.value = calmPath.chosenStandPos;
            float distanceToStandPos = Mathf.Abs(transform.position.x - calmPath.chosenStandPos.x);
            if (distanceToStandPos < 0.1)
            {
                movementInputs.walkInput = 0;
                npcCore.isStanding = true;
                npcCore.characterMaterial.SendCharToStandLayer();
            }
            else
            {
                FollowCalmPath(currentPos, lastPos, colliderCenter);
            }
        }
    }
    private void FollowCalmPath(Vector2 currentPos, PathData.NamedPosition _lastpos, float colliderCenter)
    {

        calmPath.SetPath(currentPos, _lastpos, colliderCenter);

        //handle direction
        if (calmPathToTarget.Count > 0)
        {
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
}
