using UnityEngine;
using UnityEngine.UIElements;

public class AgentMovement : StateCore
{
    //child states
    public HidingState hidingState;
    public StalkState stalkState;
    public AttackState attackState;
    public GameObject player { get; private set; }
    public LayerMask playerLayer;
    private PlayerMovement playerMovement;
    private SpriteRenderer playerSpriteRenderer;

    private Camera cam;

    // state bool
    public bool isHiding { get; private set; }
    public bool isStalking { get; private set; }
    public bool isAttacking { get; private set; }

    //state parameter bools
    public bool inCarriage { get; private set; }
    public bool inCameraView { get; private set; }
    public bool inPlayerCharacterSight { get; private set; }
    public bool isCloseToPlayer { get; private set; }
    private float closeToPlayerThreshold = 1f;

    public bool isCarriageEmpty { get; private set; }
    private ActivateCarriageBounds currentCarriage;

    void Start()
    {
        SetupInstances();
        Set(hidingState, true);

        initialGravityScale = body.gravityScale;

        player = GameObject.FindGameObjectWithTag("Player");
        playerMovement = player.GetComponent<PlayerMovement>();
        playerSpriteRenderer = player.GetComponent<SpriteRenderer>();

        cam = Camera.main;
    }

    void Update()
    {
        currentAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        CheckInCameraView();
        CheckPlayerCharacterSight();
        CheckBystandersInCarriage();

        CheckAttackTriggers();
        CheckHidingTriggers();

        SelectState();
        state.DoBranch();

    }
    private void FixedUpdate()
    {
        CheckifCloseToPlayer();


        state.FixedDoBranch();

        movementInputs.MoveWithInput();
        movementInputs.SetCollisionAdjustment();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Inside Bounds"))
        {
            inCarriage = true;
            currentCarriage = collision.GetComponent<ActivateCarriageBounds>();
            CheckBystandersInCarriage();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Inside Bounds"))
        {
            inCarriage = false;
            currentCarriage = null;
        }
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
        if (!isAttacking && !isStalking)
        {
            isHiding = true;
        }
    }
    private void CheckStalkTriggers()
    {

    }
    private void CheckAttackTriggers()
    {
        if (isCarriageEmpty || !inCarriage)
        {
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
        }
    }

    private void CheckInCameraView()
    {
        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        Vector3 camPos = cam.transform.position;
        Vector3 agentPos = this.transform.position;

        float leftBound = camPos.x - camHalfWidth;
        float rightBound = camPos.x + camHalfWidth;
        float bottomBound = camPos.y - camHalfHeight;
        float topBound = camPos.y + camHalfHeight;

        if (agentPos.x > leftBound && agentPos.x < rightBound && agentPos.y > bottomBound && agentPos.y < topBound)
        {
            inCameraView = true;
        }
        else
        {
            inCameraView = false;
        }
    }

    private void CheckPlayerCharacterSight()
    {
        if (inCameraView)
        {
            float playerXPos = player.transform.position.x;
            float agentXPos = this.transform.position.x;

            if ((!playerSpriteRenderer.flipX && playerXPos < agentXPos) || (playerSpriteRenderer.flipX && playerXPos > agentXPos))
            {
                inPlayerCharacterSight = true;
            }
            else
            {
                inPlayerCharacterSight = false;
            }
        }
    }

    private void CheckifCloseToPlayer()
    {
        Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
        Vector2 agentPos = new Vector2(this.transform.position.x, this.transform.position.y);

        Collider2D hit = Physics2D.OverlapCircle(agentPos, closeToPlayerThreshold, playerLayer);

        if (hit != null)
        {
            isCloseToPlayer = true;
        }
        else
        {
            isCloseToPlayer = false;
        }

        Helpers.DrawCircle(agentPos, closeToPlayerThreshold, isCloseToPlayer ? Color.red : Color.green, 8);
    }

    private void CheckBystandersInCarriage()
    {
        if (currentCarriage != null)
        {
            if (currentCarriage.bystanderCount == 0)
            {
                isCarriageEmpty = true;
            }
            else
            {
                isCarriageEmpty = false;
            }
        }
        else
        {
            isCarriageEmpty = false;
        }
    }
}
