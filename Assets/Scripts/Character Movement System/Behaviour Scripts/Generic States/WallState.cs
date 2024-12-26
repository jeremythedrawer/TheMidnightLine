using UnityEngine;

public class WallState : State
{
    public HangState hangState;
    public ClimbState climbState;
    public override void Enter()
    {

    }
    public override void Do()
    {
        Debug.Log("in wall state");
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }
}
