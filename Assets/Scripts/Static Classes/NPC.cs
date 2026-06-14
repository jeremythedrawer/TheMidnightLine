using System;
using UnityEngine.VFX;
using static Atlas;
public static class NPC
{
    public const float ADJUST_COLOR_TIME = 0.5f;
    public const float MIN_START_MOVE_TIME = 0.3f;
    public const float MAX_START_MOVE_TIME = 1f;
    public enum NPCState
    {
        None,
        Idling,
        Walking,
        TicketCheck,
        Behaviour,
    }

    public enum Role
    {
        Traitor,
        Bystander,
        Accomplice
    }

    public enum NPCPath
    {
        None,
        SittingInTrain,
        SittingAtStation,
        StandingInTrain,
        StandingAtStation,
        AtSlideDoor,
        ToSmokerRoom,
        ToSeatInTrain,
        ToSeatAtStation,
        ToStandInTrain,
        ToStandAtStation,
        ToSlideDoor,
        ToExitStation,
    }

    public enum NPCMark
    {
        None,
        TicketCheck,
        Suspected,
        RuledOut,
    }
    [Flags] public enum Behaviours
    {
        None = 0,
        Smoke_addict = 1 << 0,
        Takes_naps = 1 << 1,
        Always_hungry = 1 << 2,
        Listens_to_music = 1 << 3,
        Always_on_call = 1 << 4,
        Enjoys_reading = 1 << 5,
        Frequently_ill = 1 << 6,
        Known_vandal = 1 << 7,
        Count = 1 << 8, 
    }

    [Flags] public enum Appearences
    {
        None = 0,
        LongWhiteHair = 1 << 0,
        Necklace = 1 << 1,
        ShortWhiteHair = 1 << 2,
        Suit = 1 << 3,
        ShortBlackHair = 1 << 4,
        RoundGlasses = 1 << 5,
        LongBlackHair = 1 << 6,
        Beanie = 1 << 7,
        CurlyWhiteHair = 1 << 8,
        WhiteDreadlocks = 1 << 9,
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
        public int boardingStationIndex;
        public int disembarkingStationIndex;

        public int npcPrefabIndex;
        public Behaviours behaviours;
    }
    [Serializable] public struct TraitorProfile
    {
        public NPCProfile npcProfile;
        public string fullName;
        public int coveredMugshotIndex;
        public int uncoveredMugshotIndex;
        public bool found;    
    }

    [Serializable] public struct NameData
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

