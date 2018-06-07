using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarRaceBaseAgent : Agent {

    protected InfiniteRoadManager m_RoadManager;
    protected PlayerCar m_PlayerCar;
    protected AgentCamera1 m_AgentCam;
    protected float[] m_AgentObservationCache;

    protected float m_fRewardIncrement;
    protected bool m_bAction = true;

    public override void InitializeAgent()
    {
        m_RoadManager = GetComponentInChildren<InfiniteRoadManager>();
        if (m_RoadManager == null)
        {
            Debug.LogError("Can't find RoadManager");
        }
        m_RoadManager.IsTrainMode = brain.brainType == BrainType.External;
        m_PlayerCar = GetComponentInChildren<PlayerCar>();
        if (m_PlayerCar == null)
        {
            Debug.LogError("Can't find PlayerCar!");
        }
        m_PlayerCar.CarHitAction = CarHit;
        m_AgentCam = GetComponentInChildren<AgentCamera1>();
        if (m_AgentCam == null)
        {
            Debug.LogError("Can't find Agent Camera!");
        }
        m_AgentObservationCache = new float[m_AgentCam.m_iResWidth * m_AgentCam.m_iResHeight];
        brain.brainParameters.vectorObservationSize = m_AgentCam.m_iResWidth * m_AgentCam.m_iResHeight;

        m_fRewardIncrement = 1.0f / (float)agentParameters.maxStep;
    }

    public override void CollectObservations()
    {
        m_RoadManager.NextStep();
        if (m_AgentCam.GetDepthDataFloat(ref m_AgentObservationCache))
        {
            AddVectorObs(m_AgentObservationCache);
        }
    }

    public override void AgentReset()
    {
        m_RoadManager.ResetGame();
        m_PlayerCar.Reset();
        m_bAction = false;
    }

    protected virtual void CarHit()
    {
        Done();
        SetReward(-10.0f);
        Debug.Log("Car Hit at " + m_PlayerCar.transform.position);
    }

    protected void PlayerAction()
    {
        if (brain.brainType == BrainType.External)
        {
            ExternalPlayerAction();
        }
        else if (brain.brainType == BrainType.Internal)
        {
            InternalPlayerAction();
        }
    }

    //Use this for internal test
    protected virtual void InternalPlayerAction()
    {

    }

    //Use this for external train.
    protected virtual void ExternalPlayerAction()
    {

    }

    protected bool IsDanger()
    {
        return m_RoadManager.IsDanger(m_PlayerCar.transform.position);
    }

    //Put the agent in track that will face the nearest car ahead
    //This should be used in training to make it more efficient
    protected void SetPlayerToNextCar()
    {
        Vector3 curPos = m_PlayerCar.transform.position;
        int i = 0;
        Vector3 nextPos;
        while(i >= 0)
        {
            nextPos = m_RoadManager.GetNextCarPos(i);
            if (nextPos == Vector3.zero) break;
            if (nextPos.z != curPos.z)
            {
                curPos.z = nextPos.z;
                m_PlayerCar.Reset(curPos);
                break;
            }
            i++;
        }
    }

    //Just change the agent's track randomly.
    protected void SetPlayerToBesides()
    {
        Vector3 curPos = m_PlayerCar.transform.position;
        if (curPos.z != 0)
        {
            curPos.z = 0;
            m_PlayerCar.Reset(curPos);
        }
        else
        {
            curPos.z = Random.Range(0, 2) == 0 ? 5.0f : -5.0f;
            m_PlayerCar.Reset(curPos);
        }
    }
}
