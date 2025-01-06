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

    private bool hasJumped;
    private void Start()
    {
        StartCoroutine(UpdatePlayerPosition());
    }

    public void FollowAttackPath()
    {
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, (thisCollider.size.y / 2f));

        navigationSystem.SetPath(currentPos, targetPos);

        movementInputs.runInput = true;


        if (currentPos.x < pathToTarget[0].value.x)
        {
            movementInputs.walkInput = 1;
        }
        else
        {
            movementInputs.walkInput = -1;
        }

        if (pathToTarget[0].type == NavigationSystem.PosType.ClimbingBound)
        {
            bool jumpedToClimbBounds = false;
            bool startedToClimb = false;
            if (!hasJumped && npcCore.currentClimbBounds == null && !jumpedToClimbBounds)
            {
                jumpedToClimbBounds = true;
                StartCoroutine(HandleJumpInput());
            }

            if (npcCore.currentAnimStateInfo.normalizedTime >= 1)
            {
                if(!hasJumped && !npcCore.stateList.wallState.startClimb && !startedToClimb)
                {
                    StartCoroutine(HandleJumpInput());
                    Debug.Log("removed climbing point");
                    pathToTarget.RemoveAt(0); 
                }
            }
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
        hasJumped = true;
        movementInputs.jumpInput = true;
        Debug.Log("Jumped");
        yield return null;
        movementInputs.jumpInput = false;
        hasJumped = false;
    }
}
