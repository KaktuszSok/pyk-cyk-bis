using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBalancer : PartComponent {

    [Header("Altitude")]
    public float targetAltitude = 15f;
    float prevTargetAltitude = 15f;
    float altitude = 0f;
    public PID PIDAlt = new PID(5f, 0.1f, 4f, Mathf.Infinity);
    float altCorrectionAccel = 0f;
    public LayerMask groundDetectionLayers;

    [Header("Rotation")]
    public Vector3 targetEuler;
    public Vector3 trueTargetEuler; //target + bonuses
    Vector3 prevTargetEuler;
    public PID3dAngular PIDRot = new PID3dAngular(1f, 0.2f, 0.4f);
    Vector3 rotCorrectionEuler;

    [Header("Stabilisation Modifiers")]
    public float rotCorrectionMagnitudeFactor = 0.1f;
    public float usefulnessBonusForFacingDown = 0.25f;
    [HideInInspector] public Vector3 localVel = Vector3.zero;
    [HideInInspector] public Vector3 torqueBias;
    public float usefulnessBonusForTorqueBias = 3f;
    public PID3d PIDXZVelDamping = new PID3d(0.1f, 0.1f, 0.1f);
    Vector3 XZVelCorrectionValue;
    public float maxRotationBiasForXZVelDamping = 15f;
    //public float forceDampingSpeedThreshold = 10f;
    public float XVelDampingCoefficient = 1;
    public float ZVelDampingCoefficient = 1;
    [HideInInspector] public bool disregardYOrientation = false;
    bool significantTurning = false;

    [Header("Engines")]
    public PartEngine[] engines;
    List<PartSwivel> engineSwivels = new List<PartSwivel>();
    List<int> swivelingEngineIndices = new List<int>();
    List<PartAntiVelocityPID> swivelEnginePIDs = new List<PartAntiVelocityPID>();
    [SerializeField] Vector3[] engineTorques;
    float[] engineFactors;
    float totalMaxThrust;
    float totalThrustTarget = 30000;
    float highestTorque;

    [Header("Toggles")]
    public bool forceDisableAllEngines = false;
    public bool forceDisableAllEnginesThisFrame = false; //resets every frame
    public bool balance = true;
    public bool swivelCounteractVelocity = true;
    public float swivelCoefficient = 3f;

    int fixedCounter = 0;

    void UpdateEngines()
    {
        //setup
        int prevEnginesCount = engines == null ? 0 : engines.Length;
        engines = body.GetComponentsInChildren<PartEngine>();
        engineTorques = new Vector3[engines.Length];
        engineFactors = new float[engines.Length];
        totalMaxThrust = 0;
        highestTorque = 0;

        //iterate through engines and calculate info
        engineSwivels.Clear();
        swivelingEngineIndices.Clear();
        swivelEnginePIDs.Clear();
        for (int i = 0; i < engines.Length; i++)
        {
            foreach(PartComponent c in engines[i].part.components)
            {
                if(c is PartSwivel)
                {
                    engineSwivels.Add((PartSwivel)c);
                    swivelingEngineIndices.Add(i);
                }
                if(c is PartAntiVelocityPID)
                {
                    swivelEnginePIDs.Add((PartAntiVelocityPID)c);
                }
            }
            engineTorques[i] = Vector3.Cross((body.transform.InverseTransformPoint(engines[i].exhaustPoint.position) - body.rb.centerOfMass), (body.transform.InverseTransformDirection(-engines[i].exhaustPoint.forward) * engines[i].maxThrust));
            totalMaxThrust += engines[i].maxThrust;
            if(engineTorques[i].sqrMagnitude > highestTorque)
            {
                highestTorque = engineTorques[i].sqrMagnitude;
            }
        }
        highestTorque = Mathf.Sqrt(highestTorque);

        if (prevEnginesCount != engines.Length) { PIDRot.Reset(); PIDAlt.Reset(); PIDXZVelDamping.Reset(); }
    }
	
	void FixedUpdate () {
	    if(balance)
        {
            UpdateErrorValues();
            if(fixedCounter % 10 == 0) UpdateEngineSwivels();
            UpdateEngineThrottles();
            //Debug.DrawRay(body.transform.position, Quaternion.Euler(targetEuler) * Vector3.forward * 10f, Color.green);

            if (fixedCounter % 10 == 0)
            {
                if (prevTargetAltitude != targetAltitude)
                {
                    PIDAlt.Reset();
                    prevTargetAltitude = targetAltitude;
                }
                if (prevTargetEuler != targetEuler)
                {
                    PIDRot.Reset();
                    prevTargetEuler = targetEuler;
                }
            }
            if(fixedCounter >= 50) //every 50th fixedUpdate, re-calculate the engines on this craft.
            {
                fixedCounter = 0;
                PIDXZVelDamping.Reset();
                foreach(PartAntiVelocityPID pid in swivelEnginePIDs)
                {
                    pid.velPID.Reset();
                }
                UpdateEngines();
            }
            fixedCounter++;
        }
	}

    public void UpdateErrorValues()
    {
        //altitude
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(body.transform.position, Physics.gravity, out hit, 50000f, groundDetectionLayers))
        {
            altitude = hit.distance;
        }
        else
        {
            altitude = body.transform.position.y;
        }
        altCorrectionAccel = PIDAlt.Update(targetAltitude, altitude, Time.fixedDeltaTime);

        //calculating vars
        localVel = body.transform.InverseTransformDirection(body.rb.velocity);
        significantTurning = Mathf.Abs(body.rb.angularVelocity.y) > 0.5f || Mathf.Abs(torqueBias.y) > 10f;

        //damping
        //if (localVel.x > forceDampingSpeedThreshold) XVelDampingCoefficient = 1f;
        //if (localVel.z > forceDampingSpeedThreshold) ZVelDampingCoefficient = 1f;
        Vector3 eulerDampBonus = Vector3.zero;
        XZVelCorrectionValue = PIDXZVelDamping.Update(Vector3.zero, Vector3.Scale(localVel, new Vector3(XVelDampingCoefficient, 1, ZVelDampingCoefficient)), Time.fixedDeltaTime);
        if (disregardYOrientation && significantTurning) targetEuler.y = Vector3.SignedAngle(Vector3.forward, new Vector3(body.transform.forward.x, 0, body.transform.forward.z), Vector3.up); //set target Y rot to current Y rot.
        if (Mathf.Abs(altitude - targetAltitude) < 1f && body.rb.velocity.y < 0.25f) //rotate to damp xz vel if we are relatively stable in altitude and y-vel.
        {
            //Debug.DrawRay(body.transform.position, Vector3.up*10f, Color.yellow, Time.fixedDeltaTime);
            eulerDampBonus.z = -Mathf.Clamp(XZVelCorrectionValue.x, Mathf.Min(-maxRotationBiasForXZVelDamping + targetEuler.z, 0), Mathf.Max(maxRotationBiasForXZVelDamping + targetEuler.z, 0));
            eulerDampBonus.x = Mathf.Clamp(XZVelCorrectionValue.z, Mathf.Min(-maxRotationBiasForXZVelDamping - targetEuler.x, 0), Mathf.Max(maxRotationBiasForXZVelDamping - targetEuler.x, 0));
        }

        trueTargetEuler = targetEuler + eulerDampBonus;
        Vector3 eulerRelativeToTarget = (body.transform.rotation * Quaternion.Inverse(Quaternion.Euler(trueTargetEuler))).eulerAngles;
        rotCorrectionEuler = PIDRot.Update(Vector3.zero, MiscUtils.Rotation3dFormatTo180Signed(eulerRelativeToTarget), Time.fixedDeltaTime);
    }

    public void UpdateEngineSwivels()
    {
        int a = 0;
        foreach (PartSwivel s in engineSwivels)
        {
            if (s == null) continue;
            int i = swivelingEngineIndices[a];
            if (engines[i] == null || engines[i].exhaustPoint == null) continue;
            Vector3 PointVelCorrection = body.transform.InverseTransformDirection( //initially have it local because we need it for some calculations.
                swivelEnginePIDs[a].velPID.Update(Vector3.zero, body.rb.GetPointVelocity(engines[i].exhaustPoint.position), Time.fixedDeltaTime) //Get correction value this engine wants for itself, considering its point velocity.
                );
            Vector3 VelCorrectionScaleVector = new Vector3(XVelDampingCoefficient, 1, ZVelDampingCoefficient);
            if(torqueBias.y != 0)
            {
                PointVelCorrection.x *= -Mathf.Sign(torqueBias.y);
            }

            PointVelCorrection = swivelCounteractVelocity ? body.transform.TransformDirection(Vector3.Scale(PointVelCorrection*swivelCoefficient, VelCorrectionScaleVector)) + body.transform.up * 10f : body.transform.up;
            s.SetSwivelToWorldDirection(-PointVelCorrection); //swivel to counteract point velocity.
            //Debug.DrawRay(s.Swivel.position, -PointVelCorrection, Color.blue);
            engineTorques[i] = Vector3.Cross((body.transform.InverseTransformPoint(engines[i].exhaustPoint.position) - body.rb.centerOfMass), (body.transform.InverseTransformDirection(-engines[i].exhaustPoint.forward) * engines[i].maxThrust));
            a++;
        }
    }

    public void UpdateEngineThrottles()
    {
        //calculate desired total thrust to maintain target altitude. If it seems like the player is trying to change orientation, make the target thrust at least half of what is required to counteract gravity to allow for maneuvering the craft.
        totalThrustTarget = Mathf.Max(altCorrectionAccel * body.rb.mass + -Physics.gravity.y * body.rb.mass, Mathf.Abs(targetEuler.x) < maxRotationBiasForXZVelDamping && Mathf.Abs(targetEuler.z) < maxRotationBiasForXZVelDamping && torqueBias == Vector3.zero ? 0 : body.rb.mass*-Physics.gravity.y*0.5f);

        float highestFactor = 0f;
        float lowestFactor = float.MaxValue;
        //for now, factor means max usefulness.
        for(int i = 0; i < engineFactors.Length; i++)
        {
            if (engines[i] == null) continue;
            Vector3 worldTorque = body.transform.TransformDirection(engineTorques[i]);
            float usefulnessAsAngle = Vector3.Angle(rotCorrectionEuler, worldTorque); //get the angle between correction torque as a vector and this engine's global torque as a vector
            engineFactors[i] = Mathf.Cos(usefulnessAsAngle * Mathf.Deg2Rad); //get the cos so if they are the same (angle is 0) then factor is 1.
            engineFactors[i] *= Mathf.Min(rotCorrectionEuler.sqrMagnitude*rotCorrectionMagnitudeFactor, 1);
            engineFactors[i] += usefulnessBonusForFacingDown * Mathf.Max(Mathf.Cos(Vector3.Angle(engines[i].exhaustPoint.forward, -body.transform.up) * Mathf.Deg2Rad), 0);
            if(torqueBias != Vector3.zero) engineFactors[i] += usefulnessBonusForTorqueBias*Mathf.Cos(Vector3.Angle(engineTorques[i], torqueBias)*Mathf.Deg2Rad);
            
            //engineFactors[i] = Mathf.Max(engineFactors[i], 0); //if factor<0, make it 0.
            if (engineFactors[i] > highestFactor)
            {
                highestFactor = engineFactors[i];
            }
            if (engineFactors[i] < lowestFactor)
            {
                lowestFactor = engineFactors[i];
            }

            //visualisation to help show how useful each engine is (lines closer together == more useful)
            //Debug.DrawLine(engines[i].transform.position, engines[i].transform.position + rotCorrectionEuler, Color.cyan);
            //Debug.DrawLine(engines[i].transform.position, engines[i].transform.position + body.transform.TransformDirection(engineTorques[i]), Color.yellow);
        }

        /*if(lowestFactor < 0)
        {
            for (int i = 0; i < engineFactors.Length; i++)
            {
                engineFactors[i] -= lowestFactor;
            }
            highestFactor -= lowestFactor;
        }*/

        if (highestFactor == 0) //if no engine is useful, make them all fire at full power anyway. Might as well try no?
        {
            for(int i = 0; i < engineFactors.Length; i++)
            {
                engineFactors[i] = 1;
            }
        }

        float totalFactor = 0f;
        for (int i = 0; i < engineFactors.Length; i++)
        {
            //now engineFactor no longer means usefulness, it is now the actual factor to keep the craft balanced.
            if (highestFactor != lowestFactor) engineFactors[i] = Mathf.Lerp(highestFactor, lowestFactor, (highestFactor - engineFactors[i]) / (highestFactor - lowestFactor)) / highestFactor; //this is in range 0,1
            else engineFactors[i] = 1f;
            totalFactor += Mathf.Max(engineFactors[i], 0);
        }
        float thrustUpwardsFraction = 0f; //how much of the thrust is actually going against gravity?
        for (int i = 0; i < engineFactors.Length; i++)
        {
            if(engines[i] != null)
            thrustUpwardsFraction += Mathf.Max(Mathf.Cos(Vector3.Angle(engines[i].exhaustPoint.forward, Physics.gravity)*Mathf.Deg2Rad)*(engineFactors[i] / totalFactor), 0);
        }
        for (int i = 0; i < engineFactors.Length; i++)
        {
            {
                engines[i].targetThrust = (engineFactors[i] / totalFactor) * totalThrustTarget / (thrustUpwardsFraction == 0 ? 1 : thrustUpwardsFraction);
                if (forceDisableAllEngines || forceDisableAllEnginesThisFrame) engines[i].targetThrust = 0;
            }
        }

        forceDisableAllEnginesThisFrame = false;
    }

    private void LateUpdate()
    {
        /*for(int i = 0; i < engines.Length; i++)
        {
            if (engines[i] != null)
            {
                Debug.DrawLine(engines[i].exhaustPoint.position, engines[i].exhaustPoint.position + engines[i].exhaustPoint.forward * engines[i].currThrust / engines[i].maxThrust, Color.yellow);
            }
        }*/
    }

    public override void OnBodyChanged()
    {
        base.OnBodyChanged();
        UpdateEngines();
    }




    private Vector3 ApplyInertiaTensor(Vector3 v)
    {
        return body.rb.rotation * Div(Quaternion.Inverse(body.rb.rotation) * v, body.rb.inertiaTensor);
    }

    private static Vector3 Div(Vector3 v, Vector3 v2)
    {
        return new Vector3(v.x / v2.x, v.y / v2.y, v.z / v2.z);
    }
}
