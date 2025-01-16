using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcController : MonoBehaviour
{
    [Header("Parameters")]
    public float updatePlayerPosTicRate = 1f;

    [Header("References")]
    public NPCCore npcCore;
    public NavigationSystem navigationSystem;
    public Transform playerTransform;
    public BoxCollider2D playerCollider;
    public BoxCollider2D thisCollider;
    public MovementInputs movementInputs;

    public List<NavigationSystem.NamedPosition> pathToTarget => navigationSystem.pathToTarget;

    private Vector2 currentPos;
    private Vector2 targetPos;
    private float colliderCenter; 

    private void Start()
    {
        StartCoroutine(UpdatePlayerPosition());
    }

    public void FollowAttackPath()
    {
        colliderCenter = thisCollider.size.y / 2f;
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, colliderCenter);

        navigationSystem.SetPath(currentPos, targetPos, colliderCenter);

        movementInputs.runInput = true;

        //handle direction
        if (currentPos.x < pathToTarget[0].value.x)
        {
            movementInputs.walkInput = 1;
        }
        else
        {
            movementInputs.walkInput = -1;
        }

        //handle jump
        if (pathToTarget[0].type == NavigationSystem.PosType.ClimbingBound) // next position is climbing bounds
        {
            if (npcCore.currentClimbBounds == null && !movementInputs.jumpInput)
            {
                StartCoroutine(HandleJumpInput());
            }
            if (npcCore.currentClimbBounds != null)
            {
                pathToTarget.RemoveAt(0);
            }
        }
        if (npcCore.currentClimbBounds != null)
        {
            if (!movementInputs.jumpInput && npcCore.currentAnimStateInfo.normalizedTime >= 1 && npcCore.stateList.wallState.isHanging)
            {
                StartCoroutine(HandleJumpInput());
            }
        }
        if (pathToTarget[0].type == NavigationSystem.PosType.RoofEdge && navigationSystem.distanceToNextPos < navigationSystem.closeEnoughToNextPos)
        {
            StartCoroutine(HandleJumpInput());
        }
    }

    public void FollowStalkPath()
    {

    }

    private IEnumerator UpdatePlayerPosition()
    {
        while (true)
        {
            targetPos = new Vector2(playerTransform.position.x, playerTransform.position.y) + new Vector2(0, (playerCollider.size.y / 2f));
            yield return new WaitForSeconds(updatePlayerPosTicRate);
        }
    }
    private IEnumerator HandleJumpInput()
    {
        movementInputs.jumpInput = true;
        yield return null;
        movementInputs.jumpInput = false;
    }
}
