using UnityEngine;
using static Atlas;

public abstract class AtlasBaseSO : ScriptableObject
{
    public AtlasSprite[] sprites;
    public Texture2D texture;
    public Material material;
    public Color32 pivotColor = Color.red;
    public AtlasMarker[] markers;
}
