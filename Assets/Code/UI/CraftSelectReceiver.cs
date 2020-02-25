using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CraftSelectReceiver {

    public abstract void OnCraftSelected(string fileName);
    public bool allowEditing = true;
}

public class SpawnMenuCSReceiver : CraftSelectReceiver
{
    public CraftSpawnMenu spawnMenu = null;

    public override void OnCraftSelected(string fileName)
    {
        if (!Input.GetKey(KeyCode.LeftShift) || !allowEditing)
        {
            spawnMenu.SpawnSavedCraft(fileName);
        }
        else //If holding LShift, edit the craft (if allowed).
        {
            CraftEditor.instance.EditCraftFromBlueprint(CraftBlueprint.LoadFromFileByName(fileName));
        }
        spawnMenu.CloseMenu();
    }

    public SpawnMenuCSReceiver()
    {
        allowEditing = true;
    }
}
