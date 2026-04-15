using System;
using System.Collections.Generic;
using UnityEngine;

public static class NPC
{
    public const float CLOSE_TO_TARGET_BUFFER = 0.1f;
    public enum NPCState
    {
        None,
        Idling,
        Walking,
        TicketCheck,
        Smoking,
        Sleeping,
        Eating,
        Music,
        Calling,
        Reading
    }

    public enum Role
    {
        Traitor,
        Bystander
    }

    public enum Path
    {
        Standing,
        Sitting,
        ToSmokerRoom,
        ToSeat,
        ToStand,
        ToSlideDoor,
    }


    [Flags] public enum Behaviours
    {
        None = 0,
        smoke_Addict = 1 << 0,
        Takes_naps = 1 << 2,
        Always_hungry = 1 << 3,
        Listens_to_music = 1 << 4,
        Always_on_call = 1 << 5,
        Enjoys_reading = 1 << 6,
        Gets_nauseous = 1 << 7,
        Known_vandal = 1 << 8,
    }

    public enum Gender
    { 
        Male,
        Female,
    }

    public enum Ethnicity
    {
        European,
        Western,
        Arabic,
    }

    [Serializable] public struct NPCProfile
    {
        public string fullName;

        public int disembarkingStationIndex;
        public int boardingStationIndex;

        public int npcPrefabIndex;
        public int coveredMugshotIndex;
        public int uncoveredMugshotIndex;

        public Behaviours behaviours;
    }

    [Serializable] public class NameData
    {
        public FirstName[] firstNames;
        public LastName[] lastNames;
    }
    [Serializable] public struct FirstName
    {
        public string gender;
        public string ethnicity;
        public string name;
    }
    [Serializable] public struct LastName
    {
        public string ethnicity;
        public string name;
    }

    [Serializable] public struct NPCQueue
    {
        public NPCBrain[] npcs;
        public int npcsCount;
        public float timer;
    }
}

