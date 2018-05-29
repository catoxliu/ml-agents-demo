using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarFactory : MonoBehaviour {

    public List<GameObject> m_CarPrefabs;

    private static CarFactory m_Instance;
    public static CarFactory Instance
    { get { return m_Instance; } }

    private void Awake()
    {
        m_Instance = this;
    }

    private List<BaseCar> m_CarRecyclePools = new List<BaseCar>();

    private void Start()
    {
        AddPrefabsToPool();
    }

    void AddPrefabsToPool()
    {
        foreach (var car in m_CarPrefabs)
        {
            var go = Instantiate<GameObject>(car);
            var basecar = go.AddComponent<BaseCar>();
            basecar.transform.LookAt(Vector3.back);
            basecar.Hide();
            m_CarRecyclePools.Add(basecar);
        }
    }

    public BaseCar GetARandomCar()
    {
        if (m_CarRecyclePools.Count < 3) AddPrefabsToPool();
        int randomIdx = Random.Range(0, m_CarRecyclePools.Count);
        var car = m_CarRecyclePools[randomIdx];
        m_CarRecyclePools.RemoveAt(randomIdx);
        return car;
    }

    public void RecycleCar(BaseCar car)
    {
        car.Hide();
        m_CarRecyclePools.Add(car);
    }

    public GameObject GetARandomCarPrefab()
    {
        int idx = Random.Range(0, m_CarPrefabs.Count);
        var car = m_CarPrefabs[idx];
        return car;
    }


#if UNITY_EDITOR
    [ContextMenu("RefreshCarPrefabs")]
    void RefreshCarPrefabs()
    {
        var files = System.IO.Directory.GetFiles(Application.dataPath + "/VoxelCars/Prefabs/Cars/");
        m_CarPrefabs = new List<GameObject>();
        foreach (var file in files)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VoxelCars/Prefabs/Cars/"
                + System.IO.Path.GetFileNameWithoutExtension(file));
            if (prefab != null)
            {
                m_CarPrefabs.Add(prefab);
                Debug.Log("add prefab " + prefab.name);
            }
        }
        UnityEditor.AssetDatabase.SaveAssets();
    }
#endif
}
