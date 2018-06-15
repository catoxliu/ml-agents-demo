using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarRaceAcademy2 : Academy
{
    /// <summary>
    /// Try use curriculum training to train the agent with PPO model.
    /// </summary>

    public List<CarRaceAgent5> m_Agents;

    private List<InfiniteRoadManager> m_RoadManagerList;

    private int m_iCarsOnRoad, m_iGoalDistance, m_iGenerateRate;

    public override void InitializeAcademy()
    {
        m_RoadManagerList = new List<InfiniteRoadManager>();
        foreach(var agent in m_Agents)
        {
            if (!agent.gameObject.activeSelf) break;
            var road = agent.GetComponentInChildren<InfiniteRoadManager>();
            if (road == null)
            {
                Debug.LogError("Can't find RoadManager");
                break;
            }
            m_RoadManagerList.Add(road);
            road.enabled = false;
            road.InitRoads();
            agent.m_AgentReset += AgentResetCallback;
        }
    }

    public override void AcademyReset()
    {
        ResetRoad();
    }

    public override void AcademyStep()
    {
        foreach(var road in m_RoadManagerList)
        {
            road.NextStep();
        }
    }

    void ResetRoad()
    {
        m_iCarsOnRoad = (int)resetParameters["CarsOnRoad"];
        m_iGoalDistance = (int)resetParameters["GoalDistance"];
        m_iGenerateRate = (int)resetParameters["GenerateRate"];

        for(int i = 0; i < m_RoadManagerList.Count; i++)
        {
            m_RoadManagerList[i].IsTrainMode = IsCommunicatorOn();
            m_RoadManagerList[i].ClearAllCars();
            if (m_iCarsOnRoad > 0)
            {
                //Generate cars
                float[] bornCars = m_RoadManagerList[i].GenerateNewCars(m_iCarsOnRoad, m_iGoalDistance, 40);
                m_Agents[i].SetRewardPoints(bornCars);
                m_Agents[i].agentParameters.maxStep = Mathf.CeilToInt(bornCars[bornCars.Length - 1]) + 10;
            }
            else
            {
                m_Agents[i].agentParameters.maxStep = m_iGoalDistance;
            }

            m_RoadManagerList[i].CarGenerateRage = m_iGenerateRate;
        }
    }

    void AgentResetCallback(CarRaceAgent5 agent)
    {
        if (m_iCarsOnRoad > 0)
        {
            int idx = m_Agents.IndexOf(agent);
            m_RoadManagerList[idx].ClearNearCars(50.0f);
            float[] bornCars = m_RoadManagerList[idx].SupplyEnoughCarsOnRoad(m_iCarsOnRoad, m_iGoalDistance, 40);
            agent.SetRewardPoints(bornCars);
            agent.agentParameters.maxStep = Mathf.CeilToInt(bornCars[bornCars.Length - 1]) + 10;
        }
    }

}
