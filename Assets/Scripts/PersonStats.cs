using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PersonStats
{
    // TODO: Clearer variable names and cover rest of the data
    string Name;
    int Age { get; }
    Food FavFood { get; }
    Hobby Hobby { get; }
    Job CurrentJob { get; }
    Job DreamJob { get; }
    Education Education { get; }
    Origin Origin { get; }
    Creature Creature { get; }

    public PersonStats(int ageMin, int ageMax, string name)
    {
        Name = name;
        Age = UnityEngine.Random.Range(ageMin, ageMax + 1);
        FavFood = (Food)GetRandomEnumValue(FavFood);
        Hobby = (Hobby)GetRandomEnumValue(Hobby);
        CurrentJob = (Job)GetRandomEnumValue(CurrentJob);
        DreamJob = (Job)GetRandomEnumValue(DreamJob);
        Education = (Education)GetRandomEnumValue(Education);
        Origin = (Origin)GetRandomEnumValue(Origin);
        Creature = (Creature)GetRandomEnumValue(Creature);
    }

    int GetRandomEnumValue<T>(T enumType) where T : System.Enum
    {
        // not sure if +1 is needed but probably
        return UnityEngine.Random.Range(0, Enum.GetValues(typeof(T)).Cast<int>().Max()+1);
    }
}
