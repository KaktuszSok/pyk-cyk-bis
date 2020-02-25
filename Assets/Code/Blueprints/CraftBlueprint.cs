using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CraftBlueprint {

    public string craftName = "Unnamed Craft";

    public List<PartBlueprint> parts = new List<PartBlueprint>();
    public List<ConnectionBlueprint> connections = new List<ConnectionBlueprint>();

    public int originPartIndex = -1;

    public CraftBlueprint()
    {}
    public CraftBlueprint(string name)
    {
        craftName = name;
    }

    public static CraftBlueprint GetBlueprintFromCraft(MultipartPhysBody craft)
    {
        CraftBlueprint bp = new CraftBlueprint();
        bp.craftName = craft.name;

        //Generate parts list
        int i = 0;
        foreach(PhysPart part in craft.Parts)
        {
            //save part info
            PartBlueprint p = part.GetBlueprint();

            //Check if this is the origin part if originPartIndex isn't assigned yet. If it is the origin part, assign originPartIndex to reference it.
            if (bp.originPartIndex == -1 && part == craft.originPart) bp.originPartIndex = i;

            bp.parts.Add(p);
            i++;
        }

        //Generate part connections list now that we have all the parts.
        //We can use indices interchangably between the physical craft and the blueprint because the parts were added in the same order.
        int partIndex = 0;
        foreach (PhysPart part in craft.Parts)
        {
            foreach (PartConnection connection in part.connections)
            {
                ConnectionBlueprint c = new ConnectionBlueprint();
                c.fromIndex = partIndex; //set fromIndex to reference the part we are currently processing
                int toIndex = craft.Parts.IndexOf(connection.toPart);
                c.toIndex = toIndex;

                bp.connections.Add(c);
            }
            partIndex++;
        }

        return bp;
    }

    public static MultipartPhysBody SpawnCraftFromBlueprint(CraftBlueprint bp, Vector3 position, Vector3 rotation)
    {
        //Set up transform structure before adding any parts etc.
        Transform t = new GameObject(bp.craftName).transform; //Main craft
        t.position = position;
        t.eulerAngles = rotation;
        t.SetParent(GameplayManager.GameplayTransform);
        Transform body = new GameObject("Body").transform; //Body (part holder)
        body.SetParent(t);
        body.localPosition = body.localEulerAngles = Vector3.zero;

        //Set up components
        t.gameObject.AddComponent<Rigidbody>();
        MultipartPhysBody craft = t.gameObject.AddComponent<MultipartPhysBody>();

        //Set up parts
        List<PhysPart> spawnedParts = new List<PhysPart>();
        foreach(PartBlueprint p in bp.parts)
        {
            PhysPart part = p.SpawnPart(body);
            spawnedParts.Add(part);
        }
        //Set up part connections
        foreach(ConnectionBlueprint c in bp.connections)
        {
            spawnedParts[c.fromIndex].connections.Add(new PartConnection(spawnedParts[c.toIndex], spawnedParts[c.fromIndex]));
        }

        //Disable input until it gets overriden by a system which is responsible for this (player spawning, AI spawning, etc.)
        craft.SetBalancerInputActive(false);

        return craft;
    }
    
    public static string SaveToFile(CraftBlueprint bp)
    {
        string dir = "crafts";
        string fileName = bp.craftName + ".bp";
        SaveLoadJSON.SaveObjToFile(bp, dir, fileName);
        return SaveLoadJSON.GetFullPath(dir + "/" + fileName);
    }

    /// <param name="pathIsRelative">is the path relative to Application.persistentDataPath/saved/</param>
    public static CraftBlueprint LoadFromFile(string path, bool pathIsRelative)
    {
        CraftBlueprint bp = SaveLoadJSON.LoadObjFromFile<CraftBlueprint>(path, pathIsRelative);
        if (bp != null) Debug.Log("Loaded Blueprint of '" + bp.craftName + "' from file at " + path); 
        return bp;
    }
    public static CraftBlueprint LoadFromFileByName(string craftName)
    {
        return LoadFromFile("crafts/" + craftName + ".bp", true);
    }
}
