using UnityEngine;

public class WheelMovement : MonoBehaviour
{
    private TrainController trainController;
    private SpriteRenderer spriteRenderer;

    private float radius => (spriteRenderer.sprite.rect.size.x / spriteRenderer.sprite.pixelsPerUnit) / 2;
    private float circumference => 2 * Mathf.PI * radius;
    private float degreesPerMeter => 360f / circumference;

    private float lastPos;
    void Start()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        trainController = transform.root.GetComponent<TrainController>();
        lastPos = transform.position.x;
    }

    private void Update()
    {
        float deltaX = transform.position.x - lastPos;
        lastPos = transform.position.x;

        float rotAngle = -deltaX * degreesPerMeter; // no error
        transform.Rotate(0, 0, rotAngle);
    }
}
