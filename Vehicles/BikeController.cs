// BikeController.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BikeController : MonoBehaviour
{
    [Header("Permissions")]
    public bool isControlled = false;

    [Header("Movement Settings")]
    public float acceleration = 600f;
    public float maxSpeed = 30f;
    public float turnSpeed = 40f;
    public float brakeForce = 1200f;
    
    [Header("Lean Settings")]
    public float maxLeanAngle = 30f;
    public float leanSpeed = 5f;
    public float autoLeanForce = 0.8f;
    
    [Header("Stability Settings")]
    public float groundCheckDistance = 1.2f;
    public float groundStabilizationForce = 3000f;
    public float uprightTorque = 800f;
    public float wheelGrip = 200f;

    private Rigidbody rb;
    private float currentLean = 0f;
    private bool isGrounded = false;
    private bool isBraking = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.2f, 0); // Abaisse légèrement le centre de masse
    }

    private void Update()
    {
        if (!isControlled)
            return;
            
        // Récupération des entrées
        isBraking = Input.GetKey(KeyCode.Space);
        
        // Mise à jour du HUD si présent
        if (VehicleHUD.Instance != null)
        {
            float speedKmh = rb.velocity.magnitude * 3.6f;
            VehicleHUD.Instance.UpdateSpeed(speedKmh);
            VehicleHUD.Instance.UpdateStats("Mode: Normal");
            VehicleHUD.Instance.UpdateFuel(80f);
        }
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        
        if (!isControlled)
            return;
            
        HandleDriving();
        HandleLeaning();
        ApplyStabilization();
    }
    
    private void CheckGrounded()
    {
        // Vérifie si la moto est au sol
        Ray ray = new Ray(transform.position, -transform.up);
        isGrounded = Physics.Raycast(ray, groundCheckDistance);
    }

    private void HandleDriving()
    {
        // Récupération des axes d'entrée
        float forwardInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");
        
        // Accélération/Décélération
        if (isGrounded)
        {
            if (Mathf.Abs(forwardInput) > 0.1f && rb.velocity.magnitude < maxSpeed)
            {
                Vector3 force = transform.forward * forwardInput * acceleration * Time.fixedDeltaTime;
                rb.AddForce(force, ForceMode.Acceleration);
            }
            
            // Freinage
            if (isBraking && rb.velocity.magnitude > 0.5f)
            {
                rb.AddForce(-rb.velocity.normalized * brakeForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            }
            
            // Direction - la moto tourne plus fort à basse vitesse
            if (rb.velocity.magnitude > 0.5f)
            {
                float speedFactor = Mathf.Clamp01(rb.velocity.magnitude / 10f);
                float turnFactor = turnInput * turnSpeed * (1.5f - speedFactor) * Time.fixedDeltaTime;
                rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnFactor, 0f));
            }
        }
        
        // Décélération naturelle
        if (Mathf.Approximately(forwardInput, 0f) && !isBraking)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * 0.5f);
        }
    }
    
    private void HandleLeaning()
    {
        if (!isGrounded)
            return;
            
        float turnInput = Input.GetAxis("Horizontal");
        float targetLean = 0f;
        
        // L'inclinaison dépend de la vitesse et de la direction
        if (rb.velocity.magnitude > 1f)
        {
            // Inclinaison manuelle via les contrôles
            targetLean = -turnInput * maxLeanAngle;
            
            // Inclinaison automatique basée sur la force centrifuge
            float autoLean = Vector3.Dot(rb.velocity.normalized, transform.right) * autoLeanForce;
            targetLean += -autoLean * maxLeanAngle;
        }
        
        // Transition douce vers l'angle d'inclinaison cible
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.fixedDeltaTime * leanSpeed);
        
        // Application de la rotation d'inclinaison
        Quaternion targetRotation = rb.rotation * Quaternion.Euler(0f, 0f, currentLean);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * leanSpeed));
    }
    
    private void ApplyStabilization()
    {
        if (isGrounded)
        {
            // Force qui maintient la moto droite quand elle ne penche pas activement
            Vector3 uprightDirection = Vector3.up;
            Vector3 bikeUp = transform.up;
            
            // Application d'un couple pour redresser la moto
            Vector3 torqueDirection = Vector3.Cross(bikeUp, uprightDirection);
            rb.AddTorque(torqueDirection * uprightTorque * Time.fixedDeltaTime);
            
            // Stabilisation au sol - adhérence des roues
            Vector3 lateralVelocity = Vector3.Dot(rb.velocity, transform.right) * transform.right;
            rb.AddForce(-lateralVelocity * wheelGrip * Time.fixedDeltaTime, ForceMode.Acceleration);
        }
        else
        {
            // En l'air, on ajoute une légère gravité
            rb.AddForce(Physics.gravity, ForceMode.Acceleration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isControlled = true;
            other.transform.SetParent(transform, worldPositionStays: true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isControlled = false;
            other.transform.SetParent(null, worldPositionStays: true);
        }
    }
}