using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftEditor : MonoBehaviour {

    //TODO:
    //-GUI
    //-Save & Delete
    //-Parts List
    //-Moving all connected parts
    //-Rotating all connected parts
    //-Edit Component Datas
    //-Scale Parts

    [Header("Configuration")]
    public LayerMask RayToMouseMask = new LayerMask();
    public Material GhostyMaterial;

    [Header("References")]
    public static CraftEditor instance;
    public MultipartPhysBody currCraft;
    public PhysPart currPart;

    [Header("Runtime Values")]
    public bool canDoActions = true;
    int currPartTrueLayer = 0;
    Quaternion lastObjRot = Quaternion.identity;

    [Header("Runtime Options")]
    float snapResolution = 4f;

    [Header("Camera")]
    public Transform CamLookTarget;
    Vector2 camRot = Vector2.zero;
    Camera cam;

	void Awake () {
        instance = this;
        if (!PartDatabase.InitialLoadDone) PartDatabase.ReloadPartsList();
        cam = Camera.main;
	}

    #region Craft Stuff
    public CraftBlueprint GetBlueprintFromCurrCraft()
    {
        //ignore held part
        if (currPart != null)
        {
            currPart.transform.SetParent(null);
            currPart.DestroyPartLite();
        }

        MultipartPhysBody physBody = currCraft.GetComponent<MultipartPhysBody>();
        physBody.UpdatePhysParts(true);
        foreach(PhysPart p in physBody.Parts)
        {
            p.connections = CalculatePartConnections(p);
        }
        return CraftBlueprint.GetBlueprintFromCraft(physBody);
    }

    public List<PartConnection> CalculatePartConnections(PhysPart p)
    {
        if (p == currPart) return new List<PartConnection>(); //If part is currently held, it has no connections.
        List<PartConnection> connections = new List<PartConnection>();
        foreach (PhysPart otherP in currCraft.Parts)
        {
            if (otherP == p) continue; //skip if checking against self
            if (otherP == currPart) continue; //skip if checking against currently held part

            bool intersects = false;

            foreach (Collider c in p.colliders) //check for each collider of our part
            {
                foreach (Collider otherC in otherP.colliders) //and check against each collider of other part
                {
                    if (c.bounds.Intersects(otherC.bounds))
                    {
                        intersects = true;
                        break;
                    }
                }
                if (intersects) break;
            }

            if (intersects) connections.Add(new PartConnection(otherP, p));
        }
        return connections;
    }	

	public void EditCraftFromBlueprint(CraftBlueprint bp)
    {
        ClearCurrCraft();

        PlayerManager.instance.SetCraftInputEnabled(false);
        PlayerManager.instance.PlayerCraft = null;
        PlayerManager.instance.playerCraftWasNull = true; //To prevent spawn menu from opening again

        currCraft = CraftBlueprint.SpawnCraftFromBlueprint(bp, transform.position, transform.eulerAngles);

        SmoothFollowCam camComp = PlayerManager.instance.cam;
        camComp.followTarget = CamLookTarget;
        camComp.lookMode = SmoothFollowCam.LookMode.ORBIT;
        camComp.freelook_xrot = 25;
        camComp.freelook_yrot = 0;
        camComp.rotation = 45;
        camComp.zoom = 1f;

        camRot = Vector2.zero;
        foreach (PhysPart part in currCraft.GetComponentsInChildren<PhysPart>())
        {
            MakePartEditorFriendly(part);
        }
        currCraft.rb.isKinematic = true;
        currCraft.enabled = false;
    }

    public void CreateNewCraft()
    {
        EditCraftFromBlueprint(new CraftBlueprint());
    }

    public void ClearCurrCraft()
    {
        if (currCraft != null) Destroy(currCraft.gameObject);
    }

    public void SaveCurrCraft()
    {
        if (currCraft == null) return;
        CraftBlueprint.SaveToFile(GetBlueprintFromCurrCraft());
    }

    public void NameCraft(string craftName)
    {
        if (currCraft == null) return;
        currCraft.name = craftName;
    }
    #endregion
    
    #region Part Stuff
    public PhysPart GrabPartFromList(string name)
    {
        if (!PartDatabase.PartsList.ContainsKey(name))
        {
            Debug.LogError("Invalid Part Grab attempt from List - \"" + name + "\"");
            return null;
        }

        ScriptablePartBP bpScriptable = PartDatabase.PartsList[name];
        PartBlueprint bp = bpScriptable.partPrefab.GetComponent<PhysPart>().GetBlueprint();
        return SpawnPartInHand(bp);
    }

    public PhysPart SpawnPartInHand(PartBlueprint bp)
    {
        PhysPart p = SetCurrPart(SpawnEditorPart(bp));
        p.transform.rotation = lastObjRot;
        return p;
    }

    public PhysPart SpawnEditorPart(PartBlueprint bp)
    {
        PhysPart p = bp.SpawnPart(currCraft.transform.GetChild(0));
        return MakePartEditorFriendly(p);
    }

    public PhysPart SetCurrPart(PhysPart p, bool recalcConnectedParts = true)
    {
        PhysPart oldPart = currPart;
        if (oldPart) //Deal with previously held part
        {
            foreach (Collider c in oldPart.GetComponentsInChildren<Collider>()) //make previously held part tangible again
            {
                //c.enabled = true;
                c.gameObject.layer = currPartTrueLayer;
            }
            lastObjRot = oldPart.transform.rotation; //save previous obj rotation
        }

        //Deal with held part transition
        currPart = p;

        if (recalcConnectedParts)
        {
            if (oldPart) //Reconnect old held part appropriately
            {
                oldPart.connections = CalculatePartConnections(oldPart);
                oldPart.RecalcConnections(true);
            }
            if(currPart) //Disconnect new held part
            {
                currPart.DisconnectLiteFromAllParts();
            }
            MakeAllDisconnectedPartsGhosty();
        }

        //Deal with currently held part
        if (p != null)
        {
            foreach (Collider c in currPart.GetComponentsInChildren<Collider>()) //make part being held intangible
            {
                //c.enabled = false;
                currPartTrueLayer = c.gameObject.layer;
                c.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }


            MakePartGhosty(currPart, true); //make curr part render ghosty
        }

        return p;
    }

    public PhysPart MakePartEditorFriendly(PhysPart p)
    {
        //Make it non-solid
        foreach (Collider c in p.GetComponentsInChildren<Collider>())
        {
            c.isTrigger = true;
        }

        //Disable all components
        List<PartComponent> components = new List<PartComponent>(p.components);
        while(components.Count > 0)
        {
            components[0].enabled = false;
            components.RemoveAt(0);
        }
        p.enabled = false;

        //Remember materials
        p.gameObject.AddComponent<EditorPartData>().RememberRendMaterials();

        return p;
    }
    #endregion

    private void Update()
    {
        if(currCraft != null)
        {
            //Shortcuts
            if (Input.GetKeyDown(KeyCode.C))
            {
                PlayerManager.instance.cam.ReturnToOrigin();
                CraftSpawnMenu.instance.SpawnCraftFromBlueprint(GetBlueprintFromCurrCraft());
                ClearCurrCraft();
            }
            if(Input.GetKeyDown(KeyCode.O))
            {
                HighlightPart(currCraft.GetOriginPart(), Color.yellow);
            }

            //Temp
            if(Input.GetKeyDown(KeyCode.Alpha1))
            {
                GrabPartFromList("StructuralCube0");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                GrabPartFromList("Engine0");
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                GrabPartFromList("ControlChip0");
            }

            //Part Actions Input
            if (canDoActions)
            {
                if (currPart != null)
                {
                    if (Input.GetMouseButtonDown(0)) //Place
                    {
                        SetCurrPart(null);
                    }
                    if (Input.GetMouseButtonDown(1)) //Remove
                    {
                        currPart.transform.SetParent(null); //So it instantly is recognised as not a part anymore
                        currPart.DestroyPartLite();
                        SetCurrPart(null, false);
                        MakeAllDisconnectedPartsGhosty(); //In case destroyed part was the origin, make sure that parts connected to new origin are non-ghosty.
                    }

                    //Rotate
                    if (Input.GetKeyDown(KeyCode.A))
                    {
                        currPart.transform.Rotate(0, -15f, 0, Space.Self);
                    }
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        currPart.transform.Rotate(0, 15f, 0, Space.Self);
                    }

                    if (Input.GetKeyDown(KeyCode.W))
                    {
                        currPart.transform.Rotate(15f, 0, 0, Space.Self);
                    }
                    if (Input.GetKeyDown(KeyCode.S))
                    {
                        currPart.transform.Rotate(-15f, 0, 0, Space.Self);
                    }

                    if (Input.GetKeyDown(KeyCode.Q))
                    {
                        currPart.transform.Rotate(0, 0, -15f, Space.Self);
                    }
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        currPart.transform.Rotate(0, 0, 15f, Space.Self);
                    }

                    if(Input.GetKeyDown(KeyCode.R))
                    {
                        currPart.transform.eulerAngles = Vector3.zero;
                    }
                }
                else if (currPart == null)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (Input.GetKey(KeyCode.O)) //Set Origin
                        {
                            RaycastHit h = CastRayThroughMouse();
                            if (h.collider != null && h.collider.GetComponent<PhysPart>())
                            {
                                PhysPart p = h.collider.GetComponent<PhysPart>();
                                currCraft.SetOriginPart(p);
                                HighlightPart(p, Color.yellow);

                                MakeAllDisconnectedPartsGhosty();
                            }
                        }
                        else if (Input.GetKey(KeyCode.LeftAlt)) //Clone
                        {
                            RaycastHit h = CastRayThroughMouse();
                            if (h.collider != null && h.collider.GetComponent<PhysPart>())
                            {
                                PhysPart p = h.collider.GetComponent<PhysPart>();
                                PartBlueprint bp = p.GetBlueprint();
                                lastObjRot = p.transform.rotation;

                                SpawnPartInHand(bp);
                            }
                        }
                        else //Pick Up
                        {
                            RaycastHit h = CastRayThroughMouse();
                            if (h.collider != null && h.collider.GetComponent<PhysPart>())
                            {
                                SetCurrPart(h.collider.GetComponent<PhysPart>());
                            }
                        }
                    }
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (currCraft != null && currPart != null)
        {
            currPart.transform.position = CalculateHoldPosition();
        }
    }
    
    #region Utility
    Vector3 CalculateHoldPosition()
    {
        Vector3 holdPos = Vector3.down;
        bool snap = Input.GetKey(KeyCode.LeftShift);
        Vector3 snapOffset = Vector3.zero;

        Ray rayToMouse = cam.ScreenPointToRay(Input.mousePosition); //Ray from cam through mouse
        if (!IsMouseOverSomething()) //Hover
        {
            Vector3 hoverDir = rayToMouse.direction / cam.transform.InverseTransformDirection(rayToMouse.direction).z; //get direction from cam to mouse normalised in the camera's forward direction, such that no matter the direction, this vector will always end on a hypothetical plane 1 unit in front of the camera.

            float properHoldDist = Vector3.Distance(rayToMouse.origin, CamLookTarget.position); //Hold at a distance so that it just reaches where the camera is centred on, if mouse is perfectly positioned.
            holdPos = rayToMouse.origin + (hoverDir*properHoldDist);

            //Snap Mode
            if (snap)
            {
                holdPos -= snapOffset;
                holdPos = new Vector3(Mathf.Round(holdPos.x * snapResolution), Mathf.Round(holdPos.y * snapResolution), Mathf.Round(holdPos.z * snapResolution)) / snapResolution;
                holdPos += snapOffset;
            }
        }
        else //Stick to part under mouse
        {
            RaycastHit hit = CastRayThroughMouse();
            if (hit.collider != null)
            {
                //Snap where ray hits if in snap mode
                Vector3 adjustedHitPoint = hit.point;
                Vector3 adjustedNormal = hit.normal;
                if (snap)
                {
                    snapOffset = new Vector3(hit.collider.transform.position.x % 1, hit.collider.transform.position.y % 1, hit.collider.transform.position.z % 1); //To make snapping relative to the part under mouse
                    adjustedHitPoint -= snapOffset;
                    adjustedHitPoint = new Vector3(Mathf.Round(adjustedHitPoint.x * snapResolution), Mathf.Round(adjustedHitPoint.y * snapResolution), Mathf.Round(adjustedHitPoint.z * snapResolution)) / snapResolution; //Snap adjusted hit point
                    adjustedHitPoint += snapOffset;

                    //Snap snapped hit point to be flush with the surface of the part our ray has hit
                    //snap normal relative to part under mouse:
                    adjustedNormal = hit.collider.transform.InverseTransformDirection(adjustedNormal);
                    adjustedNormal.x = Mathf.Round(adjustedNormal.x);
                    adjustedNormal.y = Mathf.Round(adjustedNormal.y);
                    adjustedNormal.z = Mathf.Round(adjustedNormal.z);
                    adjustedNormal = hit.collider.transform.TransformDirection(adjustedNormal);

                    Ray normalRay = new Ray(adjustedHitPoint + adjustedNormal * 100f, -adjustedNormal);
                    RaycastHit normalHit = new RaycastHit();
                    if (hit.collider.Raycast(normalRay, out normalHit, 200f))
                    {
                        adjustedHitPoint = normalHit.point;
                    }
                }

                if (currPart.colliders.Length > 0 && currPart.colliders[0] != null) //Adjust the part placement such that there is little overlap between our part and where the ray hit.
                {
                    Vector3 currPartColliderPivot = currPart.colliders[0].transform.position;

                    Ray normalRay = new Ray(currPartColliderPivot - adjustedNormal * 100f, adjustedNormal);
                    RaycastHit normalHit = new RaycastHit();
                    if(currPart.colliders[0].Raycast(normalRay, out normalHit, 200f))
                    {
                        Vector3 moveVector = currPart.colliders[0].transform.position - normalHit.point; //Offset which will translate collider to reduce overlapping.
                        holdPos = adjustedHitPoint + (moveVector*0.99f);
                    }
                    else holdPos = adjustedHitPoint;
                }
                else //for parts with no shape set up
                {
                    holdPos = adjustedHitPoint;
                }
            }
        }

        return holdPos;
    }

    RaycastHit CastRayThroughMouse()
    {
        Ray rayToMouse = cam.ScreenPointToRay(Input.mousePosition); //Ray from cam through mouse

        RaycastHit hit = new RaycastHit();
        Physics.Raycast(rayToMouse, out hit, 100f, RayToMouseMask);
        if(currCraft != null && hit.collider != null)
        {
            if(!hit.collider.transform.IsChildOf(currCraft.transform))
            {
                hit = new RaycastHit(); //clear hit info - we hit something that was not related to the craft being edited.
            }
        }

        return hit;
    }

    bool IsMouseOverSomething()
    {
        return Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), 100f, RayToMouseMask);
    }

    #endregion
    
    #region UI/UX
    public void MakeAllDisconnectedPartsGhosty()
    {
        if (currCraft == null) return;
        //update structure
        currCraft.UpdatePhysParts(true);

        List<PhysPart> connectedParts = new List<PhysPart>(PartConnection.GetConnectedParts(currCraft.originPart));
        foreach(PhysPart part in currCraft.Parts)
        {
            if(connectedParts.Contains(part))
            {
                if(currPart == null || PartConnection.IsConnectedToPartIgnoringInvalidParts(part, currCraft.originPart, new PhysPart[1] {currPart})) //Check if part is connected to origin, IGNORING held part.
                    MakePartGhosty(part, false);
            }
            else
            {
                MakePartGhosty(part, true);
            }
        }
    }

    public void HighlightPart(PhysPart p, Color c)
    {
        if (p == null) return;
        p.gameObject.AddComponent<Proxy_HighlightPart>().HighlightPart(c);
    }

    void MakePartGhosty(PhysPart p, bool ghosty)
    {
        MeshRenderer[] rends = p.GetComponentsInChildren<MeshRenderer>();
        if(ghosty)
        {
            foreach(MeshRenderer r in rends)
            {
                r.material = GhostyMaterial;
            }
        }
        else
        {
            Material[] trueMats = p.GetComponent<EditorPartData>().trueRendMaterials;
            for(int i = 0; i < rends.Length; i++)
            {
                rends[i].material = trueMats[i];
            }
        }
        p.mat_emissiveOn = false; //Material emission will need to be turned on again
    }
    #endregion
}
