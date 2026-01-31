using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AtlasTexture", menuName = " 2D Pipeline / Atlas Texture")]
public class AtlasSO : ScriptableObject
{
    public Texture2D texture;
    public Material material;
    public MaterialIDSO materialIDs;
    public int pixelsPerUnit = 32;
    public Color32 customPivotColor = Color.red;
    public Atlas.AtlasMarker[] markers;
    public int framesPerSecond = 30;

    public Atlas.AtlasSprite[] sprites;
    public Atlas.AtlasClip[] clips;

    public Dictionary<Atlas.NPCMotion, Atlas.AtlasClip> clipDict;
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
        if (atlasBuffer != null)
        {
            atlasBuffer.Release();
            atlasBuffer = null;
        }
        atlasBuffer = Atlas.GetAtlasSpriteBuffer(this);

        material.SetBuffer(materialIDs.ids.atlasSprites, atlasBuffer);
        clipDict = Atlas.BuildClipKeys(clips);
    }
}

