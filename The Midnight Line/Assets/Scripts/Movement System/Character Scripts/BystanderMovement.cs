using UnityEngine;

public class BystanderMovement : StateCore
{
    //child states
    public CalmState calmState;
    public PanicState panicState;

    void Start()
    {
        SetupInstances();
        Set(calmState, true);

        initialGravityScale = body.gravityScale;
    }

    void Update()
    {
        SelectState();

        state.DoBranch();
    }

    private void FixedUpdate()
    {

        
        state.FixedDoBranch();
    }

    void SelectState()
    {
        if (state.isComplete)
        {
            //state logic
        }
    }
}
