using UnityEngine;

public class BystanderMovement : StateCore
{
    //child states
    public CalmState calmState;
    public PanicState panicState;
    public FleeState fleeState;
    public LeaveState leaveState;

    public GameObject player { get; private set; }
    private PlayerMovement playerMovement;

    public bool isCalm {  get; private set; }
    public bool isPanic { get; private set; }
    public bool isFleeing { get; private set; }
    public bool isLeaving { get; private set; }

    void Start()
    {
        SetupInstances();
        Set(calmState, true);

        initialGravityScale = body.gravityScale;

        player = GameObject.FindGameObjectWithTag("Player");
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    void Update()
    {
        currentAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        PanicChecker();
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
            if (isCalm)
            {
                Set(calmState, true);
            }
            if (isPanic)
            {
                Set(panicState, true);
            }
            if (isFleeing)
            {
                Set(fleeState, true);
            }
            if (isLeaving)
            {
                Set(leaveState, true);
            }
        }
    }

    private void PanicChecker()
    {
       if (playerMovement.groundState.meleeState.landedHit)
        {
            isPanic = true;
            isCalm = false;
        }
       else
        {
            isPanic = false;
            isCalm = true;
        }
    }
}
