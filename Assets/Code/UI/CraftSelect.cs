using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftSelect : MonoBehaviour {

    public static CraftSelect instance;
    public GameObject ButtonPrefab;
    public GameObject CreateNewButtonPrefab;
    public Transform ButtonParent;

    public CraftSelectReceiver currReceiver;
    public Transform CreateNewButton = null;

    public bool isOpen = false;

    private void Awake()
    {
        instance = this;
    }

    public void UpdateButtons()
    {
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach(Transform t in ButtonParent)
        {
            childrenToDestroy.Add(t.gameObject);
        }
        while (childrenToDestroy.Count > 0)
        {
            Destroy(childrenToDestroy[0]);
            childrenToDestroy.RemoveAt(0);
        }

        string[] savedCrafts = SaveLoadJSON.SearchForFiles("crafts", "*.bp");

        foreach(string s in savedCrafts)
        {
            GameObject button = Instantiate(ButtonPrefab, ButtonParent);
            button.name = s;
            button.GetComponentInChildren<Text>().text = s;
        }

        CreateNewButton = Instantiate(CreateNewButtonPrefab, ButtonParent).transform; //Button for creating a new craft
        Button CreateNewButtonComp = CreateNewButton.GetComponentInChildren<Button>();
        CreateNewButtonComp.onClick.AddListener(() => CraftEditor.instance.CreateNewCraft());
        CreateNewButtonComp.onClick.AddListener(() => CloseMenu());

    }

    public void SetButtonReceiver(CraftSelectReceiver receiver)
    {
        currReceiver = receiver;
        foreach(Transform t in ButtonParent)
        {
            if(t == CreateNewButton)
            {
                continue;
            }

            Button button = t.GetComponentInChildren<Button>();
            button.onClick.RemoveAllListeners();
            if(receiver != null) button.onClick.AddListener(() => receiver.OnCraftSelected(t.name));
        }
    }

    public void OpenMenu()
    {
        gameObject.SetActive(true);
        UpdateButtons();

        isOpen = true;
    }

    public void CloseMenu()
    {
        gameObject.SetActive(false);

        isOpen = false;
    }
}
