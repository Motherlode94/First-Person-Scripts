using UnityEngine;

public class ChangeCubeColorOnStart : MonoBehaviour
{
    // Référence au Renderer du cube
    private Renderer cubeRenderer;
    
    // Options de couleur
    public enum ColorChangeType
    {
        RandomColor,
        SpecificColor
    }
    
    // Type de changement de couleur à appliquer
    public ColorChangeType colorChangeType = ColorChangeType.RandomColor;
    
    // Couleur spécifique (utilisée si colorChangeType est SpecificColor)
    public Color specificColor = Color.red;
    
    // Tableau de couleurs pour la sélection aléatoire
    public Color[] availableColors;
    
    void Awake()
    {
        // Obtenir le composant Renderer
        cubeRenderer = GetComponent<Renderer>();
        
        // Si aucune couleur n'est définie dans l'inspecteur et que nous utilisons RandomColor,
        // créer des couleurs par défaut
        if ((availableColors == null || availableColors.Length == 0) && 
            colorChangeType == ColorChangeType.RandomColor)
        {
            availableColors = new Color[]
            {
                Color.red,
                Color.green,
                Color.blue,
                Color.yellow,
                Color.cyan,
                Color.magenta
            };
        }
    }

    void Start()
    {
        // Changer la couleur au démarrage du jeu
        if (colorChangeType == ColorChangeType.RandomColor)
        {
            SetRandomColor();
        }
        else
        {
            SetColor(specificColor);
        }
    }
    
    // Méthode pour définir une couleur spécifique
    public void SetColor(Color newColor)
    {
        cubeRenderer.material.color = newColor;
    }
    
    // Méthode pour définir une couleur aléatoire
    public void SetRandomColor()
    {
        int randomIndex = Random.Range(0, availableColors.Length);
        cubeRenderer.material.color = availableColors[randomIndex];
    }
}