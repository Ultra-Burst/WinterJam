using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PersonStats
{
    //  Clearer variable names and cover rest of the data
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
        // Adjusted for this project because several enums are still empty during prototyping,
        // and the old Max()-based selection was throwing runtime exceptions when people spawned.
        Array values = Enum.GetValues(typeof(T));
        if (values == null || values.Length == 0)
            return 0;

        int randomIndex = UnityEngine.Random.Range(0, values.Length);
        return Convert.ToInt32(values.GetValue(randomIndex));
    }
}
