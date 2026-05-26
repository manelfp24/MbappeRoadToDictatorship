using UnityEngine;

public class BotonFlotante : MonoBehaviour
{
    [Header("Configuración del movimiento")]
    public float velocidad = 1f;      // Qué tan rápido se mueve/bambolea
    public float amplitud = 10f;       // Qué tanto se desplaza en píxeles

    private Vector3 posicionInicial;

    void Start()
    {
        // Guardamos la posición original del botón en la pantalla
        posicionInicial = transform.localPosition;
    }

    void Update()
    {
        // Calculamos la nueva posición usando una onda Seno en el eje Y (vertical)
        float nuevoY = posicionInicial.y + Mathf.Sin(Time.time * velocidad) * amplitud;
        
        // Aplicamos el movimiento al botón
        transform.localPosition = new Vector3(posicionInicial.x, nuevoY, posicionInicial.z);
    }
}