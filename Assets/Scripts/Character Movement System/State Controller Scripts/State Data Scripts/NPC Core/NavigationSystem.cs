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
            movementInputs.jumpInput = true;
        }
        else
        {
            movementInputs.jumpInput = false;
        }
    }

    private void CreatePath()
    {
        Vector2 start = new Vector2(transform.position.x, transform.position.y) + new Vector2(0,(boxCollider.size.y/2f));
        Vector2 target = new Vector2(playerPos.x, playerPos.y) + new Vector2(0, (playerCollider.size.y/2f));

        RaycastHit2D hit = Physics2D.Linecast(start, target, climbingLayer);

        if (hit.collider != null)
        {
            foundClimbingBounds = true;
        }
        else
        {
            foundClimbingBounds = false;
        }
        Debug.DrawLine(start, target, foundClimbingBounds ? Color.red : Color.green);
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
