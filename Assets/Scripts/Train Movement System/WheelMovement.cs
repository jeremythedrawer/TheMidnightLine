using UnityEngine;

public class WheelMovement : MonoBehaviour
{
    private TrainController trainController;
    private SpriteRenderer spriteRenderer;

    private float radius => (spriteRenderer.sprite.rect.size.x / 2) / spriteRenderer.sprite.pixelsPerUnit;
    private float circumference => 2 * Mathf.PI * radius;
    private float degreesPerMeter => 360f / circumference;
    void Start()
    {
        trainController = transform.root.GetComponent<TrainController>();
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float degreesPerSec = trainController.mPerSec * degreesPerMeter;
        transform.Rotate(0, 0, -degreesPerSec * Time.deltaTime);
    }
}
