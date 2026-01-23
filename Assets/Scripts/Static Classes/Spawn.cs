using System;
using UnityEngine;

public static class Spawn
{
    [Flags] public enum BackgroundType
    {
        None = 0,
        Trees = 1 << 0,
        Houses = 1 << 1,
        Buildings = 1 << 2,
        Powerlines = 1 << 3,
    }

    [Serializable] public struct ParticleData
    {
        public BackgroundType backgroundType;
        public int LODLevel;
        public Texture2D atlas;
        public Sprite[] atlasSprites;
        public Vector2[] uvPositions;
        public Vector2[] uvSizes;
        public bool active;
    }

    [Serializable] public struct TimeStamp
    {
        public float metersPosition;
        public BackgroundType backgroundType;
    }

    [Serializable] public struct SpawnerData
    {
        public Material material;
        public RenderParams renderParams;
        public GraphicsBuffer uvPositionsBuffer;
        public GraphicsBuffer uvSizesBuffer;
        public bool active;
    }

}
