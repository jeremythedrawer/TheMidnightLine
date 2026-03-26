using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static NPC;

public class NPCManager : MonoBehaviour
{
    const float WAITING_FOR_SEAT_TICK_RATE = 0.3f;

    public static List<NPCBrain> npcChairList = new List<NPCBrain>();
    public static NameData nameData;
    
    public NPCsDataSO npcData;
    public TripSO trip;

    public TextAsset namesJSON;

    [Header("Generated")]
    public bool npcFindingChair;

    private void Awake()
    {
        npcData.npcsToPick = new List<NPCBrain>(npcData.npc_prefab);
        nameData = JsonUtility.FromJson<NameData>(namesJSON.text);
        
        CreateNPCTraitors();
        NPC.InitializeDescriptions();
    }
    private void Update()
    {
        if (npcChairList.Count > 0 && npcFindingChair == false)
        {
            SeatingBoardingNPCs().Forget();
        }
    }

    private void CreateNPCTraitors()
    {
        npcData.totalAgentCount = 0;
        for (int i = 0; i < trip.stations.Length; i++)
        {
            for (int j = 0; j < trip.stations[i].traitorSpawnAmount; j++)
            {
                npcData.totalAgentCount++;
            }
        }
        int profilePageIndex = 0;
        for (int i = 0; i < trip.stations.Length; i++)
        {
            for (int j = 0; j < trip.stations[i].traitorSpawnAmount; j++)
            {
                int randNPCIndex = UnityEngine.Random.Range(0, npcData.npcsToPick.Count); // pick from list

                NPCBrain npc = Instantiate(npcData.npcsToPick[randNPCIndex], transform.position, Quaternion.identity, transform);
                npcData.npcsToPick.RemoveAt(randNPCIndex);
                npc.stats.role = Role.Traitor;

                Behaviours profilePageBehaviours = npc.stats.behaviours;
                Appearence profilePageAppearence = GetRandomAppearence(npc.npc.appearence);

                npc.gameObject.SetActive(false);

                profilePageIndex++;
            }
        }
    }
    private async UniTask SeatingBoardingNPCs()
    {
        npcFindingChair = true;
        NPCBrain curNPC = npcChairList[0];
        npcChairList.RemoveAt(0);
        AssignNextNPCPosition(curNPC);
        await UniTask.WaitForSeconds(WAITING_FOR_SEAT_TICK_RATE);
        npcFindingChair = false;
    }
    public void AssignNextNPCPosition(NPCBrain npc)
    {
        float npcX = npc.transform.position.x;
        float closestDist = float.PositiveInfinity;
        uint bestIndex = int.MaxValue;

        for (uint i = 0; i < npc.curCarriage.chairData.Length; i++)
        {
            if (npc.curCarriage.chairData[i].filled) continue;

            float dist = Mathf.Abs(npcX - npc.curCarriage.chairData[i].xPos);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestIndex = i;
            }
        }

        if (bestIndex == int.MaxValue)
        {
            npc.FindStandingPosition();
        }
        else
        {
            npc.AssignChair(bestIndex);
        }
    }
    public static string GenerateName(Gender gender, Ethnicity ethnicity)
    {
        string genderString = gender.ToString();
        string ethnicityString = ethnicity.ToString();
        List<FirstName> firstNamesList = new List<FirstName>();

        for(int i = 0; i < nameData.firstNames.Length; i++)
        {
            FirstName fn = nameData.firstNames[i];
            if (fn.gender.Equals(genderString, StringComparison.OrdinalIgnoreCase) && fn.ethnicity.Equals(ethnicityString, StringComparison.OrdinalIgnoreCase))
            {
                firstNamesList.Add(fn);
            }
        }

        if (firstNamesList.Count == 0) return "NoFirstName";

        int firstNameIndex = UnityEngine.Random.Range(0, firstNamesList.Count);
        string firstName = firstNamesList[firstNameIndex].name;
        
        List<LastName> lastNameList = new List<LastName>();
        for(int i = 0; i < nameData.lastNames.Length; i++)
        {
            LastName ln = nameData.lastNames[i];
            if (ln.ethnicity.Equals(ethnicityString, StringComparison.OrdinalIgnoreCase))
            {
                lastNameList.Add(ln);
            }
        }
        if (lastNameList.Count == 0) return firstName;

        int lastNameIndex = UnityEngine.Random.Range(0, lastNameList.Count);
        string lastName = lastNameList[lastNameIndex].name;

        return firstName + " " + lastName;
    }

}

#if UNITY_EDITOR

[CustomEditor(typeof(NPCManager))]
public class NPCManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.LabelField("Chair List");

        foreach (NPCBrain item in NPCManager.npcChairList)
        {
            EditorGUILayout.LabelField(item.ToString());
        }

        if (NPCManager.nameData == null)
        {
            EditorGUILayout.HelpBox("No JSON data loaded. Assign a JSON file or enter Play mode.", MessageType.Warning);
            return;
        }
        EditorGUILayout.LabelField("Name List");

        EditorGUILayout.LabelField("First Names");

        foreach (FirstName firstName in NPCManager.nameData.firstNames)
        {
            EditorGUILayout.LabelField(firstName.name + " " + firstName.gender + " " + firstName.ethnicity);
        }

        EditorGUILayout.LabelField("Last Names");
        foreach (LastName lastName in NPCManager.nameData.lastNames)
        {
            EditorGUILayout.LabelField(lastName.name + " " + lastName.ethnicity);
        }
    }
}
#endif
