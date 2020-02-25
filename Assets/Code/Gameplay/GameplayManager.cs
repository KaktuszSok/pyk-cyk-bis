using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayManager : MonoBehaviour {

    public static GameplayManager instance;
    public static Transform GameplayTransform;
    public static Transform CanvasTransform;
    public Vector3 LevelBounds = new Vector3(1000, 1000, 1000);

    public MultipartPhysBody debug_CraftToSave;
    public CraftBlueprint debug_CraftBlueprint;
    public string debug_LoadCraftBlueprintFileName;
    public bool debug_SaveCraftBlueprint;
    public bool debug_LoadCraftBlueprint;
    public bool debug_SpawnCraftBlueprint;
    public bool debug_ForgetCraftBlueprint;

	void Awake () {
        instance = this;
        GameplayTransform = transform;
        CanvasTransform = GameObject.Find("Canvas").transform;
	}
	
	void FixedUpdate () {
		if(debug_CraftToSave != null)
        {
            debug_CraftBlueprint = CraftBlueprint.GetBlueprintFromCraft(debug_CraftToSave);
            debug_CraftToSave = null;
        }
        if (debug_CraftBlueprint != null)
        {
            if(debug_SaveCraftBlueprint)
            {
                debug_SaveCraftBlueprint = false;
                CraftBlueprint.SaveToFile(debug_CraftBlueprint);
            }
            if(debug_LoadCraftBlueprint)
            {
                debug_LoadCraftBlueprint = false;
                debug_CraftBlueprint = CraftBlueprint.LoadFromFileByName(debug_LoadCraftBlueprintFileName);
            }
            if (debug_SpawnCraftBlueprint)
            {
                CraftBlueprint.SpawnCraftFromBlueprint(debug_CraftBlueprint, Vector3.up * 10f, Vector3.zero);
                debug_SpawnCraftBlueprint = false;
            }
            if(debug_ForgetCraftBlueprint)
            {
                debug_ForgetCraftBlueprint = false;
                debug_CraftBlueprint = null;
            }
        }
	}
}
