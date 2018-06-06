using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarRaceAgent3 : CarRaceBaseAgent
{

    //Train the agent with the new Custom model
    //which is baiscally a CNN classifier
    //and SetReward is the way to label the observation
    //Finally, it works :)
    //see TFModels/car_custom_model_with_only_danger_detection.bytes

    private int m_iFactor = 0;

    public override void AgentReset()
    {
        base.AgentReset();
        m_iFactor = 0;
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        //0 - no danger 1 - in danger
        int action = Mathf.FloorToInt(vectorAction[0]);
        bool danger = IsDanger();
        if (!danger) m_iFactor = 0;
        else m_iFactor++;

        //The new model will take the model as label or right action.
        SetReward(danger ? 1 : 0);
        
        //The agent should be trained as much as possible before reset it.
        //Unless your goal is let the agent to achieve something as quickly as possible.
        if (brain.brainType == BrainType.External)
            m_bAction = m_iFactor > 10 && danger;
        else
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
