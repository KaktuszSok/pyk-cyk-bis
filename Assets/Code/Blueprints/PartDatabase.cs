using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PartDatabase {

    public static bool InitialLoadDone = false;

    public static Dictionary<string, ScriptablePartBP> PartsList = new Dictionary<string, ScriptablePartBP>();

    public static void ReloadPartsList()
    {
        Debug.Log("Parts List Reload Initiated");
        InitialLoadDone = true;

        PartsList.Clear();
        ScriptablePartBP[] partBPs = Resources.LoadAll<ScriptablePartBP>("PartBlueprints/"); //Load all scriptable part blueprints in Resources/PartBlueprints/
        foreach(ScriptablePartBP bp in partBPs)
        {
            PartsList.Add(bp.name, bp);
            Debug.Log("Parts List - Added Part \"" + bp.name + "\"", bp);
        }

        Debug.Log("Parts List Reload Complete - Total Parts: " + PartsList.Count);
    }
}
