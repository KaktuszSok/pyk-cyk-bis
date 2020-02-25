using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollowCam : MonoBehaviour {

    public Transform followTarget;
    Camera cam;
    Vector3 originPos;

    public Vector3 followOffset = new Vector3(0, 5, -10);
    public Vector3 lookOffset = new Vector3(0, 2.5f, 0);
    public float cam_rotspeed = 120f;
    public float rotation = 0f;
    public float zoom = 1f;
    Vector3 smoothdampVel = Vector3.zero;

    public enum LookMode
    {
        FOLLOW,
        FREELOOK,
        ORBIT
    }

    public LookMode lookMode;
    public bool needMMBtoLook = true;
    public float freelook_xrot = 0f;
    public float freelook_yrot = 0f;

    private void Awake()
    {
        originPos = transform.position;
        cam = GetComponent<Camera>();
    }

    void LateUpdate() {
        if (lookMode == LookMode.FREELOOK)
        {
            LookModeFree();
        }
        else if (lookMode == LookMode.ORBIT && followTarget != null)
        {
            LookModeOrbit();
        }
        else if (lookMode == LookMode.FOLLOW && followTarget != null)
        {
            LookModeFollow();
        }
    }

    void LookModeFree()
    {
        if (!needMMBtoLook || Input.GetMouseButton(2))
        {
            freelook_xrot = Mathf.Clamp(freelook_xrot - Input.GetAxisRaw("Mouse Y") * cam_rotspeed, -90, 90);
            freelook_yrot = freelook_yrot + Input.GetAxisRaw("Mouse X") * cam_rotspeed;
        }

        cam.transform.eulerAngles = new Vector3(freelook_xrot, freelook_yrot, 0);
    }
    void LookModeOrbit()
    {
        if (!needMMBtoLook || Input.GetMouseButton(2))
        {
            freelook_xrot = Mathf.Clamp(freelook_xrot - Input.GetAxisRaw("Mouse Y") * cam_rotspeed, -89, 89);
            rotation = rotation + Input.GetAxisRaw("Mouse X") * cam_rotspeed;
        }
        if (Input.mouseScrollDelta.y > 0.1f || Input.mouseScrollDelta.y < 0.1f)
        {
            zoom = Mathf.Clamp(zoom - Input.mouseScrollDelta.y * 0.25f, 0.25f, 5f);
        }

        Vector3 followOffsetEffective = new Vector3(0, 0, followOffset.z);

        cam.transform.position = followTarget.position + Quaternion.Euler(freelook_xrot, rotation, 0) * followTarget.rotation * followOffsetEffective * zoom;
        cam.transform.rotation = Quaternion.LookRotation((followTarget.position) - cam.transform.position);
    }
    void LookModeFollow()
    {
        if (Input.mouseScrollDelta.y > 0.1f || Input.mouseScrollDelta.y < 0.1f) //zoom
        {
            zoom = Mathf.Clamp(zoom - Input.mouseScrollDelta.y * 0.25f, 0.25f, 5f);
        }

        Vector3 targetCamPosition = followTarget.position + Quaternion.Euler(0, rotation, 0) * followTarget.rotation * followOffset * zoom;
        float sqrDist = (targetCamPosition - transform.position).sqrMagnitude;
        cam.transform.position = Vector3.SmoothDamp(cam.transform.position, targetCamPosition, ref smoothdampVel, 0.3f / Mathf.Max(1, sqrDist));
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation((followTarget.position + lookOffset) - cam.transform.position), 5f * Time.deltaTime);
        cam.transform.eulerAngles = new Vector3(cam.transform.eulerAngles.x, cam.transform.eulerAngles.y, 0);
    }

    public void SetFreelookToCurrRot()
    {
        freelook_xrot = -Mathf.Asin(transform.forward.y) * Mathf.Rad2Deg;
        freelook_yrot = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
    }

    public void ReturnToOrigin()
    {
        transform.position = originPos;
    }
}
