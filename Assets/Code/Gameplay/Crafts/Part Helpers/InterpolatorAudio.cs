using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterpolatorAudio : MonoBehaviour {

    AudioSource sound;
    [Header("Pitch")]
    public float pitchMin = 0.25f;
    public float pitchMax = 1f;
    public float pitchCurr = 0f;
    public float pitchTarget = 0f;
    [Header("Volume")]
    public float volumeMax = 1f;
    public float volumeCurr = 0f;
    public float volumeTarget = 0f;

    void Awake() {
        sound = GetComponent<AudioSource>();
    }
	
	void FixedUpdate () {
        if (sound && (pitchCurr != pitchTarget || volumeCurr != volumeTarget))
        {
            pitchCurr = Mathf.MoveTowards(pitchCurr, pitchTarget, (pitchMax - pitchMin) * 2.5f * Time.fixedDeltaTime);
            volumeCurr = Mathf.MoveTowards(volumeCurr, volumeTarget, volumeMax * 2.5f * Time.fixedDeltaTime);

            sound.pitch = pitchCurr;
            sound.volume = volumeCurr;
        }
    }

    public void SetTarget(float fraction)
    {
        pitchTarget = Mathf.Lerp(pitchMin, pitchMax, fraction);
        volumeTarget = volumeMax * fraction;
    }
}
