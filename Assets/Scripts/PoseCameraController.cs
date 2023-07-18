using System.Collections;
using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

public class PoseCameraController : MonoBehaviour
{
    Vector3 lookAtTarget;

    void Awake()
    {
        GameObject targetBiped = GameObject.Find("Biped");
        BipedIK bipedIk = targetBiped.GetComponent<BipedIK>();
        Vector3 headPosition = bipedIk.solvers.lookAt.head.transform.position;
        SetPosition(headPosition + new Vector3(0, 0.5f, 3.0f));
        SetLookAt(headPosition - new Vector3(0, 1.0f, 0));
    }

    void Start()
    {
        
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        float fZoomScale = 2.5f;
        float fRotationScale = 45f;
        if(Input.GetKey(KeyCode.LeftArrow))
        {
            Camera.main.transform.RotateAround(lookAtTarget, Vector3.up, fRotationScale * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            Camera.main.transform.RotateAround(lookAtTarget, Vector3.up, -fRotationScale * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            Camera.main.transform.position -= Camera.main.transform.forward * fZoomScale * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            Camera.main.transform.position += Camera.main.transform.forward * fZoomScale * Time.deltaTime;
        }
    }

    void SetPosition(Vector3 position)
    {
        Camera.main.transform.position = position;
    }

    void SetLookAt(Vector3 target)
    {
        lookAtTarget = target;
        Camera.main.transform.LookAt(lookAtTarget);
    }
}
