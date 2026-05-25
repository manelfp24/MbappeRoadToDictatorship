using UnityEngine;

public class MbappeStats : MonoBehaviour
{
    public Estadisticas statsBase = new Estadisticas() { vidaMax = 100, vidaActual = 100, ataque = 15, defensa = 10, velocidad = 10 }; // Stats iniciales de Mbappé
    public Estadisticas stats;     // Stats actuales (calculadas con el EGO)
    [Header("Sprites de Combate")]
    public Sprite spriteIdle;      // Sprite visual en estado de reposo (inicial)
    public Sprite spriteAtaque;    // Sprite visual al realizar un ataque
    public Sprite spriteMuerto;    // Sprite visual al ser derrotado en combate
    
    [Range(0, 100)] public float porcentajeEgo = 0;

    void Start()
    {
        stats = new Estadisticas(); // Inicializar estadísticas para evitar NullReferenceException
        ActualizarEstadisticasSegunEgo();
    }

    public void ActualizarEstadisticasSegunEgo()
    {
        // Si tiene el 100% de EGO se vuelve Dios para vencer a Florentino
        if (porcentajeEgo >= 100)
        {
            stats.vidaMax = 1000; stats.vidaActual = 1000;
            stats.ataque = 999; stats.defensa = 999; stats.velocidad = 999;
            Debug.Log("¡Mbappé ha alcanzado el EGO SUPREMO! Modo Dios activado.");
            return;
        }

        // Multiplicador básico de estadísticas según el ego (a más ego, más fuerte)
        float multiplicador = 1 + (porcentajeEgo / 100f); 
        stats.vidaMax = statsBase.vidaMax * multiplicador;
        stats.vidaActual = stats.vidaMax; // Curar al iniciar combate
        stats.ataque = statsBase.ataque * multiplicador;
        stats.defensa = statsBase.defensa * multiplicador;
        stats.velocidad = statsBase.velocidad * multiplicador;
    }

    public void RecibirDanio(float cantidad)
    {
        stats.vidaActual = Mathf.Max(0, stats.vidaActual - cantidad);
    }

    // Comprobación de qué ataque puede usar según vuestro diseño
    public bool PuedeUsarAtaqueFinal() => porcentajeEgo >= 100;
    public bool PuedeUsarAtaqueAvanzado() => porcentajeEgo >= 75;
    public bool PuedeUsarAtaqueMedio() => porcentajeEgo >= 50;
}
