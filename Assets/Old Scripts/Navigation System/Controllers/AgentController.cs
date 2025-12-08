using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : NPCController
{
    [Header ("Paths")]
    public StalkPath stalkPath;
    public AttackPath attackPath;

    private List<PathData.NamedPosition> attackPathToTarget => attackPath.pathData.pathToTarget;


    private float originalrunSpeed;

    private void Start()
    {
        originalrunSpeed = npcCore.characterStats.runSpeed;
    }

    public void AttackInputs()
    {
        if (npcCore.behaviourParams.isArmDistance)
        {
            movementInputs.meleeInput = true;
        }
        else
        {
            movementInputs.meleeInput = false;
            FollowAttackPath();
        }
    }

    public void StalkInputs()
    {

    }
    private void FollowAttackPath()
    {
        colliderCenter = npcCore.boxCollider2D.size.y / 2f;
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, colliderCenter);

        movementInputs.runInput = true;

        //handle direction
        if (currentPos.x < attackPathToTarget[0].value.x)
        {
            movementInputs.walkInput = 1;
        }
        else
        {
            movementInputs.walkInput = -1;
        }

        //handle jump
        if (attackPathToTarget[0].type == PathData.PosType.ClimbingBound) // next position is climbing bounds
        {
            if (npcCore.currentClimbBounds == null && !movementInputs.jumpInput)
            {
                StartCoroutine(HandleJumpInput());
            }
            if (npcCore.currentClimbBounds != null)
            {
                attackPathToTarget.RemoveAt(0);
            }
        }

        //start climbing
        if (npcCore.currentClimbBounds != null)
        {
            if (npcCore.currentAnimStateInfo.IsName(npcCore.animStates.hangAnimState))
            {
                StartCoroutine(WallSequence());
            }    
        }


        //jumping to next roof
        if (attackPathToTarget[0].type == PathData.PosType.RoofEdge && attackPath.distanceToNextPos < attackPath.closeEnoughToNextPos)
        {
            float boostSpeed = originalrunSpeed * 1.5f;
            npcCore.characterStats.runSpeed = boostSpeed;
            StartCoroutine(HandleJumpInput());
        }
        else
        {
            float t = 0;
            t += Time.deltaTime/0.5f;
            npcCore.characterStats.runSpeed = Mathf.Lerp(npcCore.characterStats.runSpeed, originalrunSpeed, Mathf.Clamp01(t));
        }


    }

    private void FollowStalkPath()
    {

    }

    private void FollowCalmPath()
    {

    }

    private IEnumerator WallSequence()
    {
        if (lastPos.value.y > attackPath.pathData.trainData.roofLevel)
        {
            yield return StartCoroutine(HandleJumpInput());
        }
        else
        {
            yield return StartCoroutine(HandleCrouchInput());
        }
    }
    private IEnumerator HandleJumpInput()
    {
        movementInputs.jumpInput = true;
        yield return null;
        movementInputs.jumpInput = false;
    }
    private IEnumerator HandleCrouchInput()
    {
        movementInputs.crouchInput = true;
        yield return new WaitForSeconds(0.2f);
        movementInputs.crouchInput = false;
    }
}
