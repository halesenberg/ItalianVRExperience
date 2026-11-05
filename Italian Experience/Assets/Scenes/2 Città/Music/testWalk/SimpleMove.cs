using UnityEngine;
using UnityEngine.InputSystem;

// Script TEMPORANEO di movimento "stile auto" per navigare la scena con le freccette.
// Versione compatibile con il nuovo Input System (package com.unity.inputsystem).
// Attaccalo a un cubo (o qualsiasi GameObject) e premi Play.
//
// Freccia Su/Giù       -> avanti/indietro (nella direzione in cui il cubo guarda)
// Freccia Sinistra/Destra -> ruota a sinistra/destra
// Q / E                -> giù / su (altezza, utile per vedere i piani alti)
// Shift                -> sprint

public class SimpleMove : MonoBehaviour
{
    [Tooltip("Velocità di movimento avanti/indietro (unità al secondo)")]
    public float speed = 10f;

    [Tooltip("Velocità di rotazione (gradi al secondo)")]
    public float rotationSpeed = 90f;

    [Tooltip("Tieni premuto Shift per andare più veloce")]
    public float sprintMultiplier = 3f;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return; // nessuna tastiera rilevata

        float currentSpeed = speed;
        if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
        {
            currentSpeed *= sprintMultiplier;
        }

        // Freccia sinistra/destra -> rotazione sul posto (come lo sterzo di un'auto)
        float rotationInput = 0f;
        if (keyboard.rightArrowKey.isPressed) rotationInput += 1f;
        if (keyboard.leftArrowKey.isPressed) rotationInput -= 1f;

        if (rotationInput != 0f)
        {
            transform.Rotate(Vector3.up, rotationInput * rotationSpeed * Time.deltaTime);
        }

        // Freccia su/giù -> avanti/indietro, sempre nella direzione in cui il cubo guarda ora
        float moveInput = 0f;
        if (keyboard.upArrowKey.isPressed) moveInput += 1f;
        if (keyboard.downArrowKey.isPressed) moveInput -= 1f;

        Vector3 move = transform.forward * moveInput;

        // Q/E per scendere/salire (movimento verticale assoluto, non legato alla rotazione)
        if (keyboard.eKey.isPressed) move += Vector3.up;
        if (keyboard.qKey.isPressed) move += Vector3.down;

        if (move != Vector3.zero)
        {
            transform.position += move.normalized * currentSpeed * Time.deltaTime;
        }
    }
}