using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {

    public static PlayerManager instance;

    public SmoothFollowCam cam;
    bool camHadTarget = false;
    float camSearchTimer = 0.1f;

    public MultipartPhysBody PlayerCraft;
    public bool playerCraftWasNull = false;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        //player craft
        if(!playerCraftWasNull && PlayerCraft == null)
        {
            CraftSpawnMenu.instance.OpenMenu();
        }
        playerCraftWasNull = PlayerCraft == null;

        if (Input.GetKeyDown(KeyCode.Backspace) && PlayerCraft != null) //autodestruct
        {
            PlayerCraft.AutoDestruct(2.5f / PlayerCraft.Parts.Count);
        }

        //menu
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (!CraftSelect.instance.isOpen) CraftSpawnMenu.instance.OpenMenu();
            else CraftSelect.instance.CloseMenu();
        }

        //camera
        if (cam.followTarget == null)
        {
            if(cam.lookMode != SmoothFollowCam.LookMode.FREELOOK) cam.lookMode = SmoothFollowCam.LookMode.FREELOOK;

            if (camSearchTimer > 0)
            {
                camSearchTimer -= Time.deltaTime;
            }
            else
            {
                if (PlayerCraft != null)
                {
                    cam.followTarget = PlayerCraft.transform;
                    cam.lookMode = SmoothFollowCam.LookMode.FOLLOW;
                    cam.rotation = 0;
                    cam.zoom = 1f;
                }

                camSearchTimer = 2.5f;
            }

            if (camHadTarget)
            {
                cam.SetFreelookToCurrRot();
            }
        }

        camHadTarget = cam.followTarget != null;
    }

    public void OnPlayerCraftSpawned(MultipartPhysBody craft)
    {
        //disable player input on previous craft
        SetCraftInputEnabled(false);

        //assign current craft and make camera follow it
        PlayerCraft = craft;
        cam.followTarget = null;
        camSearchTimer = 0f;

        //enable player input on current craft
        SetCraftInputEnabled(true);

        CraftEditor.instance.ClearCurrCraft();
    }

    public void SetCraftInputEnabled(bool enabled)
    {
        if (PlayerCraft && PlayerCraft.GetComponentInChildren<BalancerPlayerInput>())
        {
            PlayerCraft.SetBalancerInputActive(enabled);
        }
    }
}
