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

    private void Awake()
    {
        Stats = new PersonStats(ageMin, ageMax, personName);
    }
    private void Start()
    {
        // create on some ui object
        //MyProfile.CreateProfile(false, Stats);
    }


}
