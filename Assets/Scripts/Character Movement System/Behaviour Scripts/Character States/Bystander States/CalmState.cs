using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class CalmState : State
{
    //child state
    public float runSpeedMultiplier;
    public float walkSpeedMultiplier;

    public override void Enter()
    {
    }
    public override void Do()
    {
        SelectState();
        if (npcCore is BystanderMovement)
        {
            npcCore.bystanderController.CalmInputs();
        }
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
        base.Exit();
        StopAllCoroutines();
    }

    private void SelectState()
    {
        Set(stateList.groundState);
    }
}
