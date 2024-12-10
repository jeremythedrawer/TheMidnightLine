using Unity.VisualScripting;
using UnityEngine;

public class GroundState : State
{
    //child states
    public IdleState idleState;
    public RunState runState;
    public MeleeState meleeState;

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
        if (inputChecker.walkInput == 0 && !inputChecker.meleeInput)
        {
            Set(idleState, true);
        }
        else
        {
            Set(runState, true);
        }

        if (inputChecker.meleeInput)
        {
            Set(meleeState, true);
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
        if (!collisionChecker.grounded || inputChecker.walkInput != 0)
        {
            isComplete = true;
        }
    }
}
