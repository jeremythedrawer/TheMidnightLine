using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Atlas
{
    static Dictionary<AtlasSO, GraphicsBuffer> buffers = new Dictionary<AtlasSO, GraphicsBuffer>();
    static float clock;
    public enum NPCMotion
    {
        None,
        SittingAboutToEat,
        SittingAboutToRead,
        SittingBlinking,
        SittingBreathing,
        SittingCalling,
        SittingEating,
        SittingMusic,
        SittingReading,
        SittingSick,
        SittingSleeping,
        Smoking,
        StandingAboutToEat,
        StandingBlinking,
        StandingBreathing,
        StandingCalling,
        StandingEating,
        StandingMusic,
        StandingReading,
        StandingSick,
        StandingSleeping,
        Walking,
        NPCMotionCount,
    }
    public enum SpyMotion
    { 
        None,
        Walking,
        Running,
    }
    public enum ClipType
    {
        Loop,
        PingPong,
        OneShot,
    }
    [Flags] public enum MarkerType
    {
        None = 0,
        Smoke = 1 << 0,
        Phone = 1 << 1,
        SleepingZs = 1 << 2,
        Music = 1 << 3,
    }
    [Serializable] public struct AtlasMarker
    {
        public MarkerType type;
        public Color32 color;
    }
    [Serializable] public struct SpriteMarker
    {
        public MarkerType type;
        public Vector2 objectPos;
    }
    [Serializable] public struct AtlasSprite
    {
        public int index;
        public Vector2 uvPos;
        public Vector2 uvSize;
        public Vector2 uvPivot; 
        public SpriteMarker[] markers;
    }
    [Serializable] public struct AtlasKeyframe
    {
        public int spriteIndex;
        public int holdTime;
    }
    [Serializable] public struct AtlasClip
    {
        public NPCMotion motion;
        public ClipType clipType;
        public AtlasKeyframe[] keyFrames;
    }
    struct MaterialAtlasSprite
    {
        public Vector2 uvPos;
        public Vector2 uvSize;
        public Vector2 pivot;
    }
    public static int NextFrameIndex(AtlasClip clip, int fps, float keyframeClock, int curFrameIndex, int prevFrameIndex = 0)
    {
        keyframeClock *= fps;
        AtlasKeyframe curKeyFrame = clip.keyFrames[curFrameIndex];

        switch (clip.clipType)
        {
            case ClipType.Loop:
            {
                if (keyframeClock >= curKeyFrame.holdTime)
                {
                    curFrameIndex++;
                    curFrameIndex %= clip.keyFrames.Length; 
                }
            }
            break;
            case ClipType.PingPong:
            {   
                
                if (keyframeClock >= curKeyFrame.holdTime)
                {
                    if (curFrameIndex < clip.keyFrames.Length - 1 && (curFrameIndex > prevFrameIndex || curFrameIndex == 0))
                    {

                        curFrameIndex++;
                    }
                    else
                    {
                        curFrameIndex--;
                    }
                }
            }
            break;
            case ClipType.OneShot:
            {
                if (keyframeClock >= curKeyFrame.holdTime)
                {
                    curFrameIndex++;
                    curFrameIndex = Mathf.Min(curFrameIndex, clip.keyFrames.Length - 1);
                }
            }
            break;
        }

        return curFrameIndex;
    }
    public static Dictionary<NPCMotion, AtlasClip> BuildClipKeys(AtlasClip[] clips)
    {
        Dictionary<NPCMotion, AtlasClip> clipDict = new Dictionary<NPCMotion, AtlasClip>();

        for (int i = 0; i < clips.Length; i++)
        {
            AtlasClip clip = clips[i];
            clipDict[clip.motion] = clip;
        }

        return clipDict;
    }
    public static GraphicsBuffer GetAtlasSpriteBuffer(AtlasSO atlas)
    {
        int floatSize = sizeof(float);
        int float2Size = floatSize * 2;
        int atlasStride = float2Size * 3;

        GraphicsBuffer buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, atlas.sprites.Length, atlasStride);

        MaterialAtlasSprite[] matAtlasSprites = new MaterialAtlasSprite[atlas.sprites.Length];

        for (int i = 0; i < atlas.sprites.Length; i++)
        {
            AtlasSprite sprite = atlas.sprites[i];

            matAtlasSprites[i].uvPos = sprite.uvPos;
            matAtlasSprites[i].uvSize = sprite.uvSize;
            matAtlasSprites[i].pivot = sprite.uvPivot;
        }

        buffer.SetData(matAtlasSprites);
        buffers[atlas] = buffer;

        return buffer;
    }
    public static void ReleaseAll()
    {
        foreach (GraphicsBuffer buffer in buffers.Values)
        {
            buffer?.Release();
        }
        buffers.Clear();
    }
}
#if UNITY_EDITOR
[InitializeOnLoad]
static class AtlasMaterialRebinder
{
    static AtlasMaterialRebinder()
    {
        EditorApplication.projectChanged += RebindAll;
        EditorApplication.hierarchyChanged += RebindAll;
        EditorApplication.playModeStateChanged += _ => RebindAll();
    }

    static void RebindAll()
    {
        Atlas.ReleaseAll();
        foreach (AtlasSO atlas in Resources.FindObjectsOfTypeAll<AtlasSO>()) // TODO: Replace and use the one AtlasSOs
        {
            atlas.UpdateAtlas();
        }
    }
}
#endif
