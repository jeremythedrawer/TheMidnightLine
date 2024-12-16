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
        GivenDamage();
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
    }

    private void GivenDamage()
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

    private void MeleeTime()
    {
        //TODO lerp using animation clip time as t
    }
}
