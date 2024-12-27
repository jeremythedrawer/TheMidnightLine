using System.Collections;
using UnityEngine;

public class GroundState : State
{
    public AirborneState airborneState;

    //child states
    public IdleState idleState;
    public RunState runState;
    [HideInInspector] public MeleeState meleeState;
    [HideInInspector] public ShootState shootState;

    public bool canMelee;
    public bool canShoot;

    [Tooltip("How sticky the floor is")]
    [Range(0f, 1f)]
    public float groundDrag;

    public bool isAttacking { private get; set; } = false;

    public bool pendingState { get; set; }

    public bool finishedHeavyLanding { get; set; } = true;
    private string heavyLandingAnimation = "heavyLandingRight";

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
        if (state == idleState || state == meleeState || state == shootState)
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
            Set(meleeState, true);
        }

        if (movementInputs.shootInput && canShoot)
        {
            isAttacking = true;
            Set(shootState, true);
        }
    }

    private IEnumerator DelayState()
    {
        yield return null; // one frame delay
        
        if (!isAttacking)
        {
            if (movementInputs.walkInput == 0)
            {
                Set(idleState, true);
            }
            else if (movementInputs.canMove)
            {
                Set(runState, true);
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
            idleState.startAnimation = true;
            runState.startRunAnimation = true;
            runState.startWalkAnimation = true;

            if (airborneState.heavyLanding) // triggered on one frame
            {
                //TODO: roll when x input
                movementInputs.canMove = false;
                animator.Play(heavyLandingAnimation, 0, 0);
                airborneState.heavyLanding = false;
            }

            if (core.currentAnimStateInfo.normalizedTime >= 1) // when heavylanding is finished
            {
                finishedHeavyLanding = true;
                movementInputs.canMove = true;

                idleState.startAnimation = false;
                runState.startRunAnimation = false;
                runState.startWalkAnimation = false;
            } 
        }
    }
}
