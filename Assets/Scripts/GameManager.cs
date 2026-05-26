using UnityEngine;

public class GameManager : MonoBehaviour
{
    // El Singleton: permite acceder a este script desde cualquier lugar
    public static GameManager Instance { get; private set; }

    public string tipoEnemigoActual = "Entrenador"; 
    public float egoActualMbappe = 0f; // Los niveles de ego que Manel/Robert te pasarán
    public string escenaOrigenCombate = ""; // Escena a la que se vuelve tras finalizar el combate
    public int pasoDespacho = 0; // Controla la secuencia en el Despacho (0: combate contra Dictador, 1: diálogo final y fin del juego)
    public int pasoRecibidor = 0; // Controla la secuencia en el Recibidor (0: inicio, 1: Ester respondida, 2: Vini derrotado diálogo post, 3: libre para ir al Pasillo)
    public int pasoValdebebas = 0; // Controla la secuencia en Valdebebas (0: inicio, 1: Xabi respondido combate iniciado, 2: Xabi derrotado diálogo post, 3: libre para entrar al Recibidor)
    public int pasoPasillo = 0; // Controla la secuencia en el Pasillo (0: inicio, 1: combate iniciado, 2: Diglett derrotado diálogo post, 3: libre para ir al Despacho)
    public bool usarPosicionRetornoCombate = false;
    public Vector3 posicionRetornoCombate;

    public void ReiniciarVariables()
    {
        tipoEnemigoActual = "Entrenador";
        egoActualMbappe = 0f;
        escenaOrigenCombate = "";
        pasoDespacho = 0;
        pasoRecibidor = 0;
        pasoValdebebas = 0;
        pasoPasillo = 0;
        usarPosicionRetornoCombate = false;
        posicionRetornoCombate = Vector3.zero;
    }

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
