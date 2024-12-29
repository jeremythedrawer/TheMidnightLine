using UnityEngine;
using UnityEngine.UIElements;

public class AgentMovement : NPCCore
{
    //child states
    [Header("Child States")]
    public HidingState hidingState;
    public StalkState stalkState;
    public AttackState attackState;

    // state bool
    public bool isHiding { get; private set; }
    public bool isStalking { get; private set; }
    public bool isAttacking { get; private set; }

    void Start()
    {
        SetupInstances();
        Set(hidingState, true);

        initialGravityScale = body.gravityScale;
    }

    void Update()
    {
        currentAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        navigationSystem.CheckInCameraView();
        navigationSystem.CheckPlayerCharacterSight();
        navigationSystem.CheckBystandersInCarriage();

        CheckAttackTriggers();
        CheckHidingTriggers();

        SelectState();
        state.DoBranch();
    }
    private void FixedUpdate()
    {

        state.FixedDoBranch();

        movementInputs.MoveWithInput();
        movementInputs.SetCollisionAdjustment();
    }

    void SelectState()
    {

        if (state.isComplete)
        {
            if (isHiding)
            {
                Set(hidingState, true);
            }
            if (isStalking)
            {
                Set(stalkState, true);
            }
            if (isAttacking)
            {
                Set(attackState, true);
            }
        }
    }

    private void CheckHidingTriggers()
    {
        if (!isAttacking && !isStalking) //default is isHiding
        {
            isHiding = true;
        }
    }
    private void CheckStalkTriggers()
    {

    }
    private void CheckAttackTriggers()
    {
        if (navigationSystem.isCarriageEmpty || !navigationSystem.inCarriage)
        {
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
        }
    }
}
