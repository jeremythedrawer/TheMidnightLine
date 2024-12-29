using UnityEngine;

public class NavigationSystem : MonoBehaviour
{
    public float closeToPlayerThreshold = 2f;

    [Header("Player Data")]
    public LayerMask playerLayer;

    public GameObject player { get; private set; }
    private PlayerMovement playerMovement;
    private SpriteRenderer playerSpriteRenderer;

    private Camera cam;

    //parameter bools
    public bool armDistanceFromPlayer { get; private set; }
    public bool facingPlayer { get; private set; }
    public bool isCarriageEmpty { get; private set; }
    public bool inCameraView { get; private set; }
    public bool inCarriage {  get; private set; }


    private ActivateCarriageBounds currentCarriage;


    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        Gizmos.color = Color.green;
        Helpers.DrawCircle(transform.position, closeToPlayerThreshold, Color.green, 36, true);
#endif
    }
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerMovement = player.GetComponent<PlayerMovement>();
        playerSpriteRenderer = player.GetComponent<SpriteRenderer>();

        cam = Camera.main;
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

    void Update()
    {
#if UNITY_EDITOR
        GizmosLines();
#endif
    }

    public void CheckifCloseToPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, closeToPlayerThreshold, playerLayer);

        if (hit != null)
        {
            armDistanceFromPlayer = true;
        }
        else
        {
            armDistanceFromPlayer = false;
        }
    }

    public void CheckInCameraView()
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

    public void CheckPlayerCharacterSight()
    {
        if (inCameraView)
        {
            if ((!playerSpriteRenderer.flipX && player.transform.position.x < transform.position.x) || (playerSpriteRenderer.flipX && player.transform.position.x > transform.position.x))
            {
                facingPlayer = true;
            }
            else
            {
                facingPlayer = false;
            }
        }
    }
    public void CheckBystandersInCarriage()
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

    private void GizmosLines()
    {
        Helpers.DrawCircle(transform.position, closeToPlayerThreshold, armDistanceFromPlayer ? Color.red : Color.green, 8, false);
    }
}
