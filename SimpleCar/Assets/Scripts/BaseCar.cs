using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCar : MonoBehaviour {

    protected float m_CarSpeed = 0.0f, m_MoveSpeed = 0.0f;

    public void Reset(Vector3 pos)
    {
        transform.position = pos;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Move()
    {
        transform.Translate(m_MoveSpeed, 0, 0, Space.World);
    }

    public void SetSpeed(float roadSpeed, float carSpeed = 0)
    {
        if (carSpeed > 0) m_CarSpeed = carSpeed;
        m_MoveSpeed = m_CarSpeed - roadSpeed;
    }

    public void Steer(float amount)
    {
        transform.Translate(0, 0, amount, Space.World);
    }

}
