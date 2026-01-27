using System;
using UnityEngine;

public static class Atlas
{
    [Flags] public enum MarkerType
    {
        None = 0,
        Smoke = 1 << 0,
        Phone = 1 << 1,
    }
    public enum AnimationType
    {
        None,
        Walking,
        SittingBreathing,
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
        public int frame;
    }
    [Serializable] public struct Clip
    {
        public AnimationType type;
        public AtlasKeyframe[] keyFrames;
    }
}
