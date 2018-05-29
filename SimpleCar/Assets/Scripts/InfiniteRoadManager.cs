using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteRoadManager : MonoBehaviour {

    public GameObject m_RoadPrefab;

    private static readonly int m_iRoadPoolSize = 3;
    private List<Transform> m_RoadPool = new List<Transform>();
    private List<BaseCar> m_CarsOnRoad = new List<BaseCar>();
    private static readonly float m_fRoadLength = 100.0f, m_fCarLength = 10.0f, 
        m_fRoadLengthTotal = m_fRoadLength * m_iRoadPoolSize;

    //m_fSpeed: the speed of player car, which is used to move the road in face
    //m_fGenerateRate: this factor could define the new car generate rate. the smaller, the more.
    //it can not be set to less than 1;
    //m_iScore: increment every frame until player hit something.
    private float m_fSpeed = 1.0f, m_fGenerateRate = 3.0f;
    private int m_iCursor = 0, m_iScore = 0;
    private PlayerCar m_Player;

	// Use this for initialization
	void Start () {
        for(int i = 0; i < m_iRoadPoolSize; i++)
        {
            m_RoadPool.Add(GameObject.Instantiate(m_RoadPrefab).transform);
            m_RoadPool[i].position = new Vector3(m_fRoadLength * i, -0.5f, 0);
        }
        InitCars();
        CreatePlayerCar();
    }

    void ResetGame()
    {
        ClearNearCars();
        m_Player.Reset();
        m_iScore = 0;
    }
	
	void Update () {
        //For just a same look-like straight road, no need to move the road.
        //MoveRoad();
        MoveCars();
        m_iScore++;
    }

    void MoveRoad()
    {
        for (m_iCursor = 0; m_iCursor < m_iRoadPoolSize; m_iCursor++)
        {
            if (m_RoadPool[m_iCursor].position.x < -m_fRoadLength/2)
            {
                m_RoadPool[m_iCursor].Translate(m_fRoadLengthTotal - m_fSpeed, 0, 0, Space.World);
            }
            else
            {
                m_RoadPool[m_iCursor].Translate(-m_fSpeed, 0, 0, Space.World);
            }
        }
    }

    void MoveCars()
    {
        for(m_iCursor = m_CarsOnRoad.Count - 1; m_iCursor >= 0; m_iCursor--)
        {
            m_CarsOnRoad[m_iCursor].Move();
            if (m_CarsOnRoad[m_iCursor].transform.position.x < -m_fCarLength)
            {
                CarFactory.Instance.RecycleCar(m_CarsOnRoad[m_iCursor]);
                m_CarsOnRoad.RemoveAt(m_iCursor);
            }
        }
        //Here needs a algorithm to add new car on roads.
        //If the last car has run over a distance, we could generate a new one.
        if (m_CarsOnRoad[m_CarsOnRoad.Count-1].transform.position.x < m_fRoadLengthTotal - m_fCarLength * m_fGenerateRate)
        {
            GenerateNewCars(1, Random.Range(0.0f, m_fCarLength), m_fRoadLengthTotal);
        }
    }

    void InitCars()
    {
        ClearAllCars();
        int iInitCarNum = Random.Range(10, 20);
        GenerateNewCars(iInitCarNum, m_fRoadLengthTotal, m_fCarLength);
    }

    void GenerateNewCars(int car_count, float born_road_length, float born_start_pos_x = 0)
    {
        int carPosZ;
        float carSpeed, carPosX;
        for (m_iCursor = 0; m_iCursor < car_count; m_iCursor++)
        {
            var car = CarFactory.Instance.GetARandomCar();
            carPosZ = Random.Range(-1, 2);
            carPosX = Random.Range(0.1f, 1.0f);
            carPosX = born_start_pos_x + (born_road_length / car_count) * (m_iCursor + carPosX);
            carSpeed = m_fSpeed - 0.8f;//Random.Range(0.1f, m_fSpeed);
            car.Reset(new Vector3(carPosX, 0, carPosZ * 5.0f));
            car.SetSpeed(m_fSpeed, carSpeed);
            car.Show();
            m_CarsOnRoad.Add(car);
        }
    }

    void ClearAllCars()
    {
        for (m_iCursor = m_CarsOnRoad.Count - 1; m_iCursor >= 0; m_iCursor--)
        {
            CarFactory.Instance.RecycleCar(m_CarsOnRoad[m_iCursor]);
            m_CarsOnRoad.RemoveAt(m_iCursor);
        }
    }

    void ClearNearCars()
    {
        for (m_iCursor = m_CarsOnRoad.Count - 1; m_iCursor >= 0; m_iCursor--)
        {
            if (m_CarsOnRoad[m_iCursor].transform.position.x < m_fCarLength*m_fGenerateRate)
            {
                CarFactory.Instance.RecycleCar(m_CarsOnRoad[m_iCursor]);
                m_CarsOnRoad.RemoveAt(m_iCursor);
            }
        }
    }

    void CreatePlayerCar()
    {
        var prefab = CarFactory.Instance.GetARandomCarPrefab();
        var go = Instantiate<GameObject>(prefab);
        //make sure car prefab look at the backword of the world.
        go.transform.LookAt(Vector3.back);
        m_Player = go.AddComponent<PlayerCar>();
        m_Player.Reset();
        m_Player.Show();

        m_Player.CarHitAction = ResetGame;
    }

    //Socre Board to show some info
    Rect scoreBoardSize = new Rect(0, 0, 100, 30);

    private void OnGUI()
    {
        GUI.Label(scoreBoardSize, "Scores : " + m_iScore);
    }
}
