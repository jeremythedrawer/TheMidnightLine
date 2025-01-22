using UnityEngine;

public class BystanderMovement : NPCCore
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
        SetupNPCInstances();
        Set(stateList.calmState);

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

        normalizedAnimTime = currentAnimStateInfo.normalizedTime;
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
                Set(stateList.calmState);
            }
            if (isPanic)
            {
                Set(stateList.panicState);
            }
            if (isFleeing)
            {
                Set(stateList.fleeState);
            }
            if (isLeaving)
            {
                Set(stateList.leaveState);
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
