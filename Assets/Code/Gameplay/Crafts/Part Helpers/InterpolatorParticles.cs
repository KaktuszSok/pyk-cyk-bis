using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterpolatorParticles : MonoBehaviour {

    ParticleSystem particles;
    ParticleSystem.EmissionModule particlesEmission;
    public float emitMin = 0f;
    public float emitMax = 100f;
    public float emitCurr = 0f;
    public float emitTarget = 0f;

    void Awake () {
        particles = GetComponent<ParticleSystem>();
        particlesEmission = particles.emission;
    }
	
	void FixedUpdate () {
        if (particles && emitCurr != emitTarget)
        {
            emitCurr = Mathf.MoveTowards(emitCurr, emitTarget, (emitMax - emitMin) * 2.5f * Time.fixedDeltaTime);
            emitCurr = Mathf.Clamp(emitCurr, emitMin, emitMax);
            particlesEmission.rateOverTimeMultiplier = emitCurr;
        }
    }

    public void SetTarget(float fraction)
    {
        emitTarget = Mathf.Lerp(emitMin, emitMax, fraction);
    }
}
