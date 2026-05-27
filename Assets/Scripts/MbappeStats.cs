using UnityEngine;

public class MbappeStats : MonoBehaviour
{
    public Estadisticas statsBase = new Estadisticas() { vidaMax = 25, vidaActual = 25, ataque = 15, defensa = 10, velocidad = 10 }; // Stats iniciales de Mbappé con vidaMax en 25
    public Estadisticas stats;     // Stats actuales (calculadas con el EGO)
    [Header("Sprites de Combate")]
    public Sprite spriteIdle;      // Sprite visual en estado de reposo (inicial)
    public Sprite spriteAtaque;    // Sprite visual al realizar un ataque
    public Sprite spriteMuerto;    // Sprite visual al ser derrotado en combate
    
    [Range(0, 100)] public float porcentajeEgo = 0;

    void Awake()
    {
        stats = new Estadisticas(); // Inicializar estadísticas para evitar NullReferenceException
        ActualizarEstadisticasSegunEgo();
    }

    public void ActualizarEstadisticasSegunEgo()
    {
        // Limitamos el ego a un rango de 0 a 100
        float egoClamped = Mathf.Clamp(porcentajeEgo, 0f, 100f);

        // Interpolamos linealmente entre el valor base y el valor máximo de 100
        stats.vidaMax = Mathf.Round(statsBase.vidaMax + (100f - statsBase.vidaMax) * (egoClamped / 100f));
        stats.vidaActual = stats.vidaMax; // Curar al iniciar combate
        stats.ataque = Mathf.Round(statsBase.ataque + (100f - statsBase.ataque) * (egoClamped / 100f));
        stats.defensa = Mathf.Round(statsBase.defensa + (100f - statsBase.defensa) * (egoClamped / 100f));
        
        // La velocidad escala hasta un máximo de 90 para asegurar que los enemigos (velocidad 100) siempre ataquen primero
        stats.velocidad = Mathf.Round(statsBase.velocidad + (90f - statsBase.velocidad) * (egoClamped / 100f));
    }

    public void RecibirDanio(float cantidad)
    {
        stats.vidaActual = Mathf.Max(0, stats.vidaActual - cantidad);
    }

    // Comprobación de qué ataque puede usar según vuestro diseño
    public bool PuedeUsarAtaqueFinal() => porcentajeEgo >= 100;
    public bool PuedeUsarAtaqueAvanzado() => porcentajeEgo >= 75;
    public bool PuedeUsarAtaqueMedio() => porcentajeEgo >= 50;

#if UNITY_EDITOR
    // Ejecutado automáticamente en el Editor de Unity cuando se modifica un valor en el Inspector (Play o Edit Mode)
    private void OnValidate()
    {
        if (stats == null) stats = new Estadisticas();
        ActualizarEstadisticasSegunEgo();
    }
#endif
}
