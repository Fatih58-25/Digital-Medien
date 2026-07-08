using UnityEngine;
using Articy.Unity;
using System.Collections.Generic;
using Articy.Digitial_media_story;

public class Dialog_Player : MonoBehaviour, IArticyFlowPlayerCallbacks
{

    private ArticyFlowPlayer flowPlayer;
    IList<Branch> branches;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        flowPlayer = GetComponent<ArticyFlowPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            flowPlayer.Play();
        }

        if(Input.GetKeyDown(KeyCode.J))
        {
            flowPlayer.Play(branches[0]);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            flowPlayer.Play(branches[1]);
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            flowPlayer.Play(branches[2]);
        }
    }


    public void OnFlowPlayerPaused(IFlowObject aObject)
    {
        var dlgFragment = aObject as DialogueFragment;
        if (dlgFragment != null)

        {
            // it is a DialogueFragment!
            Debug.Log(dlgFragment.Speaker.TechnicalName);
            Debug.Log(dlgFragment.Text.Value);
        }
    }

    public void OnBranchesUpdated(IList<Branch> aBranches)
    {
        branches = aBranches;
        Debug.Log("Anzahl der Zweige: " + aBranches.Count);
        foreach (Branch aBranch in aBranches) {
           
            var dlgFragment = aBranch.Target as DialogueFragment;
            if (dlgFragment != null)

            {
                
                
                Debug.Log("Menü Text " + dlgFragment.MenuText.Value);
            }
        }
    }
}
