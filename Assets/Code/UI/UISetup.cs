using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISetup : MonoBehaviour {

    public GameObject[] GameObjsToEnable;

	void Awake() {
		foreach(GameObject go in GameObjsToEnable)
        {
            go.SetActive(true);
        }
	}
}
