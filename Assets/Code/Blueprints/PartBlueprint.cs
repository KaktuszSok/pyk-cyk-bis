using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PartBlueprint {

    public string partName;
    public Vector3 position;
    public Vector3 rotation;

    //Runtime values
    public float health = 0f;
    public float heat = 0f;

    //Components
    public List<ComponentBlueprint> componentDatas = new List<ComponentBlueprint>();

    public PhysPart SpawnPart(Transform parent)
    {
        Transform part = null;
        //Attempt to find part blueprint asset, which has a reference to the part prefab we want to spawn.
        ScriptablePartBP partAsset = Resources.Load<ScriptablePartBP>("PartBlueprints/" + partName);

        if (partAsset == null) //If the part blueprint can't be found, handle the error.
        {
            part = new GameObject("Invalid Part! (\"" + partName + "\")").transform;
            part.gameObject.AddComponent<PhysPart>().partName = partName;
            Debug.LogError("Invalid Part! (\"" + partName + "\")", parent);
        }
        else
        {
            part = Object.Instantiate(partAsset.partPrefab).transform; //Spawn the prefab
            part.name = partName;
        }
        part.SetParent(parent);

        PhysPart phys = part.GetComponent<PhysPart>();
        phys.connections.Clear(); //Clear connections if any are carried over e.g. if the prefab is not cleansed of connections.
        phys.ApplyBlueprint(this); //Let part handle setting its values based on the blueprint.

        return phys;
    }

}
