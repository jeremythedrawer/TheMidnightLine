using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class NavigationSystem : MonoBehaviour
{
    [Header("Parameters")]
    public float updatePlayerPosTicRate = 1f;
    public float stopMovementBuffer = 1f;

    [Header("References")]
    public MovementInputs movementInputs;
    public Transform playerTransform;
    public LayerMask climbingLayer;
    public BoxCollider2D boxCollider;
    public BoxCollider2D playerCollider;

    
    private Vector2 playerPos;
    private Vector2 currentPos;
    private bool foundClimbingBounds;

    private Vector2 targetPos;
    private Vector2 closestGangway;
    private Vector2 closestClimbingPoint;
    private Vector2 startPos;





    void Start()
    {
        StartCoroutine(UpdatePlayerPosition());
    }

    void Update()
    {
    }

    public void MoveToTarget()
    {
        CreatePath();
        /*
        currentPos = transform.position;
        if (playerPos.x - stopMovementBuffer > currentPos.x + stopMovementBuffer)
        {
            movementInputs.walkInput = 1;
        }
        else if (playerPos.x + stopMovementBuffer < currentPos.x - stopMovementBuffer)
        {
            movementInputs.walkInput = -1;
        }
        else
        {
            movementInputs.walkInput = 0;
        }

        if (foundClimbingBounds)
        {
            Debug.Log(movementInputs.jumpInput);
            //TODO: find outside carriage bounds
            movementInputs.jumpInput = true;
        }
        else
        {
            movementInputs.jumpInput = false;
        }
        */
    }

    private void CreatePath()
    {
        startPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0,(boxCollider.size.y/2f));
        targetPos = new Vector2(playerPos.x, playerPos.y) + new Vector2(0, (playerCollider.size.y/2f));

        if (ActivateCarriageBounds.Instance != null && ActivateCarriageBounds.Instance.gameObject.CompareTag("Inside Bounds"))
        {
            Vector2 rightGangwayPos = ActivateCarriageBounds.Instance.rightGangwayPos;
            Vector2 leftGangwayPos = ActivateCarriageBounds.Instance.leftGangwayPos;
            
            if (ActivateCarriageBounds.Instance.isBackCarriage)
            {
                closestGangway = rightGangwayPos;

            }
            else if (ActivateCarriageBounds.Instance.isFrontCarriage)
            {
                closestGangway = leftGangwayPos;

            }
            else if (transform.position.x - leftGangwayPos.x < rightGangwayPos.x - transform.position.x)
            {
                closestGangway = ActivateCarriageBounds.Instance.leftGangwayPos;

            }
            else
            {
                closestGangway = rightGangwayPos;

            }

            closestClimbingPoint = new Vector2(closestGangway.x, closestGangway.y + ActivateCarriageBounds.Instance.boundsHeight);
        }

            Debug.DrawLine(startPos, closestGangway, Color.magenta);
            Debug.DrawLine(closestGangway, closestClimbingPoint, Color.magenta);
            Debug.DrawLine(closestClimbingPoint, targetPos, Color.magenta);
    }

    private IEnumerator UpdatePlayerPosition()
    {
        while (true)
        {
            playerPos = playerTransform.position;
            yield return new WaitForSeconds(updatePlayerPosTicRate);
        }
    }
}
