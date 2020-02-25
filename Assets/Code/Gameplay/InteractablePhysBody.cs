using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class InteractablePhysBody : MonoBehaviour {
    public Rigidbody rb;
    public bool physicsReady = true;

    protected virtual void Awake()
    {
        gameObject.tag = "InteractablePhysBody";
    }

}
