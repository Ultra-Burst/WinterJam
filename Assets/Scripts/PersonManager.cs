using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonManager : MonoBehaviour
{
    [SerializeField] Person personPrefab;
    [SerializeField] GameObject peopleHolder;
    public int startingPeopleAmount;

    private void Start()
    {
        //CreatePeople(startingPeopleAmount);
    }

    void CreatePeople(int num)
    {
        for (int i = 0; i < num; i++)
        {
            Instantiate(personPrefab, peopleHolder.transform);
        }
    }
}
