using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AutoBalancer))]
public abstract class BalancerInput : PartComponent {

    protected AutoBalancer balancer;

    [Header("Parameters")]
    public float minAlt = 5f;
    public float maxAlt = 30f;

    [Header("Runtime")]
    public float targetYaw = 0f;
    public Vector3 targetTorque = Vector3.zero;
    protected Vector3 prevTargetTorque = Vector3.zero;
    public float targetAlt = 15f;
    protected float prevTargetAlt = 15f;
    public bool acceptingInput = true;
    public bool freeMode = false;
    protected bool prevWasFreeMode = false;
    public bool useTargetYaw = true;

    public void ChangeBalancerInputType<T>() where T: BalancerInput
    {
        T newBalancer = gameObject.AddComponent<T>();
        newBalancer.minAlt = minAlt;
        newBalancer.maxAlt = maxAlt;
        Destroy(this);
        part.UpdateComponentsList();
    }

    protected override void Start()
    {
        base.Start();
        balancer = GetComponent<AutoBalancer>();
        balancer.disregardYOrientation = !useTargetYaw;
    }

    protected virtual void FixedUpdate()
    {
        if (acceptingInput)
        {
            //rot
            //disable velocity damping if trying to accelerate
            if (targetTorque.z == 0) balancer.XVelDampingCoefficient = 1;
            else if (targetTorque.z != 0) balancer.XVelDampingCoefficient = 0;//Mathf.Sign(input.z * balancer.localVel.x);
            if (targetTorque.x == 0) balancer.ZVelDampingCoefficient = 1;
            else if (targetTorque.x != 0) balancer.ZVelDampingCoefficient = 0;//-Mathf.Sign(input.x * balancer.localVel.z);

            if (prevTargetTorque != targetTorque || targetTorque.y != 0 && !useTargetYaw || freeMode != prevWasFreeMode)
            {
                if (!freeMode)
                {
                    //set tilt
                    balancer.targetEuler.x = targetTorque.x;
                    balancer.targetEuler.z = targetTorque.z;
                    if (useTargetYaw)
                    {
                        balancer.targetEuler.y = targetTorque.y;
                    }

                    prevTargetTorque = targetTorque;

                    targetTorque.x = targetTorque.z = 0; //in non-free mode, only allow the Y target torque to reach the torqueBias. The other axes are handled by targetEuler in the balancer.
                    if (useTargetYaw) targetTorque.y = 0; //stop Y target torque from reaching torqueBias as we are controlling Y rotation through targetEuler instead.

                }
                balancer.torqueBias = targetTorque;
            }

            //alt
            if (freeMode && targetAlt < prevTargetAlt) //if dropping in free mode, disable all engines.
            {
                balancer.forceDisableAllEnginesThisFrame = true;
            }

            targetAlt = Mathf.Clamp(targetAlt, minAlt, maxAlt);
            balancer.targetAltitude = targetAlt;

            prevTargetAlt = targetAlt;

            //misc
            prevWasFreeMode = freeMode;
        }
    }
}
