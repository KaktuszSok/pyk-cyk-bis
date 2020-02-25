using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AutoBalancer))]
public class BalancerPlayerInput : BalancerInput {

    public Vector3 inputPowerAsAngle = Vector3.one * 30f;
    public float heightInputPower = 10f;

    Vector3 input;
    Vector3 prevInput;

    public BalancerPlayerInput()
    {
        useTargetYaw = false;
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void FixedUpdate () {
		if(acceptingInput)
        {
            if(Input.GetKey(KeyCode.X))
            {
                freeMode = true;
            }
            else
            {
                freeMode = false;
            }
            //rot
            input.y = Input.GetAxisRaw("Horizontal");
            input.x = Input.GetAxisRaw("Vertical");
            if(Input.GetKey(KeyCode.Q))
            {
                input.z = 1;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                input.z = -1;
            }
            else
            {
                input.z = 0;
            }

            targetTorque.x = input.x * inputPowerAsAngle.x;
            targetTorque.z = input.z * inputPowerAsAngle.z;
            targetTorque.y = input.y * inputPowerAsAngle.y;

            //alt
            if(Input.GetKey(KeyCode.Space))
            {
                targetAlt += heightInputPower * Time.fixedDeltaTime;
            }
            else if(Input.GetKey(KeyCode.LeftShift))
            {
                targetAlt -= heightInputPower * Time.fixedDeltaTime;
            }

            prevInput = input;
        }
        base.FixedUpdate();
	}
}
