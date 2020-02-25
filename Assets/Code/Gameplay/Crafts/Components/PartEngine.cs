using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartEngine : PartComponent {

    public Transform exhaustPoint;
    public float maxThrust = 100000;
    public float targetThrust = 100000;
    public float thrustMaxDelta = 0;
    public float currThrust = 0;
    float prevThrust = 0f;
    public float exhaustPushBodiesRange = 5f;
    public float exhaustPushRadius = 0.65f;
    public float exhaustPushHeatDamage = 10000f; //per second
    InteractablePhysBody exhaustPushTarget = null;
    Vector3 exhaustPushPoint;
    PhysPart exhaustPushPart;
    float exhaustTargetDistanceFactor;

    public bool active = true;

    InterpolatorParticles particlesController;
    InterpolatorAudio audioController;

    [HideInInspector]
    public Rigidbody rb = null;

    int fixedUpdateCounter = 0;

	protected override void Awake() {
        base.Awake();
        particlesController = GetComponentInChildren<InterpolatorParticles>();
        audioController = GetComponentInChildren<InterpolatorAudio>();
    }

    void FixedUpdate () {
        if (thrustMaxDelta != 0) currThrust = Mathf.MoveTowards(currThrust, targetThrust, thrustMaxDelta * Time.fixedDeltaTime); //move currThrust towards targetThrust
        else currThrust = targetThrust; //if thrustMaxDelta == 0, instantly set currThrust to targetThrust.
        currThrust = Mathf.Clamp(currThrust, 0, maxThrust);
        
        //physics
        if (active && rb != null)
        {
            ApplyThrust();
        }


        //visuals
        if (currThrust != prevThrust)
        {
            audioController.SetTarget(currThrust/maxThrust);
            particlesController.SetTarget(currThrust / maxThrust);
        }
        prevThrust = currThrust;

        fixedUpdateCounter = (int)Mathf.Repeat(fixedUpdateCounter + 1, 50);
    }

    public void ApplyThrust()
    {
        Vector3 thrustForce = -exhaustPoint.forward * currThrust;
        if (MiscUtils.IsVectorValid(thrustForce) && MiscUtils.IsVectorValid(exhaustPoint.position) && thrustForce != Vector3.zero)
        {
            //Thrust force on self
            rb.AddForceAtPosition(thrustForce, exhaustPoint.position);

            //Push force against anything hit by exhaust
            if (exhaustPushBodiesRange == 0) return;
            //Every 10th fixedupdate, scan if anything is hit by the exhaust.
            if (fixedUpdateCounter % 10 == 0)
            {
                exhaustPushTarget = null;
                exhaustPushPart = null;
                RaycastHit hit = new RaycastHit();
                if (Physics.SphereCast(exhaustPoint.position, exhaustPushRadius, exhaustPoint.forward, out hit, exhaustPushBodiesRange))
                {
                    //if (hit.transform == body.transform) return; //ignore hitting self
                    exhaustTargetDistanceFactor = Mathf.Max(1 - (hit.distance / exhaustPushBodiesRange), 0); //Scale effects down as distance increases
                    exhaustPushPoint = hit.point;
                                               
                    //If hit any interactable physics body, apply a force pushing away due to the exhaust.
                    if (hit.transform.CompareTag("InteractablePhysBody"))
                    {
                        InteractablePhysBody other = hit.transform.GetComponent<InteractablePhysBody>();
                        if (!other.physicsReady) return; //ignore if body is not set up properly yet
                        exhaustPushTarget = other; //keep track of this object until next scan
                    }
                    //if hit a part, apply heat.
                    if (hit.collider.CompareTag("PhysPart"))
                    {
                        exhaustPushPart = hit.collider.GetComponentInParent<PhysPart>(); //keep track of part until next scan
                    }
                }
            }
            //Apply force and heat to what we hit last scan every fixedupdate
            if(exhaustPushTarget != null)
            {
                Vector3 pushForce = -thrustForce * exhaustTargetDistanceFactor;
                exhaustPushTarget.rb.AddForceAtPosition(pushForce, exhaustPushPoint);

                if(exhaustPushPart)
                {
                    exhaustPushPart.TakeDamage((exhaustPushHeatDamage * exhaustTargetDistanceFactor) * Time.fixedDeltaTime, DamageType.HEAT);
                }
            }
        }
    }

    public override void OnBodyChanged()
    {
        base.OnBodyChanged();
        rb = body.rb;
    }
}
