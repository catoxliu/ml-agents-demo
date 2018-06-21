using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarRaceAgent4 : CarRaceBaseAgent
{

    //Improve Agent3 by add more "labels"
    //0 means no car ahead, 0x01 means ahead car on right track,
    //0x10 means on middle track, 0x11 means on left track

    public float m_fSteerFactor = 1f;

    private int m_iFactor = 0;
    private int m_iAction = 0;
    private int m_iRandom = -1;

    public override void AgentReset()
    {
        base.AgentReset();
        m_iFactor = 0;
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        m_iAction = Mathf.FloorToInt(vectorAction[0]);
        bool danger = IsDanger();
        if (!danger) m_iFactor = 0;
        else m_iFactor++;
        int iTrack = 0;
        if (danger)
        {
            Vector3 nextCar = m_RoadManager.GetInFrontCarPos(m_PlayerCar.transform.position);
            iTrack = (Mathf.CeilToInt(nextCar.z / 5.0f) + 2);
        }
        else
        {
            m_iRandom *= -1;
        }
        //The new model will take the model as label or right action.
        SetReward(iTrack);

        //The agent should be trained as much as possible before reset it.
        //Unless your goal is let the agent to achieve something as quickly as possible.
        if (brain.brainType == MLAgents.BrainType.External)
        {
            m_bAction = m_iFactor > 4 && danger;
            m_iAction = iTrack;
        }
        else
            m_bAction = m_iAction > 0;

        if (m_bAction)
        {
            int track = m_iAction - 2;
            if (track == 0)
            {
                m_PlayerCar.Steer(m_fSteerFactor * m_iRandom);
            }
            else
            {
                m_PlayerCar.Steer(m_fSteerFactor * -track);
            }
        }
    }



}
