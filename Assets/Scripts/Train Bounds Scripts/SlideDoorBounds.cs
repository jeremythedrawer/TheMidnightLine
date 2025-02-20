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

    private float doorWidth;
    private Vector3[] initialPositions;
    private void Start()
    {
        leftDoorSprite = leftDoor.gameObject.GetComponent<SpriteRenderer>();
        doorWidth = leftDoorSprite.sprite.bounds.size.x;
    }
    public IEnumerator OpeningDoors()
    {
        //Time Variables
        float openingTime = 3;
        float elsapedTime = 0;
        initialPositions = new Vector3[2] { leftDoor.position, rightDoor.position };
        while (elsapedTime < openingTime)
        {
            elsapedTime += Time.deltaTime;
            float normalizedTime = elsapedTime / openingTime;
            normalizedTime = doorOpeningCurve.Evaluate(normalizedTime);

            OpenDoor(leftDoor, initialPositions[0], -doorWidth, normalizedTime);
            OpenDoor(rightDoor, initialPositions[1], doorWidth, normalizedTime);

            yield return null;
        }
        leftDoor.position = new Vector3(initialPositions[0].x - doorWidth, initialPositions[0].y, initialPositions[0].z);
        rightDoor.position = new Vector3(initialPositions[1].x + doorWidth, initialPositions[1].y, initialPositions[1].z);
    }

    private void OpenDoor(Transform door, Vector3 initialPos, float offset, float time)
    {
        float updatingXPos = Mathf.Lerp(initialPos.x, initialPos.x + offset, time);
        door.position = new Vector3(updatingXPos, initialPos.y, initialPos.z);

    }
}
