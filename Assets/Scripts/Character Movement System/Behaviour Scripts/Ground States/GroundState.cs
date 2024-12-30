using System.Collections;
using UnityEngine;

public class GroundState : State
{
    public bool canMelee;
    public bool canShoot;

    [Tooltip("How sticky the floor is")]
    [Range(0f, 1f)]
    public float groundDrag;

    public bool isAttacking { private get; set; } = false;

    public bool pendingState { get; set; }

    public bool finishedHeavyLanding { get; set; } = true;


    public override void Enter()
    {

    }
    public override void Do()
    {
        BHCCorrection();
        HeavyLand();
        SelectState();
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

    }

    void SelectState()
    {
        if (!pendingState)
        {
            StartCoroutine(DelayState());
        }

        if (canMelee && movementInputs.meleeInput)
        {
            isAttacking = true;
            Set(core.stateList.meleeState, true);
        }

        if (movementInputs.shootInput && canShoot)
        {
            isAttacking = true;
            Set(core.stateList.shootState, true);
        }
    }

    private IEnumerator DelayState()
    {
        yield return null; // one frame delay
        
        if (!isAttacking)
        {
            if (movementInputs.walkInput == 0)
            {
                Set(core.stateList.idleState, true);
            }
            else if (movementInputs.canMove)
            {
                Set(core.stateList.runState, true);
            }
        }

        pendingState = false; // reset bool
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

        //state completed
        if (!collisionChecker.grounded)
        {
            isComplete = true;
        }
    }

    private void HeavyLand()
    {
        if ( core is BystanderMovement)
        {
            return;
        }

        if (!finishedHeavyLanding)
        {
            core.stateList.idleState.startAnimation = true;
            core.stateList.runState.startRunAnimation = true;
            core.stateList.runState.startWalkAnimation = true;

            if (core.stateList.airborneState.heavyLanding) // triggered on one frame
            {
                //TODO: roll when x input
                movementInputs.canMove = false;
                animator.Play(animStates.heavyAnimState, 0, 0);
                core.stateList.airborneState.heavyLanding = false;
            }

            if (core.currentAnimStateInfo.normalizedTime >= 1) // when heavylanding is finished
            {
                finishedHeavyLanding = true;
                movementInputs.canMove = true;

                core.stateList.idleState.startAnimation = false;
                core.stateList.runState.startRunAnimation = false;
                core.stateList.runState.startWalkAnimation = false;
            } 
        }
    }
}
