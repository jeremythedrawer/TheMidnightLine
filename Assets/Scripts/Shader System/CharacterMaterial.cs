using UnityEngine;

public class CharacterMaterial : MonoBehaviour
{
    [Header ("References")]
    public SpriteRenderer spriteRen;

    private float minBrightness = 0.001f;
    private float maxBrightness = 0.02f;

    private MaterialPropertyBlock propBlock;

    private bool charIsSeated;
    private bool charIsStanding;
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

            spriteRen.sortingOrder = 4;
            charIsSeated = true;
        }
    }
    public void SendCharToStandLayer()
    {
        if (propBlock != null && !charIsStanding)
        {
            spriteRen.GetPropertyBlock(propBlock);

            int randomStandingLayer = Random.Range(6, 8);
            spriteRen.sortingOrder = randomStandingLayer;
            charIsStanding = true;
        }
    }
}
