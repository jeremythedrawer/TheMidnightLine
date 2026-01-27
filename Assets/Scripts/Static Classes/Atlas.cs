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

    [Serializable] public struct Sprite
    {
        public int index;
        public Vector2 uvPos;
        public Vector2 uvSize;
        public Vector2 normPivot; 
        public SpriteMarker[] markers;
    }

    public enum AnimationType
    {
        Walking,
        SittingBreathing,
    }

    [Serializable] public struct AnimationSelection
    {
        public AnimationType type;
        public Color32 color;
    }
    [Serializable] public struct Keyframe
    {
        public Sprite sprite;
        public float frame;
    }
    [Serializable] public struct Clip
    {
        public AnimationType type;
        public Keyframe[] keyFrames;
    }
}
