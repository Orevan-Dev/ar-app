using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class OrevanAR3DMarker : DefaultTrackableEventHandler
{
    public ParticleSystem appearanceVFX;
    public GameObject Woman;
    public GameObject ReplayBtn;


    private bool isTracked = false;
    private bool isTalking = false;

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void OnTrackingFound()
    {
        PointCameraGuide.instance.OnMarkerFound();
        isTracked = true;

        StartCoroutine("PlayTheShow");

    }

    IEnumerator PlayTheShow()
    {
        appearanceVFX.transform.position = Woman.transform.position;
        //appearanceVFX.transform.rotation = Woman.transform.rotation;

        appearanceVFX.GetComponent<Renderer>().enabled = true;

        appearanceVFX.gameObject.GetComponent<AudioSource>().Play();
        appearanceVFX.Play();
        yield return new WaitForSeconds(3f);

        foreach (Renderer r in Woman.GetComponentsInChildren<Renderer>())
        {
            r.enabled = true;
        }
        yield return new WaitForSeconds(1f);
        Woman.GetComponent<Animator>().SetTrigger("talk");
        isTalking = true;
        Woman.GetComponent<AudioSource>().Play();
        yield return new WaitForSeconds(Woman.GetComponent<AudioSource>().clip.length);
        Woman.GetComponent<Animator>().SetTrigger("idle");
        isTalking = false;
        ReplayBtn.SetActive(true);
    }

    protected override void OnTrackingLost()
    {
        base.OnTrackingLost();
        StopAllCoroutines();
        PointCameraGuide.instance.OnMarkerLost();
        

        foreach (Renderer r in Woman.GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }
        if(isTalking)
        {
            Woman.GetComponent<Animator>().SetTrigger("idle");
            isTalking = false;
        }
            
        ReplayBtn.SetActive(false);

        if (appearanceVFX.isPlaying)
            appearanceVFX.Stop();

        isTracked = false;
    }

    public void OnReplay()
    {
        StartCoroutine("PlayTheShow");
        ReplayBtn.SetActive(false);
    }


}
