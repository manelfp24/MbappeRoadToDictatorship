using UnityEngine;

public class DiglettDeMbappe : EnemigoBase
{
    void Awake()
    {
        nombreEnemigo = "Diglett de Mbappé";
        nombreAtaqueEnemigo = "Salpicadura extraña";
        stats = new Estadisticas(); 
        stats.vidaMax = 15; stats.vidaActual = 15;
        stats.ataque = 0; // Sin daño
        stats.defensa = 15; stats.velocidad = 100; // Velocidad 100 para atacar antes

        // Solo configuramos las de victoria, ya que es imposible perder
        fraseEnemigoDerrotado = "“¡Aghh! ¡Qué meneo!”";
        replicaMbappeVictoria = "“Ufff... Como me gustan los transformers en acción...”";

        fraseEnemigoVictorioso = ""; 
        replicaMbappeDerrota = "";
    }

    public override void EjecutarTurno(MbappeStats mbappe)
    {
        // No hace nada de daño
        mbappe.RecibirDanio(0);
        Debug.Log($"{nombreEnemigo} ataca con {nombreAtaqueEnemigo} y hace 0 de daño a Mbappé.");
    }
}
