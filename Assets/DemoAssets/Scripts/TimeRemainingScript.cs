using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimeRemainingScript : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timeRemainingText;
   
    // Update is called once per frame
    void Update()
    {
        _timeRemainingText.text = Player.singleton.TimeToDeath().ToString("F2");
    }
}
