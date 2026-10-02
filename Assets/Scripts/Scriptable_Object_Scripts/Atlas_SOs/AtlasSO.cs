using System.Collections.Generic;
using UnityEngine;
using static Atlas;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "AtlasSprite_SO", menuName = "Atlas / Atlas Sprites")]
public class AtlasSO : ScriptableObject
{
    public Texture2D texture;
    public Texture2D markerTexture;

    public EntityMotionType entityMotionType;

    [Header("Generated")]
    public MotionSprite[] motionSprites;
    public SimpleSprite[] simpleSprites;
    public SliceSprite[] slicedSprites;
    public AtlasClip[] clips;
    
    public Dictionary<int, AtlasClip> clipDict;

    public void UpdateClipDictionary()
    {
        clipDict = BuildClipKeys(clips);
    }
}

