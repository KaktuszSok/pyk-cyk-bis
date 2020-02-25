using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Proxy_HighlightPart : MonoBehaviour {

    PhysPart part;

    public Color highlightColour;
    public Color currColour = Color.clear;
    public Color startColour = Color.clear;
    public float progress = 0f;

    public bool started = false;

    private void Awake()
    {
        part = GetComponent<PhysPart>();
    }

    public void HighlightPart(Color colour)
    {
        highlightColour = colour;

        //Remove interfering highlight proxies
        int amountOfInterferingProxies = GetComponents<Proxy_HighlightPart>().Length - 1; //Every highlight proxy on this GO that is not this one.
        for(int i = 0; i < amountOfInterferingProxies; i++)
        {
            if(i == amountOfInterferingProxies - 1) //We're on the newest one
            {
                Proxy_HighlightPart proxyToInheritFrom = GetComponent<Proxy_HighlightPart>();
                startColour = proxyToInheritFrom.currColour; //Inherit last proxy's colour to use as starting point.
            }

            Destroy(GetComponent<Proxy_HighlightPart>()); //Destroy oldest proxy. This will be repeated until all proxies but this one are destroyed.
        }

        started = true;
    }

    private void Update()
    {
        if (started)
        {
            if (progress <= 1)
            {
                if (progress < 0.05f)
                {
                    currColour = Color.Lerp(startColour, highlightColour, progress / 0.1f);//Colour fades from start @ p=0 to full @ p=0.1
                }
                else
                {
                    currColour = highlightColour*(1 - (progress - 0.1f) / 0.9f); //Colour fades from full @ p=0.1 to none @ p=1
                }

                part.SetEmissiveColour(currColour);

                progress += Time.deltaTime/0.5f; //Full effect takes 0.5s
            }
            else
            {
                part.SetEmissiveColour(Color.clear);
                Destroy(this);
            }
        }
    }
}
