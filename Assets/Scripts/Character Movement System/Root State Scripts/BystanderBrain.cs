using UnityEngine;

public class BystanderBrain : NPCCore
{
    public StationData departureStation;
    public Transform playerTransform { get; private set; }
    private PlayerBrain playerBrain;

    public bool isCalm {  get; private set; }
    public bool isPanic { get; private set; }
    public bool isFleeing { get; private set; }
    public bool isLeaving { get; private set; }


    private void Start()
    {
        SetupInstances();
        SetupNPCInstances();
        Set(stateList.calmState);

        initialGravityScale = body.gravityScale;

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        playerBrain = playerTransform.GetComponent<PlayerBrain>();
        
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
       if (playerBrain.stateList.meleeState.landedHit)
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
