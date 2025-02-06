using UnityEngine;

public class SkyLine : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public ParallaxController parallaxController;

    [Header("Parameters")]
    public float lengthInMeters  = 1000f;

#if UNITY_EDITOR
    public void ApplyLength()
    {
        spriteRenderer.size = new Vector2(lengthInMeters * parallaxController.parallaxFactor, spriteRenderer.size.y);
        transform.localScale = Vector2.one;
    }
#endif
}
