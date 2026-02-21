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
    public enum TrainMotion
    {
        None,
        TrainDoor,
    }
    public enum EntityMotionType
    {
        NPC,
        Spy,
        Clipboard,
        Train,
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
    public enum SpriteMode
    {
        Simple,
        Motion,
        Slice,
    }
    [Serializable] public struct MarkerKey
    {
        public Color32 color;
        public MarkerType type;
    }
    [Serializable] public struct MarkerPosition
    {
        public Vector2 objectPos;
        public MarkerType type;
    }
    [Serializable] public struct SimpleSprite
    {
        public Vector4 uvSizeAndPos;
        public Vector2 uvPivot;
        public Vector3 worldSize;

        public int index;
    }
    [Serializable] public struct MotionSprite
    {
        public SimpleSprite sprite;
        public MarkerPosition[] markers;
    }
    [Serializable] public struct SliceSprite
    {
        public SimpleSprite sprite;
        public Vector4 slice;
        public Vector4[] uvSizeAndPos;
        public Vector4 worldSlices;
    }

    [Serializable] public struct ParticleSprite
    {
        public SimpleSprite sprite;
        public int LOD;
    }
    [Serializable] public struct AtlasKeyframe
    {
        public MotionSprite motionSprite;
        public int holdTime;
    }
    [Serializable] public class AtlasClip
    {
#if UNITY_EDITOR
        public string clipName;
#endif
        public AtlasKeyframe[] keyFrames;
        public ClipType clipType;
        public int motionIndex;
        public float time;

        public SimpleSprite GetNextSprite(ref float keyframeClock, ref int curFrameIndex, ref int prevFrameIndex)
        {

            float frameTime = keyframeClock * FRAMES_PER_SEC;
            if (curFrameIndex >= keyFrames.Length) curFrameIndex = 0;
            AtlasKeyframe curKeyFrame = keyFrames[curFrameIndex];

            switch (clipType)
            {
                case ClipType.Loop:
                {
                    if (frameTime >= curKeyFrame.holdTime)
                    {
                        prevFrameIndex = curFrameIndex;
                        curFrameIndex++;

                        if (curFrameIndex >= keyFrames.Length)
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
                        if (curFrameIndex < keyFrames.Length - 1 && (curFrameIndex > prevFrameIndex || curFrameIndex == 0))
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
                        if (curFrameIndex < keyFrames.Length - 1)
                        {
                            curFrameIndex++;
                        }
                        keyframeClock = 0;
                    }
                }
                break;
            }

            return keyFrames[curFrameIndex].motionSprite.sprite;
        }
    }


    public static readonly Dictionary<EntityMotionType, Type> MotionEnumDictionary =
    new Dictionary<EntityMotionType, Type>
    {
        { EntityMotionType.NPC, typeof(NPCMotion) },
        { EntityMotionType.Spy, typeof(SpyMotion) },
        { EntityMotionType.Train, typeof(TrainMotion) },
    };

    public const int PIXELS_PER_UNIT = 180;
    public const int FRAMES_PER_SEC = 30;


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
