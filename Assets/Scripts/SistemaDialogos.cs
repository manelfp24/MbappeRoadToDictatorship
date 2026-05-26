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
    private bool combateAlFinalizar = false;
    private string enemigoParaCombate = "";
    private string escenaACargarAlFinalizar = ""; // Escena alternativa a cargar al terminar (ej: "Final")
    private bool conversacionActiva = false; // Controla si se pueden procesar inputs para pasar páginas

    // Delegado y evento para interceptar frases especiales y pausar el flujo
    public delegate void InterceptorFrase(string frase, System.Action alTerminarAccion);
    public InterceptorFrase OnSiguienteFrase;

    // Evento disparado al finalizar un diálogo narrativo
    public System.Action OnDialogoTerminado;

    void Awake()
    {
        frases = new Queue<string>();
        
        // Autodetectar panelDialogo si está vacío por error de asignación en el Inspector
        if (panelDialogo == null)
        {
            panelDialogo = GameObject.Find("HUD_DialogoFin");
            if (panelDialogo == null)
            {
                panelDialogo = GameObject.Find("PanelDialogo");
            }
            if (panelDialogo == null)
            {
#pragma warning disable CS0618
                var dialogos = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#pragma warning restore CS0618
                foreach (var go in dialogos)
                {
                    if (go.name.Contains("Dialogo") || go.name.Contains("HUD_Dialogo"))
                    {
                        panelDialogo = go;
                        break;
                    }
                }
            }
        }

        if (panelDialogo != null)
        {
            panelDialogo.SetActive(false); // Empezamos con la caja oculta
        }
        else
        {
            Debug.LogWarning("[SistemaDialogos] panelDialogo es nulo y no se pudo autodetectar en la escena.");
        }
    }

    // Función que llamaremos cuando Mbappé se cruce con un personaje
    public void IniciarDialogo(string nombrePersonaje, string[] textos, bool iniciarCombateAlFinalizar = false, string tipoEnemigo = "", string escenaACargar = "")
    {
        if (panelDialogo != null)
        {
            panelDialogo.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[SistemaDialogos] Intentando iniciar diálogo pero panelDialogo es nulo.");
        }

        if (componenteNombre != null)
        {
            componenteNombre.text = nombrePersonaje;
        }
        
        frases.Clear();

        // Metemos todas las frases en la cola
        foreach (string frase in textos)
        {
            frases.Enqueue(frase);
        }

        // Recordamos si este diálogo debe iniciar un combate
        combateAlFinalizar = iniciarCombateAlFinalizar;
        enemigoParaCombate = tipoEnemigo;
        escenaACargarAlFinalizar = escenaACargar; // Recordamos si hay una escena de carga especial
        conversacionActiva = true; // Activamos la lectura de inputs

        MostrarSiguienteFrase();
    }

    void Update()
    {
        // Si el diálogo está abierto, leemos el clic del ratón o espacio/enter para avanzar de frase
        if (conversacionActiva)
        {
            bool haPulsadoClick = false;
            bool haPulsadoTeclado = false;

            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                haPulsadoClick = true;

            if (UnityEngine.InputSystem.Keyboard.current != null && 
               (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame || 
                UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame))
                haPulsadoTeclado = true;

            if (haPulsadoClick || haPulsadoTeclado)
            {
                MostrarSiguienteFrase();
            }
        }
    }

    public void MostrarSiguienteFrase()
    {
        // Si ya no quedan más frases, cerramos el diálogo
        if (frases.Count == 0)
        {
            TerminarDialogo();
            return;
        }

        // Sacamos la siguiente frase de la lista para evaluarla
        string fraseActual = frases.Peek();

        if (OnSiguienteFrase != null)
        {
            // Desactivamos temporalmente los inputs para evitar que el usuario pase la frase mientras se ejecuta la acción
            conversacionActiva = false;
            
            OnSiguienteFrase.Invoke(fraseActual, () => {
                // Callback al terminar la acción: reanudamos la conversación, sacamos la frase de la cola y la mostramos
                conversacionActiva = true;
                if (frases.Count > 0)
                {
                    frases.Dequeue();
                    ProcesarYMostrarFrase(fraseActual);
                }
            });
        }
        else
        {
            frases.Dequeue();
            ProcesarYMostrarFrase(fraseActual);
        }
    }

    void ProcesarYMostrarFrase(string fraseActual)
    {
        // Si la frase tiene el formato "Nombre|Texto", actualizamos dinámicamente el nombre del hablante
        if (fraseActual.Contains("|"))
        {
            string[] partes = fraseActual.Split('|');
            if (partes.Length >= 2)
            {
                string nombre = partes[0].Trim();
                string texto = partes[1].Trim();

                if (componenteNombre != null)
                {
                    componenteNombre.text = nombre;
                    componenteTexto.text = texto;
                }
                else
                {
                    // Fallback si no hay campo de Nombre separado: combinamos en componenteTexto al estilo combate
                    componenteTexto.text = $"<b>{nombre}:</b>\n{texto}";
                }
            }
            else
            {
                componenteTexto.text = fraseActual;
            }
        }
        else
        {
            componenteTexto.text = fraseActual;
        }
    }

    void TerminarDialogo()
    {
        conversacionActiva = false; // Desactivamos los inputs
        if (panelDialogo != null)
        {
            panelDialogo.SetActive(false);
        }
        Debug.Log("Fin del diálogo. ¡Que empiece el combate!");

        // Si se especificó combate u otra transición al finalizar el diálogo
        if (combateAlFinalizar)
        {
            string escenaObjetivo = string.IsNullOrEmpty(escenaACargarAlFinalizar) ? "EscenaCombate" : escenaACargarAlFinalizar;

            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[SistemaDialogos] GameManager.Instance es nulo. Creando uno de emergencia para persistencia.");
                GameObject goGM = new GameObject("GameManager_Emergencia");
                goGM.AddComponent<GameManager>();
            }

            if (GameManager.Instance != null)
            {
                if (escenaObjetivo == "EscenaCombate")
                {
                    // 1. Inyectamos los datos del combate actual en el GameManager persistente
                    GameManager.Instance.tipoEnemigoActual = enemigoParaCombate;
                }
                // Guardamos el nombre de la escena actual de forma dinámica para poder volver a ella al terminar
                GameManager.Instance.escenaOrigenCombate = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }

            // 2. Cargamos la escena correspondiente (EscenaCombate o una escena final como "Final")
            UnityEngine.SceneManagement.SceneManager.LoadScene(escenaObjetivo);
        }

        // Invocar el evento de finalización del diálogo si está suscrito
        if (OnDialogoTerminado != null)
        {
            OnDialogoTerminado.Invoke();
        }
    }
}