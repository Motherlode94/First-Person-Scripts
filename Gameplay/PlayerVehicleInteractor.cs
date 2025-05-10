using UnityEngine;

public class PlayerVehicleInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode enterExitKey = KeyCode.E;
    [SerializeField] private Transform exitRayOrigin;
    [SerializeField] private LayerMask vehicleLayer;
    
    private IVehicle currentVehicle;
    public bool isInVehicle { get; private set; } = false;
    
    private void Update()
    {
        if (Input.GetKeyDown(enterExitKey))
        {
            if (isInVehicle)
                ExitVehicle();
            else
                TryEnterVehicle();
        }
    }
    
    private void TryEnterVehicle()
    {
        // Cast a ray to find a vehicle
        Ray ray = new Ray(exitRayOrigin != null ? exitRayOrigin.position : transform.position, transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionDistance, vehicleLayer))
        {
            // Try to get the vehicle component
            IVehicle vehicle = hit.collider.GetComponentInParent<IVehicle>();
            
            if (vehicle != null && vehicle.IsOperational)
            {
                EnterVehicle(vehicle);
            }
        }
    }
    
    private void EnterVehicle(IVehicle vehicle)
    {
        if (vehicle == null) return;
        
        currentVehicle = vehicle;
        isInVehicle = true;
        
        // Disable player movement and other components as needed
        var characterController = GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = false;
            
        // Notify the vehicle that the player has entered
        vehicle.OnPlayerEnter(gameObject);
    }
    
    public void ExitVehicle()
    {
        if (currentVehicle == null) return;
        
        // Find a safe exit position
        Transform exitPoint = currentVehicle.ExitPoint;
        Vector3 exitPosition = exitPoint != null ? exitPoint.position : transform.position;
        
        // Check if exit position is clear
        Collider[] colliders = Physics.OverlapSphere(exitPosition, 0.5f);
        bool isBlocked = false;
        
        foreach (var collider in colliders)
        {
            if (collider.gameObject != gameObject && collider.gameObject != currentVehicle as MonoBehaviour)
            {
                isBlocked = true;
                break;
            }
        }
        
        if (isBlocked)
        {
            Debug.Log("Exit blocked, cannot exit vehicle");
            return;
        }
        
        // Move player to exit position
        transform.position = exitPosition;
        
        // Notify the vehicle that the player has exited
        currentVehicle.OnPlayerExit();
        
        // Re-enable player movement and other components
        var characterController = GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = true;
            
        currentVehicle = null;
        isInVehicle = false;
    }
    
    // This method is called by the vehicle when it is destroyed or by other systems
    public void ForceExitVehicle()
    {
        if (currentVehicle == null) return;
        
        // Move player to the safest position possible
        Transform exitPoint = currentVehicle.ExitPoint;
        Vector3 exitPosition = exitPoint != null ? exitPoint.position : transform.position + Vector3.up;
        
        // Notify vehicle
        currentVehicle.OnPlayerExit();
        
        // Re-enable player components
        var characterController = GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = true;
            
        // Set player position
        transform.position = exitPosition;
        
        currentVehicle = null;
        isInVehicle = false;
    }
}