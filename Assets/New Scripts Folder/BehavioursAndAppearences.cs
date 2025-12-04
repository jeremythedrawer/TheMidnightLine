using System;
using UnityEngine;

[Flags]
public enum Appearence
{
    Nothing = 0,
    Heavy = 1 << 0,
    Tall = 1 << 1,
    Old = 1 << 2,
    Glasses = 1 << 3,
    SuitAndTie = 1 << 4,
    Bald = 1 << 5,
    DarkHair = 1 << 6,
    Dress = 1 << 7,
    Shorts = 1 << 8,
    Bag = 1 << 9,
    Headware = 1 << 10,
}

[Flags]
public enum Behaviours
{
    Nothing = 0,
    Smoker = 1 << 0,
    Isolater = 1 << 1,
    TimeTracker = 1 << 2,
    Sleeper = 1 << 3,
    Stander = 1 << 4,
    Puker = 1 << 5,
    Eater = 1 << 6,
    HeavyMetalEnjoyer = 1 << 7,
}

