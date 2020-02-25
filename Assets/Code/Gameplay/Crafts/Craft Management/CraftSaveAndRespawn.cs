using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftSaveAndRespawn : MonoBehaviour {

    CraftBlueprint savedCraft;
    public bool respawnOnStart = false;

    void Start() {
        if(respawnOnStart)
        {
            SaveAndRespawn();
        }
    }

    public CraftBlueprint SaveCraft(bool overwriteCurrentSave = true)
    {
        if (overwriteCurrentSave)
        {
            savedCraft = CraftBlueprint.GetBlueprintFromCraft(GetComponent<MultipartPhysBody>());
            return savedCraft;
        }
        else
        {
            return CraftBlueprint.GetBlueprintFromCraft(GetComponent<MultipartPhysBody>());
        }
    }

    public void SaveAndRespawn()
    {
        SaveCraft();
        CraftBlueprint.SpawnCraftFromBlueprint(savedCraft, transform.GetChild(0).position, transform.GetChild(0).transform.eulerAngles);

        transform.position = Vector3.down * 55555f;
        Destroy(gameObject);
        gameObject.SetActive(false);
    }
}
