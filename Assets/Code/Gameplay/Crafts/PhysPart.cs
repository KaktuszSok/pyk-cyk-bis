using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysPart : MonoBehaviour {

    [Header("Part Parameters")]
    public string partName = "";
    public float mass = 350;
    public float connectionStrength = 1500; //impulse exceeding this will break a connection
    public float internalStrength = 3500; //impulse exceeding this will destroy the part
    public float stressCarryFactor = 0.35f; //how much stress is carried when a connection is broken/the part is destroyed
    public float maxHealth = 100000; //effective strengths are interpolated between 0 and their raw value based on GetTrueHealth()/maxHealth.
    public float heatLossRate = 3000f; //how much currentHeat is lost per second
    public float mat_heatEmissionFactor = 1f; //how bright the material emissive colour gets as heat increases
    public Vector3 CoMoffset = Vector3.zero;

    [Header("Runtime Values")]
    public float health = 100000; //part can be damaged by weapons. This is "structural health", while true health accounts for heat etc.
    public float currentHeat = 0f;
    [HideInInspector]
    public bool mat_emissiveOn = false;

    [Header("Audiovisual")]
    public float pfxScale = 1f;
    public SFXCollection ImpactSounds;
    public PFXCollection ImpactParticles;
    public enum DestructionType {
        GENERIC
    }
    public SFXCollection DestroyGenericSounds;
    public PFXCollection DestroyGenericParticles;

    [Header("References")]
    public Collider[] colliders;
    public Material[] mats = new Material[0];
    public List<PartConnection> connections = new List<PartConnection>();
    public PartComponent[] components = new PartComponent[0];

    [Header("Debugging")]
    public bool debug_showConnectToOrigin = false;
    public bool debugButton_destroyPart = false;

    #region Setup

    void Awake()
    {
        gameObject.tag = "PhysPart";
        colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            if (!c.isTrigger)
            {
                c.tag = "PhysPart";
            }
        }
        if (mats.Length == 0)
        {
            MeshRenderer[] rends = GetComponentsInChildren<MeshRenderer>();
            mats = new Material[rends.Length];
            for(int i = 0; i < rends.Length; i++)
            {
                mats[i] = rends[i].material;
            }
        }
        components = GetComponents<PartComponent>();
    }

    public PartComponent[] UpdateComponentsList()
    {
        components = GetComponents<PartComponent>();
        return components;
    }

    void Start () {
        if (partName == null || partName == "")
        {
            partName = gameObject.name;
        }

        foreach (PartConnection c in connections)
        {
            c.fromPart = this;
        }
        RecalcConnections();
        if (debug_showConnectToOrigin)
        {
            PartConnection.IsConnectedToPart(this, GetBodyOrigin(), true);
        }
	}

    #endregion

    #region GameObjects and Hierarchy Structure

    public void DestroyPart(float carryStress = 0f, bool useFX = true, DestructionType destructionType = DestructionType.GENERIC)
    {
        if (useFX)
        {
            bool fallbackToGeneric = false;
            switch (destructionType) {
                case DestructionType.GENERIC:
                    fallbackToGeneric = true;
                    break;
                default:
                    fallbackToGeneric = true;
                    break;
            }
            if(fallbackToGeneric)
            {
                if (DestroyGenericSounds) DestroyGenericSounds.PlayRandomSoundAtPosition(transform.position);
                if (DestroyGenericParticles) DestroyGenericParticles.PlayRandomEffectAtPosition(transform.position, transform.eulerAngles, pfxScale);
            }
        }

        while(connections.Count > 0)
        {
            DisconnectPart(connections[0].toPart, carryStress);
        }
        MultipartPhysBody body = GetBody();
        body.StartCoroutine(body.FullRecalcPhysNextFixedUpdate());
        Destroy(gameObject);
    }

    public void DestroyPartLite()
    {
        DisconnectLiteFromAllParts();
        Destroy(gameObject);
    }

    public void DisconnectLiteFromAllParts()
    {
        while (connections.Count > 0)
        {
            DisconnectPart(connections[0].toPart, 0, true);
        }
    }

    public void DisconnectPart(PhysPart part, float carryStress = 0f, bool lite = false)
    {
        if(part == null)
        {
            RecalcConnections();
            return;
        }
        List<PartConnection> connectionsToRemove = new List<PartConnection>();
        foreach(PartConnection c in connections) //remove connections from this to other
        {
            if(c.toPart == part)
            {
                connectionsToRemove.Add(c);
            }
        }
        while(connectionsToRemove.Count > 0)
        {
            connections.Remove(connectionsToRemove[0]);
            connectionsToRemove.RemoveAt(0);
        }

        foreach (PartConnection c in part.connections) //remove connections from other to this
        {
            if (c.toPart == this)
            {
                connectionsToRemove.Add(c);
            }
        }
        while (connectionsToRemove.Count > 0)
        {
            part.connections.Remove(connectionsToRemove[0]);
            connectionsToRemove.RemoveAt(0);
        }

        if (!lite)
        {
            UpdateTransformStructureIfDisconnected();
            part.UpdateTransformStructureIfDisconnected();
        }

        if (carryStress > 0) //carry leftover stress
        {
            part.ApplyStress(carryStress);
        }
    }

    public void UpdateTransformStructureIfDisconnected()
    {
        MultipartPhysBody oldParent = GetBody();
        if(!CheckConnectedToOrigin())
        {
            PhysPart[] ConnectedParts = PartConnection.GetConnectedParts(this);

            Transform newDebrisCraft = new GameObject(oldParent.name + " debris").transform;
            newDebrisCraft.SetParent(GameplayManager.GameplayTransform);

            Transform newParent = new GameObject("Body").transform;
            newParent.SetParent(newDebrisCraft);

            newParent.transform.position = newDebrisCraft.transform.position = transform.position;
            newParent.transform.rotation = newDebrisCraft.transform.rotation = transform.rotation;

            foreach(PhysPart p in ConnectedParts)
            {
                p.transform.SetParent(newParent);
            }

            newDebrisCraft.gameObject.AddComponent<Rigidbody>().velocity = oldParent.rb.GetPointVelocity(transform.TransformPoint(CoMoffset));
            newDebrisCraft.gameObject.AddComponent<MultipartPhysBody>();

            oldParent.FullRecalcPhysics();
            foreach(PhysPart p in ConnectedParts)
            {
                foreach(PartComponent c in p.components)
                {
                    c.OnBodyChanged();
                }
            }
        }
    }

    public bool CheckConnectedToOrigin()
    {
        return PartConnection.IsConnectedToPart(this, GetBodyOrigin());
    }

    public MultipartPhysBody GetBody()
    {
        return GetComponentInParent<MultipartPhysBody>();
    }
    public PhysPart GetBodyOrigin()
    {
        return GetBody().originPart;
    }

    public void RecalcConnections(bool lite = false) //remove null connections and make sure parts we are connected to are connected to us as well.
    {
        List<PartConnection> nullConnections = new List<PartConnection>();
        foreach (PartConnection c in connections)
        {
            c.fromPart = this;
            if(c.toPart == null)
            {
                nullConnections.Add(c);
            }
            else
            {
                bool connectedBothWays = false;
                foreach (PartConnection c2 in c.toPart.connections)
                {
                    if(c2.toPart == this)
                    {
                        connectedBothWays = true;
                        break;
                    }
                }
                if (!connectedBothWays)
                {
                    c.toPart.connections.Add(new PartConnection(this, c.toPart));
                }
            }
        }
        while(nullConnections.Count > 0)
        {
            connections.Remove(nullConnections[0]);
            nullConnections.RemoveAt(0);
        }

        if(!lite) UpdateTransformStructureIfDisconnected();
    }

    #endregion

    #region Stress

    public float GetTrueHealth()
    {
        return health - currentHeat;
    }

    public float GetEffectiveStrength(float baseStrength)
    {
        return baseStrength * (GetTrueHealth() / maxHealth);
    }

    public void TakeDamage(float dmg, DamageType type)
    {
        if (enabled == false) return;

        if(type == DamageType.HEAT)
        {
            currentHeat += dmg;
            OnHeatChanged();
        }
        else
        {
            health -= dmg;
        }
        if (GetTrueHealth() <= 0)
        {
            DestroyPart();
        }
    }

    public void OnHeatChanged()
    {
        if (mats.Length > 0)
        {
            Color heatColour = new Color(currentHeat / maxHealth, 0, 0);
            SetEmissiveColour(heatColour * mat_heatEmissionFactor);
        }
    }

    public void SetEmissiveColour(Color c)
    {
        foreach (Material mat in mats)
        {
            if (!mat_emissiveOn) //enable emission if not already done
            {
                mat.EnableKeyword("_EMISSION");
            }

            mat.SetColor("_EmissionColor", c);
        }
        mat_emissiveOn = true;
    }

    public void OnImpact(Collision collision)
    {
        if (enabled == false) return;

        if (collision.relativeVelocity.sqrMagnitude < 0.1f) return; //ignore if slow
        //Damage
        float damage = Vector3.Dot(collision.contacts[0].normal, collision.relativeVelocity) * collision.impulse.magnitude/mass;
        damage = Mathf.Abs(damage);
        //Debug.Log(damage + " collision damage to " + transform.name, this);

        //Effects
        if (ImpactSounds)
        {
            float impactLoudness = damage / internalStrength; //scale loudness based on how much damage the impact did compared to maximum internal strength
            if (impactLoudness >= 0.01f)
                ImpactSounds.PlayRandomSoundAtPosition(collision.contacts[0].point, impactLoudness, 1f, impactLoudness);
        }
        if(ImpactParticles)
        {
            ImpactParticles.PlayRandomEffectAtPosition(transform.position, transform.eulerAngles, pfxScale);
        }

        ApplyStress(damage);
    }

    public void ApplyStress(float stress)
    {
        if (enabled == false) return;

        if (stress > GetEffectiveStrength(internalStrength)) //destroy part if impact too strong
        {
            DestroyPart(GetStressToCarry(stress)); //destroy and carry stress
        }
        else //just break connections if impact not strong enough to destroy this part
        {
            List<PartConnection> brokenConnections = new List<PartConnection>();
            foreach (PartConnection c in connections)
            {
                if (stress > c.GetTrueConnectionStrength())
                {
                    if (PartConnection.IsConnectedToPartIgnoringInvalidParts(c.toPart, GetBodyOrigin(), new PhysPart[1] { this }, true))
                    {
                        brokenConnections.Add(c);
                    }
                }
            }
            while (brokenConnections.Count > 0)
            {
                DisconnectPart(brokenConnections[0].toPart, GetStressToCarry(stress)); //disconenct and carry stress
                brokenConnections.RemoveAt(0);
            }
        }
    }

    public float GetStressToCarry(float stress)
    {
        return Mathf.Max(stress - GetEffectiveStrength(internalStrength), 0) * stressCarryFactor;
    }

    #endregion

    void FixedUpdate () {
        if(debugButton_destroyPart)
        {
            DestroyPart();
        }

        if(currentHeat > 0)
        {
            currentHeat = Mathf.Max(currentHeat - heatLossRate*Time.fixedDeltaTime, 0);
            OnHeatChanged();
        }
	}

    public PartBlueprint GetBlueprint()
    {
        PartBlueprint bp = new PartBlueprint();
        bp.partName = partName;
        bp.position = transform.localPosition; //All parts should be direct children of "body". If that weren't the standard, things could get weird considering multiple connections per part etc.
        bp.rotation = transform.localEulerAngles;

        bp.health = health;
        bp.heat = currentHeat;

        //if supported, save each component's extra info
        int i = 0;
        foreach (PartComponent component in components)
        {
            ComponentBlueprint cbp = component.GetBlueprint();
            if (cbp == null) continue;

            cbp.componentIndex = i; //keep track of the index of this component to apply it correctly when loading.
            bp.componentDatas.Add(cbp);
            i++;
        }

        return bp;
    }

    /// <summary>
    /// Applies blueprint's properties to this part.
    /// </summary>
    public void ApplyBlueprint(PartBlueprint bp)
    {
        transform.localPosition = bp.position;
        transform.localEulerAngles = bp.rotation;

        health = bp.health;
        currentHeat = bp.heat;

        //if supported, apply each component's extra info
        foreach (ComponentBlueprint cbp in bp.componentDatas)
        {
            PartComponent component = components[cbp.componentIndex];
            component.ApplyBlueprint(cbp);            
        }
    }
}
