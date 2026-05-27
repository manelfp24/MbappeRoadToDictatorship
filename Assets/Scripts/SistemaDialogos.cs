using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SistemaDialogos : MonoBehaviour
{
    [Header("Componentes de UI")]
    public TextMeshProUGUI componenteNombre;
    public TextMeshProUGUI componenteTexto;
    public GameObject panelDialogo; // Para encender y apagar la caja entera

    [Header("Contenido del Diálogo")]
    private Queue<string> frases; // Una cola para almacenar las frases en orden

    void Awake()
    {
        frases = new Queue<string>();
        panelDialogo.SetActive(false); // Empezamos con la caja oculta
    }

    // Función que llamaremos cuando Mbappé se cruce con Vinicius
    public void IniciarDialogo(string nombrePersonaje, string[] textos)
    {
        panelDialogo.SetActive(true);
        componenteNombre.text = nombrePersonaje;
        
        frases.Clear();

        // Metemos todas las frases en la cola
        foreach (string frase in textos)
        {
            frases.Enqueue(frase);
        }

        MostrarSiguienteFrase();
    }

    public void MostrarSiguienteFrase()
    {
        // Si ya no quedan más frases, cerramos el diálogo
        if (frases.Count == 0)
        {
            TerminarDialogo();
            return;
        }

        // Sacamos la siguiente frase de la lista y la mostramos
        string fraseActual = frases.Dequeue();
        componenteTexto.text = fraseActual;
    }

    void TerminarDialogo()
    {
        panelDialogo.SetActive(false);
        Debug.Log("Fin del diálogo. ¡Que empiece el combate!");
        // Aquí más adelante activarás la lógica para cargar la escena de pelea
    }
}