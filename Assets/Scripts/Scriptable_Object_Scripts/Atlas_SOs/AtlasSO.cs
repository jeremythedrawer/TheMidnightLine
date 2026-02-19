using System.Collections.Generic;
using UnityEngine;
using static Atlas;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "AtlasSprite_SO", menuName = "Atlas / Atlas Sprites")]
public class AtlasSO : ScriptableObject
{
    public Texture2D texture;
    public bool isParticleAtlas;

    [Header("Motion Settings")]
    public EntityMotionType entityMotionType;
    public Color32 pivotColor = Color.red;
    
    [Header("Sliced Settings")]
    public Color32 sliceColor = Color.blue;

    [Header("Particle Settings")]
    public ParticleType particleType;

    [Header("Generated")]
    public MotionSprite[] motionSprites;
    public SimpleSprite[] simpleSprites;
    public SliceSprite[] slicedSprites;
    public ParticleSprite[] particleSprites;
    public Vector4[] particleUVSizeAndPosArray;
    public AtlasClip[] clips;
    public MarkerKey[] markers;
    
    public Dictionary<int, AtlasClip> clipDict;
    public void UpdateClipDictionary()
    {
        clipDict = BuildClipKeys(clips);
    }
}

