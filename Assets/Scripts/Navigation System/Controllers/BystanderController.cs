using System.Collections.Generic;
using UnityEngine;
using static SeatBounds;

public class BystanderController : NPCController
{
    [Header("Paths")]
    public CalmPath calmPath;

    private List<PathData.NamedPosition> calmPathToTarget => calmPath.pathData.pathToTarget;
    private int trainGroundLayer => LayerMask.NameToLayer("Train Ground");
    private int stationGroundLayer => LayerMask.NameToLayer("Station Ground");
    private float targetPos = float.MaxValue;

    public void CalmInputs()
    {
        colliderCenter = npcCore.boxCollider2D.size.y / 2f;
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, colliderCenter);
        float distanceToTarget = DistanceToTarget(targetPos);
        bool closeEnough = CloseEnough(distanceToTarget);

        if (calmPathToTarget.Count == 0 && !closeEnough) 
        { 
            FollowCalmPath(currentPos, lastPos, colliderCenter); 
            return;
        }

        PathData.PosType nextPosType = calmPathToTarget[0].type;
        targetPos = lastPos.value.x;

        switch(nextPosType)
        {
            case PathData.PosType.SlidingDoors:
                lastPos.value = calmPath.chosenSlidingDoorsPos;
                if (closeEnough)
                {
                    calmPath.pathData.chosenSlideDoorBounds.OpenDoors();
                    movementInputs.walkInput = 0;
                    if (calmPath.pathData.chosenSlideDoorBounds.normMoveDoorTime >= 1)
                    {
                        npcCore.spriteRenderer.sortingOrder = 6;
                        npcCore.boxCollider2D.excludeLayers |= 1 << stationGroundLayer;
                        npcCore.boxCollider2D.excludeLayers &= ~(1 << trainGroundLayer);
                        npcCore.collisionChecker.activeGroundLayer = 1 << trainGroundLayer;
                        npcCore.pathData.trainData.charactersList.Add(npcCore);
                        npcCore.pathData.trainData.currentStation.charactersList.Remove(npcCore);
                        FollowCalmPath(currentPos, lastPos, colliderCenter);
                    }
                }
                else
                {
                    FollowCalmPath(currentPos, lastPos, colliderCenter);
                }
                break;

            case PathData.PosType.Seat:
                lastPos.value = calmPath.chosenSeat.pos;
                if (closeEnough)
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
                break;
            case PathData.PosType.Stand:
                lastPos.value = calmPath.chosenStandPos;
                if (closeEnough)
                {
                    movementInputs.walkInput = 0;
                    npcCore.isStanding = true;
                    npcCore.characterMaterial.SendCharToStandLayer();
                }
                else
                {
                    FollowCalmPath(currentPos, lastPos, colliderCenter);
                }
                break;
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

    private bool CloseEnough(float distanceToTarget)
    {
        return distanceToTarget < 0.1;
    }
    private float DistanceToTarget(float targetPosX)
    {
        return Mathf.Abs(transform.position.x - targetPosX);
    }
}
