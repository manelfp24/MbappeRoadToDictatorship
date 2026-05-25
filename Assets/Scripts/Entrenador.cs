using UnityEngine;

public class Entrenador : EnemigoBase
{
    void Start()
    {
        nombreEnemigo = "Xabi Alonso";
        stats = new Estadisticas(); 
        stats.vidaMax = 20; stats.vidaActual = 20;
        stats.ataque = 8; stats.defensa = 5; stats.velocidad = 5;

        // Frases de continuación tras el despido provocado por Mbappé
        fraseEnemigoDerrotado = "“Me echaste del club firmando mi carta de despido... pero jamás podrás borrar mi dignidad.”";
        replicaMbappeVictoria = "“Tu dignidad no paga las cláusulas, Xabi. En este nuevo Madrid, yo soy el entrenador.”";

        fraseEnemigoVictorioso = "“El karma existe, Kylian. Te creías el dueño del club y has caído ante el entrenador que tú mismo despediste.”";
        replicaMbappeDerrota = "“No... esto es imposible... ¡Llamad a mi madre! ¡Esto es un complot!”";
    }
}
