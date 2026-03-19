using System.Collections.Generic;
using UnityEngine;
using static Atlas;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "AtlasSprite_SO", menuName = "Atlas / Atlas Sprites")]
public class AtlasSO : ScriptableObject
{
    public Texture2D texture;
    public Color32 spriteConnectColor = Color.clear;


    [Header("Motion Settings")]
    public EntityMotionType entityMotionType;
    public Color32 pivotColor = Color.clear;
    
    [Header("Sliced Settings")]
    public Color32 sliceColor = Color.clear;

    [Header("Particle Settings")]
    public ZoneParticleType zoneType;

    [Header("Generated")]
    public MotionSprite[] motionSprites;
    public SimpleSprite[] simpleSprites;
    public SliceSprite[] slicedSprites;
    public AtlasClip[] clips;
    public MarkerKey[] markers;
    
    public Dictionary<int, AtlasClip> clipDict;

    private void OnEnable()
    {
        UpdateClipDictionary();
    }
    public void UpdateClipDictionary()
    {
        clipDict = BuildClipKeys(clips);
    }
}

