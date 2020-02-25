using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftSpawnMenu : MonoBehaviour {

    public static CraftSpawnMenu instance;
    SpawnMenuCSReceiver CraftSelectReceiver = new SpawnMenuCSReceiver();
    public Transform SpawnPoint;

    private void Awake()
    {
        instance = this;
        CraftSelectReceiver.spawnMenu = this;
    }

    void Start()
    {
        if(SpawnPoint == null)
        {
            SpawnPoint = GameObject.Find("SpawnPoints").transform.GetChild(0);
        }
    }

    public void SpawnSavedCraft(string craftName)
    {
        SpawnCraftFromBlueprint(CraftBlueprint.LoadFromFileByName(craftName));
    }

    public void SpawnCraftFromBlueprint(CraftBlueprint bp)
    {
        MultipartPhysBody craft = CraftBlueprint.SpawnCraftFromBlueprint(bp, SpawnPoint.position, SpawnPoint.eulerAngles);
        PlayerManager.instance.OnPlayerCraftSpawned(craft);
    }

    public void OpenMenu()
    {
        CraftSelect.instance.OpenMenu();
        CraftSelect.instance.SetButtonReceiver(CraftSelectReceiver);
    }

    public void CloseMenu()
    {
        CraftSelect.instance.CloseMenu();
    }
}
