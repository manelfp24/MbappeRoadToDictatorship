using UnityEngine;
using UnityEngine.SceneManagement; // Esta línea es para cambiar de escena
using UnityEngine.UI; //Esto nos permite controlar elementos de la UI como Sliders o Toggles
public class MenuPrincipal : MonoBehaviour
{
    // Variables para guardar los paneles y poder encenderlos/apagarlos
    public GameObject panelMenuPrincipal;
    public GameObject panelOpciones;

    // Variable para controlar el volumen de la música de fondo
    public AudioSource musicaFondo;

    // 1. CAMBIO DE ESCENA
    public void JugarJuego()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReiniciarVariables();
        }
        SceneManager.LoadScene("Valdebebas"); 
    }

    // 2. AJUSTE DE VOLUMEN
    public void CambiarVolumen(float valor)
    {
        // Esto cambia el volumen general del juego (va de 0.0 a 1.0)
        AudioListener.volume = valor;
        if (musicaFondo != null)
        {
            musicaFondo.volume = valor; // Asegura que la música de fondo se ajuste
        }
        Debug.Log("Volumen cambiado a: " + valor);
    }

    // 3. PANTALLA COMPLETA
    public void CambiarPantallaCompleta(bool esCompleta)
    {
        Screen.fullScreen = esCompleta;
        Debug.Log("Pantalla completa: " + esCompleta);
    }

    // 4. CERRAR JUEGO
    public void SalirJuego()
    {
        Debug.Log("El jugador ha salido del juego.");
        Application.Quit(); 
    }
}