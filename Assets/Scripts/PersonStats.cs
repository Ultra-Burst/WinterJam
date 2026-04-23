using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PersonStats
{
    // TODO: Clearer variable names and cover rest of the data
    string Name { get; }
    int Age { get; }
    Food FavFood { get; }
    Food LastEatenFood { get; }
    Hobby Hobby { get; }
    Job CurrentJob { get; }
    Job DreamJob { get; }
    Education Education { get; }
    Origin Origin { get; }
    Creature Creature { get; }
    FamilyRelation FamilyRelation { get; }
    Activity TodayActivity { get; }
    Activity YesterdayActivity { get; }
    LivingWith LivingWith { get; }

    public PersonStats(int ageMin, int ageMax, string name)
    {
        Name = name;
        Age = UnityEngine.Random.Range(ageMin, ageMax + 1);
        FavFood = (Food)GetRandomEnumValue(FavFood);
        LastEatenFood = (Food)GetRandomEnumValue(LastEatenFood);
        Hobby = (Hobby)GetRandomEnumValue(Hobby);
        CurrentJob = (Job)GetRandomEnumValue(CurrentJob);
        DreamJob = (Job)GetRandomEnumValue(DreamJob);
        Education = (Education)GetRandomEnumValue(Education);
        Origin = (Origin)GetRandomEnumValue(Origin);
        Creature = (Creature)GetRandomEnumValue(Creature);
        FamilyRelation = (FamilyRelation)GetRandomEnumValue(FamilyRelation);
        TodayActivity = (Activity)GetRandomEnumValue(TodayActivity);
        YesterdayActivity = (Activity)GetRandomEnumValue(YesterdayActivity);
        LivingWith = (LivingWith)GetRandomEnumValue(LivingWith);
    }

    int GetRandomEnumValue<T>(T enumType) where T : System.Enum
    {
        // not sure if +1 is needed but probably
        return UnityEngine.Random.Range(0, Enum.GetValues(typeof(T)).Cast<int>().Max()+1);
    }
}
