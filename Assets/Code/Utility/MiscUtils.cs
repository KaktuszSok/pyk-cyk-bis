using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MiscUtils {
    
    //takes any angle and returns an angle in the range -180, 180.
    public static float AngleFormatTo180Signed(float angle360)
    {
        if (angle360 >= -180 && angle360 <= 180)
        {
            return angle360;
        }
        else if (angle360 > 180)
        {
            return (angle360 % 360) > 180 ? (angle360 % 360) - 360 : (angle360 % 360);
        }
        else if(angle360 < -180)
        {
            angle360 *= -1;
            return ((angle360 % 360) > 180 ? (angle360 % 360) - 360 : (angle360 % 360))*-1;
        }
        Debug.LogWarning("Something went wrong when attempting func. AngleFormatTo180Signed. Returning input value.");
        return angle360;
    }

    public static Vector3 Rotation3dFormatTo180Signed(Vector3 rotation3d)
    {
        return new Vector3(AngleFormatTo180Signed(rotation3d.x), AngleFormatTo180Signed(rotation3d.y), AngleFormatTo180Signed(rotation3d.z));
    }

    /// <summary>
    /// Returns false if float f is +/-Infinity or NaN
    /// </summary>
    public static bool IsFloatValid(float f)
    {
        return !float.IsNaN(f) && !float.IsInfinity(f);
    }
    /// <summary>
    /// Returns false if any component of vector f is +/-Infinity or NaN
    /// </summary>
    public static bool IsVectorValid(Vector3 v)
    {
        return IsFloatValid(v.x) && IsFloatValid(v.y) && IsFloatValid(v.z);
    }
}
