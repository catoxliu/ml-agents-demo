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
    //m_iDriveLength: increment every frame until player hit something.
    //m_iPassCarCount: how many car player passed until player hit something.
    //m_iScoreRecord: the highest score player has achieved, score = m_iDriveLength * m_iPassCarCount
    private float m_fSpeed = 1.0f, m_fGenerateRate = 3.0f;
    private int m_iCursor = 0, m_iDriveLength = 0, m_iPassCarCount = 0, m_iScoreRecord = 0;
    private PlayerCar m_Player;

	// Use this for initialization
	void Start () {
        for(int i = 0; i < m_iRoadPoolSize; i++)
        {
            m_RoadPool.Add(GameObject.Instantiate(m_RoadPrefab).transform);
            m_RoadPool[i].position = new Vector3(m_fRoadLength * i, -0.5f, 0);
        }
        InitCars();
        //In ML-Agent, create player car should be somewhere else
        //CreatePlayerCar();
    }

    public void ResetGame()
    {
        ClearNearCars();
        Vector3 carPos = m_CarsOnRoad[0].transform.position;
        carPos.z = 0;
        m_CarsOnRoad[0].Reset(carPos);
        m_iDriveLength = 0;
        m_iPassCarCount = 0;
    }

    //Use Angent To update road manager.
    public void NextStep() {
        //For just a same look-like straight road, no need to move the road.
        //MoveRoad();
        MoveCars();
        m_iDriveLength++;
        if (m_iDriveLength * m_iPassCarCount > m_iScoreRecord)
            m_iScoreRecord = m_iDriveLength * m_iPassCarCount;
    }

    public Vector3 GetNextCarPos(int startIdx = 0)
    {
        if (startIdx < m_CarsOnRoad.Count - 1)
            return m_CarsOnRoad[startIdx + 1].transform.position;
        else
            return Vector3.zero;
    }

    //To use the simplest way to detect whether player is facing a car in front
    public bool IsDanger(Vector3 pos)
    {
        foreach (var car in m_CarsOnRoad)
        {
            //if the car is to near or in diffrent track, continue
            if (car.transform.position.x < 8 || car.transform.position.z != pos.z)
                continue;
            //if it is still far away, break;
            if (car.transform.position.x - pos.x > 30)
                break;
            //if the car in front of player and less than X, return true.
            if (car.transform.position.x - pos.x < 15)
                return true;
        }
        return false;
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
            if (m_CarsOnRoad[m_iCursor].transform.position.x < 0)
            {
                CarFactory.Instance.RecycleCar(m_CarsOnRoad[m_iCursor]);
                m_CarsOnRoad.RemoveAt(m_iCursor);
                m_iPassCarCount++;
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
        int iInitCarNum = Random.Range(6, 12);
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
            if (m_CarsOnRoad[m_iCursor].transform.position.x < m_fCarLength+8)
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
    Rect scoreBoardSize = new Rect(0, 0, 300, 30);

    private void OnGUI()
    {
        GUI.Label(scoreBoardSize, string.Format("Length:{0,5:00000} PassCar:{1,3:000} Score Record:{2} ", 
            m_iDriveLength, m_iPassCarCount, m_iScoreRecord));
    }
}
