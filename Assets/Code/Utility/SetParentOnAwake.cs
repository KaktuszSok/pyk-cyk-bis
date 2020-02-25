using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetParentOnAwake : MonoBehaviour
{

    public Transform parent;
    public bool removeWhenDone = true;

    void Awake()
    {
        transform.SetParent(parent);
        if (removeWhenDone) Destroy(this);
    }

}