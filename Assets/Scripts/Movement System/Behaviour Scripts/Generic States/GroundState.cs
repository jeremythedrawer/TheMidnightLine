using UnityEngine;

public class GroundState : State
{
    //child states
    public IdleState idleState;
    public RunState runState;
    [HideInInspector] public MeleeState meleeState;
    [HideInInspector] public ShootState shootState;

    public bool canMelee;
    public bool canShoot;

    public override void Enter()
    {

    }
    public override void Do()
    {
        BHCCorrection();
        SelectState();
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }

    void SelectState()
    {
        if (movementInputs.walkInput == 0 && !movementInputs.meleeInput)
        {
            Set(idleState, true);
        }
        else
        {
            Set(runState, true);
        }

        if (movementInputs.meleeInput && canMelee)
        {
            Set(meleeState, true);
        }

        if (movementInputs.shootInput && canShoot)
        {
            Set(shootState, true);
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

        //state completed
        if (!collisionChecker.grounded || movementInputs.walkInput != 0)
        {
            isComplete = true;
        }
    }
}
