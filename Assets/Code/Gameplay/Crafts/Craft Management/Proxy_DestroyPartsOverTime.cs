using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Proxy_DestroyPartsOverTime : MonoBehaviour {

	public void DestroyParts(List<PhysPart> parts, float timePerPart)
    {
        StartCoroutine(DestroyPartsCoroutine(parts, timePerPart));
    }

    IEnumerator DestroyPartsCoroutine(List<PhysPart> parts, float timePerPart)
    {
        WaitForSeconds wait = new WaitForSeconds(timePerPart);
        while (parts.Count > 0)
        {
            //choose random part
            int index = Random.Range(0, parts.Count - 1);
            if (parts[index] == null) { parts.RemoveAt(index); continue; } //skip and remove if something went wrong

            //keep track of part, remove from list, destroy part
            PhysPart p = parts[index];
            parts.RemoveAt(index);
            p.DestroyPart(0, true, PhysPart.DestructionType.GENERIC);
            yield return wait; //wait specified delay until processing next part
        }
        gameObject.AddComponent<Autodestroy>().destroyTimer = 0.01f; //destroy this gameobject
    }
}
