using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AtlasTexture", menuName = " 2D Pipeline / Atlas Texture")]
public class AtlasSO : ScriptableObject
{

    public Texture2D texture;
    public int pixelsPerUnit = 32;
    public Color32 customPivotColor = Color.red;
    public Atlas.AtlasMarker[] markers;
    public int framesPerSecond = 30;

    public Atlas.AtlasSprite[] sprites;
    public Atlas.AtlasClip[] clips;

    public Dictionary<Atlas.NPCMotion, Atlas.AtlasClip> clipDict;

    private void OnEnable()
    {
        clipDict = Atlas.BuildClipKeys(clips);
    }
}

