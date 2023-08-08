using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Gem : MonoBehaviour
{
    private bool isCheckingForKey = false;
    [SerializeField]private GameObject _UIElement;

    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _UIElement.SetActive(true);
            isCheckingForKey = true;
            StartCoroutine(CheckForKey());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _UIElement.SetActive(false);
            isCheckingForKey = false;
            StopCoroutine(CheckForKey());
            
        }
    }

    private IEnumerator CheckForKey()
    {
        while (isCheckingForKey)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                Player.singleton.AddScore(1);
                Destroy(gameObject);
                isCheckingForKey = false;
            }
            yield return null;
        }
    }
}
