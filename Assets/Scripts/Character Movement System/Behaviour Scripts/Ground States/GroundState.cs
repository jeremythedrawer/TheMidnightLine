using System.Collections;
using UnityEngine;

public class GroundState : State
{
    public bool canMelee;
    public bool canShoot;

    [Tooltip("How sticky the floor is")]
    [Range(0f, 1f)]
    public float groundDrag;

    public bool isAttacking { private get; set; } 
    public bool isHeavyLanding { get; set; }


    public override void Enter()
    {

    }
    public override void Do()
    {
        BHCCorrection();
        HeavyLand();
        SelectState();

        if (movementInputs.meleeInput)
        {
            isAttacking = true;
        }
    }

    public override void FixedDo()
    {
        if (state == core.stateList.idleState || state == core.stateList.meleeState || state == core.stateList.shootState)
        {
            body.linearVelocityX *= groundDrag;
        }
    }
    public override void Exit()
    {
        base.Exit();
    }

    void SelectState()
    {
        if (isAttacking)
        {
            if (canMelee && movementInputs.meleeInput)
            {
                Set(core.stateList.meleeState);
            }

            if (movementInputs.shootInput && canShoot)
            {
                Set(core.stateList.shootState);
            }
        }
        else if (movementInputs.walkInput != 0 && movementInputs.canMove)
        {
            Set(core.stateList.runState);
        }
        else
        {
            Set(core.stateList.idleState);
        }
    }

    private void BHCCorrection()
    {
        //bumped head correction (corrects box collider when grounded and jumped state failed to move the collider in time
        if (Mathf.Abs(collisionChecker.characterBoxCollider.offset.x) > 0)
        {
            Vector2 currentOffset = collisionChecker.characterBoxCollider.offset;
            currentOffset.x = 0;
            collisionChecker.characterBoxCollider.offset = currentOffset;
        }

    }

    private void HeavyLand()
    {
        if ( core is BystanderBrain)
        {
            return;
        }

        if (isHeavyLanding)
        {

            if (core.stateList.airborneState.aboutToHeavyLand) // triggered on one frame
            {
                //TODO: roll when x input
                movementInputs.canMove = false;
                PlayAnimation(animStates.heavyAnimState);
                core.stateList.airborneState.aboutToHeavyLand = false;
            }

            if (core.currentAnimStateInfo.normalizedTime >= 1) // when heavylanding is finished
            {
                isHeavyLanding = false;
                movementInputs.canMove = true;
            } 
        }
    }
}
