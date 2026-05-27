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
        fraseEnemigoDerrotado = "“¡Ssssh! ¡No me pegues más, jefe! ¡Solo soy tu topo! Ya he filtrado a la prensa todo lo que me pediste...”";
        replicaMbappeVictoria = "“Buen chico. Sigue espiando en las sombras... el despacho de Florentino es el siguiente.”";

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
