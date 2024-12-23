using UnityEngine;

public enum Direction { Left, Right }
public class TrainMovement : MonoBehaviour
{
    public float speed = 1000f;
    public float acceleration = 1f;
    public Direction trainDirection;
    public float trainSpeed { get; private set; }

    private void Update()
    {
        trainSpeed = trainDirection == Direction.Left ? speed : -speed; 
    }
}
