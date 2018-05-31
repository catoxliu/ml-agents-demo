using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarRaceAgent1 : Agent
{
    private InfiniteRoadManager m_RoadManager;
    private PlayerCar m_PlayerCar;
    private AgentCamera1 m_AgentCam;
    private float[] m_AgentObservationCache;

    private float m_fRewardIncrement;
    private bool m_bAction = true;

    public override void InitializeAgent()
    {
        m_RoadManager = GetComponentInChildren<InfiniteRoadManager>();
        if (m_RoadManager == null)
        {
            Debug.LogError("Can't find RoadManager");
        }
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

    //It is very hard to train a good model since every step we request an action
    //To simple this, only request action when the "System" recognise it's in danger.
    private void LateUpdate()
    {
        m_RoadManager.NextStep();
        if (m_RoadManager.IsDanger(m_PlayerCar.transform.position))
        {
            RequestDecision();
            m_bAction = false;
        }
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
        //Easy mode, action choose the road
        // 0 - right, 1 - middle, 2 - left
        //Hard mode, action tell car how to steer
        // 0 - stay, 1 - left, 2 - right;
        int action = Mathf.FloorToInt(vectorAction[0]);
        action--;

        //Train for easy mode, if it return the other two road, reward positive.
        //if it return the same road of current, reward negative using add to increment
        int currentPos = (int)(transform.position.z / 5.0) + 1;
        if (action != currentPos)
        {
            Vector3 pos = m_PlayerCar.transform.position;
            pos.z = 5.0f * action;
            m_PlayerCar.Reset(pos);
            SetReward(0.1f);
            m_bAction = true;
        }
        else
        {
            AddReward(-0.1f);
        }

        //Hard mode will control the car
        //if (action == 1)
        //{
        //    m_PlayerCar.Steer(5);
        //}
        //if (action == 2)
        //{
        //    m_PlayerCar.Steer(-5);
        //}
    }

    public override void AgentReset()
    {
        m_RoadManager.ResetGame();
    }

    void CarHit()
    {
        Done();
        SetReward(-1.0f);
        Debug.Log("Car Hit!");
    }

}
