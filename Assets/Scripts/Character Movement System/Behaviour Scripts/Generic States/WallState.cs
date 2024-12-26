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
        Debug.Log("Entered WallState");
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }
}
