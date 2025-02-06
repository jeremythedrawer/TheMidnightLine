using UnityEngine;

public class TrainWheelMovement : MonoBehaviour
{
    public TrainController trainMovement;
    void Update()
    {
        transform.Rotate(0,0, -trainMovement.KmPerHour);
    }
}
