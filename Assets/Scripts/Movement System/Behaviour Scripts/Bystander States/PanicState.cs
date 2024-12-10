using UnityEngine;

public class PanicState : State
{
    public BystanderMovement bystanderMovement;

    public float runSpeedMultiplier;
    public float walkSpeedMultiplier;
    public override void Enter()
    {
    }
    public override void Do()
    {
        if (bystanderMovement.isCalm)
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
