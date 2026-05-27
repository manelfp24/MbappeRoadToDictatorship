using UnityEngine;

public class GameManager : MonoBehaviour
{
    // El Singleton: permite acceder a este script desde cualquier lugar
    public static GameManager Instance { get; private set; }

    // Aquí guardaremos qué tipo de enemigo va a pelear (ej: "Entrenador", "Jugador", "Dictador")
    public string tipoEnemigoActual = "Entrenador"; 
    public float egoActualMbappe = 0f; // Los niveles de ego que Manel/Robert te pasarán

    private void Awake()
    {
        // Esto asegura que solo exista un GameManager en todo el juego y no se borre entre escenas
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
