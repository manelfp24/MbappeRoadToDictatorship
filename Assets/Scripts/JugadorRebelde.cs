using UnityEngine;

public class JugadorRebelde : EnemigoBase
{
    void Start()
    {
        nombreEnemigo = "Vinicius Jr.";
        stats = new Estadisticas(); 
        stats.vidaMax = 45; stats.vidaActual = 45;
        stats.ataque = 12; stats.defensa = 10; stats.velocidad = 14;

        fraseEnemigoDerrotado = "“¡Ay...! El vestuario... el vestuario nunca aceptará a un dictador...”";
        replicaMbappeVictoria = "“El vestuario es mío, Vini. Ahora ve a hacer tus bailes a otra parte.”";

        fraseEnemigoVictorioso = "“Se acabó el show. El Bernabéu corea mi nombre, no el tuyo.”";
        replicaMbappeDerrota = "“¿Cómo... cómo se ha girado la grada en mi contra? ¡Esto es un boicot!”";
    }
}
