using System.Collections.Generic;
using UnityEngine;
using static Atlas;

[CreateAssetMenu(fileName = "AtlasTexture", menuName = " 2D Pipeline / Atlas Texture")]
public class AtlasSO : ScriptableObject
{
    public EntityType entityType;
    public Texture2D texture;
    public Material material;
    public MaterialIDSO materialIDs;
    public int pixelsPerUnit = 32;
    public Color32 customPivotColor = Color.red;
    public AtlasMarker[] markers;
    public AtlasClip[] clips;
    public int framesPerSecond = 30;
    public AtlasSprite[] sprites;

    public Dictionary<int, AtlasClip> clipDict;

    GraphicsBuffer atlasBuffer;

    private void OnEnable()
    {
        UpdateAtlas();
    }

    public void UpdateAtlas()
    {
        if (!material)
        {
            Debug.LogWarning($"Material not set on {name}", this);
            return;
        }
        if (!materialIDs)
        {
            Debug.LogWarning($"Material IDS SO not set on {name}", this);
            return;
        }
        if (atlasBuffer != null)
        {
            atlasBuffer.Release();
            atlasBuffer = null;
        }
        atlasBuffer = GetAtlasSpriteBuffer(this);

        material.SetBuffer(materialIDs.ids.atlasSprites, atlasBuffer);
        clipDict = BuildClipKeys(clips);
    }
}

