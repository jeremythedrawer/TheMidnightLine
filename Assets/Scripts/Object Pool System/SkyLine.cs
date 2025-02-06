using UnityEngine;

public class SkyLine : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public ParallaxController parallaxController;

    [Header("Parameters")]
    public float lengthInMeters  = 1000f;
    public float positionFromStart = 0f;

#if UNITY_EDITOR
    public void ApplyTileAndOffset()
    {
        spriteRenderer.size = new Vector2(lengthInMeters * parallaxController.parallaxFactor, spriteRenderer.size.y);
        transform.localScale = Vector2.one;

        transform.position = new Vector3(positionFromStart, transform.position.y, transform.position.z);
    }
#endif
}
