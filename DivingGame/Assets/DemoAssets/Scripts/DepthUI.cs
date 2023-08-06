using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DepthUI : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI depthText;
    [SerializeField] public Transform depthTransform;
    
    // Update is called once per frame
    void Update()
    {
        int depth = -(int) depthTransform.position.y;
        depthText.text = "Depth: " + depth + "m";
    }
}
