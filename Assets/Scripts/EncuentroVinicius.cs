using UnityEngine;
public class EncuentroVinicius : MonoBehaviour
{
    public SistemaDialogos sistemaDialogos;
    [Header("Configuración del Evento")]
    public Transform posicionDestino;  // El punto del pasillo donde Vinicius se frenará
    public float velocidadCarrera = 5f; // Qué tan rápido entra corriendo
    
    private bool haLlegado = false;
    private Animator miAnimator;

    void Start()
    {
        // Buscamos el componente Animator que tiene el personaje al arrancar
        miAnimator = GetComponent<Animator>();

        // Nos aseguramos de que empiece con la animación de correr activada
        if (miAnimator != null)
        {
            miAnimator.SetBool("estaCorriendo", true);
        }
    }

    void Update()
    {
        // Si ya llegó y empezó a hablar, no hacemos nada más en el Update
        if (haLlegado) return;

        if (posicionDestino != null)
        {
            // Movemos a Vinicius hacia el punto de destino fotograma a fotograma
            transform.position = Vector3.MoveTowards(transform.position, posicionDestino.position, velocidadCarrera * Time.deltaTime);

            // Comprobamos si la distancia al destino es casi cero (ya llegó)
            if (Vector3.Distance(transform.position, posicionDestino.position) < 0.1f)
            {
                haLlegado = true;
                if (miAnimator != null)
                {
                    miAnimator.SetBool("estaCorriendo", false);
                }
                DispararDialogoVinicius();
            }
        }
    }

    void DispararDialogoVinicius()
    {
        // Definimos lo que va a decir Vinicius
        string[] conversacion = new string[] {
            "¡¡Ya estoy harto de ti!!",
            "¡Todo el mundo habla de Mbappé, Mbappé, Mbappé!",
            "¡¡Este era MI club!!"
        };

        // Activamos el sistema
        if (sistemaDialogos != null)
        {
            sistemaDialogos.IniciarDialogo("Vinicius Jr.", conversacion);
        }
    }
}