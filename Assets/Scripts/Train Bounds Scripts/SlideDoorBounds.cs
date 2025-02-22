using UnityEngine;
using System.Collections;

public class SlideDoorBounds : Bounds
{
    [Header("References")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Parameters")]
    public AnimationCurve doorOpeningCurve;

    private SpriteRenderer leftDoorSprite;

    public bool openDoor {  get; private set; }

    private float doorWidth;
    private Vector3[] initialPositions;

    private InsideBounds insideBounds;
    private int trainGroundLayer => LayerMask.NameToLayer("Train Ground");
    private void Start()
    {
        leftDoorSprite = leftDoor.gameObject.GetComponent<SpriteRenderer>();
        doorWidth = leftDoorSprite.sprite.bounds.size.x;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Inside Bounds") && insideBounds == null)
        {
            insideBounds = collision.GetComponent<InsideBounds>();
        }
        if (collision.gameObject.CompareTag("Agent Collider") || collision.gameObject.CompareTag("Bystander Collider"))
        {
            NPCCore nPCCore = collision.GetComponentInParent<NPCCore>();
            StartCoroutine(TriggeringInsideBounds(collision, nPCCore));
        }
    }

    private IEnumerator TriggeringInsideBounds(Collider2D collision, NPCCore npcCore)
    {
        yield return new WaitUntil(() => npcCore.collisionChecker.activeGroundLayer == 1 << trainGroundLayer);
        if (npcCore.pathData.chosenInsideBounds != null) yield break;
        if (insideBounds.boxCollider.bounds.Intersects(collision.bounds))
        {
            insideBounds.OnTriggerEnter2D(collision);
        }
    }
    public void UnlockDoors()
    {
        StartCoroutine(DoorLock(false));
    }
    public void LockDoors()
    {
        StartCoroutine(DoorLock(true));
    }
    private IEnumerator DoorLock(bool lockDoors)
    {
        float timer = 0.1f;
        float elsapedTime = 0;
        initialPositions = new Vector3[2] { leftDoor.position, rightDoor.position };
        float moveDistance = lockDoors ? -doorWidth / 40 : doorWidth / 40;

        while (elsapedTime < timer)
        {
            elsapedTime += Time.deltaTime;
            float normalizedTime = elsapedTime / timer;

            MoveDoor(leftDoor, initialPositions[0], -moveDistance, normalizedTime);
            MoveDoor(rightDoor, initialPositions[1], moveDistance, normalizedTime);

            yield return null;
        }

        leftDoor.position = new Vector3(initialPositions[0].x - moveDistance, initialPositions[0].y, initialPositions[0].z);
        rightDoor.position = new Vector3(initialPositions[1].x + moveDistance, initialPositions[1].y, initialPositions[1].z);
    }

    public void OpenDoors()
    {
        if (!openDoor)
        {
            StartCoroutine(MoveDoors(false));
            openDoor = true;
        }
    }
    public void CloseDoors()
    {
        if (openDoor)
        {
            StartCoroutine(MoveDoors(true));
            openDoor = false;
        }
    }
    private IEnumerator MoveDoors(bool closeDoors)
    {
        //Time Variables
        float openingTime = 3;
        float elsapedTime = 0;
        initialPositions = new Vector3[2] { leftDoor.position, rightDoor.position };
        float moveDistance = closeDoors ? -doorWidth : doorWidth;

        while (elsapedTime < openingTime)
        {
            elsapedTime += Time.deltaTime;
            float normalizedTime = elsapedTime / openingTime;
            normalizedTime = doorOpeningCurve.Evaluate(normalizedTime);

            MoveDoor(leftDoor, initialPositions[0], -moveDistance, normalizedTime);
            MoveDoor(rightDoor, initialPositions[1], moveDistance, normalizedTime);

            yield return null;
        }
        leftDoor.position = new Vector3(initialPositions[0].x - moveDistance, initialPositions[0].y, initialPositions[0].z);
        rightDoor.position = new Vector3(initialPositions[1].x + moveDistance, initialPositions[1].y, initialPositions[1].z);
    }

    private void MoveDoor(Transform door, Vector3 initialPos, float offset, float time)
    {
        float updatingXPos = Mathf.Lerp(initialPos.x, initialPos.x + offset, time);
        door.position = new Vector3(updatingXPos, initialPos.y, initialPos.z);
    }
}
