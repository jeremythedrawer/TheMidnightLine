using UnityEngine;
using UnityEngine.UIElements;

public class AgentBrain : NPCCore
{
    // state bool
    public bool isHiding { get; private set; }
    public bool isStalking { get; private set; }
    public bool isAttacking { get; private set; }
    public AgentController agentController => controller as AgentController;
    void Start()
    {
        SetupInstances();
        SetupNPCInstances();
        Set(stateList.calmState);

        initialGravityScale = body.gravityScale;
    }

    void Update()
    {
        currentAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        behaviourParams.CheckInCameraView();
        behaviourParams.CheckPlayerCharacterSight();
        behaviourParams.CheckBystandersInCarriage();

        movementInputs.JumpController();

        CheckAttackTriggers();
        CheckHidingTriggers();

        SelectState();
        state.DoBranch();

        normalizedAnimTime = currentAnimStateInfo.normalizedTime;
    }
    private void FixedUpdate()
    {
        movementInputs.MoveWithInput();
        movementInputs.SetCollisionAdjustment();

        state.FixedDoBranch();

    }

    void SelectState()
    {
        if (isHiding)
        {
            Set(stateList.calmState);
        }
        if (isStalking)
        {
            Set(stateList.stalkState);
        }
        if (isAttacking)
        {
            Set(stateList.attackState);
        }
    }

    private void CheckHidingTriggers()
    {
        isHiding = (!isAttacking && !isStalking) ? true : false;
    }
    private void CheckStalkTriggers()
    {
        //outside camera view
        //player is not facing npc and player is not too close
    }
    private void CheckAttackTriggers()
    {
        if (behaviourParams.isCarriageEmpty || !behaviourParams.inCarriage)
        {
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
        }
    }
}
