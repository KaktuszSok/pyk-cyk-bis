using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultipartPhysBody : InteractablePhysBody {

    public List<PhysPart> Parts = new List<PhysPart>();
    public PhysPart originPart;

    float totalMass = 0;
    Vector3 CoM = Vector3.zero;

    float currAutodestructPartInterval = float.PositiveInfinity;
    int fixedCounter = 0;

    public bool debug_disassembleConnectedParts = false;

    protected override void Awake()
    {
        base.Awake();
        physicsReady = false;
        rb = GetComponent<Rigidbody>();
        rb.drag = rb.angularDrag = 0.2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        FullRecalcPhysics();
        physicsReady = true;
    }

    #region Other Component Management
    public void SetBalancerInput<T>() where T : BalancerInput
    {
        foreach(BalancerInput bi in GetComponentsInChildren<BalancerInput>(true))
        {
            bi.ChangeBalancerInputType<T>();
        }
        UpdatePhysParts();
    }
    public void SetBalancerInputActive(bool inputActive)
    {
        bool first = true;
        foreach (BalancerInput bi in GetComponentsInChildren<BalancerInput>(true))
        {
            bi.enabled = inputActive ? first : false; //if input set to inactive, disable. if input set to active, enable if this is the first bi and disable otherwise.
            first = false;
        }
    }
    #endregion

    #region Self-Management

    #region Self-Operations
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    public void AutoDestruct(float timeBetweenPartDestroy = 0f)
    {
        if (timeBetweenPartDestroy >= currAutodestructPartInterval) return; //If already autodestructing at same speed or faster, ignore this autodestruct request.

        currAutodestructPartInterval = timeBetweenPartDestroy;
        List<PhysPart> partsToDestroy = Parts;
        if (timeBetweenPartDestroy == 0)
        {
            while (partsToDestroy.Count > 0)
            {
                partsToDestroy[0].DestroyPart(0, true, PhysPart.DestructionType.GENERIC);
                partsToDestroy.RemoveAt(0);
            }
        }
        else
        {
            GameObject proxy = new GameObject("AutoDestruct Proxy (" + name + ")");
            proxy.transform.SetParent(GameplayManager.GameplayTransform);
            proxy.AddComponent<Proxy_DestroyPartsOverTime>().DestroyParts(partsToDestroy, timeBetweenPartDestroy);
        }
    }

    public void SetOriginPart(PhysPart p)
    {
        if (p == null)
        {
            Debug.LogError("Tried to set origin of craft \"" + name + "\" to null part.", this);
            return;
        }
        if(!p.transform.IsChildOf(transform))
        {
            Debug.LogError("Tried to set origin of craft \"" + name + "\" to part \"" + p.name + "\", which does not belong to the craft.", p);
            return;
        }

        originPart = p;
        p.transform.SetAsFirstSibling();
        if(Parts.Contains(p)) Parts.Remove(p);
        Parts.Insert(0, p);
    }
    #endregion

    #region Physics
    public IEnumerator FullRecalcPhysNextFixedUpdate()
    {
        yield return new WaitForEndOfFrame();
        FullRecalcPhysics();
    }

    public void FullRecalcPhysics () {
        UpdatePhysParts();
        if(Parts.Count != 0)
            RecalcPhysics();
	}

    public void UpdatePhysParts(bool simpleRecalc = false)
    {
        Parts = new List<PhysPart>(GetComponentsInChildren<PhysPart>());
        if(Parts.Count == 0)
        {
            if(!simpleRecalc) DestroySelf();
            return;
        }
        if (!simpleRecalc)
        {
            //Disable all but the first AutoBalancer component
            AutoBalancer[] balancerParts = GetComponentsInChildren<AutoBalancer>();
            for (int i = 0; i < balancerParts.Length; i++)
            {
                if (i == 0) balancerParts[i].enabled = true;
                else balancerParts[i].enabled = false;
            }

            //Destroy all invalid parts
            foreach(PhysPart p in Parts)
            {
                if(p.name.Contains("Invalid Part!"))
                {
                    p.DestroyPart();
                }
            }
        }
        originPart = Parts[0];
    }

    public void RecalcPhysics()
    {
        totalMass = 0;
        CoM = Vector3.zero;
        
        foreach(PhysPart p in Parts)
        {
            totalMass += p.mass;
            CoM += transform.InverseTransformPoint(p.transform.position) * p.mass;
        }
        CoM /= totalMass;

        rb.mass = totalMass;
        rb.centerOfMass = CoM;
        FixTransformPos();
        rb.ResetInertiaTensor();
    }

    public void FixTransformPos()
    {
        CoM = transform.TransformPoint(CoM); //temporarily set CoM to world coords
        List<Transform> children = new List<Transform>();
        if(transform.childCount == 0)
        {
            DestroySelf();
            return;
        }

        foreach(Transform child in transform)
        {
            if(child != null)
                children.Add(child);
        }
        foreach(Transform child in children)
        {
            child.SetParent(null);
        }
        transform.position = CoM;
        transform.rotation = originPart.transform.rotation;
        foreach (Transform child in children)
        {
            child.SetParent(transform);
        }

        CoM = rb.centerOfMass = Vector3.zero;
    }
    #endregion

    #endregion



    void FixedUpdate() {
        //Debug stuff
        if (debug_disassembleConnectedParts)
        {
            debug_disassembleConnectedParts = false;
            PhysPart[] connectedParts = PartConnection.GetConnectedParts(originPart, true);
            List<PartConnection> connections = new List<PartConnection>();
            foreach (PhysPart p in connectedParts)
            {
                foreach (PartConnection c in p.connections)
                {
                    connections.Add(c);
                }
            }

            foreach (PartConnection c in connections)
            {
                c.fromPart.DisconnectPart(c.toPart);
            }
        }

        //Logic
        if (fixedCounter % 49 == 0) //once a second
        {
            //Auto-destroy if we are OOB
            if (Mathf.Abs(transform.position.x) > GameplayManager.instance.LevelBounds.x || Mathf.Abs(transform.position.y) > GameplayManager.instance.LevelBounds.y || Mathf.Abs(transform.position.z) > GameplayManager.instance.LevelBounds.z)
            {
                AutoDestruct(2.5f/Parts.Count);
            }
        }
        fixedCounter = (int)Mathf.Repeat(fixedCounter + 1, 50);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Transfer collisions to appropriate parts
        PhysPart partHit = null;
        if (collision.contacts[0].thisCollider.GetComponent<PhysPart>())
        {
            partHit = collision.contacts[0].thisCollider.GetComponent<PhysPart>();
        }
        else if (collision.contacts[0].otherCollider.GetComponent<PhysPart>())
        {
            partHit = collision.contacts[0].otherCollider.GetComponent<PhysPart>();
        }

        if(partHit != null)
        {
            partHit.OnImpact(collision);
        }
    }

    public PhysPart GetOriginPart()
    {
        UpdatePhysParts(true);
        return originPart;
    }
}
