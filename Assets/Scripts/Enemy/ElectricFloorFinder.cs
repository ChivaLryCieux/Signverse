using UnityEngine;

public class ElectricFloorFinder : MonoBehaviour
{
    void Start()
    {
        StampingPiston[] electricFloors = FindObjectsOfType<StampingPiston>(true);

        Debug.Log($"Found {electricFloors.Length} ElectricFloor objects.");

        foreach (StampingPiston floor in electricFloors)
        {
            Debug.Log(GetHierarchyPath(floor.gameObject));
        }
    }

    string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;

        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}