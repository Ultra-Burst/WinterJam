using Fungus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Person : MonoBehaviour
{
    public int ageMin;
    public int ageMax;
    public string personName;
    PersonStats Stats;
    public Profile MyProfile;
    private Personality MyPersonality;
    public Flowchart flowchart;

    private void Awake()
    {
        Stats = new PersonStats(ageMin, ageMax, personName);
    }
    private void Start()
    {
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
