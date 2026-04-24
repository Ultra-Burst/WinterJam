using Fungus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Person : MonoBehaviour
{
    public int ageMin;
    public int ageMax;
    public string personName;
    [SerializeField] private Sprite portrait;
    [SerializeField] private string startBlockName = "Start";

    private PersonStats stats;
    public Profile MyProfile;
    private Personality MyPersonality;
    public Flowchart flowchart;

    public PersonStats Stats
    {
        get
        {
            EnsureStatsInitialized();
            return stats;
        }
    }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Stats.Name))
                return Stats.Name;

            if (!string.IsNullOrWhiteSpace(personName))
                return personName;

            return gameObject.name;
        }
    }

    public int DisplayAge => Stats.Age;
    public Sprite Portrait => portrait;

    private void Awake()
    {
        EnsureStatsInitialized();
        ApplyStatsToFlowchart();
    }

    private void Start()
    {
        // The selection UI needs stable profile data before the player starts a conversation,
        // so the generated stats are cached and reapplied whenever a flowchart is launched.
        ApplyStatsToFlowchart();
    }

    public void StartConversation()
    {
        StartConversation(-1);
    }

    public void StartConversation(int remainingMatches)
    {
        EnsureStatsInitialized();

        if (flowchart == null)
        {
            Debug.LogWarning($"{name} cannot start a conversation because no Flowchart is assigned.", this);
            return;
        }

        // Reset the flowchart before every date so visited menu options and temporary
        // variables do not leak from a previous conversation into the next one.
        flowchart.Reset(true, true);
        ApplyStatsToFlowchart();

        if (remainingMatches >= 0)
            flowchart.SetIntegerVariable("RemainingMatches", remainingMatches);

        if (!string.IsNullOrWhiteSpace(startBlockName) && flowchart.ExecuteIfHasBlock(startBlockName))
            return;

        if (!flowchart.ExecuteIfHasBlock("Start"))
        {
            Debug.LogWarning($"{name} could not find a conversation block named '{startBlockName}' or 'Start'.", this);
        }
    }

    private void EnsureStatsInitialized()
    {
        if (stats != null)
            return;

        stats = new PersonStats(ageMin, ageMax, personName);
    }

    private void ApplyStatsToFlowchart()
    {
        if (flowchart == null)
            return;

        EnsureStatsInitialized();

        // create on some ui object
        //MyProfile.CreateProfile(false, Stats);
        flowchart.SetStringVariable("Name", Stats.Name);
        flowchart.SetIntegerVariable("Age", Stats.Age);
        flowchart.SetStringVariable("FavFood", Stats.FavFood.ToString());
        flowchart.SetStringVariable("LastEatenFood", Stats.LastEatenFood.ToString());
        flowchart.SetStringVariable("Hobby", Stats.Hobby.ToString());
        flowchart.SetStringVariable("CurrentJob", Stats.CurrentJob.ToString());
        flowchart.SetStringVariable("DreamJob", Stats.DreamJob.ToString());
        flowchart.SetStringVariable("Education", Stats.Education.ToString());
        flowchart.SetStringVariable("Origin", Stats.Origin.ToString());
        flowchart.SetStringVariable("Creature", Stats.Creature.ToString());
        flowchart.SetStringVariable("FamilyRelation", Stats.FamilyRelation.ToString());
        flowchart.SetStringVariable("TodayActivity", Stats.TodayActivity.ToString());
        flowchart.SetStringVariable("YesterdayActivity", Stats.YesterdayActivity.ToString());
        flowchart.SetStringVariable("LivingWith", Stats.LivingWith.ToString());
        flowchart.SetIntegerVariable("Group", Random.Range(1, 4));
    }
}
