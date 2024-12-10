using UnityEngine;

public class CalmState : State
{
    public BystanderMovement bystanderMovement;

    public float runSpeedMultiplier;
    public float walkSpeedMultiplier;
    public override void Enter()
    {
    }
    public override void Do()
    {
        if (bystanderMovement.isPanic)
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
