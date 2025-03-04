using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SlideDoorBounds : Bounds
{
    [Header("References")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Parameters")]
    public AnimationCurve doorOpeningCurve;

    public static SlideDoorBounds Instance { get; private set; }

    private SpriteRenderer leftDoorSprite;
    public bool openDoor {  get; private set; }

    private float doorWidth;
    private Vector3[] initialPositions;

    private InsideBounds insideBounds;
    private int trainGroundLayer => GlobalReferenceManager.Instance.trainGroundLayer;
    private int stationGroundLayer => GlobalReferenceManager.Instance.stationGroundLayer;

    public float normMoveDoorTime {  get; private set; }
    public BoxCollider2D boxCollider { get; private set; }

    public Queue<StateCore> enteringcharacterQueue = new Queue<StateCore>();
    public Queue<StateCore> exitingcharacterQueue = new Queue<StateCore>();

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }
    private void Start()
    {
        leftDoorSprite = leftDoor.gameObject.GetComponent<SpriteRenderer>();
        doorWidth = leftDoorSprite.sprite.bounds.size.x;
        TransferCharacters();

    }
    private async void TransferCharacters()
    {
        while (true)
        {
            while (exitingcharacterQueue.Count > 0 && normMoveDoorTime >= 1)
            {
                StateCore characterExitingTrain = exitingcharacterQueue.Peek();
                await TransferCharacter(characterExitingTrain, false);
                await Task.Delay(250);
                exitingcharacterQueue.Dequeue();
            }

            while (enteringcharacterQueue.Count > 0 && normMoveDoorTime >= 1)
            {
                StateCore characterEnteringTrain = enteringcharacterQueue.Peek();
                await TransferCharacter(characterEnteringTrain, true);
                await Task.Delay(250);
                enteringcharacterQueue.Dequeue();
            }
            await Task.Yield();
        }
    }

    private async Task TransferCharacter(StateCore character, bool enteringTrain)
    {
        character.spriteRenderer.sortingOrder = enteringTrain? 6 : 1;
        character.boxCollider2D.excludeLayers |= 1 << (enteringTrain ? stationGroundLayer : trainGroundLayer);
        character.boxCollider2D.excludeLayers &= ~(1 << (enteringTrain ? trainGroundLayer : stationGroundLayer));
        character.collisionChecker.activeGroundLayer = 1 << (enteringTrain ? trainGroundLayer : stationGroundLayer);

        if (enteringTrain)
        {
            trainData.charactersOnTrain.Add(character);
            trainData.nextStation.charactersList.Remove(character);
        }
        else
        {
            trainData.charactersOnTrain.Remove(character);
        }
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
