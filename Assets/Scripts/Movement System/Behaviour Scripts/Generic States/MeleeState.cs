using UnityEngine;

public class MeleeState : State
{
    //child states
    public IdleState idleState;
    public RunState runState;

    public bool landedHit {  get; private set; }

    public override void Enter()
    {

    }
    public override void Do()
    {
        SelectState();
        DamagedGiven();
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
    }

    private void DamagedGiven()
    {
        if (landedHit == false)
        {
            landedHit = true;
        }
        else
        {
            landedHit = false;
        }
        //TODO damage logic
    }
}
