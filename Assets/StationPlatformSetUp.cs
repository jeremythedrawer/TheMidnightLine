using UnityEngine;

public class StationPlatformSetUp : MonoBehaviour
{
    [Header("References")]
    public BoxCollider2D boxCollider;
    public SpriteRenderer spriteRenderer;

    private void OnValidate()
    {
        if (boxCollider != null && spriteRenderer != null)
        {
            boxCollider.offset = new Vector2(spriteRenderer.size.x/2, spriteRenderer.size.y/2);
            boxCollider.size = new Vector2(spriteRenderer.size.x, spriteRenderer.size.y - 0.25f);
        }
        else
        {
            Debug.LogWarning("The platform of " + gameObject.transform.parent.name + " needs references filled");
        }
    }
}
