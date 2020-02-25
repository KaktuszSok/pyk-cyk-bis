using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPartBlueprint", menuName = "ScriptableObjects/Part Blueprint")]
public class ScriptablePartBP : ScriptableObject {
    public GameObject partPrefab;
}
