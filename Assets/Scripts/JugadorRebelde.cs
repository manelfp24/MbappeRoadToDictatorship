using UnityEngine;

public class JugadorRebelde : EnemigoBase
{
    void Awake()
    {
        nombreEnemigo = "Vinicius Jr.";
        nombreAtaqueEnemigo = "Balón de Playa";
        stats = new Estadisticas(); 
        stats.vidaMax = 45; stats.vidaActual = 45;
        stats.ataque = 12; stats.defensa = 10; stats.velocidad = 100; // Velocidad 100 para atacar antes

        fraseEnemigoDerrotado = "“¡Ay...! El vestuario... el vestuario nunca aceptará a un dictador...”";
        replicaMbappeVictoria = "“El vestuario es mío, Vini. Ahora ve a hacer tus bailes a otra parte.”";

        fraseEnemigoVictorioso = "“Se acabó el show. El Bernabéu corea mi nombre, no el tuyo.”";
        replicaMbappeDerrota = "“¿Cómo... cómo se ha girado la grada en mi contra? ¡Esto es un boicot!”";
    }

    public override void EjecutarTurno(MbappeStats mbappe)
    {
        float danio = 0f;
        // Si Mbappé tiene más de 35% de vida, el primer golpe lo deja al 30% exacto
        if (mbappe.stats.vidaActual > mbappe.stats.vidaMax * 0.35f)
        {
            danio = mbappe.stats.vidaActual - (mbappe.stats.vidaMax * 0.30f);
        }
        else
        {
            // En el siguiente turno, lo elimina
            danio = mbappe.stats.vidaActual;
        }

        danio = Mathf.Max(0, danio);
        mbappe.RecibirDanio(danio);
        Debug.Log($"{nombreEnemigo} ataca con {nombreAtaqueEnemigo} haciendo {danio} de daño a Mbappé.");
    }
}
