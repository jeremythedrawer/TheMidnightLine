using UnityEngine;

public class ShootState : State
{
    //child states
    public IdleState idleState;
    public RunState runState;
    public override void Enter()
    {
    }
    public override void Do()
    {
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
        if (movementInputs.walkInput == 0 && !movementInputs.shootInput)
        {
            Set(idleState, true);
        }
        else
        {
            Set(runState, true);
        }
    }
}
