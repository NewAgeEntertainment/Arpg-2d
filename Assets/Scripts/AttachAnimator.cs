using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AttachAnimator : MonoBehaviour
{
    public PlayableDirector playableDirector;
    public int trackIndex;
    Animator animator;

    void Awake()
    {
        GameObject model = GameObject.FindGameObjectWithTag("Player");
        int index = 0;
        if (model != null)
        {
            animator = model.GetComponent<Animator>();
        }
        TimelineAsset timelineAsset = playableDirector.playableAsset as TimelineAsset;

        IEnumerable<TrackAsset> trackList = timelineAsset.GetOutputTracks();

        foreach (TrackAsset trackAsset in trackList)
        {
            // check to see if this is the one you are looking for (by name, index etc)
            if (index == trackIndex)
            {
                // bind the track to our new actor instance
                playableDirector.SetGenericBinding(trackAsset, animator);
                break;
            }

            index += 1;
        }
    }
}

