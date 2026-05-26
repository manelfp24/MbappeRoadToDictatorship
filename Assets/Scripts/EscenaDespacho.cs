using UnityEngine;
using System.Collections;

public class EscenaDespacho : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    public SistemaDialogos sistemaDialogos;
    public MbappeController mbappeController;

    [Header("Audio y Música")]
    public AudioSource fuenteMusica;
    public AudioClip musicaDespacho;

    [Header("Efecto de Transición (Fade)")]
    public UnityEngine.UI.Image panelFade;
    public float duracionFade = 2.5f;

    [Header("Movimiento Cinemático de Mbappé")]
    public Transform posicionCaminarMbappe;
    public float velocidadCaminarCinematico = 2f;

    void Start()
    {
        // 1. Desactivar el movimiento del jugador Mbappé
        if (mbappeController == null)
        {
            mbappeController = FindAnyObjectByType<MbappeController>();
        }

        if (mbappeController != null)
        {
            mbappeController.enabled = false; // Desactivamos el script para que no responda a WASD/Input
            mbappeController.isMoving = false;
            mbappeController.input = Vector2.zero;
        }
        else
        {
            Debug.LogWarning("[EscenaDespacho] No se encontró MbappeController en la escena para bloquear el movimiento.");
        }

        // 2. Reproducir la música de fondo
        if (fuenteMusica == null)
        {
            fuenteMusica = gameObject.AddComponent<AudioSource>();
        }

        if (musicaDespacho != null)
        {
            fuenteMusica.clip = musicaDespacho;
            fuenteMusica.loop = true;
            fuenteMusica.playOnAwake = false;
            fuenteMusica.Play();
        }
        else
        {
            Debug.LogWarning("[EscenaDespacho] No se ha asignado el clip de música de fondo (musicaDespacho).");
        }

        // 3. Iniciar el efecto de desvanecido inicial (Fade) y luego el diálogo
        if (panelFade != null)
        {
            StartCoroutine(EfectoFadeInicial());
        }
        else
        {
            // Fallback si no está asignado: se dispara el diálogo al instante
            DispararDialogoDespacho();
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

        // Disparamos la conversación una vez la escena se ha aclarado completamente
        DispararDialogoDespacho();
    }

    void DispararDialogoDespacho()
    {
        int paso = 0;
        if (GameManager.Instance != null)
        {
            paso = GameManager.Instance.pasoDespacho;
        }

        if (sistemaDialogos == null)
        {
            sistemaDialogos = FindAnyObjectByType<SistemaDialogos>();
        }

        if (sistemaDialogos == null)
        {
            Debug.LogError("[EscenaDespacho] No se encontró el componente SistemaDialogos en la escena para disparar el diálogo.");
            return;
        }

        // Suscribirse al interceptor de frases antes de iniciar el diálogo
        sistemaDialogos.OnSiguienteFrase = InterceptarFrasesDespacho;

        if (paso == 0)
        {
            // Fase 1: Diálogo inicial y combate directo contra Dictador (Florentino Pérez)
            string[] conversacion = new string[] {
                "Florentino|Gracias por mantener contenta a la tortuga.",
                "Florentino|Tus talentos han sido… muy útiles.",
                "Mbappé|¿Todo esto era mentira…?",
                "Mbappé|¿Me manipulabas?",
                "Florentino|El fútbol moderno funciona así.",
                "Florentino|Y tú eras solo otra inversión.",
                "Mbappé|¡¡SE ACABÓ!!",
                "Mbappé|¡¡Voy a tomar el control del Real Madrid!!"
            };

            // Iniciamos diálogo para cargar directamente el combate contra "Dictador" (Florentino Pérez)
            sistemaDialogos.IniciarDialogo("Florentino", conversacion, true, "Dictador");
        }
        else if (paso == 1)
        {
            // Fase 2: Diálogo verdadero final tras derrotar a Florentino
            string[] conversacion = new string[] {
                "Mbappé|Jajajaja... ¡Mírate, Florentino! Moribundo en el suelo de tu propio despacho.",
                "Florentino|Mbappé... por favor... el club... el Real Madrid...",
                "Mbappé|¡¿El club?! ¡A la mierda el Real Madrid! ¡Y a la mierda el fútbol moderno!",
                "Mbappé|Nunca me importó el madridismo... ¡Yo solo quería el poder absoluto!",
                "Mbappé|¡¡A partir de hoy, yo soy el único Dictador!!"
            };

            // Diálogo final: al acabar, cargamos la escena llamada "MenuInicio"
            sistemaDialogos.IniciarDialogo("Mbappé", conversacion, true, "", "MenuInicio");
        }
    }

    void InterceptarFrasesDespacho(string frase, System.Action alTerminarAccion)
    {
        if (frase.StartsWith("Mbappé|¿Todo esto era mentira"))
        {
            // Iniciamos la corrutina para mover a Mbappé
            StartCoroutine(MoverMbappeCinematico(alTerminarAccion));
        }
        else
        {
            // Continuamos el diálogo inmediatamente
            alTerminarAccion();
        }
    }

    IEnumerator MoverMbappeCinematico(System.Action alTerminarAccion)
    {
        if (mbappeController == null)
        {
            mbappeController = FindAnyObjectByType<MbappeController>();
        }

        if (mbappeController == null || posicionCaminarMbappe == null)
        {
            Debug.LogWarning("[EscenaDespacho] No se puede ejecutar el movimiento cinemático: mbappeController o posicionCaminarMbappe son nulos.");
            alTerminarAccion();
            yield break;
        }

        Animator animator = mbappeController.GetComponent<Animator>();
        SpriteRenderer spriteRenderer = mbappeController.GetComponent<SpriteRenderer>();
        Transform transformMbappe = mbappeController.transform;

        // Guardamos la Z original de Mbappé de forma estricta para evitar que desaparezca detrás del fondo
        float zOriginal = transformMbappe.position.z;

        // Desactivar temporalmente la física dinámica si tiene un Rigidbody2D para evitar rebotes o desvíos violentos al colisionar
        Rigidbody2D rb = mbappeController.GetComponent<Rigidbody2D>();
        RigidbodyType2D tipoOriginalBody = RigidbodyType2D.Dynamic;
        bool tieneRb = (rb != null);
        if (tieneRb)
        {
            tipoOriginalBody = rb.bodyType;
            rb.bodyType = RigidbodyType2D.Kinematic;
#pragma warning disable CS0618
            rb.velocity = Vector2.zero;
#pragma warning restore CS0618
        }

        Vector3 posDestino = posicionCaminarMbappe.position;
        posDestino.z = zOriginal;

        // Calculamos la dirección del movimiento para el Animator y el SpriteRenderer
        Vector3 direccion = (posDestino - transformMbappe.position).normalized;

        if (animator != null)
        {
            animator.SetFloat("moveX", direccion.x);
            animator.SetFloat("moveY", direccion.y);
            animator.SetBool("isMoving", true);
        }

        if (spriteRenderer != null)
        {
            if (direccion.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else if (direccion.x > 0)
            {
                spriteRenderer.flipX = false;
            }
        }

        // Movemos a Mbappé hacia la posición objetivo, forzando la Z original en cada frame
        while (Vector3.Distance(transformMbappe.position, posDestino) > 0.05f)
        {
            Vector3 nuevaPos = Vector3.MoveTowards(transformMbappe.position, posDestino, velocidadCaminarCinematico * Time.deltaTime);
            nuevaPos.z = zOriginal; // Mantener la profundidad intacta
            transformMbappe.position = nuevaPos;
            yield return null;
        }

        // Fijamos la posición exacta final, forzando la Z original
        Vector3 posFinal = posDestino;
        posFinal.z = zOriginal;
        transformMbappe.position = posFinal;

        // Apagamos las animaciones de caminar
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetFloat("moveX", 0f);
            animator.SetFloat("moveY", 0f);
        }

        // Restauramos el Rigidbody al tipo original
        if (tieneRb)
        {
            rb.bodyType = tipoOriginalBody;
#pragma warning disable CS0618
            rb.velocity = Vector2.zero;
#pragma warning restore CS0618
        }

        // Continuamos con el diálogo
        alTerminarAccion();
    }
}
