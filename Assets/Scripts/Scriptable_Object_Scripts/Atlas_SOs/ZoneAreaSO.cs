using System;
using UnityEngine;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "ZoneAreaSO_SO", menuName = "Atlas / Zone Area")]
public class ZoneAreaSO : ScriptableObject
{
    public ZoneLabel label;
    public ZoneAtlas[] zoneAtlases;

    public MaterialPropertyBlock mpb;
    public int particleCount;
    public int computeGroupSize;
    public int kernelID_init;
    public int kernelID_initSlice;
    public int kernelID_update;
    public ZoneState state;
}
