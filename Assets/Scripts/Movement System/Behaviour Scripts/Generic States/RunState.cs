using UnityEngine;

public class RunState : State
{
    public GroundState groundState;
    public override void Enter()
    {
    }
    public override void Do()
    {
        if (!collisionChecker.grounded || movementInputs.walkInput == 0)
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
