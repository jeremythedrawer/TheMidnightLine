using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class Atlas
{
    static Dictionary<AtlasSO, GraphicsBuffer> buffers = new Dictionary<AtlasSO, GraphicsBuffer>();
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
        StandingBreathing,
        Climbing,
        Hanging,
        GrabLedge,
        StartRun,
        StandingCalling,
        Clipboard,
        Jump,
        Fall,
        HeavyLand,
        Death,
    }

    public enum EntityType
    {
        NPC,
        Spy
    }
    public enum ClipType
    {
        Loop,
        PingPong,
        OneShot,
        Manual,
    }
    [Flags] public enum MarkerType
    {
        None = 0,
        Smoke = 1 << 0,
        Talking = 1 << 1,
        SleepingZs = 1 << 2,
        Music = 1 << 3,
        Climb = 1 << 4,
        TrainPhone = 1 << 5,
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
        public ClipType clipType;
        public int motionIndex;
#if UNITY_EDITOR
        public string clipName;
#endif
        public AtlasKeyframe[] keyFrames;
    }

    [Serializable] public struct SpyClip
    {
        public SpyMotion motion;
        public AtlasClip clip;
    }
    [Serializable] public struct NPCClip
    {
        public NPCMotion motion;
        public AtlasClip clip;
    }
    struct MaterialAtlasSprite
    {
        public Vector2 uvPos;
        public Vector2 uvSize;
        public Vector2 pivot;
    }
    public static void SetNextFrameIndex(AtlasClip clip, int fps, ref float keyframeClock, ref int curFrameIndex, ref int prevFrameIndex)
    {
        float frameTime =  keyframeClock * fps;
        AtlasKeyframe curKeyFrame = clip.keyFrames[curFrameIndex];

        switch (clip.clipType)
        {
            case ClipType.Loop:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    prevFrameIndex = curFrameIndex;
                    curFrameIndex++;

                    if (curFrameIndex >= clip.keyFrames.Length)
                    {
                        curFrameIndex = 0;
                    }

                    keyframeClock = 0;
                }
            }
            break;
            case ClipType.PingPong:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    if (curFrameIndex < clip.keyFrames.Length - 1 && (curFrameIndex > prevFrameIndex || curFrameIndex == 0))
                    {
                        prevFrameIndex = curFrameIndex;
                        curFrameIndex++;
                    }
                    else
                    {
                        prevFrameIndex = curFrameIndex;
                        curFrameIndex--;
                    }
                    keyframeClock = 0;
                }
            }
            break;
            case ClipType.OneShot:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    prevFrameIndex = curFrameIndex;
                    if (curFrameIndex < clip.keyFrames.Length - 1)
                    {
                        curFrameIndex++;
                    }
                    keyframeClock = 0;
                }
            }
            break;
        }
    }
    public static void NextFrameIndexManual(AtlasClip clip, float holdTime, ref float currentValue, ref int curFrameIndex)
    {
        if (currentValue > holdTime)
        {
            if (curFrameIndex < clip.keyFrames.Length - 1)
            {
                curFrameIndex++;
            }
            currentValue = 0;
        }
    }

    public static float GetManualKeyframeHoldTime(AtlasClip clip, float targetValue, float startValue)
    {
        return (targetValue - startValue) / (clip.keyFrames.Length - 1);
    }
    public static Dictionary<int, AtlasClip> BuildClipKeys(AtlasClip[] clips)
    {
        Dictionary<int, AtlasClip> clipDict = new Dictionary<int, AtlasClip>();

        for (int i = 0; i < clips.Length; i++)
        {
            AtlasClip clip = clips[i];
            clipDict[clip.motionIndex] = clip;
        }

        return clipDict;
    }
    public static GraphicsBuffer GetAtlasSpriteBuffer(AtlasSO atlas)
    {
        int floatSize = sizeof(float);
        int float2Size = floatSize * 2;
        int atlasStride = float2Size * 3;

        if (atlas.sprites.Length == 0) 
        { 
            Debug.LogWarning($" {atlas.name} Does not have sprites generated yet"); 
            return null; 
        }

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
