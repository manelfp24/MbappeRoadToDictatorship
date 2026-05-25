using UnityEngine;

public class Dictador : EnemigoBase
{
    void Start()
    {
        nombreEnemigo = "Florentino Pérez";
        stats = new Estadisticas(); 
        stats.vidaMax = 99; stats.vidaActual = 99;
        stats.ataque = 30; stats.defensa = 20; stats.velocidad = 25;

        fraseEnemigoDerrotado = "“Has roto... los contratos... las cláusulas... lo he perdido todo...”";
        replicaMbappeVictoria = "“Tus hilos ya no me atan, presidente. Ahora firmarás mi decreto definitivo.”";

        fraseEnemigoVictorioso = "“Pensabas que un simple jugador podía desafiar a la institución. Qué tierno...”";
        replicaMbappeDerrota = "“La directiva... todo el mundo me ha dado la espalda... He caído en tu trampa...”";
    }
}
