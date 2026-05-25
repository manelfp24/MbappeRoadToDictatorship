using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CombatManager : MonoBehaviour
{
    public MbappeStats mbappe;
    public EnemigoBase enemigoActual; 
    
    public Transform posicionMbappe;
    public Transform posicionEnemigo;

    public enum EstadoCombate { INICIO, TURNO_JUGADOR, TURNO_ENEMIGO, VICTORIA, DERROTA }
    public EstadoCombate estado;

    public GameObject panelBotones;

    public Button botonBasico;
    public Button botonEgo50;
    public Button botonEgo75;
    public Button botonEgo100;

    [Header("HUD de Vida")]
    public Image barraMbappe;
    public TextMeshProUGUI textoMbappe;
    public Image barraEnemigo;
    public TextMeshProUGUI textoEnemigo;

    [Header("Sprites Visuales")]
    public SpriteRenderer spriteRendererMbappe;
    public SpriteRenderer spriteRendererEnemigo;

    [Header("UI de Diálogo Final")]
    public GameObject panelDialogoFin;
    public TextMeshProUGUI textoDialogoFin;

    [Header("Efecto de Transición Inicial")]
    public Image panelFadeNegro; // Arrastrar el Panel_Fade aquí

    [Header("Audio - Fuentes")]
    public AudioSource fuenteMusica;
    public AudioSource fuenteSFX;
    public AudioSource fuenteAlarmaVidaBaja;

    [Header("Audio - Clips de Sonido")]
    public AudioClip musicaCombate;
    public AudioClip sfxAtaqueMbappe;
    public AudioClip sfxAtaqueEnemigo; // <- NUEVO: Clip de ataque de enemigo
    public AudioClip sfxRecibirDanio;
    public AudioClip sfxAlarmaLoop;
    public AudioClip sfxMuerteUniversal;
    public AudioClip musicaVictoria; // Canción de victoria

    [Header("Imágenes Estáticas - Duración de Mostrar Ataque")]
    public float duracionAtaqueMbappe = 0.5f;   // Tiempo de espera para el sprite de ataque de Mbappé
    public float duracionAtaqueEnemigo = 0.5f;  // Tiempo de espera para el sprite de ataque del enemigo

    [Header("Sprites de Combate - Enemigo")]
    public Sprite enemigoSpriteVisual;
    public Sprite enemigoSpriteAtaque;
    public Sprite enemigoSpriteMuerto;

    private bool conversacionActiva = false;
    private int pasoConversacion = 0;
    private bool ganoJugadorEnEsteCombate = false;
    private bool alarmaSonando = false;

    void Start()
    {
        estado = EstadoCombate.INICIO;
        
        // REGLA: Los botones permanecen encendidos en la pantalla (pero no interactivos hasta el turno del jugador)
        if (panelBotones != null) panelBotones.SetActive(true);
        if (panelDialogoFin != null) panelDialogoFin.SetActive(false);

        // Vincular eventos de clic de forma programática para evitar fallos de enlace en el Inspector de Unity
        if (botonBasico != null)
        {
            botonBasico.onClick.RemoveAllListeners();
            botonBasico.onClick.AddListener(() => SeleccionarAtaque(1f, 1));
        }
        if (botonEgo50 != null)
        {
            botonEgo50.onClick.RemoveAllListeners();
            botonEgo50.onClick.AddListener(() => SeleccionarAtaque(1.5f, 2));
        }
        if (botonEgo75 != null)
        {
            botonEgo75.onClick.RemoveAllListeners();
            botonEgo75.onClick.AddListener(() => SeleccionarAtaque(2f, 3));
        }
        if (botonEgo100 != null)
        {
            botonEgo100.onClick.RemoveAllListeners();
            botonEgo100.onClick.AddListener(() => SeleccionarAtaque(5f, 4));
        }

        // Desactivamos la interactividad inicial durante el fundido
        DesactivarInteractividadBotones();

        GenerarCombateDinamico();
        
        // Arrancamos la transición Pokémon y el combate
        StartCoroutine(TransicionInicialYComienzo());
    }

    IEnumerator TransicionInicialYComienzo()
    {
        // 1. Asegurar que el panel es negro puro al empezar para cubrir la pantalla completa
        if (panelFadeNegro != null)
        {
            panelFadeNegro.gameObject.SetActive(true);
            Color c = panelFadeNegro.color;
            c.a = 1f;
            panelFadeNegro.color = c;
        }

        // Arrancamos la música de combate de fondo
        ReproducirMusica(musicaCombate);

        // 2. Desvanecer el negro lentamente durante 2.5 segundos (de opacidad 100% a 0%)
        float tiempoFade = 0f;
        float duracionFade = 2.5f; 
        
        while (tiempoFade < duracionFade)
        {
            tiempoFade += Time.deltaTime;
            if (panelFadeNegro != null)
            {
                Color c = panelFadeNegro.color;
                c.a = Mathf.Lerp(1f, 0f, tiempoFade / duracionFade);
                panelFadeNegro.color = c;
            }
            yield return null;
        }

        if (panelFadeNegro != null) panelFadeNegro.gameObject.SetActive(false);

        // 3. Esperar un segundo de cortesía y empezar los turnos
        yield return StartCoroutine(ConfigurarCombate());
    }

    void GenerarCombateDinamico()
    {
        if (GameManager.Instance != null)
        {
            mbappe.porcentajeEgo = GameManager.Instance.egoActualMbappe;
        }

        GameObject goEnemigo = new GameObject("Enemigo_Logic");
        
        string tipo = GameManager.Instance != null ? GameManager.Instance.tipoEnemigoActual : "Entrenador";

        if (tipo == "Entrenador") enemigoActual = goEnemigo.AddComponent<Entrenador>();
        else if (tipo == "Jugador") enemigoActual = goEnemigo.AddComponent<JugadorRebelde>();
        else if (tipo == "Tercero") enemigoActual = goEnemigo.AddComponent<DiglettDeMbappe>();
        else if (tipo == "Dictador") enemigoActual = goEnemigo.AddComponent<Dictador>();

        // Asignar los sprites configurados en el Inspector directamente al enemigo
        enemigoActual.spriteVisual = enemigoSpriteVisual;
        enemigoActual.spriteAtaque = enemigoSpriteAtaque;
        enemigoActual.spriteMuerto = enemigoSpriteMuerto;

        if (spriteRendererEnemigo != null && enemigoActual.spriteVisual != null)
        {
            spriteRendererEnemigo.sprite = enemigoActual.spriteVisual;
        }

        // Guardar el sprite inicial de Mbappé en spriteIdle como fallback si no se ha configurado en el Inspector
        if (mbappe != null && mbappe.spriteIdle == null && spriteRendererMbappe != null)
        {
            mbappe.spriteIdle = spriteRendererMbappe.sprite;
        }

        ActualizarTextosHUD();
    }

    IEnumerator ConfigurarCombate()
    {
        mbappe.ActualizarEstadisticasSegunEgo();
        yield return new WaitForSeconds(1f);

        if (mbappe.stats.velocidad >= enemigoActual.stats.velocidad)
            DeterminarTurnoJugador();
        else
            StartCoroutine(TurnoEnemigo());
    }

    void DeterminarTurnoJugador()
    {
        estado = EstadoCombate.TURNO_JUGADOR;
        
        // Habilitar botones de forma interactiva según el Ego en el turno del jugador
        if (botonBasico != null) botonBasico.interactable = true;
        if (botonEgo50 != null)  botonEgo50.interactable = mbappe.PuedeUsarAtaqueMedio();
        if (botonEgo75 != null)  botonEgo75.interactable = mbappe.PuedeUsarAtaqueAvanzado();
        if (botonEgo100 != null) botonEgo100.interactable = mbappe.PuedeUsarAtaqueFinal();
    }

    void DesactivarInteractividadBotones()
    {
        if (botonBasico != null) botonBasico.interactable = false;
        if (botonEgo50 != null)  botonEgo50.interactable = false;
        if (botonEgo75 != null)  botonEgo75.interactable = false;
        if (botonEgo100 != null) botonEgo100.interactable = false;
    }

    // Firma modificada para saber qué ataque de Mbappé se selecciona
    public void SeleccionarAtaque(float potenciaEfecto, int tipoAtaque)
    {
        if (estado != EstadoCombate.TURNO_JUGADOR) return;
        DesactivarInteractividadBotones();

        // 1. Sonido y cambio al sprite de ataque de Mbappé
        PlaySFX(sfxAtaqueMbappe);

        if (spriteRendererMbappe != null && mbappe != null && mbappe.spriteAtaque != null)
        {
            spriteRendererMbappe.sprite = mbappe.spriteAtaque;
        }

        // Calcular el daño pero no parpadear ni bajar barra inmediatamente
        float danio = Mathf.Max(1, (mbappe.stats.ataque * potenciaEfecto) - enemigoActual.stats.defensa);
        enemigoActual.RecibirDanio(danio);

        StartCoroutine(EfectoDanioEnemigo());
    }

    IEnumerator TurnoEnemigo()
    {
        estado = EstadoCombate.TURNO_ENEMIGO;
        DesactivarInteractividadBotones();
        
        yield return new WaitForSeconds(1f);

        // 1. Sonido y cambio al sprite de ataque del enemigo
        PlaySFX(sfxAtaqueEnemigo);

        if (spriteRendererEnemigo != null && enemigoActual != null && enemigoActual.spriteAtaque != null)
        {
            spriteRendererEnemigo.sprite = enemigoActual.spriteAtaque;
        }

        // Aplicar el daño
        enemigoActual.EjecutarTurno(mbappe);

        StartCoroutine(EfectoDanioMbappe());
    }

    IEnumerator EfectoDanioEnemigo()
    {
        // AJUSTE: Esperar a que la imagen de ataque de Mbappé se muestre antes de procesar el impacto
        yield return new WaitForSeconds(duracionAtaqueMbappe);

        // Restaurar el sprite de Mbappé a su estado Idle
        if (spriteRendererMbappe != null && mbappe != null && mbappe.spriteIdle != null)
        {
            spriteRendererMbappe.sprite = mbappe.spriteIdle;
        }

        // 2. Sonido e impacto visual de recibir daño
        PlaySFX(sfxRecibirDanio);

        // Parpadeo visual del sprite receptor (idle) 2 veces más lento
        if (spriteRendererEnemigo != null)
        {
            for (int i = 0; i < 3; i++)
            {
                spriteRendererEnemigo.enabled = false;
                yield return new WaitForSeconds(0.2f);
                spriteRendererEnemigo.enabled = true;
                yield return new WaitForSeconds(0.2f);
            }
        }

        float barraInicio = barraEnemigo != null ? barraEnemigo.fillAmount : 0f;
        float barraDestino = (enemigoActual != null && enemigoActual.stats != null && enemigoActual.stats.vidaMax > 0) ? (float)enemigoActual.stats.vidaActual / enemigoActual.stats.vidaMax : 0f;
        float tiempo = 0f;

        // Descenso de la barra 3 veces más lento al mismo tiempo que el impacto
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * 0.66f;
            if (barraEnemigo != null) barraEnemigo.fillAmount = Mathf.Lerp(barraInicio, barraDestino, tiempo);
            ActualizarTextosHUD();
            yield return null;
        }
        if (barraEnemigo != null) barraEnemigo.fillAmount = barraDestino;

        if (enemigoActual.stats.vidaActual <= 0)
        {
            if (spriteRendererEnemigo != null && enemigoActual.spriteMuerto != null)
                spriteRendererEnemigo.sprite = enemigoActual.spriteMuerto;
            
            PlaySFX(sfxMuerteUniversal);
            yield return new WaitForSeconds(1.5f);
            FinDelCombate(true);
        }
        else
        {
            StartCoroutine(TurnoEnemigo());
        }
    }

    IEnumerator EfectoDanioMbappe()
    {
        // AJUSTE: Esperar a que la imagen de ataque del enemigo se muestre antes de procesar el impacto
        yield return new WaitForSeconds(duracionAtaqueEnemigo);

        // Restaurar el sprite del enemigo a su estado Idle/Visual inicial
        if (spriteRendererEnemigo != null && enemigoActual != null && enemigoActual.spriteVisual != null)
        {
            spriteRendererEnemigo.sprite = enemigoActual.spriteVisual;
        }

        // 2. Sonido e impacto visual de recibir daño
        PlaySFX(sfxRecibirDanio);

        // Parpadeo visual de Mbappé 2 veces más lento
        if (spriteRendererMbappe != null)
        {
            for (int i = 0; i < 3; i++)
            {
                spriteRendererMbappe.enabled = false;
                yield return new WaitForSeconds(0.2f);
                spriteRendererMbappe.enabled = true;
                yield return new WaitForSeconds(0.2f);
            }
        }

        float barraInicio = barraMbappe != null ? barraMbappe.fillAmount : 0f;
        float barraDestino = (mbappe != null && mbappe.stats != null && mbappe.stats.vidaMax > 0) ? (float)mbappe.stats.vidaActual / mbappe.stats.vidaMax : 0f;
        float tiempo = 0f;

        // Descenso de la barra de Mbappé 3 veces más lento al mismo tiempo que el impacto
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * 0.66f;
            if (barraMbappe != null) barraMbappe.fillAmount = Mathf.Lerp(barraInicio, barraDestino, tiempo);
            ActualizarTextosHUD();
            ControlarAlarmaVidaBaja();
            yield return null;
        }
        if (barraMbappe != null) barraMbappe.fillAmount = barraDestino;

        if (mbappe.stats.vidaActual <= 0)
        {
            if (spriteRendererMbappe != null && mbappe.spriteMuerto != null)
                spriteRendererMbappe.sprite = mbappe.spriteMuerto;

            PlaySFX(sfxMuerteUniversal);
            yield return new WaitForSeconds(1.5f);
            FinDelCombate(false);
        }
        else
        {
            DeterminarTurnoJugador();
        }
    }

    void ControlarAlarmaVidaBaja()
    {
        if (mbappe == null || mbappe.stats == null || mbappe.stats.vidaMax <= 0) return;

        float porcentajeVida = (float)mbappe.stats.vidaActual / mbappe.stats.vidaMax;
        
        if (porcentajeVida <= 0.25f && porcentajeVida > 0 && !alarmaSonando)
        {
            alarmaSonando = true;
            if (fuenteAlarmaVidaBaja != null && sfxAlarmaLoop != null)
            {
                fuenteAlarmaVidaBaja.clip = sfxAlarmaLoop;
                fuenteAlarmaVidaBaja.loop = true;
                fuenteAlarmaVidaBaja.Play();
            }
        }
        else if ((porcentajeVida > 0.25f || porcentajeVida <= 0) && alarmaSonando)
        {
            ApagarAlarma();
        }
    }

    void ApagarAlarma()
    {
        alarmaSonando = false;
        if (fuenteAlarmaVidaBaja != null)
        {
            fuenteAlarmaVidaBaja.Stop();
        }
    }

    void ReproducirMusica(AudioClip clipMusica)
    {
        if (fuenteMusica != null && clipMusica != null)
        {
            fuenteMusica.clip = clipMusica;
            fuenteMusica.loop = true;
            fuenteMusica.Play();
        }
    }

    void PlaySFX(AudioClip clip)
    {
        if (fuenteSFX != null && clip != null)
        {
            fuenteSFX.PlayOneShot(clip);
        }
    }

    void ActualizarTextosHUD()
    {
        if (textoMbappe != null && mbappe != null && mbappe.stats != null)
        {
            textoMbappe.text = $"Mbappé: {Mathf.Max(0, mbappe.stats.vidaActual)} / {mbappe.stats.vidaMax}";
        }
        
        if (textoEnemigo != null && enemigoActual != null && enemigoActual.stats != null)
        {
            string nombre = string.IsNullOrEmpty(enemigoActual.nombreEnemigo) ? "Enemigo" : enemigoActual.nombreEnemigo;
            textoEnemigo.text = $"{nombre}: {Mathf.Max(0, enemigoActual.stats.vidaActual)} / {enemigoActual.stats.vidaMax}";
        }
    }

    void FinDelCombate(bool ganoMbappe)
    {
        ganoJugadorEnEsteCombate = ganoMbappe;
        estado = ganoMbappe ? EstadoCombate.VICTORIA : EstadoCombate.DERROTA;
        
        // Bloquear los botones de inmediato al concluir
        DesactivarInteractividadBotones();

        // 1. Apagar la alarma en todos los casos
        ApagarAlarma();

        // 2. Si ganamos, apagamos inmediatamente la música tensa de combate
        // Si perdemos, la música de combate sigue sonando durante el diálogo
        if (ganoMbappe)
        {
            if (fuenteMusica != null) fuenteMusica.Stop();
        }

        // 3. Activamos el panel de diálogo final
        if (panelDialogoFin != null)
        {
            panelDialogoFin.SetActive(true);
            pasoConversacion = 0; // Paso 0 para anunciar el resultado
            conversacionActiva = true;

            // REGLA: Reproducir la música de victoria INMEDIATAMENTE al aparecer la pantalla de victoria (Paso 0)
            if (ganoMbappe)
            {
                ReproducirMusica(musicaVictoria);
            }

            MostrarSiguienteFrase();
        }
        else
        {
            FinalizarEscenaCombate();
        }
    }

    void Update()
    {
        if (conversacionActiva)
        {
            bool haPulsadoClick = false;
            bool haPulsadoTeclado = false;

            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                haPulsadoClick = true;
            }

            if (UnityEngine.InputSystem.Keyboard.current != null && 
               (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame || 
                UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame))
            {
                haPulsadoTeclado = true;
            }

            if (haPulsadoClick || haPulsadoTeclado)
            {
                pasoConversacion++;
                MostrarSiguienteFrase();
            }
        }
    }

    void MostrarSiguienteFrase()
    {
        string nombreEnemigoUpper = enemigoActual != null ? enemigoActual.nombreEnemigo.ToUpper() : "ENEMIGO";

        if (ganoJugadorEnEsteCombate)
        {
            // --- FLUJO EN CASO DE VICTORIA DE MBAPPÉ ---
            if (pasoConversacion == 0)
            {
                // Muestra el anuncio de victoria. La música de victoria ya está sonando desde FinDelCombate()
                if (textoDialogoFin != null)
                    textoDialogoFin.text = "<color=green><b>¡VICTORIA!</b></color>\nMbappé ha ganado el combate.";
            }
            else if (pasoConversacion == 1)
            {
                if (textoDialogoFin != null && enemigoActual != null)
                    textoDialogoFin.text = $"<b>{nombreEnemigoUpper}:</b>\n{enemigoActual.fraseEnemigoDerrotado}";
            }
            else if (pasoConversacion == 2)
            {
                if (textoDialogoFin != null && enemigoActual != null)
                    textoDialogoFin.text = $"<b>MBAPPÉ:</b>\n{enemigoActual.replicaMbappeVictoria}";
            }
            else
            {
                FinalizarEscenaCombate();
            }
        }
        else
        {
            // --- FLUJO EN CASO DE DERROTA DE MBAPPÉ ---
            if (pasoConversacion == 0)
            {
                // Muestra el anuncio de derrota
                if (textoDialogoFin != null)
                    textoDialogoFin.text = "<color=red><b>¡DERROTA!</b></color>\nMbappé ha mordido el polvo.";
            }
            else if (pasoConversacion == 1)
            {
                if (textoDialogoFin != null && enemigoActual != null)
                    textoDialogoFin.text = $"<b>{nombreEnemigoUpper}:</b>\n{enemigoActual.fraseEnemigoVictorioso}";
            }
            else if (pasoConversacion == 2)
            {
                if (textoDialogoFin != null && enemigoActual != null)
                    textoDialogoFin.text = $"<b>MBAPPÉ:</b>\n{enemigoActual.replicaMbappeDerrota}";
            }
            else
            {
                FinalizarEscenaCombate();
            }
        }
    }

    void FinalizarEscenaCombate()
    {
        conversacionActiva = false;
        if (panelDialogoFin != null) panelDialogoFin.SetActive(false);

        // Si perdimos, detenemos la música de combate ahora al salir de la pantalla de diálogo
        if (!ganoJugadorEnEsteCombate)
        {
            if (fuenteMusica != null) fuenteMusica.Stop();
        }

        if (ganoJugadorEnEsteCombate)
            Debug.Log("Transición: Cargar cinemática o siguiente nivel.");
        else
            Debug.Log("Transición: Cargar pantalla de GAME OVER.");
    }
}