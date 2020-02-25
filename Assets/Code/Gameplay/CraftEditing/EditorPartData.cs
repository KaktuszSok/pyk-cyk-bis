using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Stores additional data about the part that the editor may need.
public class EditorPartData : MonoBehaviour {

    public Material[] trueRendMaterials;

    public Material[] RememberRendMaterials()
    {
        MeshRenderer[] rends = GetComponentsInChildren<MeshRenderer>();
        trueRendMaterials = new Material[rends.Length];
        for(int i = 0; i < rends.Length; i++)
        {
            trueRendMaterials[i] = rends[i].material;
        }

        return trueRendMaterials;
    }
}
