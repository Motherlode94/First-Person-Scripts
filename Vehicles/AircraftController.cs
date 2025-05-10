// AircraftController.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AircraftController : MonoBehaviour
{
    public enum VehicleType { Plane, Helicopter }
    public VehicleType vehicleType = VehicleType.Plane;

    [Header("Permissions")]
    public bool isControlled = false;

    [Header("Common Settings")]
    public float maxSpeed     = 120f;
    public float throttlePower = 2000f;

    [Header("Plane Settings")]
    public float liftCoefficient = 0.5f;
    public float pitchSpeed      = 40f;
    public float rollSpeed       = 50f;
    public float yawSpeed        = 20f;

    [Header("Helicopter Settings")]
    public float hoverLiftForce = 3000f;
    public float heliPitchSpeed = 30f;
    public float heliRollSpeed  = 30f;
    public float heliYawSpeed   = 40f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // 1) Si c'est un hélico, on applique toujours un peu de portance pour qu'il ne tombe pas
        if (vehicleType == VehicleType.Helicopter)
        {
            rb.AddForce(transform.up * hoverLiftForce * Time.fixedDeltaTime, ForceMode.Acceleration);
        }

        // 2) Si pas contrôlé, on sort
        if (!isControlled)
            return;

        // 3) Contrôles du joueur
        float throttle = Input.GetAxis("Throttle");
        float pitch    = Input.GetAxis("Vertical");
        float roll     = Input.GetAxis("Horizontal");
        float yaw      = Input.GetAxis("Yaw");

        if (vehicleType == VehicleType.Plane)
        {
            // Poussée
            rb.AddForce(transform.forward * throttle * throttlePower * Time.fixedDeltaTime, ForceMode.Acceleration);
            // Portance (fonction de la vitesse)
            float speed = rb.velocity.magnitude;
            rb.AddForce(Vector3.up * speed * liftCoefficient * Time.fixedDeltaTime, ForceMode.Acceleration);
            // Pitch / Yaw / Roll
            Vector3 rotPlane = new Vector3(-pitch * pitchSpeed, yaw * yawSpeed, -roll * rollSpeed);
            rb.MoveRotation(rb.rotation * Quaternion.Euler(rotPlane * Time.fixedDeltaTime * Mathf.Sign(speed)));
        }
        else // Helicopter
        {
            // Hélico : on augmente ou baisse la portance avec la manette
            // (le lift de base a déjà été appliqué plus haut)
            Vector3 lift = transform.up * ((throttle - 1f) * hoverLiftForce) * Time.fixedDeltaTime;
            rb.AddForce(lift, ForceMode.Acceleration);

            // Contrôles de l'assiette
            Vector3 rotHeli = new Vector3(-pitch * heliPitchSpeed, yaw * heliYawSpeed, -roll * heliRollSpeed);
            rb.MoveRotation(rb.rotation * Quaternion.Euler(rotHeli * Time.fixedDeltaTime));
        }

        // 4) Limitation de vitesse
        if (rb.velocity.magnitude > maxSpeed)
            rb.velocity = rb.velocity.normalized * maxSpeed;
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
