using UnityEngine;
using NeoFPS;
using NeoFPS.SinglePlayer;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager s_Instance = null;
    
    public static PlayerManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                // Rechercher une instance existante
                s_Instance = FindObjectOfType<PlayerManager>();
                
                // Si aucune instance n'est trouvée, en créer une
                if (s_Instance == null)
                {
                    GameObject go = new GameObject("PlayerManager");
                    s_Instance = go.AddComponent<PlayerManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return s_Instance;
        }
    }
    
    // Référence au joueur local
    private GameObject m_Player = null;
    
    private void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Update()
    {
        // Mise à jour de la référence au joueur si elle est nulle
        if (m_Player == null)
        {
            if (FpsSoloCharacter.localPlayerCharacter != null)
                m_Player = FpsSoloCharacter.localPlayerCharacter.gameObject;
        }
    }
    
    // Obtenir le composant de santé du joueur
    public IHealthManager GetHealthComponent()
    {
        if (m_Player != null)
        {
            var healthManager = m_Player.GetComponent<IHasHealthManager>();
            if (healthManager != null)
                return healthManager.healthManager;
        }
        return null;
    }
    
    // Méthode pour détecter si le joueur est dans un véhicule
    public bool IsInVehicle()
    {
        if (m_Player != null)
        {
            var vehicleInteractor = m_Player.GetComponent<PlayerVehicleInteractor>();
            return vehicleInteractor != null && vehicleInteractor.isInVehicle;
        }
        return false;
    }
}