using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarRaceAgent2 : CarRaceBaseAgent {

    //This Agent only for danger detection train
    //Try to tune the rewards the agent achieved, still failed!
    //The trained model will rather ignore the danger or 
    //just keep think it's danger all the time
    //Anyway, I could not figure out a way to train the PPO model 
    //to distinguish if a car is in front it or not.

    private int m_iFactor = 0, m_iCorrectCount = 0;

    public override void InitializeAgent()
    {
        base.InitializeAgent();
        m_fRewardIncrement = m_fRewardIncrement * 10;
    }

    public override void AgentReset()
    {
        base.AgentReset();
        m_iFactor = 0;
        m_iCorrectCount = 0;
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        //0 - no danger 1 - in danger
        int action = Mathf.FloorToInt(vectorAction[0]);
        bool danger = IsDanger();
        if (m_bAction && danger) m_iFactor = 0;
        if (m_iFactor < 10) m_iFactor++;
        if (danger)
        {
            if (action == 0)
            {
                AddReward(-0.2f * m_iFactor);
            }
            else
            {
                m_iCorrectCount++;
                AddReward(0.8f);
                m_iFactor = 0;
            }
        }
        else if (!danger && action == 0)
        {
            AddReward(0.1f * m_iCorrectCount);
        }
        else
        {
            AddReward(-0.1f * m_iFactor);
            m_iCorrectCount = 0;
            if (brain.brainType == BrainType.External) return;
        }

        m_bAction = action == 1;

        PlayerAction();
    }

    protected override void ExternalPlayerAction()
    {
        if (m_bAction)
        {
            SetPlayerToNextCar();
        }
    }

    protected override void InternalPlayerAction()
    {
        if (m_bAction)
        {
            SetPlayerToBesides();
        }
    }

}
