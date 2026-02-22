using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trick2SceneBootstrap : MonoBehaviour
{
    public PlayerClassTag scenePlayerClassTag;

    private void Awake()
    {
        var selected = PlayerClassSelection.Load();

        if (scenePlayerClassTag == null)
            scenePlayerClassTag = FindObjectOfType<PlayerClassTag>();

        if (scenePlayerClassTag == null)
        {
            Debug.LogError("[Trick2SceneBootstrap] PlayerClassTag not found!");
            return;
        }

        scenePlayerClassTag.classType = selected;
        Debug.Log($"[Trick2SceneBootstrap] Applied PlayerClassTag.classType = {selected}");
    }
}
