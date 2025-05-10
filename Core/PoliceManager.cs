using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoliceManager : MonoBehaviour
{
    public static PoliceManager Instance { get; private set; }
    
    [Header("Configuration")]
    [SerializeField] private GameObject[] policeUnitPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minSpawnDistance = 15f;
    [SerializeField] private float maxSpawnDistance = 30f;
    
    [Header("Chase Parameters")]
    [SerializeField] private bool isPlayerWanted = false;
    [SerializeField] private int maxActiveUnits = 5;
    [SerializeField] private float unitSpawnInterval = 15f;
    [SerializeField] private float chaseIntensityMultiplier = 1.0f;
    
    private List<GameObject> activePoliceUnits = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private float lastPlayerSpottedTime = 0f;
    private bool isInitialized = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        isInitialized = true;
        
        // S'abonner au changement de niveau de réputation
        if (ReputationManager.instance != null)
        {
            ReputationManager.instance.OnReputationLevelChanged += HandleReputationLevelChanged;
        }
    }
    
    private void OnDestroy()
    {
        if (ReputationManager.instance != null)
        {
            ReputationManager.instance.OnReputationLevelChanged -= HandleReputationLevelChanged;
        }
    }
    
    private void Update()
    {
        // Vérifications périodiques
        if (isPlayerWanted)
        {
            // Logique de poursuite
            ManageActiveUnits();
        }
    }
    
    // Gérer le changement de réputation
    private void HandleReputationLevelChanged(string faction, ReputationManager.ReputationLevel oldLevel, ReputationManager.ReputationLevel newLevel)
    {
        if (faction != "Police") return;
        
        // Si la réputation devient hostile, déclencher la poursuite
        if (newLevel == ReputationManager.ReputationLevel.Hostile && !isPlayerWanted)
        {
            StartPoliceChase(1.0f); // Intensité standard
        }
        // Si la réputation n'est plus hostile, arrêter la poursuite
        else if (oldLevel == ReputationManager.ReputationLevel.Hostile && newLevel != ReputationManager.ReputationLevel.Hostile)
        {
            StopPoliceChase();
        }
        // Si la réputation reste hostile mais devient plus mauvaise, augmenter l'intensité
        else if (newLevel == ReputationManager.ReputationLevel.Hostile && oldLevel == ReputationManager.ReputationLevel.Hostile)
        {
            ModifyChaseIntensity(0.2f); // Augmenter légèrement
        }
    }
    
    // Démarrer une poursuite policière
    public void StartPoliceChase(float intensity = 1.0f)
    {
        if (isPlayerWanted) return;
        
        isPlayerWanted = true;
        chaseIntensityMultiplier = intensity;
        
        // Notifier le joueur
        if (ReputationNotifier.instance != null)
        {
            ReputationNotifier.instance.ShowNotification("Police", 0, "Vous êtes activement recherché!");
        }
        
        // Démarrer le spawn des unités
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
            
        spawnCoroutine = StartCoroutine(SpawnPoliceUnitsCoroutine());
        
        // Notifier les autres systèmes de jeu
        BroadcastWantedStatus(true);
    }
    
    // Arrêter la poursuite
    public void StopPoliceChase()
    {
        if (!isPlayerWanted) return;
        
        isPlayerWanted = false;
        
        // Arrêter le spawn
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        // Notifier le joueur
        if (ReputationNotifier.instance != null)
        {
            ReputationNotifier.instance.ShowNotification("Police", 0, "Vous n'êtes plus recherché");
        }
        
        // Nettoyer les unités actives (progressivement ou instantanément)
        StartCoroutine(DespawnPoliceUnits());
        
        // Notifier les autres systèmes de jeu
        BroadcastWantedStatus(false);
    }
    
    // Modifier l'intensité de la poursuite
    public void ModifyChaseIntensity(float deltaIntensity)
    {
        chaseIntensityMultiplier = Mathf.Clamp(chaseIntensityMultiplier + deltaIntensity, 0.5f, 3.0f);
        
        // Adapter dynamiquement les paramètres
        maxActiveUnits = Mathf.RoundToInt(maxActiveUnits * chaseIntensityMultiplier);
    }
    
    // Coroutine pour faire apparaître les unités de police
    private IEnumerator SpawnPoliceUnitsCoroutine()
    {
        while (isPlayerWanted)
        {
            // Vérifier si on peut faire apparaître plus d'unités
            if (activePoliceUnits.Count < maxActiveUnits)
            {
                GameObject policeUnit = SpawnPoliceUnit();
                if (policeUnit != null)
                {
                    activePoliceUnits.Add(policeUnit);
                }
            }
            
            // Attendre un intervalle ajusté par l'intensité de la poursuite
            float adjustedInterval = unitSpawnInterval / chaseIntensityMultiplier;
            yield return new WaitForSeconds(adjustedInterval);
        }
    }
    
    // Spawn d'une unité de police
    private GameObject SpawnPoliceUnit()
    {
        if (policeUnitPrefabs.Length == 0 || spawnPoints.Length == 0)
            return null;
            
        // Choisir un point de spawn approprié
        Transform spawnPoint = GetBestSpawnPoint();
        if (spawnPoint == null)
            return null;
            
        // Choisir un préfab d'unité de police
        GameObject prefab = policeUnitPrefabs[Random.Range(0, policeUnitPrefabs.Length)];
        
        // Instancier l'unité
        GameObject unit = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        
        // Configurer l'unité pour poursuivre le joueur
        ConfigurePoliceUnitAI(unit);
        
        return unit;
    }
    
    // Trouver le meilleur point de spawn
    private Transform GetBestSpawnPoint()
    {
        if (spawnPoints.Length == 0)
            return null;
            
        // Logique pour trouver un bon point de spawn
        // Par exemple, choisir un point qui n'est pas visible par le joueur
        // et qui est assez éloigné
        
        // Pour simplifier, on choisit un point aléatoire
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
    
    // Configurer l'IA de l'unité de police
    private void ConfigurePoliceUnitAI(GameObject unit)
    {
        // Configurer l'IA selon votre système
        // Par exemple, définir le joueur comme cible, agressivité, etc.
        
        EnemyAI ai = unit.GetComponent<EnemyAI>();
        if (ai != null)
        {
            // Exemple de configuration
            ai.SetDifficultyModifier(chaseIntensityMultiplier * 0.5f);
        }
    }
    
    // Gérer les unités actives
    private void ManageActiveUnits()
    {
        // Nettoyer les références nulles
        activePoliceUnits.RemoveAll(unit => unit == null);
    }
    
    // Faire disparaître progressivement les unités
    private IEnumerator DespawnPoliceUnits()
    {
        // Pour chaque unité active
        foreach (var unit in new List<GameObject>(activePoliceUnits))
        {
            if (unit != null)
            {
                // Définir un comportement de retrait
                EnemyAI ai = unit.GetComponent<EnemyAI>();
                if (ai != null)
                {
                    // Désactiver l'attaque et activer le retrait
                    // Selon votre implémentation
                }
                
                // Attendre un peu entre chaque unité
                yield return new WaitForSeconds(1.0f);
            }
        }
        
        // Vider la liste
        activePoliceUnits.Clear();
    }
    
    // Notifier les autres systèmes du statut recherché
    private void BroadcastWantedStatus(bool isWanted)
    {
        // Notifier tous les systèmes qui doivent réagir au statut recherché
        // Par exemple, les PNJ civils qui pourraient fuir ou appeler la police
        
        // Envoyer une notification aux systèmes d'IA
    }
}