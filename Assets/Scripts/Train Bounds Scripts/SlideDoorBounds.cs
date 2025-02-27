using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class SlideDoorBounds : Bounds
{
    [Header("References")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Parameters")]
    public AnimationCurve doorOpeningCurve;

    public static SlideDoorBounds Instance { get; private set; }

    private SpriteRenderer leftDoorSprite;
    private static TrainData trainData => GlobalReferenceManager.Instance.trainData;
    public bool openDoor {  get; private set; }

    private float doorWidth;
    private Vector3[] initialPositions;

    private InsideBounds insideBounds;
    private int trainGroundLayer => GlobalReferenceManager.Instance.trainGroundLayer;
    private int stationGroundLayer => GlobalReferenceManager.Instance.stationGroundLayer;

    public float normMoveDoorTime {  get; private set; }
    public BoxCollider2D boxCollider { get; private set; }

    public Queue<StateCore> characterQueue = new Queue<StateCore>();

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }
    private void Start()
    {
        leftDoorSprite = leftDoor.gameObject.GetComponent<SpriteRenderer>();
        doorWidth = leftDoorSprite.sprite.bounds.size.x;
        TransferNPCsToTrain();

    }
    private async void TransferNPCsToTrain()
    {
        while (true)
        {
            while (characterQueue.Count > 0 && normMoveDoorTime >= 1)
            {
                StateCore characterEnteringTrain = characterQueue.Peek();
                await BoardCharacters(characterEnteringTrain);
                await Task.Delay(250);
                characterQueue.Dequeue();
            }
            await Task.Yield();
        }

    }

    private async Task BoardCharacters(StateCore character)
    {
        character.spriteRenderer.sortingOrder = 6;
        character.boxCollider2D.excludeLayers |= 1 << stationGroundLayer;
        character.boxCollider2D.excludeLayers &= ~(1 << trainGroundLayer);
        character.collisionChecker.activeGroundLayer = 1 << trainGroundLayer;
        trainData.charactersList.Add(character);
        trainData.currentStation.charactersList.Remove(character);
        await Task.Yield();
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
            if (!nPCCore.onTrain) StartCoroutine(TriggeringInsideBounds(collision, nPCCore));
        }

        if (collision.gameObject.CompareTag("Player Collider")) Instance = this;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider")) Instance = null;
    }

    private IEnumerator TriggeringInsideBounds(Collider2D collision, NPCCore npcCore)
    {
        yield return new WaitUntil(() => npcCore.onTrain);

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
            normMoveDoorTime = elsapedTime / openingTime;
            normMoveDoorTime = doorOpeningCurve.Evaluate(normMoveDoorTime);

            MoveDoor(leftDoor, initialPositions[0], -moveDistance, normMoveDoorTime);
            MoveDoor(rightDoor, initialPositions[1], moveDistance, normMoveDoorTime);

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
