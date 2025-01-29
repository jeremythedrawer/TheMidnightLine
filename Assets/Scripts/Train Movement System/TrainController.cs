using UnityEngine;

public class TrainController : MonoBehaviour
{
    public enum Direction { Left, Right }
    public Direction trainDirection;
    public float maxSpeed = 1f;

    public float distanceTravelled {  get; private set; }
    private float currentSpeed;


    private void Update()
    {
        currentSpeed = trainDirection == Direction.Right ? maxSpeed : -maxSpeed;

        float frameDistance = currentSpeed * Time.deltaTime;

        distanceTravelled += frameDistance;
    }
}
