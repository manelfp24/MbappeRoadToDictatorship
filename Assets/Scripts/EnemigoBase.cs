using UnityEngine;

public class EnemigoBase : MonoBehaviour
{
    public string nombreEnemigo;
    public Estadisticas stats;
    public Sprite spriteVisual; // Sprite visual para mostrar en combate
    public Sprite spriteMuerto; // Sprite visual al ser derrotado
    public Sprite spriteAtaque; // Sprite visual al realizar un ataque

    [Header("Diálogos de Combate")]
    [TextArea(2, 5)] public string fraseEnemigoDerrotado;
    [TextArea(2, 5)] public string replicaMbappeVictoria;
    [TextArea(2, 5)] public string fraseEnemigoVictorioso;
    [TextArea(2, 5)] public string replicaMbappeDerrota;
    
    // Método virtual por si algún enemigo ataca de forma diferente en el futuro
    public virtual void EjecutarTurno(MbappeStats mbappe)
    {
        // Lógica de daño básica: Daño = Ataque - Defensa (mínimo 1 de daño)
        float danio = Mathf.Max(1, stats.ataque - mbappe.stats.defensa);
        mbappe.RecibirDanio(danio);
        Debug.Log($"{nombreEnemigo} ataca y hace {danio} de daño a Mbappé.");
    }

    public void RecibirDanio(float cantidad)
    {
        stats.vidaActual = Mathf.Max(0, stats.vidaActual - cantidad);
    }
}
