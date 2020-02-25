using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parts can keep track of their PartComponents e.g. engine etc.
/// </summary>
[RequireComponent(typeof(PhysPart))]
public abstract class PartComponent : MonoBehaviour {
    public PhysPart part;
    public MultipartPhysBody body;

    protected virtual void Awake() //called as soon as object is instantiated
    {
        part = GetComponent<PhysPart>();
    }

    protected virtual void Start()
    {
        OnBodyChanged();
    }

    /// <summary>
    /// Called when craft breaks into multiple pieces for example
    /// </summary>
    public virtual void OnBodyChanged()
    {
        body = part.GetBody();
    }

    //Should include modifiable info to keep track of
    public virtual ComponentBlueprint GetBlueprint()
    {
        return null;
    }

    public virtual void ApplyBlueprint(ComponentBlueprint bp)
    {
        return;
    }
}
