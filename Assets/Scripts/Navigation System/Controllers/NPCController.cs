using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static SeatBounds;

public abstract class NPCController : MonoBehaviour
{
    [Header("Paths")]
    public CalmPath calmPath;

    [Header("Parameters")]
    public float updateTargetTickRate = 1f;

    [Header("References")]
    public NPCCore npcCore;
    public UnityEngine.Transform playerTransform;
    public BoxCollider2D playerCollider;
    public MovementInputs movementInputs;

    protected Vector2 currentPos;
    protected PathData.NamedPosition lastPos;
    protected float colliderCenter;

    private float distanceToTarget;
    private bool closeEnough;
    private List<PathData.NamedPosition> calmPathToTarget => calmPath.pathData.pathToTarget;
    private float targetPosX = float.MaxValue;


    public void CalmInputs()
    {
        colliderCenter = npcCore.boxCollider2D.size.y / 2f;
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, colliderCenter);
        if (calmPathToTarget.Count == 0)
        {
            if(!closeEnough)
            {
                FollowCalmPath(currentPos, lastPos, colliderCenter);
                return;
            }
            else
            {
                return;
            }
        }

        PathData.PosType nextPosType = calmPathToTarget[0].type;
        targetPosX = calmPathToTarget[0].value.x;
        distanceToTarget = Mathf.Abs(currentPos.x - targetPosX);
        closeEnough = distanceToTarget < 0.1;

        switch (nextPosType)
        {
            case PathData.PosType.SlidingDoors:
                if (closeEnough)
                {
                    calmPath.pathData.chosenSlideDoorBounds.OpenDoors();
                    movementInputs.walkInput = 0;

                    if (!calmPath.pathData.chosenSlideDoorBounds.characterQueue.Contains(npcCore))
                    {
                        calmPath.pathData.chosenSlideDoorBounds.characterQueue.Enqueue(npcCore);
                    }
                    if (calmPath.pathData.chosenSlideDoorBounds.normMoveDoorTime >= 1)
                    {
                        if (npcCore.onTrain)
                        {
                            FollowCalmPath(currentPos, lastPos, colliderCenter);
                        }
                    }
                }
                else
                {
                    FollowCalmPath(currentPos, lastPos, colliderCenter);
                }
                break;

            case PathData.PosType.Seat:
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
        else
        {
            movementInputs.walkInput = 0;
        }
    }

    protected IEnumerator TargetPosIsPlayer() // TODO: lastPOs.value cannot be updated
    {
        while (true)
        {
            lastPos.value = new Vector2(playerTransform.position.x, playerTransform.position.y) + new Vector2(0, (playerCollider.size.y / 2f));
            yield return new WaitForSeconds(updateTargetTickRate);
        }
    }

}
