using UnityEngine;

public class TrainWheelMovement : MonoBehaviour
{
    public TrainMovement trainMovement;
    void Update()
    {
        transform.Rotate(0,0, trainMovement.trainSpeed * Time.deltaTime);
    }
}
