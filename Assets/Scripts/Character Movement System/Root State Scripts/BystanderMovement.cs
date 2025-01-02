using UnityEngine;

public class BystanderMovement : StateCore
{
    public GameObject player { get; private set; }
    private PlayerMovement playerMovement;

    public bool isCalm {  get; private set; }
    public bool isPanic { get; private set; }
    public bool isFleeing { get; private set; }
    public bool isLeaving { get; private set; }

    void Start()
    {
        SetupInstances();
        Set(stateList.calmState, true);

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
                Set(stateList.calmState, true);
            }
            if (isPanic)
            {
                Set(stateList.panicState, true);
            }
            if (isFleeing)
            {
                Set(stateList.fleeState, true);
            }
            if (isLeaving)
            {
                Set(stateList.leaveState, true);
            }
        }
    }

    private void PanicChecker()
    {
       if (playerMovement.stateList.meleeState.landedHit)
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
