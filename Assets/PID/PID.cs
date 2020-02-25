using UnityEngine;

[System.Serializable]
public class PID {
	public float pFactor, iFactor, dFactor;
    public float iLimit = 0f;
		
	protected float integral;
	protected float lastError;
	

    public PID()
    {
        pFactor = 0f;
        iFactor = 0f;
        dFactor = 0f;
        iLimit = 0f;
    }
	
	public PID(float pFactor, float iFactor, float dFactor, float iLimit = 0f) {
		this.pFactor = pFactor;
		this.iFactor = iFactor;
		this.dFactor = dFactor;
        this.iLimit = iLimit;
	}
	
	
	public virtual float Update(float setpoint, float actual, float timeFrame) {
		float present = setpoint - actual;
		integral += present * timeFrame;
        if(iLimit != 0) integral = Mathf.Clamp(integral, -iLimit, iLimit);
		float deriv = (present - lastError) / timeFrame;
		lastError = present;
		return present * pFactor + integral * iFactor + deriv * dFactor;
	}

    public void Reset()
    {
        integral = lastError = 0f;
    }
}
[System.Serializable]
public class PIDAngular : PID
{

    public PIDAngular(float pFactor, float iFactor, float dFactor, float iLimit = 180f)
    {
        this.pFactor = pFactor;
        this.iFactor = iFactor;
        this.dFactor = dFactor;
        this.iLimit = iLimit;
    }

    public override float Update(float setpoint, float actual, float timeFrame)
    {
        float present = setpoint - actual;
        if(present > 180) present -= 360;
        else if (present < -180) present += 360;
        integral += present * timeFrame;
        integral = Mathf.Clamp(integral, -iLimit, iLimit);
        float deriv = (present - lastError) / timeFrame;
        lastError = present;
        return present * pFactor + integral * iFactor + deriv * dFactor;
    }
}
  //------------------//
 //--------3D--------//
//------------------//
[System.Serializable]
public class PID3d
{
    public float pFactor = 0.1f;
    public float iFactor = 0.1f;
    public float dFactor = 0f;
    public float iLimit = 0f;

    protected Vector3 integral;
    protected Vector3 lastError;

    public PID3d()
    {
        pFactor = 0f;
        iFactor = 0f;
        dFactor = 0f;
        iLimit = 0f;
    }

    public PID3d(float pFactor, float iFactor, float dFactor, float iLimit = 0f)
    {
        this.pFactor = pFactor;
        this.iFactor = iFactor;
        this.dFactor = dFactor;
        this.iLimit = iLimit;
    }


    public virtual Vector3 Update(Vector3 setpoint, Vector3 actual, float timeFrame)
    {
        Vector3 present = setpoint - actual;
        integral += present * timeFrame;
        if(iLimit != 0) integral = Vector3.ClampMagnitude(integral, iLimit);
        Vector3 deriv = (present - lastError) / timeFrame;
        lastError = present;
        return present * pFactor + integral * iFactor + deriv * dFactor;
    }

    public void Reset()
    {
        integral = lastError = Vector3.zero;
    }
}
[System.Serializable]
public class PID3dAngular : PID3d
{
    public PID3dAngular(float pFactor, float iFactor, float dFactor, float iLimit = 180f)
    {
        this.pFactor = pFactor;
        this.iFactor = iFactor;
        this.dFactor = dFactor;
        this.iLimit = iLimit;
    }

    public override Vector3 Update(Vector3 setpoint, Vector3 actual, float timeFrame)
    {
        Vector3 present = setpoint - actual;
        if (present.x > 180) present.x -= 360;
        else if (present.x < -180) present.x += 360;
        if (present.y > 180) present.y -= 360;
        else if (present.y < -180) present.y += 360;
        if (present.z > 180) present.z -= 360;
        else if (present.z < -180) present.z += 360;
        integral += present * timeFrame;
        if (iLimit != 0) integral = Vector3.ClampMagnitude(integral, iLimit);
        Vector3 deriv = (present - lastError) / timeFrame;
        lastError = present;
        return present * pFactor + integral * iFactor + deriv * dFactor;
    }
}
