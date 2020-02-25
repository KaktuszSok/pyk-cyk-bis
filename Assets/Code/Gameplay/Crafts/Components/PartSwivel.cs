using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartSwivel : PartComponent {

    public Transform Swivel;
    public Quaternion swivelDefault;
    public Vector3 swivelExtents = new Vector3(10f, 0f, 10f); //Pitch, Yaw, Roll
    public Quaternion swivelCurr = Quaternion.identity;
    public Quaternion swivelTarget = Quaternion.identity;
    public float swivelSpeed = 45f;

	protected override void Awake () {
        base.Awake();
        swivelDefault = Swivel.localRotation;

        if (GetComponent<PartEngine>())
        {
            gameObject.AddComponent<PartAntiVelocityPID>(); //All engines with swivels need a local PID for the autobalancer to use.
            part.UpdateComponentsList();
        }
    }

    /// <summary>
    /// Set swivel to some local angle, relative to default angle.
    /// </summary>
    /// <param name="xyz">Local pitch, yaw and roll in degrees</param>
    public void SetSwivel(Vector3 xyz) //X - pitch, Y - yaw, Z - roll
    {
        xyz = MiscUtils.Rotation3dFormatTo180Signed(xyz);
        xyz.x = Mathf.Clamp(xyz.x, -swivelExtents.x, swivelExtents.x);
        xyz.y = Mathf.Clamp(xyz.y, -swivelExtents.y, swivelExtents.y);
        xyz.z = Mathf.Clamp(xyz.z, -swivelExtents.z, swivelExtents.z);

        swivelTarget = Quaternion.Euler(0, xyz.y, 0) * Quaternion.Euler(xyz.x, 0, 0) * Quaternion.Euler(0, 0, xyz.z);
        swivelCurr = Quaternion.RotateTowards(swivelCurr, swivelTarget, swivelSpeed * Time.fixedDeltaTime);

        Swivel.localRotation = swivelDefault*swivelCurr;

        //Debug.DrawRay(transform.position, Swivel.forward*5f);
    }

    public void SetSwivelToWorldDirection(Vector3 dir)
    {
        Vector3 pitchYawRoll = Vector3.zero;
        pitchYawRoll = (Quaternion.Inverse(transform.rotation*swivelDefault)*Quaternion.LookRotation(dir, transform.up)).eulerAngles;
        SetSwivel(pitchYawRoll);
        //Debug.DrawRay(transform.position, dir.normalized * 5f, Color.gray);
    }
}
