using UnityEditor;
using UnityEngine;
using static Atlas;
using static AtlasSpawn;
[CreateAssetMenu(fileName = "TextAtlas", menuName = "Atlas / Text Atlas")]
public class TextAtlas : ScriptableObject
{
    public AtlasSO atlas;

    [Header("Generated")]
    public float typeWorldHeight;

    public void SetWorldSpaceLineHeight()
    {
        float maxHeight = float.MinValue;
        for (int i = 0; i < atlas.simpleSprites.Length; i++)
        {
            SimpleSprite sprite = atlas.simpleSprites[i];

            if (sprite.worldSize.y > maxHeight)
            {
                maxHeight = sprite.worldSize.y;
            }
        }
        typeWorldHeight = maxHeight;
    }
}
