using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this class will copy from a person and if they are a doppelganger, change a property or two from the person's values
public class Profile : MonoBehaviour
{
    PersonStats Stats;
    public void CreateProfile(bool isDoppel, PersonStats stats)
    {
        Stats = stats;
        int num;
        if (isDoppel)
        {
            num = Random.Range(-5, 5);
            if (num >= 0)
            {
                num += 1;
            }
        }
        else
        {
            num = 0;
        }
        Offset = num;
    }
    // offset of value (0 if not doppel)
    int Offset;
    // when offsetting, account for when val+offset < 0
}
