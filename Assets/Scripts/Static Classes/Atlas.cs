using System;
using System.Collections.Generic;
using UnityEngine;
public static class Atlas
{
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
    [Flags]
    public enum MarkerType
    {
        None = 0,
        Smoke = 1 << 0,
        Talking = 1 << 1,
        SleepingZs = 1 << 2,
        Music = 1 << 3,
        Climb = 1 << 4,
        TrainPhone = 1 << 5,
    }
    [Serializable]
    public struct AtlasMarker
    {
        public MarkerType type;
        public Color32 color;
    }
    [Serializable]
    public struct SpriteMarker
    {
        public MarkerType type;
        public Vector2 objectPos;
    }
    [Serializable]
    public struct AtlasSprite
    {
        public int index;
        public Vector2 uvSize;
        public Vector2 uvPos;
        public Vector2 uvPivot;
        public SpriteMarker[] markers;
    }
    [Serializable]
    public struct AtlasKeyframe
    {
        public int spriteIndex;
        public int holdTime;
    }
    [Serializable]
    public struct AtlasClip
    {
        public ClipType clipType;
        public int motionIndex;
#if UNITY_EDITOR
        public string clipName;
#endif
        public AtlasKeyframe[] keyFrames;
    }

    [Serializable]
    public struct SpyClip
    {
        public SpyMotion motion;
        public AtlasClip clip;
    }
    [Serializable]
    public struct NPCClip
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
    public const int PIXELS_PER_UNIT = 180;

    public static void SetNextFrameIndex(AtlasClip clip, int fps, ref float keyframeClock, ref int curFrameIndex, ref int prevFrameIndex)
    {
        float frameTime = keyframeClock * fps;
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
}
