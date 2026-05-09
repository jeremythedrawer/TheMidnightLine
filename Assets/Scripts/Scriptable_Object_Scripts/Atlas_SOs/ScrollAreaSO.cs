using System;
using UnityEngine;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "ScrollArea_SO", menuName = "Atlas / Scroll Area")]
public class ScrollAreaSO : ScriptableObject
{
    public AtlasSO atlas;
    public ParticlePosData[] zoneAtlases;
    public MaterialPropertyBlock mpb;
    public int particleCount;
}
