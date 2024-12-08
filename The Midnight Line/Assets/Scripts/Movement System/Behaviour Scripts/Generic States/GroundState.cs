using Unity.VisualScripting;
using UnityEngine;

public class GroundState : State
{
    //child states
    public IdleState idleState;
    public RunState runState;

    public override void Enter()
    {

    }
    public override void Do()
    {

        //setting states
        if (inputChecker.xInput == 0)
        {
            Set(idleState, true);
        }
        else
        {
            Set(runState, true);
        }


        //bumped head correction (corrects box collider when grounded and jumped state failed to move the collider in time
        if (Mathf.Abs(collisionChecker.characterBoxCollider.offset.x) > 0)
        {
            Vector2 currentOffset = collisionChecker.characterBoxCollider.offset;
            currentOffset.x = 0;
            collisionChecker.characterBoxCollider.offset = currentOffset;
        }

        //state completed
        if (!collisionChecker.grounded || inputChecker.xInput != 0)
        {
            isComplete = true;
        }
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }
}
