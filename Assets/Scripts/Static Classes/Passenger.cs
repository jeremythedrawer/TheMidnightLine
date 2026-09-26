using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

using static Atlas;
using static Passenger;
public static class Passenger
{
    public const float MIN_START_MOVE_TIME = 0.3f;
    public const float MAX_START_MOVE_TIME = 1f;

    public enum PassengerState
    {
        None,
        Idling,
        Walking,
        TicketCheck,
        Behaviour,
    }
    public enum PassengerSubState
    { 
        
    }

    public enum Role
    {
        Traitor,
        Bystander,
        Accomplice
    }

    public enum PassengerPath
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
    [Flags] public enum Habits
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
        Gets_distracted = 1 << 8,
        Takes_photos = 1 << 9,
    }
    public const int HABIT_COUNT = 10;

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

    public enum HenchmanState
    {
        None,
        Walking,
        Suitcase,
        Idle,
    }
    [Serializable] public struct PassengerProfile
    {
        public int boardingStationIndex;
        public int disembarkingStationIndex;

        public int npcPrefabIndex;
        public Habits habits;
    }
    [Serializable] public struct TraitorProfile
    {
        public PassengerProfile passengerProfile;
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

    [Serializable] public struct PassengerQueue
    {
        public PassengerBrain[] passengers;
        public int passengerCount;
        public float timer;
    }


    public const int MAX_GRAFFITI_RENDERERS = 8;

    public static Dictionary<VisualEffect, Queue<VisualEffect>> GlyphPoolDict;
    public static Dictionary<PassengerBrain, Queue<PassengerBrain>> PassengerPoolDict;
    public static Graffiti[] GraffitiPool;
    public static int graffitiActiveCount;

    public static void QuickSortPassengerByXPos(PassengerBrain[] passengers, int left, int right)
    {
        if (left >= right) return;

        int index = PartitionNPC(passengers, left, right);

        QuickSortPassengerByXPos(passengers, left, index - 1);
        QuickSortPassengerByXPos(passengers, index, right);
    }
    private static int PartitionNPC(PassengerBrain[] passengers, int left, int right)
    {
        float pivot = passengers[(left + right) / 2].transform.position.x;

        while (left <= right)
        {
            while (passengers[left].transform.position.x < pivot) left++;

            while (passengers[right].transform.position.x > pivot) right--;

            if (left <= right)
            {
                (passengers[left], passengers[right]) = (passengers[right], passengers[left]);
                left++;
                right--;
            }
        }

        return left;
    }
    public static Dictionary<Habits, HabitData> SetBehaviourContextDictionary(HabitData[] behaviourContexts)
    {
        Dictionary<Habits, HabitData> dict = new Dictionary<Habits, HabitData>();

        foreach (HabitData context in behaviourContexts)
        {
            dict[context.habit] = context;
        }

        return dict;
    }
    public static Habits GetHabitAtIndex(Habits habits, int index)
    {
        int count = 0;
        foreach (Habits flag in Enum.GetValues(typeof(Habits)))
        {
            if (flag == Habits.None) continue;

            if ((habits & flag) != 0)
            {
                if (count == index) return flag;
                count++;
            }
        }
        return Habits.None;
    }
    public static Dictionary<TEnum, string> InitEnumToStringDict<TEnum>() where TEnum : Enum
    {
        Dictionary<TEnum, string> dict = new Dictionary<TEnum, string>();

        Array values = Enum.GetValues(typeof(TEnum));

        foreach (TEnum value in values)
        {
            int int32 = Convert.ToInt32(value);
            if (int32 == 0) continue;
            dict.Add(value, value.ToString().Replace("_", " "));
        }
        return dict;
    }

}

