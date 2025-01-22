using System.Collections;
using UnityEditor.Profiling;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public GameObject player;

    public float height = 0f;
    public float regularProjectionSize = 5.0f;
    public float roofProjectionSize = 8.0f;
    public float damping = 5.0f;
    public float cameraBiasOffset = 1f;
    public bool seeGizmos;
    //Camera bias
    private float widthOffset;

    private float initialDamping;
    private float initialHeight;

    private float camHeight;
    private float camWidth;


    //Falling camera function
    private float fallstateEndTime = 0.4f;
    private float currentfallStateTime;

    private float horizontalDamping;
    private float verticalDamping;

    private float rootFourT;

    private Vector3 target;

    private float updatingBoundOffset;

    private float camHalfWidthWorld;

    // setting camera projection size
    private float currentCamSizeTransTime = 0.0f;

    //camera components
    private Camera cam;

    //train components
    private TrainBounds trainCamBounds;

    //player components
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D playerRb;
    private PlayerMovement playerMovement;
    private AirborneState airborneState;

    private void Start()
    {
        cam = Camera.main;

        spriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
        playerRb = player.GetComponent<Rigidbody2D>();
        playerMovement = player.GetComponent<PlayerMovement>();
        airborneState = playerMovement.stateList.airborneState;

        trainCamBounds = Object.FindFirstObjectByType<TrainBounds>();

        camWidth = cam.pixelWidth;
        camHeight = cam.pixelHeight;

        //camHalfWidthWorld = 
        //TODO: find the start value of camHalfWidthWorld
        initialDamping = damping;
        initialHeight = height;
    }
    private void Update()
    {
        SetCameraTarget();
        SetCameraPos();
        SetCameraSize();

        DrawDebugLines();
    }
    private void CameraBias()
    {
        if (Mathf.Abs(playerRb.linearVelocityX) < playerMovement.characterStats.runSpeed)
        {
            widthOffset = spriteRenderer.flipX ? -cameraBiasOffset : cameraBiasOffset;
        }
        else
        {
            widthOffset = 0.0f;
        }
    }

    private void SetCameraTarget()
    {
        CameraBias();
        target = player.transform.TransformPoint(widthOffset, height, 0);
        horizontalDamping = Mathf.Abs(widthOffset) > 0 ? initialDamping : initialDamping * 2;
        FallingCamera();

        if (InsideBounds.Instance == null)
        {
            target.y = Mathf.Max(target.y, trainCamBounds.wheelLevel + cam.orthographicSize);
        }
        else
        {
            CalculateCarriageBoundOffset();
            target.x = Mathf.Clamp(target.x, InsideBounds.Instance.leftEdge + updatingBoundOffset, InsideBounds.Instance.rightEdge - updatingBoundOffset);
            target.y = trainCamBounds.wheelLevel + cam.orthographicSize;
        }
    }

    private void SetCameraPos()
    {
        float camWorldPosX = Mathf.Lerp(transform.position.x, target.x, Time.deltaTime * horizontalDamping);
        float camWorldPosY = Mathf.Lerp(transform.position.y, target.y, Time.deltaTime * verticalDamping);

        transform.position = new Vector2 (camWorldPosX, camWorldPosY);
    }

    private void FallingCamera()
    {
        if (playerRb.linearVelocityY <= -airborneState.clampedFallSpeed) // when player is falling
        {
            currentfallStateTime += Time.deltaTime;
            verticalDamping = initialDamping * 2f;
            float t = Mathf.Clamp(currentfallStateTime / fallstateEndTime, 0f, 1f);
            height = initialHeight - Mathf.Lerp(initialHeight, 0.5f * cam.orthographicSize, t);
        }
        else
        {
            currentfallStateTime = 0;
            verticalDamping = initialDamping;
            height = initialHeight;
        }
    }

    private void CalculateCarriageBoundOffset()
    {
        float carriageOriginX = InsideBounds.Instance.transform.position.x;
        float playerOriginX = player.transform.position.x;
        float carriageSize = InsideBounds.Instance.rightEdge - InsideBounds.Instance.leftEdge;
        float normalizedCarriageX = 1.0f - ((Mathf.Abs(carriageOriginX - playerOriginX) / carriageSize) * 2.0f);

        rootFourT = Mathf.Pow(normalizedCarriageX, 0.25f);
        camHalfWidthWorld = cam.orthographicSize * cam.aspect;

        updatingBoundOffset = Mathf.Lerp(0, camHalfWidthWorld, rootFourT);
    }

    private void SetCameraSize()
    {
        float endTime = 1.0f;
        float t = currentCamSizeTransTime / endTime;

        if (player.transform.position.y > trainCamBounds.roofLevel)
        {
            currentCamSizeTransTime = Mathf.Min(currentCamSizeTransTime + Time.deltaTime, endTime);
        }
        else
        {
            currentCamSizeTransTime = Mathf.Max(currentCamSizeTransTime - Time.deltaTime, 0);
        }
        Camera.main.orthographicSize = Mathf.Lerp(regularProjectionSize, roofProjectionSize, t);
    }

    private void DrawDebugLines()
    {
#if UNITY_EDITOR
        if (seeGizmos)
        {
            if (cam == null) return;

            Vector3 top = cam.ScreenToWorldPoint(new Vector3(camWidth / 2.0f, camHeight, 0.0f));
            Vector3 bottom = cam.ScreenToWorldPoint(new Vector3(camWidth / 2.0f, 0.0f, 0.0f));
            Vector3 left = cam.ScreenToWorldPoint(new Vector3(0.0f, camHeight / 2.0f, 0.0f));
            Vector3 right = cam.ScreenToWorldPoint(new Vector3(camWidth, camHeight / 2.0f, 0.0f));

            // Draw the lines
            Debug.DrawLine(top, bottom, Color.red); // Vertical line
            Debug.DrawLine(left, right, Color.blue); // Horizontal line
        }
#endif
    }

}
