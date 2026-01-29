using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
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
        public Vector2 normPivot; 
        public SpriteMarker[] markers;
    }

    [Serializable] public struct AtlasKeyframe
    {
        public int spriteIndex;
        public int framePosition;
    }
    [Serializable] public struct AtlasClip
    {
        public NPCMotion motion;
        public AtlasKeyframe[] keyFrames;
        public float totalPlayTime;
        public int curFrameIndex;
    }

    struct MaterialAtlasSprite
    {
        public Vector2 uvPos;
        public Vector2 uvSize;
        public Vector2 pivot;
    }

    public static int GetCurrentSpriteIndex(AtlasClip clip, ClipType clipType, int fps)
    {
        clip.totalPlayTime = Mathf.Floor((clip.totalPlayTime + Time.deltaTime) * fps);
        float cycleTime = clip.totalPlayTime;

        switch (clipType)
        {
            case ClipType.Loop:
            {
                cycleTime %= clip.keyFrames[^1].framePosition;
                clip.curFrameIndex %= clip.keyFrames.Length;
            }
            break;
            case ClipType.PingPong:
            {
                int maxFramePos = clip.keyFrames[^1].framePosition;
                int pingPongCycle = maxFramePos * 2;
                float pingPongTime = clip.totalPlayTime % pingPongCycle;

                if (pingPongTime > maxFramePos)
                {
                    cycleTime = pingPongCycle - pingPongTime;
                    int prevIndex = clip.curFrameIndex--;
                    AtlasKeyframe prevKeyFrame = clip.keyFrames[prevIndex];
                    if (cycleTime < prevKeyFrame.framePosition)
                    {
                        clip.curFrameIndex = prevIndex;
                    }
                }
            }
            break;
            case ClipType.OneShot:
            {
                cycleTime = Mathf.Min(clip.totalPlayTime, clip.keyFrames[^1].framePosition);
                clip.curFrameIndex = Mathf.Min(clip.curFrameIndex, clip.keyFrames.Length);
            }
            break;
        }

        AtlasKeyframe curKeyFrame = clip.keyFrames[clip.curFrameIndex];
        if (cycleTime > curKeyFrame.framePosition)
        {
            clip.curFrameIndex++;
        }
        return curKeyFrame.spriteIndex;
    }
    public static Dictionary<NPCMotion, AtlasClip> BuildClipKeys(Atlas.AtlasClip[] clips)
    {
        Dictionary<Atlas.NPCMotion, Atlas.AtlasClip> clipDict = new Dictionary<Atlas.NPCMotion, Atlas.AtlasClip>();

        for (int i = 0; i < clips.Length; i++)
        {
            Atlas.AtlasClip clip = clips[i];
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
            matAtlasSprites[i].pivot = sprite.normPivot;
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
        foreach (AtlasSO atlas in Resources.FindObjectsOfTypeAll<AtlasSO>())
        {
            atlas.SetAtlasSpriteBuffer();
        }
    }
}
#endif
