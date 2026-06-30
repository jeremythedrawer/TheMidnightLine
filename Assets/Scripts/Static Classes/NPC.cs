using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static Atlas;
public static class NPC
{
    public const float MIN_START_MOVE_TIME = 0.3f;
    public const float MAX_START_MOVE_TIME = 1f;

    public const int MERIDIA_COLOR_BIT = 4;
    public const int DIAGONAL_TEXTURE_BIT = 3;
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
        AtSmokerRoom,
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
        Gets_Distracted = 1 << 8,
        Count = 1 << 9, 
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
        public int mugShotIndex;
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

    public static void QuickSortNPCByXPos(NPCBrain[] npcs, int left, int right)
    {
        if (left < right)
        {
            int pivot = PartitionNPC(npcs, left, right);

            if (pivot > 1)
            {
                QuickSortNPCByXPos(npcs, left, pivot - 1);
            }

            int pivotAhead = pivot + 1;
            if (pivotAhead < right)
            {
                QuickSortNPCByXPos(npcs, pivotAhead, right);
            }
        }
    }
    private static int PartitionNPC(NPCBrain[] npcs, int left, int right)
    {
        NPCBrain leftNPC = npcs[left];

        while (true)
        {
            while (npcs[left].transform.position.x > leftNPC.transform.position.x)
            {
                left++;
            }

            while (npcs[right].transform.position.x < leftNPC.transform.position.x)
            {
                right--;
            }

            if (left < right)
            {
                if (npcs[left] == npcs[right]) return right;

                NPCBrain npcTemp = npcs[left];

                npcs[left] = npcs[right];
                npcs[right] = npcTemp;
            }
            else
            {
                return right;
            }
        }
    }
}

