using UnityEngine;

public class BystanderMovement : StateCore
{
    //child states
    public CalmState calmState;
    public PanicState panicState;

    public GameObject player { get; private set; }
    private PlayerMovement playerMovement;

    public bool isCalm {  get; private set; }
    public bool isPanic { get; private set; }

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
