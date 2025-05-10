// HoverVehicleController.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverVehicleController : MonoBehaviour
{
    [Header("Permissions")]
    public bool isControlled = false;

    [Header("Hover Settings")]
    public float hoverHeight  = 2f;
    public float hoverForce   = 3000f;
    public float hoverDamping = 450f;

    [Header("Movement Settings")]
    public float acceleration = 2000f;
    public float turnSpeed    = 60f;
    public float maxSpeed     = 25f;

    [Header("Vertical Control")]
    public float ascendForce = 1500f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    private void FixedUpdate()
    {
        // 1) Toujours la suspension
        ApplyHoverPhysics();

        // 2) Puis, si le joueur n'est pas dedans, on sort
        if (!isControlled)
            return;

        // 3) Sinon on traite l'input
        HandleVerticalInput();
        HandleHorizontalInput();
    }

    private void ApplyHoverPhysics()
    {
        Ray ray = new Ray(transform.position, -transform.up);
        if (Physics.Raycast(ray, out RaycastHit hit, hoverHeight))
        {
            float disp   = hoverHeight - hit.distance;
            float spring = disp * hoverForce;
            float damper = -rb.velocity.y * hoverDamping;
            rb.AddForce((spring + damper) * transform.up);
        }
        else
        {
            // (optionnel car useGravity=false, mais on peut ajouter si besoin)
            rb.AddForce(Physics.gravity);
        }
    }

    private void HandleVerticalInput()
    {
        if (Input.GetKey(KeyCode.Space))
            rb.AddForce(transform.up * ascendForce * Time.fixedDeltaTime, ForceMode.Acceleration);
        if (Input.GetKey(KeyCode.LeftControl))
            rb.AddForce(-transform.up * ascendForce * Time.fixedDeltaTime, ForceMode.Acceleration);
    }

    private void HandleHorizontalInput()
    {
        float forward = Input.GetAxis("Vertical");
        float turn    = Input.GetAxis("Horizontal");

        // Avance/arrière
        Vector3 planarVel = Vector3.ProjectOnPlane(rb.velocity, transform.up);
        if (Mathf.Abs(forward) > 0.1f && planarVel.magnitude < maxSpeed)
            rb.AddForce(transform.forward * forward * acceleration * Time.fixedDeltaTime, ForceMode.Acceleration);

        // Rotation
        if (Mathf.Abs(turn) > 0.1f)
        {
            float rot = turn * turnSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rot, 0f));
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
