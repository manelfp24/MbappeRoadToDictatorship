using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class EscenaPasillo : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    public SistemaDialogos sistemaDialogos;
    public MbappeController mbappeController;

    [Header("Puntos de Interés de la Escena")]
    public Transform Trigger_Revista;
    public Transform Trigger_EntradaPasillo;

    [Header("Configuración de Distancias")]
    public float distanciaInteraccionRevista = 1.5f;
    public float distanciaBloqueoDespacho = 1.2f;

    [Header("Audio y Sonidos")]
    public AudioSource fuenteSFX;
    public AudioClip sfxEgoBonus; // Sonido de subida de estadísticas al máximo

    [Header("Audio y Música de Fondo")]
    public AudioSource fuenteMusica;
    public AudioClip musicaPasillo;

    [Header("Efecto de Transición (Fade)")]
    public UnityEngine.UI.Image panelFade;
    public float duracionFade = 2.5f;

    private string dialogoActivo = "";
    private bool advertenciaEnProgreso = false;
    private bool yaInteractuoConRevistaLocal = false;

    void Start()
    {
        // Asegurar la existencia de GameManager para persistencia de pasos locales
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[EscenaPasillo] GameManager.Instance es nulo. Creando uno de emergencia para la persistencia local.");
            GameObject goGM = new GameObject("GameManager_Emergencia");
            goGM.AddComponent<GameManager>();
        }

        // 1. Obtener referencias si no están asignadas
        if (mbappeController == null)
            mbappeController = FindAnyObjectByType<MbappeController>();

        if (sistemaDialogos == null)
            sistemaDialogos = FindAnyObjectByType<SistemaDialogos>();

        // Asegurar que el panel de diálogo comience apagado si no hay diálogos activos
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

        // Asegurar que el trigger de la revista esté en el mismo plano Z que Mbappé para evitar bugs de distancia
        if (Trigger_Revista != null && mbappeController != null)
        {
            Vector3 pos = Trigger_Revista.position;
            pos.z = mbappeController.transform.position.z;
            Trigger_Revista.position = pos;
        }

        // Reproducir la música de fondo
        if (fuenteMusica == null)
        {
            fuenteMusica = gameObject.AddComponent<AudioSource>();
        }

        if (musicaPasillo != null)
        {
            fuenteMusica.clip = musicaPasillo;
            fuenteMusica.loop = true;
            fuenteMusica.playOnAwake = false;
            fuenteMusica.Play();
        }
        else
        {
            Debug.LogWarning("[EscenaPasillo] No se ha asignado el clip de música de fondo (musicaPasillo).");
        }

        // 2. Gestionar el estado de la escena según el paso persistido en GameManager
        int paso = 0;
        if (GameManager.Instance != null)
        {
            paso = GameManager.Instance.pasoPasillo;
        }

        if (paso >= 1)
        {
            yaInteractuoConRevistaLocal = true;
        }

        // Posicionar a Mbappé en el punto de retorno si viene de un combate
        if (GameManager.Instance != null && GameManager.Instance.usarPosicionRetornoCombate && mbappeController != null)
        {
            mbappeController.transform.position = GameManager.Instance.posicionRetornoCombate;
            GameManager.Instance.usarPosicionRetornoCombate = false;
        }

        // Desplazar el punto de entrada al Despacho 1 unidad hacia arriba tras derrotar a Diglett
        if (paso >= 2 && Trigger_EntradaPasillo != null)
        {
            Trigger_EntradaPasillo.position = new Vector3(Trigger_EntradaPasillo.position.x, Trigger_EntradaPasillo.position.y + 1.0f, Trigger_EntradaPasillo.position.z);
        }

        // Desactivar proactivamente el script antiguo EncuentroVinicius si estuviera en la revista por error
        if (Trigger_Revista != null)
        {
            EncuentroVinicius scriptAntiguo = Trigger_Revista.GetComponent<EncuentroVinicius>();
            if (scriptAntiguo != null) scriptAntiguo.enabled = false;
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
        int paso = GameManager.Instance != null ? GameManager.Instance.pasoPasillo : 0;

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
        int paso = GameManager.Instance != null ? GameManager.Instance.pasoPasillo : 0;

        // Si Mbappé se puede mover, leemos proximidades
        if (mbappeController != null && mbappeController.enabled)
        {
            // 1. Proximidad a la Revista (Paso 0)
            if (paso == 0 && !yaInteractuoConRevistaLocal && Trigger_Revista != null)
            {
                float distRevista = Vector3.Distance(mbappeController.transform.position, Trigger_Revista.position);
                if (distRevista <= distanciaInteraccionRevista)
                {
                    // Detectamos interactuar (pulsar la tecla E o Click)
                    var keyboard = UnityEngine.InputSystem.Keyboard.current;
                    bool pulsoE = keyboard != null && keyboard.eKey.wasPressedThisFrame;
                    bool pulsoClick = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;

                    if (pulsoE || pulsoClick)
                    {
                        IniciarDialogoRevista();
                    }
                }
            }

            // 2. Proximidad a la Entrada del Despacho
            if (Trigger_EntradaPasillo != null)
            {
                float distDespacho = Vector3.Distance(mbappeController.transform.position, Trigger_EntradaPasillo.position);
                if (distDespacho <= distanciaBloqueoDespacho)
                {
                    if (paso == 0 || paso == 1)
                    {
                        // Intentó pasar sin leer la revista y derrotar a Diglett
                        BloquearYAdvertirMbappe();
                    }
                    else if (paso == 3)
                    {
                        // Libre para pasar a la siguiente escena "Despacho"
                        mbappeController.enabled = false;
                        SceneManager.LoadScene("Despacho");
                    }
                }
            }
        }
    }

    // --- INTERACCIÓN CON LA REVISTA (PASO 0) ---
    void IniciarDialogoRevista()
    {
        yaInteractuoConRevistaLocal = true;

        mbappeController.enabled = false;
        mbappeController.isMoving = false;
        mbappeController.input = Vector2.zero;

        dialogoActivo = "RevistaPregunta";
        string[] dialogo = new string[] {
            "Mbappé|Y esto?",
            "Mbappé|Que revista tan... peculiar... *Mbappé se excita y se sonroja*",
            "Mbappé|¿¡¿¡ QUE ES ESTO !?!?"
        };

        // Iniciar el diálogo que enviará al combate contra "Tercero" (Diglett de Mbappé) al terminar
        sistemaDialogos.IniciarDialogo("Mbappé", dialogo, true, "Tercero");

        // Guardamos la posición en la escena y registramos que empezó el combate
        if (GameManager.Instance != null && mbappeController != null)
        {
            GameManager.Instance.posicionRetornoCombate = mbappeController.transform.position;
            GameManager.Instance.usarPosicionRetornoCombate = true;
            GameManager.Instance.pasoPasillo = 1; // Combate iniciado
        }
    }

    // --- BLOQUEO PUERTA DESPACHO ---
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

        dialogoActivo = "AdvertenciaDespacho";
        string[] dialogo = new string[] {
            "Mbappé|No puedo entrar al despacho del presidente todavía...",
            "Sistema|Hay algo brillante sobre la mesa de la derecha que te llama la atención. Deberías examinarlo."
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

        // Maximizar estadísticas del jugador de forma efectiva
        if (GameManager.Instance != null)
        {
            GameManager.Instance.egoActualMbappe = 100f; // Ego al 100% (estadísticas máximas)
        }
        
        PlaySFX(sfxEgoBonus);

        dialogoActivo = "VictoriaDiglettDialogo";
        string[] dialogo = new string[] {
            "Sistema|¡Increíble! El Diglett de Mbappé ha sido pulverizado.",
            "Sistema|Kylian ha asimilado la energía de la revista. ¡Todas las estadísticas y su Ego han subido al máximo!",
            "Mbappé|Ahora si que estoy listo!"
        };

        sistemaDialogos.IniciarDialogo("Sistema", dialogo);
    }

    // --- PROCESAMIENTO DE FINALES DE DIÁLOGO ---
    void AlTerminarDialogo()
    {
        if (dialogoActivo == "AdvertenciaDespacho")
        {
            dialogoActivo = "";
            advertenciaEnProgreso = false;
            mbappeController.enabled = true; // Devolver control
        }
        else if (dialogoActivo == "VictoriaDiglettDialogo")
        {
            dialogoActivo = "";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.pasoPasillo = 3; // Diglett derrotado y diálogo post-combate finalizado
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
