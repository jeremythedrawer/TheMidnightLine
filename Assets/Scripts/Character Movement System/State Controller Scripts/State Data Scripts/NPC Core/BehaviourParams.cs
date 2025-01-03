using Unity.VisualScripting;
using UnityEngine;

public class BehaviourParams : MonoBehaviour
{
    [Header("Radius Thesholds")]
    public float armDistance = 2f;
    public float awareOfPlayerDistance = 10f;

    [Header("Player Data")]
    public LayerMask playerLayer;


    //parameter bools
    public bool armDistanceFromPlayer { get; private set; }
    public bool awareOfPlayer { get; private set; }
    public bool playerIsFacingNPC { get; private set; }
    public bool isCarriageEmpty { get; private set; }
    public bool inCameraView { get; private set; }
    public bool inCarriage {  get; private set; }

    public bool onRoof { get; private set; }

    //types
    public GameObject player { get; private set; }

    public bool seeGizmos;

    private SpriteRenderer playerSpriteRenderer;

    private Camera cam;

    private ActivateCarriageBounds currentCarriage;
    private TrainBounds trainBounds;

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if(seeGizmos)
        {
            if (Application.isPlaying) return;
            Helpers.DrawCircle(transform.position, armDistance, Color.red, 36, true);
            Helpers.DrawCircle(transform.position, awareOfPlayerDistance, Color.yellow, 36, true);
        }
#endif
    }
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerSpriteRenderer = player.GetComponent<SpriteRenderer>();

        cam = Camera.main;

        trainBounds = Object.FindFirstObjectByType<TrainBounds>();
    }

    private void Update()
    {
        CheckInCameraView();
        CheckPlayerCharacterSight();
        CheckBystandersInCarriage();
        CheckOnRoof();

#if UNITY_EDITOR
        if (seeGizmos)
        {
            GizmosLines();
        }
#endif
    }

    private void FixedUpdate()
    {
        ChechRadiusThreshold(armDistance, armDistanceFromPlayer);
        ChechRadiusThreshold(awareOfPlayerDistance, awareOfPlayer);
        
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

    public void ChechRadiusThreshold(float distanceThreshold, bool parameter)
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, distanceThreshold, playerLayer);

        if (hit != null)
        {
            parameter = true;
        }
        else
        {
            parameter = false;
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
                playerIsFacingNPC = true;
            }
            else
            {
                playerIsFacingNPC = false;
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

    public void CheckOnRoof()
    {
        if (transform.position.y > trainBounds.roofLevel)
        {
            onRoof = true;
        }
        else
        {
            onRoof = false;
        }
    }

    private void GizmosLines()
    {
        Helpers.DrawCircle(transform.position, armDistance, armDistanceFromPlayer ? Color.red : Color.green, 8, false);
    }
}
