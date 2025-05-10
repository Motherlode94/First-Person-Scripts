// File: SimpleCarController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleCarController : MonoBehaviour
{
    [Header("Mouvements")]
    public float acceleration = 800f;
    public float maxSpeed = 20f;
    public float turnSpeed = 50f;
    public float brakeForce = 1500f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isBraking;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Récupère les axes configurés dans l'Input Manager ("Horizontal" et "Vertical")
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        moveInput = new Vector2(h, v);

        // Freinage sur la touche Espace
        isBraking = Input.GetKey(KeyCode.Space);

        if (isBraking)
        {
            // Applique une force opposée à la vitesse actuelle
            rb.AddForce(-rb.velocity.normalized * brakeForce * Time.fixedDeltaTime, ForceMode.Acceleration);
        }
    }

    private void FixedUpdate()
    {
        float forwardInput = moveInput.y;
        float turnInput = moveInput.x;

        // Accélération si on n'a pas atteint la vitesse max
        if (Mathf.Abs(forwardInput) > 0.1f && rb.velocity.magnitude < maxSpeed)
        {
            Vector3 force = transform.forward * forwardInput * acceleration * Time.fixedDeltaTime;
            rb.AddForce(force, ForceMode.Acceleration);
            Debug.Log("Force appliquée: " + force);
        }

        // Rotation en fonction de l'entrée horizontale et du sens de la marche/arrière
        if (Mathf.Abs(forwardInput) > 0.1f)
        {
            float rotation = turnInput * turnSpeed * Time.fixedDeltaTime * Mathf.Sign(forwardInput);
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotation, 0f));
        }

        // Mise à jour de l'affichage HUD si présent
        float speedKmh = rb.velocity.magnitude * 3.6f;
        if (VehicleHUD.Instance != null)
        {
            VehicleHUD.Instance.UpdateSpeed(speedKmh);
            VehicleHUD.Instance.UpdateStats("Mode: Sport");
            VehicleHUD.Instance.UpdateDamage(15f);
            VehicleHUD.Instance.UpdateFuel(75f);
        }

        // Décélération naturelle quand on relâche l'accélérateur
        if (Mathf.Approximately(forwardInput, 0f))
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * 2f);
        }
    }
}
