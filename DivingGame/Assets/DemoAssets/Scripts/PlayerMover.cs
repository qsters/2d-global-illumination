using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMover : MonoBehaviour
{
    private Transform playerTransform;
    private Rigidbody2D playerRigidbody;
    private DemoActions actions;

    [SerializeField] private float swimSpeed = 10f;
    private void Awake()
    {
        playerTransform = GetComponent<Transform>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        actions = new DemoActions();
        
        actions.Swimming.PointerPosition.performed += RotatePlayer;
    }

    private void Update()
    {
        bool movingForwardHeld = actions.Swimming.SwimForward.ReadValue<float>() > 0.1f;

        if (movingForwardHeld)
        {
            // Apply force to the rigidbody
            playerRigidbody.AddForce(transform.up * (Time.deltaTime * swimSpeed));
        }
    }

    private void RotatePlayer(InputAction.CallbackContext context)
    {
        Vector2 readValue = context.ReadValue<Vector2>();
        Vector2 centeredValue = new Vector2(readValue.x - (Screen.width / 2f), readValue.y - (Screen.height / 2f));

        float angle = Mathf.Atan2(centeredValue.y, centeredValue.x);
        
        playerTransform.rotation = Quaternion.Euler(0f,0f, angle * Mathf.Rad2Deg - 90f);
    }

    private void OnDestroy()
    {
        actions.Swimming.PointerPosition.performed -= RotatePlayer;
    }

    private void OnEnable()
    {
        actions.Enable();
    }

    private void OnDisable()
    {
        actions.Disable();
    }
}
