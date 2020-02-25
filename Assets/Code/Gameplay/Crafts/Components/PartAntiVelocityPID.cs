using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartAntiVelocityPID : PartComponent {

    public PID3d velPID = new PID3d(5, 0.1f, 0.1f);
}
