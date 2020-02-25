using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftSpawner : MonoBehaviour {

    public CraftBlueprint Blueprint;
    public bool spawnOnStart = false;

    void Start()
    {
        if(spawnOnStart)
        {
            SpawnCraft();
        }
    }

    public void SpawnCraft()
    {
        CraftBlueprint.SpawnCraftFromBlueprint(Blueprint, transform.position, transform.eulerAngles);
    }
}
