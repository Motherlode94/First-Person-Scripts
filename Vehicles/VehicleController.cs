// Assets/Scripts/Vehicles/VehicleController.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour, IVehicle
{
    #region IVehicle Implementation
    [Header("Vehicle Identity")]
    [SerializeField] private string vehicleID = "car_01";
    [SerializeField] private string vehicleType = "Car";
    [SerializeField] private string displayName = "Sport Car";
    
    [Header("Vehicle Status")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private bool requiresFuel = true;
    [SerializeField] private float fuelConsumptionRate = 0.1f;
    
    [Header("Exit Configuration")]
    [SerializeField] private Transform playerExitPoint;
    
    private bool isControlled = false;
    private float currentHealth;
    private float currentFuel;
    
    // Implémentation des propriétés de l'interface IVehicle
    public string VehicleID => vehicleID;
    public string VehicleType => vehicleType;
    public string DisplayName => displayName;
    public bool IsControlled 
    { 
        get => isControlled; 
        set 
        { 
            isControlled = value;
            
            // Active/désactive les éléments visuels spécifiques au joueur
            if (driverModel != null)
                driverModel.SetActive(!value);
                
            if (value)
            {
                if (enterSound != null)
                    audioSource.PlayOneShot(enterSound);
                    
                // On informe le gestionnaire de missions si nécessaire
                if (MissionManager.Instance != null)
                    MissionManager.Instance.NotifyObjectives(ObjectiveType.EquipWeapon, vehicleID);
            }
            else
            {
                if (exitSound != null)
                    audioSource.PlayOneShot(exitSound);
            }
        }
    }
    
    public Transform ExitPoint => playerExitPoint != null ? playerExitPoint : transform;
    
    public float CurrentSpeed => rb != null ? rb.velocity.magnitude * 3.6f : 0f; // Conversion en km/h
    
    public float Health => currentHealth;
    
    public float FuelLevel => currentFuel;
    
    public bool IsOperational => currentHealth > 0 && (!requiresFuel || currentFuel > 0);
    
    public void OnPlayerEnter(GameObject player)
    {
        currentPlayer = player;
        IsControlled = true;
        
        // Active la caméra du véhicule si présente
        if (vehicleCamera != null)
            vehicleCamera.enabled = true;
            
        // Active l'interface utilisateur du véhicule
        if (VehicleHUD.Instance != null)
            VehicleHUD.Instance.ShowHUD(this);
            
        // Désactive la caméra du joueur temporairement
        if (player.GetComponentInChildren<Camera>() is Camera playerCamera)
            playerCamera.enabled = false;
    }
    
    public void OnPlayerExit()
    {
        IsControlled = false;
        
        // Désactive la caméra du véhicule
        if (vehicleCamera != null)
            vehicleCamera.enabled = false;
            
        // Masque l'interface utilisateur du véhicule
        if (VehicleHUD.Instance != null)
            VehicleHUD.Instance.HideHUD();
            
        // Réactive la caméra du joueur
        if (currentPlayer != null && currentPlayer.GetComponentInChildren<Camera>() is Camera playerCamera)
            playerCamera.enabled = true;
            
        currentPlayer = null;
    }
    
    public void ToggleLights()
    {
        areLightsOn = !areLightsOn;
        
        // Active/désactive les lumières
        if (headlights != null)
        {
            foreach (var light in headlights)
            {
                if (light != null)
                    light.enabled = areLightsOn;
            }
        }
        
        // Active/désactive les émissions de matériaux
        if (lightMaterials != null)
        {
            foreach (var renderer in lightMaterials)
            {
                if (renderer != null)
                {
                    foreach (var material in renderer.materials)
                    {
                        material.SetFloat("_EmissionIntensity", areLightsOn ? 1f : 0f);
                    }
                }
            }
        }
        
        if (areLightsOn && lightsOnSound != null)
            audioSource.PlayOneShot(lightsOnSound);
        else if (!areLightsOn && lightsOffSound != null)
            audioSource.PlayOneShot(lightsOffSound);
    }
    
    public void UseHorn()
    {
        if (hornSound != null && audioSource != null && Time.time >= nextHornTime)
        {
            audioSource.PlayOneShot(hornSound);
            nextHornTime = Time.time + hornCooldown;
        }
    }
    #endregion
    
    #region Vehicle Specific Implementation
    [Header("Vehicle Configuration")]
    [SerializeField] private float acceleration = 800f;
    [SerializeField] private float reverseAcceleration = 400f;
    [SerializeField] private float maxForwardSpeed = 120f; // km/h
    [SerializeField] private float maxReverseSpeed = 30f;  // km/h
    [SerializeField] private float turnSpeed = 40f;
    [SerializeField] private float brakeForce = 1500f;
    [SerializeField] private float dragCoefficient = 0.05f;
    [SerializeField] private float downforceCoefficient = 1.0f;
    [SerializeField] private Transform centerOfMass;
    [SerializeField] private AnimationCurve speedTurnInfluence;
    
    [Header("Audio")]
    [SerializeField] private AudioClip engineStartSound;
    [SerializeField] private AudioClip engineLoopSound;
    [SerializeField] private AudioClip hornSound;
    [SerializeField] private AudioClip collisionSound;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip exitSound;
    [SerializeField] private AudioClip lightsOnSound;
    [SerializeField] private AudioClip lightsOffSound;
    
    [Header("Visual Elements")]
    [SerializeField] private GameObject driverModel;
    [SerializeField] private Camera vehicleCamera;
    [SerializeField] private Light[] headlights;
    [SerializeField] private Renderer[] lightMaterials;
    [SerializeField] private ParticleSystem[] exhaustParticles;
    [SerializeField] private WheelCollider[] wheels;
    [SerializeField] private Transform[] wheelMeshes;
    
    // Components
    private Rigidbody rb;
    private AudioSource audioSource;
    private AudioSource engineAudioSource;
    
    // État
    private Vector2 moveInput;
    private bool isBraking;
    private bool isEngineOn = false;
    private bool areLightsOn = false;
    private float nextHornTime = 0f;
    private float hornCooldown = 0.5f;
    private GameObject currentPlayer = null;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configuration du centre de masse pour une meilleure stabilité
        if (centerOfMass != null)
            rb.centerOfMass = centerOfMass.localPosition;
            
        // Configuration des sources audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Source audio dédiée au moteur en boucle
        engineAudioSource = gameObject.AddComponent<AudioSource>();
        engineAudioSource.loop = true;
        engineAudioSource.spatialBlend = 1f; // 3D sound
        engineAudioSource.volume = 0.6f;
        
        // Initialisation des valeurs
        currentHealth = maxHealth;
        currentFuel = maxFuel;
        
        // Si pas de point de sortie défini, on en crée un
        if (playerExitPoint == null)
        {
            GameObject exitPointObj = new GameObject("ExitPoint");
            exitPointObj.transform.SetParent(transform);
            exitPointObj.transform.localPosition = new Vector3(2f, 0f, 0f); // Position à droite du véhicule
            playerExitPoint = exitPointObj.transform;
        }
        
        // Si pas de caméra de véhicule, essaie d'en trouver une
        if (vehicleCamera == null)
            vehicleCamera = GetComponentInChildren<Camera>();
            
        // Désactiver la caméra du véhicule au démarrage
        if (vehicleCamera != null)
            vehicleCamera.enabled = false;
    }
    
    private void Start()
    {
        // Désactiver les lumières au démarrage
        if (headlights != null)
        {
            foreach (var light in headlights)
            {
                if (light != null)
                    light.enabled = false;
            }
        }
    }
    
    private void Update()
    {
        // Ne prend les entrées que si le véhicule est contrôlé
        if (!IsControlled) return;
        
        // Récupère les entrées
        float hInput = Input.GetAxis("Horizontal");
        float vInput = Input.GetAxis("Vertical");
        moveInput = new Vector2(hInput, vInput);
        
        // Freinage
        isBraking = Input.GetKey(KeyCode.Space);
        
        // Démarrer/arrêter le moteur
        if (Input.GetKeyDown(KeyCode.M))
            ToggleEngine();
            
        // Allumer/éteindre les phares
        if (Input.GetKeyDown(KeyCode.L))
            ToggleLights();
            
        // Klaxon
        if (Input.GetKeyDown(KeyCode.H))
            UseHorn();
            
        // Ajuste le son du moteur en fonction de la vitesse
        if (engineAudioSource != null && engineLoopSound != null && isEngineOn)
        {
            float speedRatio = Mathf.Clamp01(rb.velocity.magnitude / (maxForwardSpeed / 3.6f));
            engineAudioSource.pitch = Mathf.Lerp(0.8f, 1.5f, speedRatio);
            engineAudioSource.volume = Mathf.Lerp(0.4f, 0.8f, speedRatio);
        }
        
        // Mise à jour de l'affichage HUD
        if (VehicleHUD.Instance != null)
        {
            VehicleHUD.Instance.UpdateSpeed(CurrentSpeed);
            VehicleHUD.Instance.UpdateDamage(100f - (currentHealth / maxHealth * 100f));
            VehicleHUD.Instance.UpdateFuel(currentFuel / maxFuel * 100f);
            VehicleHUD.Instance.UpdateStats($"Mode: {(isBraking ? "Brake" : "Drive")}");
        }
    }
    
    private void FixedUpdate()
    {
        if (!IsOperational) return;
        
        // Consommation de carburant
        if (requiresFuel && isEngineOn && currentFuel > 0)
        {
            float consumptionThisFrame = fuelConsumptionRate * Time.fixedDeltaTime * (1f + rb.velocity.magnitude / 30f);
            currentFuel = Mathf.Max(0f, currentFuel - consumptionThisFrame);
            
            // Si plus de carburant, arrête le moteur
            if (currentFuel <= 0f)
            {
                isEngineOn = false;
                if (engineAudioSource != null)
                    engineAudioSource.Stop();
            }
        }
        
        // Mouvements du véhicule seulement si contrôlé et moteur en marche
        if (IsControlled && isEngineOn)
        {
            float forwardInput = moveInput.y;
            float turnInput = moveInput.x;
            
            // Force de propulsion avant/arrière
            if (Mathf.Abs(forwardInput) > 0.1f)
            {
                float currentSpeedKmh = rb.velocity.magnitude * 3.6f;
                bool isMovingForward = Vector3.Dot(transform.forward, rb.velocity) >= 0;
                
                // Vérifier si on ne dépasse pas les limites de vitesse
                bool canAccelerate = true;
                if (forwardInput > 0 && currentSpeedKmh >= maxForwardSpeed)
                    canAccelerate = false;
                else if (forwardInput < 0 && currentSpeedKmh >= maxReverseSpeed && !isMovingForward)
                    canAccelerate = false;
                
                if (canAccelerate)
                {
                    float appliedAcceleration = forwardInput > 0 ? acceleration : reverseAcceleration;
                    Vector3 force = transform.forward * forwardInput * appliedAcceleration * Time.fixedDeltaTime;
                    rb.AddForce(force, ForceMode.Acceleration);
                }
            }
            
            // Rotation (direction)
            if (rb.velocity.magnitude > 0.5f)
            {
                float speedFactor = speedTurnInfluence.Evaluate(rb.velocity.magnitude / 30f);
                float direction = Mathf.Sign(Vector3.Dot(transform.forward, rb.velocity));
                float rotation = turnInput * turnSpeed * speedFactor * Time.fixedDeltaTime * direction;
                rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotation, 0f));
            }
            
            // Freinage
            if (isBraking)
            {
                rb.AddForce(-rb.velocity.normalized * brakeForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            }
            
            // Force vers le bas pour meilleure adhérence
            rb.AddForce(-transform.up * rb.velocity.sqrMagnitude * downforceCoefficient, ForceMode.Acceleration);
            
            // Résistance de l'air (drag)
            rb.AddForce(-rb.velocity.normalized * rb.velocity.sqrMagnitude * dragCoefficient, ForceMode.Acceleration);
            
            // Mise à jour visuelle des roues
            UpdateWheelVisuals();
        }
        else
        {
            // Décélération naturelle quand le véhicule n'est pas contrôlé
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * 0.5f);
        }
    }
    
    private void UpdateWheelVisuals()
    {
        if (wheels == null || wheelMeshes == null || wheels.Length != wheelMeshes.Length)
            return;
            
        for (int i = 0; i < wheels.Length; i++)
        {
            WheelCollider wheel = wheels[i];
            Transform wheelMesh = wheelMeshes[i];
            
            // Position
            Vector3 position;
            Quaternion rotation;
            wheel.GetWorldPose(out position, out rotation);
            
            wheelMesh.position = position;
            wheelMesh.rotation = rotation;
        }
    }
    
    public void ToggleEngine()
    {
        if (!IsOperational) return;
        
        isEngineOn = !isEngineOn;
        
        if (isEngineOn)
        {
            // Jouer le son de démarrage
            if (engineStartSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(engineStartSound);
            }
            
            // Après un petit délai, démarrer le son de moteur en boucle
            StartCoroutine(StartEngineLoopAfterDelay(0.5f));
            
            // Activer les particules d'échappement
            if (exhaustParticles != null)
            {
                foreach (var exhaust in exhaustParticles)
                {
                    if (exhaust != null)
                        exhaust.Play();
                }
            }
        }
        else
        {
            // Arrêter le son du moteur
            if (engineAudioSource != null)
                engineAudioSource.Stop();
                
            // Arrêter les particules d'échappement
            if (exhaustParticles != null)
            {
                foreach (var exhaust in exhaustParticles)
                {
                    if (exhaust != null)
                        exhaust.Stop();
                }
            }
        }
    }
    
    private IEnumerator StartEngineLoopAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (isEngineOn && engineAudioSource != null && engineLoopSound != null)
        {
            engineAudioSource.clip = engineLoopSound;
            engineAudioSource.Play();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Calcul des dommages basé sur la force de l'impact
        if (collision.relativeVelocity.magnitude > 5f)
        {
            float damage = collision.relativeVelocity.magnitude * 0.5f;
            TakeDamage(damage);
            
            // Son de collision
            if (collisionSound != null && audioSource != null)
            {
                float volume = Mathf.Clamp01(collision.relativeVelocity.magnitude / 20f);
                audioSource.PlayOneShot(collisionSound, volume);
            }
        }
    }
    
    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        
        // Vérifier si le véhicule est détruit
        if (currentHealth <= 0f)
        {
            OnVehicleDestroyed();
        }
    }
    
    public void RepairVehicle(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
    
    public void RefuelVehicle(float amount)
    {
        currentFuel = Mathf.Min(maxFuel, currentFuel + amount);
    }
    
    private void OnVehicleDestroyed()
    {
        // Arrêter le moteur et tous les systèmes
        isEngineOn = false;
        if (engineAudioSource != null)
            engineAudioSource.Stop();
            
        // Éjecter le joueur s'il est dedans
        if (IsControlled && currentPlayer != null)
        {
            PlayerVehicleInteractor interactor = currentPlayer.GetComponent<PlayerVehicleInteractor>();
            if (interactor != null)
                interactor.ForceExitVehicle();
        }
        
        // Effets visuels de destruction
        // TODO: Ajouter explosion, fumée, etc.
    }
    #endregion
}