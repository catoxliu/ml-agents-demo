using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CarRaceAgent5 : Agent
{
    public UnityAction m_AgentReset;
    public PlayerCar m_PlayerCar;
    protected AgentCamera1 m_AgentCam;

    protected float[] m_AgentObservationCache, m_RewardPoints;

    protected float m_fRewardIncrement;

    protected int m_iStepCount = 0, m_iRewardCount = 0;

    protected bool m_bCarOnRoad = false;

    // Use this for initialization
    public override void InitializeAgent()
    {
        m_PlayerCar.CarHitAction += CarHit;
        m_AgentCam = GetComponentInChildren<AgentCamera1>();
        if (m_AgentCam == null)
        {
            Debug.LogError("Can't find Agent Camera!");
        }
        m_AgentObservationCache = new float[m_AgentCam.m_iResWidth * m_AgentCam.m_iResHeight];

        m_fRewardIncrement = 1.0f / (float)agentParameters.maxStep;
    }

    public override void CollectObservations()
    {
        if (m_AgentCam.GetDepthDataFloat(ref m_AgentObservationCache))
        {
            AddVectorObs(m_AgentObservationCache);
        }
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        float action = Mathf.Clamp(vectorAction[0], -1f, 1f);
        m_PlayerCar.Steer(0.8f * action);
        if (!IsDone())
        {
            float carBias = Mathf.Abs(m_PlayerCar.transform.position.z) % 5;
            carBias = Mathf.Sqrt(Mathf.Abs(2.5f - carBias) / 2.5f);
            AddReward(m_fRewardIncrement * carBias);
            m_iStepCount++;
            if (m_bCarOnRoad && m_iStepCount >= m_RewardPoints[m_iRewardCount])
            {
                AddReward(1.0f);
                m_iRewardCount++;
                m_bCarOnRoad = m_iRewardCount < m_RewardPoints.Length;
            }
            if (m_iStepCount >= agentParameters.maxStep)
            {
                Done();
            }
        }
    }

    public override void AgentReset()
    {
        m_PlayerCar.Reset();
        m_fRewardIncrement = 1.0f / (float)agentParameters.maxStep;
        m_iStepCount = 0;
        m_iRewardCount = 0;
        if (m_AgentReset != null) m_AgentReset();
    }

    public void SetRewardPoints(float[] bornCars)
    {
        m_RewardPoints = (float [])bornCars.Clone();
        m_bCarOnRoad = m_RewardPoints.Length > 0;
    }

    protected virtual void CarHit()
    {
        Done();
        SetReward(-1.0f);
        Debug.Log("Car Hit at " + m_PlayerCar.transform.position);
    }

}
