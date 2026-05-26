using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class EscenaRecibidor : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    public SistemaDialogos sistemaDialogos;
    public MbappeController mbappeController;

    [Header("Puntos de Interés de la Escena")]
    public Transform NPC_Ester;
    public Transform entradaPasillo;
    public Transform Punto_Parada_Vini;

    [Header("GameObjects de Vinicius Jr.")]
    public GameObject viniCorriendoGO; // El GameObject que tiene la animación de correr y se desplaza (vini_corriendo 1_8)
    public GameObject viniIdleGO;      // El GameObject de Vinicius estático/reposo (idle_vini-_8)

    [Header("UI de Decisiones (Local)")]
    public GameObject panelDecisiones;
    public UnityEngine.UI.Button botonA;
    public UnityEngine.UI.Button botonB;
    public TMPro.TextMeshProUGUI textoA;
    public TMPro.TextMeshProUGUI textoB;

    [Header("Configuración de Cinemática")]
    public float velocidadCarreraVini = 5f;
    public float distanciaInteraccionEster = 1.5f;
    public float distanciaBloqueoPasillo = 1.2f;

    [Header("Audio y Sonidos")]
    public AudioSource fuenteSFX;
    public AudioClip sfxEgoBonus; // Opcional

    [Header("Audio y Música de Fondo")]
    public AudioSource fuenteMusica;
    public AudioClip musicaRecibidor;

    [Header("Efecto de Transición (Fade)")]
    public UnityEngine.UI.Image panelFade;
    public float duracionFade = 2.5f;

    private string dialogoActivo = "";
    private bool cinematicaViniActiva = false;
    private bool advertenciaEnProgreso = false;
    private bool panelDecisionesActivo = false;
    private int opcionSeleccionadaIndex = 0;
    private bool yaInteractuoConEsterLocal = false;

    void Start()
    {
        // Asegurar la existencia de GameManager para persistencia de pasos locales
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[EscenaRecibidor] GameManager.Instance es nulo. Creando uno de emergencia para la persistencia local.");
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

        // Reproducir la música de fondo
        if (fuenteMusica == null)
        {
            fuenteMusica = gameObject.AddComponent<AudioSource>();
        }

        if (musicaRecibidor != null)
        {
            fuenteMusica.clip = musicaRecibidor;
            fuenteMusica.loop = true;
            fuenteMusica.playOnAwake = false;
            fuenteMusica.Play();
        }
        else
        {
            Debug.LogWarning("[EscenaRecibidor] No se ha asignado el clip de música de fondo (musicaRecibidor).");
        }

        // 2. Gestionar el estado de la escena según el paso persistido en GameManager
        int paso = 0;
        if (GameManager.Instance != null)
        {
            paso = GameManager.Instance.pasoRecibidor;
        }

        if (paso >= 1)
        {
            yaInteractuoConEsterLocal = true;
        }

        // Posicionar a Mbappé en el punto de retorno si viene de un combate
        if (GameManager.Instance != null && GameManager.Instance.usarPosicionRetornoCombate && mbappeController != null)
        {
            mbappeController.transform.position = GameManager.Instance.posicionRetornoCombate;
            GameManager.Instance.usarPosicionRetornoCombate = false;
        }

        // Desplazar el punto de entrada al pasillo 1 unidad hacia arriba tras derrotar a Vinicius
        if (paso >= 2 && entradaPasillo != null)
        {
            entradaPasillo.position = new Vector3(entradaPasillo.position.x, entradaPasillo.position.y + 1.0f, entradaPasillo.position.z);
        }

        // Desactivar el script antiguo EncuentroVinicius si está en los GameObjects de Vinicius para evitar interferencias
        if (viniIdleGO != null)
        {
            EncuentroVinicius scriptAntiguo = viniIdleGO.GetComponent<EncuentroVinicius>();
            if (scriptAntiguo != null) scriptAntiguo.enabled = false;
        }
        if (viniCorriendoGO != null)
        {
            EncuentroVinicius scriptAntiguo = viniCorriendoGO.GetComponent<EncuentroVinicius>();
            if (scriptAntiguo != null) scriptAntiguo.enabled = false;
        }

        // Configuración inicial de personajes según el estado
        if (paso == 0 || paso == 1)
        {
            if (viniCorriendoGO != null) viniCorriendoGO.SetActive(false); // Ocultar correr
            if (viniIdleGO != null) viniIdleGO.SetActive(false);          // Ocultar idle
            if (panelDecisiones != null) panelDecisiones.SetActive(false);
        }
        else if (paso == 2 || paso == 3)
        {
            // Mbappé vuelve de ganarle a Vinicius en combate o ya fue derrotado (mostrar el idle)
            if (viniCorriendoGO != null) viniCorriendoGO.SetActive(false);
            if (viniIdleGO != null)
            {
                viniIdleGO.SetActive(true);
                if (Punto_Parada_Vini != null)
                {
                    Vector3 pos = Punto_Parada_Vini.position;
                    // Forzar el plano Z de Mbappé para asegurar la visibilidad del sprite
                    if (mbappeController != null) pos.z = mbappeController.transform.position.z;
                    viniIdleGO.transform.position = pos;
                }
            }
            if (panelDecisiones != null) panelDecisiones.SetActive(false);
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
        int paso = GameManager.Instance != null ? GameManager.Instance.pasoRecibidor : 0;

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
                    SeleccionarOpcionEster(opcionSeleccionadaIndex == 0);
                }
            }
        }

        int paso = GameManager.Instance != null ? GameManager.Instance.pasoRecibidor : 0;

        // Si Mbappé se puede mover, leemos proximidades
        if (mbappeController != null && mbappeController.enabled)
        {
            // 1. Proximidad a Ester (Paso 0)
            if (paso == 0 && !yaInteractuoConEsterLocal && NPC_Ester != null)
            {
                float distEster = Vector3.Distance(mbappeController.transform.position, NPC_Ester.position);
                if (distEster <= distanciaInteraccionEster)
                {
                    // Detectamos interactuar (pulsar la tecla E o Click)
                    var keyboard = UnityEngine.InputSystem.Keyboard.current;
                    bool pulsoE = keyboard != null && keyboard.eKey.wasPressedThisFrame;
                    bool pulsoClick = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;

                    if (pulsoE || pulsoClick)
                    {
                        IniciarDialogoEster();
                    }
                }
            }

            // 2. Proximidad a la Entrada del Pasillo
            if (entradaPasillo != null)
            {
                float distPasillo = Vector3.Distance(mbappeController.transform.position, entradaPasillo.position);
                if (distPasillo <= distanciaBloqueoPasillo)
                {
                    if (paso == 0)
                    {
                        // Intentó pasar sin hablar con Ester
                        BloquearYAdvertirMbappe();
                    }
                    else if (paso == 1 && !cinematicaViniActiva)
                    {
                        // Intentó pasar tras hablar con Ester -> Aparece Vinicius
                        cinematicaViniActiva = true;
                        StartCoroutine(CinematicaAparicionVinicius());
                    }
                    else if (paso == 3)
                    {
                        // Libre para pasar a la siguiente escena "Pasillo"
                        mbappeController.enabled = false;
                        SceneManager.LoadScene("Pasillo");
                    }
                }
            }
        }
    }

    // --- INTERACCIÓN CON ESTER (PASO 0) ---
    void IniciarDialogoEster()
    {
        yaInteractuoConEsterLocal = true;

        mbappeController.enabled = false;
        mbappeController.isMoving = false;
        mbappeController.input = Vector2.zero;

        dialogoActivo = "EsterPregunta";
        string[] dialogo = new string[] {
            "Ester|Sabía que llegarías hasta aquí…",
            "Ester|Pero antes dime algo.",
            "Ester|¿Qué prefieres realmente?"
        };

        sistemaDialogos.IniciarDialogo("Ester", dialogo);
    }

    void MostrarPanelDecisiones()
    {
        if (panelDecisiones != null)
        {
            panelDecisiones.SetActive(true);
            panelDecisiones.transform.SetAsLastSibling(); // Traer al frente para asegurar que recibe clics del ratón

            panelDecisionesActivo = true;
            opcionSeleccionadaIndex = 0;

            if (botonA != null)
            {
                botonA.onClick.RemoveAllListeners();
                botonA.onClick.AddListener(() => SeleccionarOpcionEster(true));
            }

            if (botonB != null)
            {
                botonB.onClick.RemoveAllListeners();
                botonB.onClick.AddListener(() => SeleccionarOpcionEster(false));
            }

            ActualizarVisualDecisiones();

            // Mantener visible la caja inferior de diálogo con la pregunta
            if (sistemaDialogos != null && sistemaDialogos.panelDialogo != null)
            {
                sistemaDialogos.panelDialogo.SetActive(true);
                if (sistemaDialogos.componenteNombre != null) sistemaDialogos.componenteNombre.text = "Ester";
                if (sistemaDialogos.componenteTexto != null) sistemaDialogos.componenteTexto.text = "¿Qué prefieres realmente?";
            }
        }
        else
        {
            Debug.LogWarning("[EscenaRecibidor] panelDecisiones no está asignado. Elegimos Opción A por defecto.");
            SeleccionarOpcionEster(true);
        }
    }

    void ActualizarVisualDecisiones()
    {
        if (opcionSeleccionadaIndex == 0)
        {
            if (textoA != null) textoA.text = "<b><color=#FFFF00>> A ti <</color></b>";
            if (textoB != null) textoB.text = "La Champions";
            if (botonA != null) botonA.Select();
        }
        else
        {
            if (textoA != null) textoA.text = "A ti";
            if (textoB != null) textoB.text = "<b><color=#FFFF00>> La Champions <</color></b>";
            if (botonB != null) botonB.Select();
        }
    }

    void SeleccionarOpcionEster(bool elegioA)
    {
        panelDecisionesActivo = false;

        // Restaurar textos originales sin los indicadores de selección por teclado
        if (textoA != null) textoA.text = "A ti";
        if (textoB != null) textoB.text = "La Champions";

        if (panelDecisiones != null) panelDecisiones.SetActive(false);

        if (elegioA)
        {
            // Opción A: "A ti" (+26 Ego y animación humorística)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.egoActualMbappe = Mathf.Clamp(GameManager.Instance.egoActualMbappe + 26f, 0f, 100f);
            }
            PlaySFX(sfxEgoBonus);
            
            StartCoroutine(AnimacionHumoristicaMbappe(() => {
                dialogoActivo = "EsterRespuestaA";
                string[] dialogoRespuesta = new string[] {
                    "Ester|Así me gusta…",
                    "Sistema|Ester ha motivado muchísimo a Mbappé. (¡Ego aumentado en un 26%!)"
                };
                sistemaDialogos.IniciarDialogo("Ester", dialogoRespuesta);
            }));
        }
        else
        {
            // Opción B: "La Champions" (+51 Ego)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.egoActualMbappe = Mathf.Clamp(GameManager.Instance.egoActualMbappe + 51f, 0f, 100f);
            }
            PlaySFX(sfxEgoBonus);

            dialogoActivo = "EsterRespuestaB";
            string[] dialogoRespuesta = new string[] {
                "Ester|Los futbolistas siempre elegís lo mismo…",
                "Sistema|El ansia de gloria europea endurece su mirada. (¡Ego aumentado en un 51%!)"
            };
            sistemaDialogos.IniciarDialogo("Ester", dialogoRespuesta);
        }
    }

    IEnumerator AnimacionHumoristicaMbappe(System.Action alTerminar)
    {
        SpriteRenderer sr = mbappeController.GetComponent<SpriteRenderer>();
        Color colorOriginal = sr != null ? sr.color : Color.white;
        Vector3 posOriginal = mbappeController.transform.position;

        float tiempo = 0f;
        float duracion = 1.5f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            // Saltos graciosos en el eje Y
            float offset = Mathf.Abs(Mathf.Sin(tiempo * 12f)) * 0.35f;
            mbappeController.transform.position = new Vector3(posOriginal.x, posOriginal.y + offset, posOriginal.z);

            // Parpadeo de color amoroso (rosa/fucsia)
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

    // --- BLOQUEO PASILLO (PASO 0) ---
    void BloquearYAdvertirMbappe()
    {
        if (advertenciaEnProgreso) return;
        advertenciaEnProgreso = true;

        mbappeController.enabled = false;
        mbappeController.isMoving = false;
        mbappeController.input = Vector2.zero;

        // Pequeño retroceso para sacarlo de la zona de bloqueo (empujar hacia abajo en el eje Y)
        Vector3 posRetroceso = mbappeController.transform.position;
        posRetroceso.y -= 1.0f; // Empujar 1 posición hacia abajo
        mbappeController.transform.position = posRetroceso;

        dialogoActivo = "AdvertenciaPasillo";
        string[] dialogo = new string[] {
            "Mbappé|No debería cruzar por aquí todavía...",
            "Sistema|Ester se sentirá ignorada si la dejas plantada en el recibidor."
        };
        sistemaDialogos.IniciarDialogo("Mbappé", dialogo);
    }

    // --- APARICIÓN DE VINICIUS (PASO 1) ---
    IEnumerator CinematicaAparicionVinicius()
    {
        // Desactivar el script de encuentro de Vinicius
        var scriptVini = GetComponent<EncuentroVinicius>();
        if (scriptVini != null) scriptVini.enabled = false;

        mbappeController.enabled = false;
        mbappeController.isMoving = false;
        mbappeController.input = Vector2.zero;

        // Asegurar que Vinicius de correr esté activo e idle desactivado al comenzar
        if (viniCorriendoGO != null)
        {
            viniCorriendoGO.SetActive(true);
            
            // Forzar plano Z de Mbappé para el sprite de correr
            if (mbappeController != null)
            {
                Vector3 posCorr = viniCorriendoGO.transform.position;
                posCorr.z = mbappeController.transform.position.z;
                viniCorriendoGO.transform.position = posCorr;
            }
        }
        if (viniIdleGO != null)
        {
            viniIdleGO.SetActive(false);
        }

        if (viniCorriendoGO == null || Punto_Parada_Vini == null)
        {
            Debug.LogWarning("[EscenaRecibidor] viniCorriendoGO o Punto_Parada_Vini nulos. Iniciando diálogo directo.");
            
            // Fallback: encendemos el de reposo si existe por seguridad
            if (viniIdleGO != null)
            {
                viniIdleGO.SetActive(true);
                if (mbappeController != null)
                {
                    Vector3 pos = viniIdleGO.transform.position;
                    pos.z = mbappeController.transform.position.z;
                    viniIdleGO.transform.position = pos;
                }
            }
            
            DispararDialogoVinicius();
            yield break;
        }

        Transform transformVini = viniCorriendoGO.transform;
        Vector3 posDestino = Punto_Parada_Vini.position;
        if (mbappeController != null) posDestino.z = mbappeController.transform.position.z;
        else posDestino.z = transformVini.position.z;

        // Mover a Vinicius desde su posición inicial hasta el punto de parada
        while (Vector3.Distance(transformVini.position, posDestino) > 0.05f)
        {
            transformVini.position = Vector3.MoveTowards(transformVini.position, posDestino, velocidadCarreraVini * Time.deltaTime);
            yield return null;
        }

        transformVini.position = posDestino;

        // Llegó al destino: desactivamos el de correr y activamos el de reposo (idle)
        if (viniCorriendoGO != null)
        {
            viniCorriendoGO.SetActive(false);
        }

        if (viniIdleGO != null)
        {
            viniIdleGO.SetActive(true);
            Vector3 pos = posDestino;
            if (mbappeController != null) pos.z = mbappeController.transform.position.z; // Plano exacto de Mbappé
            viniIdleGO.transform.position = pos;
        }

        // Guardamos la posición actual de Mbappé para posicionarlo aquí mismo al volver del combate
        if (GameManager.Instance != null && mbappeController != null)
        {
            GameManager.Instance.posicionRetornoCombate = mbappeController.transform.position;
            GameManager.Instance.usarPosicionRetornoCombate = true;
        }

        DispararDialogoVinicius();
    }

    void DispararDialogoVinicius()
    {
        dialogoActivo = "RetoVinicius";
        string[] dialogo = new string[] {
            "Vinicius Jr.|¡¡Ya estoy harto de ti!!",
            "Vinicius Jr.|¡Todo el mundo habla de Mbappé, Mbappé, Mbappé!",
            "Vinicius Jr.|¡¡Este era MI club!!"
        };

        // Inicia el diálogo de Vinicius que nos enviará al combate contra "Jugador" (Vinicius Jr.)
        sistemaDialogos.IniciarDialogo("Vinicius Jr.", dialogo, true, "Jugador");
    }

    // --- RETORNO DE COMBATE (PASO 2) ---
    IEnumerator DispararDialogoPostCombate()
    {
        // Pequeña pausa estética de asentamiento de la UI
        yield return new WaitForSeconds(0.2f);

        mbappeController.enabled = false;
        mbappeController.isMoving = false;
        mbappeController.input = Vector2.zero;

        dialogoActivo = "VictoriaViniDialogo";
        string[] dialogo = new string[] {
            "Mbappé|¿Qué pasa, Vini? ¿Ya no bailas de alegría?",
            "Mbappé|Tu Balón de Playa no sirve para nada contra el verdadero dictador del Real Madrid.",
            "Vinicius Jr.|Ggggh... esto no se va a quedar así, Kylian... ¡Te has adueñado del vestuario!"
        };

        sistemaDialogos.IniciarDialogo("Mbappé", dialogo);
    }

    // --- PROCESAMIENTO DE FINALES DE DIÁLOGO ---
    void AlTerminarDialogo()
    {
        int paso = GameManager.Instance != null ? GameManager.Instance.pasoRecibidor : 0;

        if (dialogoActivo == "EsterPregunta")
        {
            dialogoActivo = "";
            MostrarPanelDecisiones();
        }
        else if (dialogoActivo == "EsterRespuestaA" || dialogoActivo == "EsterRespuestaB")
        {
            dialogoActivo = "";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.pasoRecibidor = 1; // Ester respondida, listo para Vini
            }
            mbappeController.enabled = true; // Devolver control
        }
        else if (dialogoActivo == "AdvertenciaPasillo")
        {
            dialogoActivo = "";
            advertenciaEnProgreso = false;
            mbappeController.enabled = true; // Devolver control
        }
        else if (dialogoActivo == "VictoriaViniDialogo")
        {
            dialogoActivo = "";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.pasoRecibidor = 3; // Vinicius derrotado y diálogo terminado
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
