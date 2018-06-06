using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarRaceAgent1 : CarRaceBaseAgent
{
    //It is very hard to train a good model since every step we request an action
    //To simple this, only request action when the "System" recognise it's in danger.
    //Result: Failed. The agent is still perform random in internal brain.
    private void LateUpdate()
    {
        //m_RoadManager.NextStep();
        //if (m_RoadManager.IsDanger(m_PlayerCar.transform.position))
        //{
        //    RequestDecision();
        //    m_bAction = false;
        //}
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
        //int currentPos = (int)(transform.position.z / 5.0) + 1;
        //if (action != currentPos)
        //{
        //    Vector3 pos = m_PlayerCar.transform.position;
        //    pos.z = 5.0f * action;
        //    m_PlayerCar.Reset(pos);
        //    SetReward(0.1f);
        //    m_bAction = true;
        //}
        //else
        //{
        //    AddReward(-0.1f);
        //}

        //Train 2
        //if car hit another car, not reset, just set negative reward
        //
        int currentPos = (int)(transform.position.z / 5.0) + 1;
        if (m_RoadManager.IsDanger(m_PlayerCar.transform.position))
        {
            if (action != currentPos)
            {
                Vector3 pos = m_PlayerCar.transform.position;
                pos.z = 5.0f * action;
                m_PlayerCar.Reset(pos);
                SetReward(0.1f);
            }
            else
            {
                AddReward(-0.01f);
            }
        }
        else
        {
            if (action == currentPos)
            {
                SetReward(m_fRewardIncrement);
            }
            else
            {
                SetReward(-0.01f);
            }
        }

        //Internal Brain
        //Vector3 pos = m_PlayerCar.transform.position;
        //pos.z = 5.0f * action;
        //m_PlayerCar.Reset(pos);


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

}
