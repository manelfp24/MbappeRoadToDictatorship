using UnityEngine;

public class DiglettDeMbappe : EnemigoBase
{
    void Start()
    {
        nombreEnemigo = "Diglett de Mbappé";
        stats = new Estadisticas(); 
        stats.vidaMax = 15; stats.vidaActual = 15;
        stats.ataque = 1; // Ataque inofensivo
        stats.defensa = 15; stats.velocidad = 10;

        // Solo configuramos las de victoria, ya que es imposible perder
        fraseEnemigoDerrotado = "“¡Ssssh! ¡No me pegues más, jefe! ¡Solo soy tu topo! Ya he filtrado a la prensa todo lo que me pediste...”";
        replicaMbappeVictoria = "“Buen chico. Sigue espiando en las sombras... el despacho de Florentino es el siguiente.”";

        fraseEnemigoVictorioso = ""; 
        replicaMbappeDerrota = "";
    }
}
