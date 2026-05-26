using UnityEngine;

public class Entrenador : EnemigoBase
{
    void Awake()
    {
        nombreEnemigo = "Xabi Alonso";
        nombreAtaqueEnemigo = "Bronca Técnica";
        stats = new Estadisticas(); 
        stats.vidaMax = 20; stats.vidaActual = 20;
        stats.ataque = 8; stats.defensa = 5; stats.velocidad = 100; // Velocidad 100 para atacar antes

        // Frases de continuación tras el despido provocado por Mbappé
        fraseEnemigoDerrotado = "“Me echaste del club firmando mi carta de despido... pero jamás podrás borrar mi dignidad.”";
        replicaMbappeVictoria = "“Tu dignidad no paga las cláusulas, Xabi. En este nuevo Madrid, yo soy el entrenador.”";

        fraseEnemigoVictorioso = "“El karma existe, Kylian. Te creías el dueño del club y has caído ante el entrenador que tú mismo despediste.”";
        replicaMbappeDerrota = "“No... esto es imposible... ¡Llamad a mi madre! ¡Esto es un complot!”";
    }

    public override void EjecutarTurno(MbappeStats mbappe)
    {
        // El ataque hace muy poco daño constante (5 de daño) para que sea inofensivo
        float danio = 5f;
        
        // Regla específica: Imposible que Xabi Alonso mate a Mbappé (dejar a mínimo 1 de vida)
        if (mbappe.stats.vidaActual - danio <= 0)
        {
            danio = mbappe.stats.vidaActual - 1;
        }
        danio = Mathf.Max(0, danio);
        mbappe.RecibirDanio(danio);
        Debug.Log($"{nombreEnemigo} ataca con {nombreAtaqueEnemigo} haciendo {danio} de daño a Mbappé.");
    }
}
