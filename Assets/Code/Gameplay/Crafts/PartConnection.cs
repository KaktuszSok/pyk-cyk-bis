using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PartConnection {

    public PhysPart toPart;
    public PhysPart fromPart;

    public PartConnection(PhysPart to, PhysPart from)
    {
        toPart = to;
        fromPart = from;
    }

    public float GetTrueConnectionStrength()
    {
        return Mathf.Min(toPart.GetEffectiveStrength(toPart.connectionStrength), fromPart.GetEffectiveStrength(fromPart.connectionStrength));
    }

    public static PhysPart[] GetConnectedParts(PhysPart startPart, bool drawDebugLines = false)
    {
        List<PhysPart> visitedParts = new List<PhysPart>();
        List<PhysPart> partsToVisit = new List<PhysPart>();
        partsToVisit.Add(startPart);
        PhysPart currPart = partsToVisit[0];

        while (partsToVisit.Count > 0)
        {
            Vector3 prevPartPos = currPart.transform.position;
            currPart = partsToVisit[0];
            if (drawDebugLines) Debug.DrawLine(prevPartPos, currPart.transform.position, Color.cyan, 1.5f);
            visitedParts.Add(currPart);
            partsToVisit.Remove(currPart);
            foreach (PartConnection c in currPart.connections)
            {
                if (!visitedParts.Contains(c.toPart) && !partsToVisit.Contains(c.toPart))
                {
                    partsToVisit.Add(c.toPart);
                }
            }
        }

        return visitedParts.ToArray();
    }

    public static bool IsConnectedToPart(PhysPart startPart, PhysPart targetPart, bool drawDebugLines = false)
    {
        if (startPart == null || targetPart == null) return false;

        List<PhysPart> visitedParts = new List<PhysPart>();
        List<PhysPart> partsToVisit = new List<PhysPart>();
        partsToVisit.Add(startPart);
        PhysPart currPart = partsToVisit[0];
        bool foundConnection = false;
        while (partsToVisit.Count > 0)
        {
            if(partsToVisit[0] == null)
            {
                visitedParts.Add(partsToVisit[0]);
                partsToVisit.RemoveAt(0);
                currPart = partsToVisit[0]; //make next part currPart
                continue;
            }
            Vector3 prevPartPos = currPart.transform.position;
            currPart = partsToVisit[0];
            if(drawDebugLines) Debug.DrawLine(prevPartPos, currPart.transform.position, Color.green*0.95f, 1.5f);
            visitedParts.Add(currPart);
            partsToVisit.Remove(currPart);
            if(currPart == targetPart)
            {
                foundConnection = true;
                break;
            }
            else
            {
                foreach(PartConnection c in currPart.connections)
                {
                    if(!visitedParts.Contains(c.toPart) && !partsToVisit.Contains(c.toPart))
                    {
                        partsToVisit.Add(c.toPart);
                    }
                }
            }
        }
        return foundConnection;
    }

    /// <summary>
    /// checks if the part is connected to another with connections which don't go through parts listed in invalidParts.
    /// </summary>
    public static bool IsConnectedToPartIgnoringInvalidParts(PhysPart startPart, PhysPart targetPart, PhysPart[] invalidParts, bool drawDebugLines = false)
    {
        if (startPart == null || targetPart == null) return false;

        List<PhysPart> visitedParts = new List<PhysPart>();
        visitedParts.AddRange(invalidParts);
        List<PhysPart> partsToVisit = new List<PhysPart>();
        partsToVisit.Add(startPart);
        PhysPart currPart = partsToVisit[0];
        bool foundConnection = false;
        while (partsToVisit.Count > 0)
        {
            Vector3 prevPartPos = currPart.transform.position;
            currPart = partsToVisit[0];
            if (drawDebugLines) Debug.DrawLine(prevPartPos, currPart.transform.position, Color.green, 1.5f);
            visitedParts.Add(currPart);
            partsToVisit.Remove(currPart);
            if (currPart == targetPart)
            {
                foundConnection = true;
                break;
            }
            else
            {
                foreach (PartConnection c in currPart.connections)
                {
                    if (!visitedParts.Contains(c.toPart))
                    {
                        partsToVisit.Add(c.toPart);
                    }
                }
            }
        }
        return foundConnection;
    }
}
