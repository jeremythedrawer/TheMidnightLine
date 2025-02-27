using UnityEngine;

public class WheelData : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public float radius => (spriteRenderer.sprite.rect.size.x / spriteRenderer.sprite.pixelsPerUnit) / 2;
    public float circumference => 2 * Mathf.PI * radius;
    public float degreesPerMeter => 360f / circumference;

    private void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }
}
