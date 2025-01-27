using UnityEngine;

public class CharacterMaterial : MonoBehaviour
{
    [Header ("References")]
    public SpriteRenderer spriteRen;

    private float minBrightness = 0.01f;
    private float maxBrightness = 0.05f;

    private MaterialPropertyBlock propBlock;

    private bool charIsSeated;
    private void Start()
    {
        propBlock = new MaterialPropertyBlock();
    }

    public void SendCharToSeatLayer()
    {
        if (propBlock != null && !charIsSeated)
        {
            spriteRen.GetPropertyBlock(propBlock);

            float randomBrightness = Random.Range(minBrightness, maxBrightness);
            propBlock.SetFloat("_Brightness", randomBrightness);

            spriteRen.SetPropertyBlock(propBlock);

            spriteRen.sortingOrder = 3;
            charIsSeated = true;
        }
    }
}
