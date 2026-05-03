using System;
using System.Collections.Generic;
using UnityEngine;
using static AtlasRendering;
public static class Atlas
{
    public const int PIXELS_PER_UNIT = 180;
    public const int FRAMES_PER_SEC = 30;
    public readonly static float UNITS_PER_PIXEL = 1 / (float)PIXELS_PER_UNIT;
    public static int FLOAT_SIZE = sizeof(float);
    public static int FLOAT2_SIZE = FLOAT_SIZE * 2;
    public static int FLOAT3_SIZE = FLOAT_SIZE * 3;
    public static int FLOAT4_SIZE = FLOAT_SIZE * 4;
    public static int INT_SIZE = sizeof(uint);
    public enum NPCMotion // NOTE(Jeremy): If adding a new motion put it at the bottom or it will mess with the clip indexes for the clip dictionary.
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
        Vandalising,
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

    public enum NotepadMotion
    { 
        None,
        FlipHand,
        FlipPage,
        RotatingPencil,
    }

    public enum EntityMotionType
    {
        NPC,
        Spy,
        Notepad,
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
        Coughing = 1 << 5,
    }
    public enum SpriteMode
    {
        Simple,
        Motion,
        Slice,
        UISimple,
        UIMotion,
        UISlice,
        UIText,
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
    [Serializable] public struct AtlasKeyframe
    {
        public MotionSprite motionSprite;
        public int holdTime;
    }
    [Serializable] public struct AtlasClip
    {
        public string clipName; // HACK: Take out for production build. Cant use #if UNITY_EDITOR because it'll crash the build. :(
        public AtlasKeyframe[] keyFrames;
        public ClipType clipType;
        public int motionIndex;
        public float time;
    }

    public static readonly Dictionary<EntityMotionType, Type> MotionEnumDictionary =
    new Dictionary<EntityMotionType, Type>
    {
        { EntityMotionType.NPC, typeof(NPCMotion) },
        { EntityMotionType.Spy, typeof(SpyMotion) },
        { EntityMotionType.Train, typeof(TrainMotion) },
        { EntityMotionType.Notepad, typeof(NotepadMotion) },
    };

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
