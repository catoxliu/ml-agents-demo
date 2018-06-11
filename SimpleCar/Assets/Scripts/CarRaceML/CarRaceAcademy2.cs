using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarRaceAcademy2 : Academy
{
    /// <summary>
    /// Try use curriculum training to train the agent with PPO model.
    /// </summary>

    public CarRaceAgent5 m_Agent;

    private InfiniteRoadManager m_RoadManager;

    private int m_iCarsOnRoad, m_iGoalDistance, m_iGenerateRate;

    public override void InitializeAcademy()
    {
        m_RoadManager = GetComponentInChildren<InfiniteRoadManager>();
        if (m_RoadManager == null)
        {
            Debug.LogError("Can't find RoadManager");
        }
        m_RoadManager.enabled = false;
        m_RoadManager.InitRoads();

        m_Agent.m_AgentReset += AgentResetCallback;
    }

    public override void AcademyReset()
    {
        ResetRoad();
    }

    public override void AcademyStep()
    {
        m_RoadManager.NextStep();
    }

    void ResetRoad()
    {
        m_RoadManager.IsTrainMode = IsCommunicatorOn();
        m_iCarsOnRoad = (int)resetParameters["CarsOnRoad"];
        m_iGoalDistance = (int)resetParameters["GoalDistance"];
        m_iGenerateRate = (int)resetParameters["GenerateRate"];

        m_RoadManager.ClearAllCars();
        if (m_iCarsOnRoad > 0)
        {
            //Generate cars
            float[] bornCars = m_RoadManager.GenerateNewCars(m_iCarsOnRoad, m_iGoalDistance, 30);
            m_Agent.SetRewardPoints(bornCars);
        }

        m_RoadManager.CarGenerateRage = m_iGenerateRate;

        m_Agent.agentParameters.maxStep = m_iGoalDistance;
    }

    void AgentResetCallback()
    {
        if (m_iCarsOnRoad > 0)
        {
            m_RoadManager.ResetGame();
            float[] bornCars = m_RoadManager.SupplyEnoughCarsOnRoad(m_iCarsOnRoad, m_iGoalDistance);
            m_Agent.SetRewardPoints(bornCars);
        }
    }

}
