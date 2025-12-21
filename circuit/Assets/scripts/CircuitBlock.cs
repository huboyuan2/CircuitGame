using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitBlock : MonoBehaviour
{
    public List<bool> wasdBoundaryConnection = new List<bool>();
    public short rotateStatus = 0;
    Transform children;
    
    // Start is called before the first frame update
    void Start()
    {
        children = transform.Find("Cube");
    }

    // Update is called once per frame
    void Update()
    {

    }
    void ClockWiseRotete()
    {

        if (children)
        {
            children.RotateAround(children.transform.position, Vector3.up, 90);
        }
        ClockWiseRoteUpdateData();
    }
    void AnticlockWiseRotete()
    {

        if (children)
        {
            children.RotateAround(children.transform.position, Vector3.up, -90);
        }
        AnticlockWiseRoteUpdateData();
    }
    void ClockWiseRoteUpdateData()
    {
        rotateStatus = (short)((rotateStatus + 1) % 4);
        if (wasdBoundaryConnection.Count > 0)
        {
            LeftMoveListInPlace(wasdBoundaryConnection);
        }
    }
    void AnticlockWiseRoteUpdateData()
    {
        rotateStatus = (short)((rotateStatus + 3) % 4);
        if (wasdBoundaryConnection.Count > 0)
        {
            RightMoveListInPlace(wasdBoundaryConnection);
        }
    }
    void LeftMoveListInPlace(List<bool> list)
    {
        if (list.Count <= 1) return;
        bool first = list[0];
        for (int i = 0; i < list.Count - 1; i++)
        {
            list[i] = list[i + 1];
        }
        list[list.Count - 1] = first;
    }
    void RightMoveListInPlace(List<bool> list)
    {
        if (list.Count <= 1) return;
        bool last = list[list.Count - 1];

        for (int i = list.Count - 1; i > 0; i--)
        {
            list[i] = list[i - 1];
        }

        list[0] = last;
    }
}