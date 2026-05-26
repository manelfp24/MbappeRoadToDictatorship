using UnityEngine;

public class Dictador : EnemigoBase
{
    void Awake()
    {
        nombreEnemigo = "Florentino Pérez";
        nombreAtaqueEnemigo = "Control de Medios";
        stats = new Estadisticas(); 
        stats.vidaMax = 99; stats.vidaActual = 99;
        stats.ataque = 30; stats.defensa = 20; stats.velocidad = 100; // Velocidad 100 para atacar antes

        fraseEnemigoDerrotado = "“Has roto... los contratos... las cláusulas... lo he perdido todo...”";
        replicaMbappeVictoria = "“Tus hilos ya no me atan, presidente. Ahora firmarás mi decreto definitivo.”";

        fraseEnemigoVictorioso = "“Pensabas que un simple jugador podía desafiar a la institución. Qué tierno...”";
        replicaMbappeDerrota = "“La directiva... todo el mundo me ha dado la espalda... He caído en tu trampa...”";
    }

    public override void EjecutarTurno(MbappeStats mbappe)
    {
        // Hace siempre un 33% del total de la vida máxima de Mbappé
        float danio = Mathf.Round(mbappe.stats.vidaMax * 0.33f);
        mbappe.RecibirDanio(danio);
        Debug.Log($"{nombreEnemigo} ataca con {nombreAtaqueEnemigo} haciendo {danio} (33% max HP) de daño a Mbappé.");
    }
}
