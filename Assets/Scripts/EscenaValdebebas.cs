using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class EscenaValdebebas : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    public SistemaDialogos sistemaDialogos;
    public MbappeController mbappeController;

    [Header("Puntos de Interés de la Escena")]
    public Transform NPC_Xabi;
    public Transform entradaRecibidor;

    [Header("UI de Decisiones (Local)")]
    public GameObject panelDecisiones;
    public UnityEngine.UI.Button botonA;
    public UnityEngine.UI.Button botonB;
    public TMPro.TextMeshProUGUI textoA;
    public TMPro.TextMeshProUGUI textoB;

    [Header("Configuración de Distancias")]
    public float distanciaInteraccionXabi = 1.5f;
    public float distanciaBloqueoRecibidor = 1.2f;

    [Header("Audio y Sonidos")]
    public AudioSource fuenteSFX;
    public AudioClip sfxEgoBonus; // Sonido de subida de estadística

    [Header("Audio y Música de Fondo")]
    public AudioSource fuenteMusica;
    public AudioClip musicaValdebebas;

    [Header("Efecto de Transición (Fade)")]
    public UnityEngine.UI.Image panelFade;
    public float duracionFade = 2.5f;

    private string dialogoActivo = "";
    private bool advertenciaEnProgreso = false;
    private bool panelDecisionesActivo = false;
    private int opcionSeleccionadaIndex = 0;
    private bool yaInteractuoConXabiLocal = false;

    void Start()
    {
        // Asegurar la existencia de GameManager para persistencia de pasos locales
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[EscenaValdebebas] GameManager.Instance es nulo. Creando uno de emergencia para la persistencia local.");
            GameObject goGM = new GameObject("GameManager_Emergencia");
            goGM.AddComponent<GameManager>();
        }

        // 1. Obtener referencias si no están asignadas
        if (mbappeController == null)
            mbappeController = FindAnyObjectByType<MbappeController>();

        if (sistemaDialogos == null)
            sistemaDialogos = FindAnyObjectByType<SistemaDialogos>();

        // Asegurar que el panel de diálogo comience apagado si no hay diálogos
        if (sistemaDialogos != null && sistemaDialogos.panelDialogo != null)
        {
            sistemaDialogos.panelDialogo.SetActive(false);
        }

        // Desactivar temporalmente el movimiento durante el fade inicial
        if (mbappeController != null)
        {
            mbappeController.enabled = false;
            mbappeController.isMoving = false;
            mbappeController.input = Vector2.zero;
        }

        // Suscribirse a los eventos del sistema de diálogos
        if (sistemaDialogos != null)
        {
            sistemaDialogos.OnDialogoTerminado += AlTerminarDialogo;
        }

        // Asegurar que Xabi esté en el mismo plano Z que Mbappé para evitar parpadeos (Z-fighting) y desapariciones tras el combate
        if (NPC_Xabi != null)
        {
            Vector3 pos = NPC_Xabi.position;
            if (mbappeController != null)
            {
                pos.z = mbappeController.transform.position.z;
            }
            else
            {
                pos.z = -1f; // Forzar una profundidad visible estándar por encima del fondo (Z=0)
            }
            NPC_Xabi.position = pos;

            // Desactivar el script antiguo EncuentroVinicius si está en Xabi para evitar que lo controle u oculte
            EncuentroVinicius scriptAntiguo = NPC_Xabi.GetComponent<EncuentroVinicius>();
            if (scriptAntiguo != null) scriptAntiguo.enabled = false;
        }

        // Reproducir la música de fondo
        if (fuenteMusica == null)
        {
            fuenteMusica = gameObject.AddComponent<AudioSource>();
        }

        if (musicaValdebebas != null)
        {
            fuenteMusica.clip = musicaValdebebas;
            fuenteMusica.loop = true;
            fuenteMusica.playOnAwake = false;
            fuenteMusica.Play();
        }
        else
        {
            Debug.LogWarning("[EscenaValdebebas] No se ha asignado el clip de música de fondo (musicaValdebebas).");
        }

        // 2. Gestionar el estado de la escena según el paso persistido en GameManager
        int paso = 0;
        if (GameManager.Instance != null)
        {
            paso = GameManager.Instance.pasoValdebebas;
        }

        if (paso >= 1)
        {
            yaInteractuoConXabiLocal = true;
        }

        // Posicionar a Mbappé en el punto de retorno si viene de un combate
        if (GameManager.Instance != null && GameManager.Instance.usarPosicionRetornoCombate && mbappeController != null)
        {
            mbappeController.transform.position = GameManager.Instance.posicionRetornoCombate;
            GameManager.Instance.usarPosicionRetornoCombate = false;
        }

        // Desplazar el punto de entrada al Recibidor 1 unidad hacia arriba tras derrotar a Xabi
        if (paso >= 2 && entradaRecibidor != null)
        {
            entradaRecibidor.position = new Vector3(entradaRecibidor.position.x, entradaRecibidor.position.y + 1.0f, entradaRecibidor.position.z);
        }

        // Configuración inicial de personajes según el estado
        if (panelDecisiones != null)
        {
            panelDecisiones.SetActive(false);
        }

        // 3. Iniciar el efecto de fade inicial
        if (panelFade != null)
        {
            StartCoroutine(EfectoFadeInicial());
        }
        else
        {
            HabilitarEscenaTrasFade();
        }
    }

    IEnumerator EfectoFadeInicial()
    {
        if (panelFade != null)
        {
            panelFade.gameObject.SetActive(true);
            Color c = panelFade.color;
            c.a = 1f;
            panelFade.color = c;
        }

        float tiempoFade = 0f;

        while (tiempoFade < duracionFade)
        {
            tiempoFade += Time.deltaTime;
            if (panelFade != null)
            {
                Color c = panelFade.color;
                c.a = Mathf.Lerp(1f, 0f, tiempoFade / duracionFade);
                panelFade.color = c;
            }
            yield return null;
        }

        if (panelFade != null)
        {
            panelFade.gameObject.SetActive(false);
        }

        HabilitarEscenaTrasFade();
    }

    void HabilitarEscenaTrasFade()
    {
        int paso = GameManager.Instance != null ? GameManager.Instance.pasoValdebebas : 0;

        if (paso == 2)
        {
            // Lanzamos de inmediato el diálogo de post-combate
            StartCoroutine(DispararDialogoPostCombate());
        }
        else
        {
            // En otros pasos se devuelve control a Mbappé
            if (mbappeController != null)
            {
                mbappeController.enabled = true;
            }
        }
    }

    void OnDestroy()
    {
        if (sistemaDialogos != null)
        {
            sistemaDialogos.OnDialogoTerminado -= AlTerminarDialogo;
        }
    }

    void Update()
    {
        // 1. Control del panel de decisiones por teclado
        if (panelDecisionesActivo)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                // Navegación con flechas / WASD
                if (keyboard.aKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || 
                    keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                {
                    if (opcionSeleccionadaIndex != 0)
                    {
                        opcionSeleccionadaIndex = 0;
                        ActualizarVisualDecisiones();
                    }
                }
                else if (keyboard.dKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame || 
                         keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                {
                    if (opcionSeleccionadaIndex != 1)
                    {
                        opcionSeleccionadaIndex = 1;
                        ActualizarVisualDecisiones();
                    }
                }

                // Confirmación con Espacio / Enter
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || 
                    keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    SeleccionarOpcionXabi(opcionSeleccionadaIndex == 0);
                }
            }
        }

        int paso = GameManager.Instance != null ? GameManager.Instance.pasoValdebebas : 0;

        // Si Mbappé se puede mover, leemos proximidades
        if (mbappeController != null && mbappeController.enabled)
        {
            // 1. Proximidad a Xabi Alonso (Paso 0)
            if (paso == 0 && !yaInteractuoConXabiLocal && NPC_Xabi != null)
            {
                float distXabi = Vector3.Distance(mbappeController.transform.position, NPC_Xabi.position);
                if (distXabi <= distanciaInteraccionXabi)
                {
                    // Detectamos interactuar (pulsar la tecla E o Click)
                    var keyboard = UnityEngine.InputSystem.Keyboard.current;
                    bool pulsoE = keyboard != null && keyboard.eKey.wasPressedThisFrame;
                    bool pulsoClick = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;

                    if (pulsoE || pulsoClick)
                    {
                        IniciarDialogoXabi();
                    }
                }
            }

            // 2. Proximidad a la Entrada del Recibidor
            if (entradaRecibidor != null)
            {
                float distRecibidor = Vector3.Distance(mbappeController.transform.position, entradaRecibidor.position);
                if (distRecibidor <= distanciaBloqueoRecibidor)
                {
                    if (paso == 0 || paso == 1)
                    {
                        // Intentó pasar sin derrotar a Xabi
                        BloquearYAdvertirMbappe();
                    }
                    else if (paso == 3)
                    {
                        // Libre para pasar a la siguiente escena "Recibidor"
                        mbappeController.enabled = false;
                        SceneManager.LoadScene("Recibidor");
                    }
                }
            }
        }
    }

    // --- INTERACCIÓN CON XABI ALONSO (PASO 0) ---
    void IniciarDialogoXabi()
    {
        yaInteractuoConXabiLocal = true;

        mbappeController.enabled = false;
        mbappeController.isMoving = false;
        mbappeController.input = Vector2.zero;

        dialogoActivo = "XabiPregunta";
        string[] dialogo = new string[] {
            "Xabi Alonso|Así que al final era verdad…",
            "Xabi Alonso|Todo el mundo dice que tú pediste mi cabeza.",
            "Xabi Alonso|Dime la verdad, Mbappé.",
            "Xabi Alonso|¿Me echaron por tu culpa?"
        };

        sistemaDialogos.IniciarDialogo("Xabi Alonso", dialogo);
    }

    void MostrarPanelDecisiones()
    {
        if (panelDecisiones != null)
        {
            panelDecisiones.SetActive(true);
            panelDecisiones.transform.SetAsLastSibling(); // Traer al frente para asegurar clics del ratón

            panelDecisionesActivo = true;
            opcionSeleccionadaIndex = 0;

            if (botonA != null)
            {
                botonA.onClick.RemoveAllListeners();
                botonA.onClick.AddListener(() => SeleccionarOpcionXabi(true));
            }

            if (botonB != null)
            {
                botonB.onClick.RemoveAllListeners();
                botonB.onClick.AddListener(() => SeleccionarOpcionXabi(false));
            }

            ActualizarVisualDecisiones();

            // Mantener visible la caja inferior de diálogo con la pregunta
            if (sistemaDialogos != null && sistemaDialogos.panelDialogo != null)
            {
                sistemaDialogos.panelDialogo.SetActive(true);
                if (sistemaDialogos.componenteNombre != null) sistemaDialogos.componenteNombre.text = "Xabi Alonso";
                if (sistemaDialogos.componenteTexto != null) sistemaDialogos.componenteTexto.text = "¿Me echaron por tu culpa?";
            }
        }
        else
        {
            Debug.LogWarning("[EscenaValdebebas] panelDecisiones no está asignado. Elegimos Opción A por defecto.");
            SeleccionarOpcionXabi(true);
        }
    }

    void ActualizarVisualDecisiones()
    {
        if (opcionSeleccionadaIndex == 0)
        {
            if (textoA != null) textoA.text = "<b><color=#FFFF00>> Sí. Este club necesita obedecerme. <</color></b>";
            if (textoB != null) textoB.text = "No era mi intención…";
            if (botonA != null) botonA.Select();
        }
        else
        {
            if (textoA != null) textoA.text = "Sí. Este club necesita obedecerme.";
            if (textoB != null) textoB.text = "<b><color=#FFFF00>> No era mi intención… <</color></b>";
            if (botonB != null) botonB.Select();
        }
    }

    void SeleccionarOpcionXabi(bool elegioA)
    {
        panelDecisionesActivo = false;

        // Restaurar textos originales sin indicadores
        if (textoA != null) textoA.text = "Sí. Este club necesita obedecerme.";
        if (textoB != null) textoB.text = "No era mi intención…";

        if (panelDecisiones != null) panelDecisiones.SetActive(false);

        if (elegioA)
        {
            // Opción A: (+25 Ego, sonido de subida de estadística y animación de saltos)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.egoActualMbappe = Mathf.Clamp(GameManager.Instance.egoActualMbappe + 25f, 0f, 100f);
            }
            PlaySFX(sfxEgoBonus);
            
            StartCoroutine(AnimacionSaltosMbappe(() => {
                dialogoActivo = "XabiReaccionA";
                string[] dialogoRespuesta = new string[] {
                    "Xabi Alonso|Eres un niño caprichoso…",
                    "Xabi Alonso|Y acabarás destruyendo todo lo que toques."
                };
                // Al finalizar este diálogo, inicia el combate contra "Entrenador"
                sistemaDialogos.IniciarDialogo("Xabi Alonso", dialogoRespuesta, true, "Entrenador");
            }));
        }
        else
        {
            // Opción B: (+24 Ego)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.egoActualMbappe = Mathf.Clamp(GameManager.Instance.egoActualMbappe + 24f, 0f, 100f);
            }
            PlaySFX(sfxEgoBonus);

            dialogoActivo = "XabiReaccionB";
            string[] dialogoRespuesta = new string[] {
                "Xabi Alonso|Al menos te queda algo de humanidad…",
                "Xabi Alonso|Pero ya es demasiado tarde."
            };
            // Al finalizar este diálogo, inicia el combate contra "Entrenador"
            sistemaDialogos.IniciarDialogo("Xabi Alonso", dialogoRespuesta, true, "Entrenador");
        }

        // Antes de que se vaya al combate, guardamos la posición en la escena
        if (GameManager.Instance != null && mbappeController != null)
        {
            GameManager.Instance.posicionRetornoCombate = mbappeController.transform.position;
            GameManager.Instance.usarPosicionRetornoCombate = true;
            GameManager.Instance.pasoValdebebas = 1; // Avanzar el paso local a 1 para saber que ya interactuó
        }
    }

    IEnumerator AnimacionSaltosMbappe(System.Action alTerminar)
    {
        SpriteRenderer sr = mbappeController.GetComponent<SpriteRenderer>();
        Color colorOriginal = sr != null ? sr.color : Color.white;
        Vector3 posOriginal = mbappeController.transform.position;

        float tiempo = 0f;
        float duracion = 1.5f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            // Saltos en el eje Y
            float offset = Mathf.Abs(Mathf.Sin(tiempo * 12f)) * 0.35f;
            mbappeController.transform.position = new Vector3(posOriginal.x, posOriginal.y + offset, posOriginal.z);

            // Parpadeo de color amoroso/ego (rosa/fucsia)
            if (sr != null)
            {
                sr.color = Color.Lerp(colorOriginal, new Color(1f, 0.4f, 0.6f), Mathf.PingPong(tiempo * 6f, 1f));
            }

            yield return null;
        }

        // Restaurar estado
        mbappeController.transform.position = posOriginal;
        if (sr != null) sr.color = colorOriginal;

        alTerminar();
    }

    // --- BLOQUEO PUERTA EDIFICIO ---
    void BloquearYAdvertirMbappe()
    {
        if (advertenciaEnProgreso) return;
        advertenciaEnProgreso = true;

        mbappeController.enabled = false;
        mbappeController.isMoving = false;
        mbappeController.input = Vector2.zero;

        // Retroceder 1 posición hacia abajo en el eje Y para salir del trigger
        Vector3 posRetroceso = mbappeController.transform.position;
        posRetroceso.y -= 1.0f;
        mbappeController.transform.position = posRetroceso;

        dialogoActivo = "AdvertenciaPuerta";
        string[] dialogo = new string[] {
            "Mbappé|No puedo entrar al recibidor del edificio todavía...",
            "Sistema|Debes hablar con Xabi Alonso antes de acceder a las instalaciones de Valdebebas."
        };
        sistemaDialogos.IniciarDialogo("Mbappé", dialogo);
    }

    // --- RETORNO DE COMBATE (PASO 2) ---
    IEnumerator DispararDialogoPostCombate()
    {
        yield return new WaitForSeconds(0.2f);

        mbappeController.enabled = false;
        mbappeController.isMoving = false;
        mbappeController.input = Vector2.zero;

        dialogoActivo = "VictoriaXabiDialogo";
        string[] dialogo = new string[] {
            "Mbappé|¡Ja, ja, ja! Mírate, Xabi. Qué patético.",
            "Mbappé|¿Intentar hacernos aprender tácticas y pizarra? Qué pérdida de tiempo en el fútbol moderno.",
            "Mbappé|Lo único verdaderamente importante son los goles, y los goles me pertenecen a mí.",
            "Xabi Alonso|Kylian... tu soberbia será... la ruina... del vestuario..."
        };

        sistemaDialogos.IniciarDialogo("Mbappé", dialogo);
    }

    // --- PROCESAMIENTO DE FINALES DE DIÁLOGO ---
    void AlTerminarDialogo()
    {
        if (dialogoActivo == "XabiPregunta")
        {
            dialogoActivo = "";
            MostrarPanelDecisiones();
        }
        else if (dialogoActivo == "AdvertenciaPuerta")
        {
            dialogoActivo = "";
            advertenciaEnProgreso = false;
            mbappeController.enabled = true; // Devolver control
        }
        else if (dialogoActivo == "VictoriaXabiDialogo")
        {
            dialogoActivo = "";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.pasoValdebebas = 3; // Xabi derrotado y burla final completada
            }
            mbappeController.enabled = true; // Devolver control
        }
    }

    void PlaySFX(AudioClip clip)
    {
        if (fuenteSFX != null && clip != null)
        {
            fuenteSFX.PlayOneShot(clip);
        }
    }
}
