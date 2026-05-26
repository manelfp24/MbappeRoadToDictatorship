using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; // Necesario para detectar las escenas actuales

public class CombatManager : MonoBehaviour
{
    public MbappeStats mbappe;
    public EnemigoBase enemigoActual; 
    
    public Transform posicionMbappe;
    public Transform posicionEnemigo;

    public enum EstadoCombate { INICIO, TURNO_JUGADOR, TURNO_ENEMIGO, VICTORIA, DERROTA, PROCESANDO_ACCION }
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
    public SpriteRenderer spriteRendererFondo; // El fondo de combate que cambiará según la escena

    [Header("UI de Diálogo (Combate y Fin)")]
    public GameObject panelDialogoFin; // El panel inferior de texto
    public TextMeshProUGUI textoDialogoFin; // El texto dentro del panel

    [Header("Efecto de Transición Inicial")]
    public Image panelFadeNegro; 

    [Header("Audio - Fuentes")]
    public AudioSource fuenteMusica;
    public AudioSource fuenteSFX;
    public AudioSource fuenteAlarmaVidaBaja;

    [Header("Audio - Clips de Sonido")]
    public AudioClip musicaCombate;
    public AudioClip sfxAtaqueMbappe;
    public AudioClip sfxAtaqueEnemigo; 
    public AudioClip sfxRecibirDanio;
    public AudioClip sfxAlarmaLoop;
    public AudioClip sfxMuerteUniversal;
    public AudioClip musicaVictoria; 

    [Header("Imágenes Estáticas - Duración de Mostrar Ataque")]
    public float duracionAtaqueMbappe = 1.5f;   // Tiempo para leer el ataque de Mbappé
    public float duracionAtaqueEnemigo = 1.5f;  // Tiempo para leer el ataque del enemigo

    [Header("Configuración de Enemigos")]
    public DatosEnemigoEditor[] enemigosConfig;

    [Header("Pruebas en Editor")]
    public bool forzarEgoDelInspector = false; // Si se activa, no se sobrescribe el Ego con el del GameManager

    private bool conversacionActiva = false;
    private int pasoConversacion = 0;
    private bool ganoJugadorEnEsteCombate = false;
    private bool alarmaSonando = false;
    private Vector3 escalaBaseEnemigo; // Guarda la escala nativa del transform en la escena del GameObject de enemigo
    private Vector3 posicionBaseOriginalEnemigo; // Guarda la posición local nativa del transform en la escena del GameObject de enemigo
    private Vector3 posicionBaseOriginalFondo; // Guarda la posición local nativa del fondo de combate en la escena

    void Start()
    {
        estado = EstadoCombate.INICIO;
        
        // Autodetectar Mbappé por si se desvinculó en el Inspector por error
        if (mbappe == null)
        {
            mbappe = FindAnyObjectByType<MbappeStats>();
            if (mbappe == null)
            {
                Debug.LogWarning("[CombatManager] No se encontró MbappeStats en la escena. Creando una instancia temporal de emergencia.");
                GameObject goMbappe = new GameObject("Mbappe_Emergencia");
                mbappe = goMbappe.AddComponent<MbappeStats>();
            }
        }

        if (mbappe != null && mbappe.stats == null)
        {
            mbappe.stats = new Estadisticas();
            mbappe.ActualizarEstadisticasSegunEgo();
        }
        
        if (spriteRendererEnemigo != null)
        {
            escalaBaseEnemigo = spriteRendererEnemigo.transform.localScale;
            posicionBaseOriginalEnemigo = spriteRendererEnemigo.transform.localPosition;
        }
        
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

        DesactivarInteractividadBotones();
        GenerarCombateDinamico();
        
        StartCoroutine(TransicionInicialYComienzo());
    }

    IEnumerator TransicionInicialYComienzo()
    {
        if (panelFadeNegro != null)
        {
            panelFadeNegro.gameObject.SetActive(true);
            Color c = panelFadeNegro.color;
            c.a = 1f;
            panelFadeNegro.color = c;
        }

        ReproducirMusica(musicaCombate);

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

        yield return StartCoroutine(ConfigurarCombate());
    }

    void GenerarCombateDinamico()
    {
        if (mbappe != null)
        {
            if (GameManager.Instance != null && !forzarEgoDelInspector)
            {
                mbappe.porcentajeEgo = GameManager.Instance.egoActualMbappe;
            }
            mbappe.ActualizarEstadisticasSegunEgo();
        }

        string tipo = "Entrenador";

        // 1. Si ya arrastraste un enemigo al Inspector de forma manual, usamos ese y saltamos la detección automática
        if (enemigoActual != null)
        {
            Debug.Log($"[CombatManager] Usando enemigo configurado manualmente: {enemigoActual.gameObject.name}");
            
            // Si es un prefab de proyecto (no está instanciado en la escena), lo instanciamos
            if (enemigoActual.gameObject.scene.name == null)
            {
                Vector3 pos = posicionEnemigo != null ? posicionEnemigo.position : Vector3.zero;
                EnemigoBase instancia = Instantiate(enemigoActual, pos, Quaternion.identity);
                enemigoActual = instancia;
            }

            // Deducimos el identificador a partir del tipo de componente de C# asignado
            if (enemigoActual is Entrenador) tipo = "Entrenador";
            else if (enemigoActual is JugadorRebelde) tipo = "Jugador";
            else if (enemigoActual is DiglettDeMbappe) tipo = "Tercero";
            else if (enemigoActual is Dictador) tipo = "Dictador";
        }
        else
        {
            // Si está vacío (None), se genera dinámicamente y se detecta el jefe según la escena activa
            GameObject goEnemigo = new GameObject("Enemigo_Logic");
            string nombreEscenaActual = SceneManager.GetActiveScene().name;

            if (nombreEscenaActual == "Valdebebas")
            {
                enemigoActual = goEnemigo.AddComponent<Entrenador>(); // Xabi Alonso
                tipo = "Entrenador";
            }
            else if (nombreEscenaActual == "Recibidor")
            {
                enemigoActual = goEnemigo.AddComponent<JugadorRebelde>(); // Vinicius
                tipo = "Jugador";
            }
            else if (nombreEscenaActual == "Pasillo")
            {
                enemigoActual = goEnemigo.AddComponent<DiglettDeMbappe>(); // Diglett
                tipo = "Tercero";
            }
            else if (nombreEscenaActual == "Despacho")
            {
                enemigoActual = goEnemigo.AddComponent<Dictador>(); // Florentino Pérez
                tipo = "Dictador";
            }
            else
            {
                // Fallback por si ejecutas directamente la escena aislada "EscenaCombate" o "SampleScene"
                tipo = GameManager.Instance != null ? GameManager.Instance.tipoEnemigoActual : "Entrenador";
                string tipoLower = tipo.Trim().ToLower();
                if (tipoLower == "entrenador") enemigoActual = goEnemigo.AddComponent<Entrenador>();
                else if (tipoLower == "jugador") enemigoActual = goEnemigo.AddComponent<JugadorRebelde>();
                else if (tipoLower == "tercero" || tipoLower == "diglett") enemigoActual = goEnemigo.AddComponent<DiglettDeMbappe>();
                else if (tipoLower == "dictador" || tipoLower == "florentino") enemigoActual = goEnemigo.AddComponent<Dictador>();
                else enemigoActual = goEnemigo.AddComponent<Entrenador>(); // Fallback por defecto
            }
        }

        // Asegurar que enemigoActual y sus stats no sean nulos
        if (enemigoActual != null && enemigoActual.stats == null)
        {
            Debug.LogWarning($"[CombatManager] enemigoActual.stats era nulo para {enemigoActual.nombreEnemigo}. Inicializando estadísticas de emergencia.");
            enemigoActual.stats = new Estadisticas();
            if (enemigoActual is Dictador)
            {
                enemigoActual.stats.vidaMax = 99; enemigoActual.stats.vidaActual = 99;
                enemigoActual.stats.ataque = 30; enemigoActual.stats.defensa = 20; enemigoActual.stats.velocidad = 100;
            }
            else if (enemigoActual is DiglettDeMbappe)
            {
                enemigoActual.stats.vidaMax = 15; enemigoActual.stats.vidaActual = 15;
                enemigoActual.stats.ataque = 0; enemigoActual.stats.defensa = 15; enemigoActual.stats.velocidad = 100;
            }
            else if (enemigoActual is JugadorRebelde)
            {
                enemigoActual.stats.vidaMax = 45; enemigoActual.stats.vidaActual = 45;
                enemigoActual.stats.ataque = 12; enemigoActual.stats.defensa = 10; enemigoActual.stats.velocidad = 100;
            }
            else
            {
                enemigoActual.stats.vidaMax = 20; enemigoActual.stats.vidaActual = 20;
                enemigoActual.stats.ataque = 8; enemigoActual.stats.defensa = 5; enemigoActual.stats.velocidad = 100;
            }
        }

        // 2. Buscamos en la lista de configuración del Inspector los sprites que corresponden a este "tipo"
        DatosEnemigoEditor configEncontrada = null;
        if (enemigosConfig != null && enemigoActual != null)
        {
            foreach (var config in enemigosConfig)
            {
                if (config != null)
                {
                    string idConfig = config.nombreIdentificador.Trim().ToLower();
                    string idTipo = tipo.Trim().ToLower();
                    
                    bool esMismoEnemigo = (idConfig == idTipo);
                    
                    // Comprobaciones alternativas para el tercer enemigo (Diglett de Mbappé)
                    if (!esMismoEnemigo && (idTipo == "tercero" || idTipo == "diglett" || idTipo == "diglettdembappe" || idTipo == "diglett de mbappé"))
                    {
                        if (idConfig == "tercero" || idConfig == "diglett" || idConfig == "diglettdembappe" || idConfig == "diglett de mbappé")
                        {
                            esMismoEnemigo = true;
                        }
                    }
                    
                    // Comprobaciones alternativas para Jugador (Vinicius)
                    if (!esMismoEnemigo && (idTipo == "jugador" || idTipo == "vinicius" || idTipo == "jugadorrebelde" || idTipo == "vinicius jr."))
                    {
                        if (idConfig == "jugador" || idConfig == "vinicius" || idConfig == "jugadorrebelde" || idConfig == "vinicius jr.")
                        {
                            esMismoEnemigo = true;
                        }
                    }

                    // Comprobaciones alternativas para Entrenador (Xabi Alonso)
                    if (!esMismoEnemigo && (idTipo == "entrenador" || idTipo == "xabi" || idTipo == "xabi alonso"))
                    {
                        if (idConfig == "entrenador" || idConfig == "xabi" || idConfig == "xabi alonso")
                        {
                            esMismoEnemigo = true;
                        }
                    }

                    // Comprobaciones alternativas para Dictador (Florentino Pérez)
                    if (!esMismoEnemigo && (idTipo == "dictador" || idTipo == "florentino" || idTipo == "florentino pérez" || idTipo == "florentino perez"))
                    {
                        if (idConfig == "dictador" || idConfig == "florentino" || idConfig == "florentino pérez" || idConfig == "florentino perez")
                        {
                            esMismoEnemigo = true;
                        }
                    }

                    if (esMismoEnemigo)
                    {
                        enemigoActual.spriteVisual = config.spriteVisual;
                        enemigoActual.spriteAtaque = config.spriteAtaque;
                        enemigoActual.spriteMuerto = config.spriteMuerto;
                        configEncontrada = config;
                        break;
                    }
                }
            }
        }

        // 3. Aplicamos el sprite visual inicial en el SpriteRenderer y ajustamos la escala
        if (spriteRendererEnemigo != null && enemigoActual != null)
        {
            if (enemigoActual.spriteVisual != null)
            {
                spriteRendererEnemigo.sprite = enemigoActual.spriteVisual;
            }

            // Aplicamos la escala desde la configuración si se especificó y no es (0,0,0)
            if (configEncontrada != null && configEncontrada.escalaVisual != Vector3.zero)
            {
                spriteRendererEnemigo.transform.localScale = configEncontrada.escalaVisual;
            }
            else
            {
                // Fallback por defecto si no está configurada la escala en el Inspector
                if (enemigoActual is Entrenador || enemigoActual is JugadorRebelde)
                {
                    // Reducimos la escala en 1 punto con respecto a la escala base del transform
                    spriteRendererEnemigo.transform.localScale = new Vector3(
                        escalaBaseEnemigo.x - 1f,
                        escalaBaseEnemigo.y - 1f,
                        escalaBaseEnemigo.z - 1f
                    );
                }
                else
                {
                    // Mantenemos la escala base original de la escena
                    spriteRendererEnemigo.transform.localScale = escalaBaseEnemigo;
                }
            }

            // Aplicamos la posición con offset desde la configuración si se especificó
            if (configEncontrada != null)
            {
                spriteRendererEnemigo.transform.localPosition = posicionBaseOriginalEnemigo + configEncontrada.posicionOffset;
            }
            else
            {
                spriteRendererEnemigo.transform.localPosition = posicionBaseOriginalEnemigo;
            }

            // Alertas detalladas en consola si faltan asignaciones en Unity
            if (enemigoActual.spriteVisual == null)
            {
                Debug.LogWarning($"[CombatManager] El sprite visual (Idle) para '{enemigoActual.nombreEnemigo}' es nulo. Por favor, asígnalo en 'enemigosConfig' en el Inspector (ID: '{tipo}').");
            }
            if (enemigoActual.spriteAtaque == null)
            {
                Debug.LogWarning($"[CombatManager] El sprite de ataque para '{enemigoActual.nombreEnemigo}' es nulo. Por favor, asígnalo en 'enemigosConfig' en el Inspector.");
            }
            if (enemigoActual.spriteMuerto == null)
            {
                Debug.LogWarning($"[CombatManager] El sprite muerto para '{enemigoActual.nombreEnemigo}' es nulo. Por favor, asígnalo en 'enemigosConfig' en el Inspector.");
            }
        }

        // Buscar el objeto FondoCombate de forma automática en la escena si no se ha asignado en el Inspector
        if (spriteRendererFondo == null)
        {
            GameObject goFondo = GameObject.Find("FondoCombate");
            if (goFondo != null)
            {
                spriteRendererFondo = goFondo.GetComponent<SpriteRenderer>();
            }
        }

        if (spriteRendererFondo != null)
        {
            posicionBaseOriginalFondo = spriteRendererFondo.transform.localPosition;
        }

        // Aplicamos el fondo de combate y la música correspondiente si se encontró la configuración
        if (configEncontrada != null)
        {
            if (spriteRendererFondo != null)
            {
                if (configEncontrada.spriteFondo != null)
                {
                    spriteRendererFondo.sprite = configEncontrada.spriteFondo;
                }
                
                // Aplicamos la posición con offset desde la configuración para el fondo
                spriteRendererFondo.transform.localPosition = posicionBaseOriginalFondo + configEncontrada.posicionFondoOffset;
            }
            
            if (configEncontrada.musicaCombate != null)
            {
                musicaCombate = configEncontrada.musicaCombate;
            }
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

        // Al inicio del combate, el jugador siempre tiene el control para elegir su acción primero
        DeterminarTurnoJugador();
    }

    void DeterminarTurnoJugador()
    {
        estado = EstadoCombate.TURNO_JUGADOR;
        if (panelDialogoFin != null) panelDialogoFin.SetActive(false); // Ocultar textos de combate para elegir acción
        
        if (botonBasico != null) botonBasico.interactable = true;
        if (botonEgo50 != null)  botonEgo50.interactable = mbappe.PuedeUsarAtaqueMedio(); // Ajustado a la función real de MbappeStats
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

    // El jugador pulsa un botón
    public void SeleccionarAtaque(float potenciaEfecto, int tipoAtaque)
    {
        if (estado != EstadoCombate.TURNO_JUGADOR) return;
        
        // Bloquear UI para evitar dobles clics
        estado = EstadoCombate.PROCESANDO_ACCION;
        DesactivarInteractividadBotones();

        // Traducir ID a nombre del ataque de Mbappé
        string nombreAtaque = "Ataque en Rueda de Prensa";
        if (tipoAtaque == 2) nombreAtaque = "Mirada Fulminante";
        if (tipoAtaque == 3) nombreAtaque = "Golpe Caparazón";
        if (tipoAtaque == 4) nombreAtaque = "Golpe de Estado";

        // Lanzar la secuencia completa del turno
        StartCoroutine(EjecutarTurnoCompleto(potenciaEfecto, nombreAtaque, tipoAtaque));
    }

    // Corrutina controladora del flujo del turno (estilo Pokémon)
    IEnumerator EjecutarTurnoCompleto(float potenciaEfecto, string nombreAtaque, int tipoAtaque)
    {
        // Salvaguarda robusta para evitar NullReferenceException
        if (mbappe == null || mbappe.stats == null || enemigoActual == null || enemigoActual.stats == null)
        {
            Debug.LogError("[CombatManager] Error crítico: mbappe, enemigoActual o sus estadísticas son nulas en EjecutarTurnoCompleto.");
            DeterminarTurnoJugador();
            yield break;
        }

        // El enemigo (velocidad 100) suele ser más rápido que Mbappé (velocidad <= 90)
        if (mbappe.stats.velocidad >= enemigoActual.stats.velocidad)
        {
            // --- CASO 1: Mbappé es más rápido ---
            yield return StartCoroutine(EjecutarAccionJugador(potenciaEfecto, nombreAtaque, tipoAtaque));

            // Si el enemigo muere, termina el turno y el combate con victoria
            if (enemigoActual.stats.vidaActual <= 0)
            {
                FinDelCombate(true);
                yield break;
            }

            yield return StartCoroutine(EjecutarAccionEnemigo());

            // Si Mbappé muere, termina el turno y el combate con derrota
            if (mbappe.stats.vidaActual <= 0)
            {
                FinDelCombate(false);
                yield break;
            }
        }
        else
        {
            // --- CASO 2: El enemigo es más rápido (caso por defecto) ---
            yield return StartCoroutine(EjecutarAccionEnemigo());

            // Si Mbappé muere, termina el turno y el combate con derrota
            if (mbappe.stats.vidaActual <= 0)
            {
                FinDelCombate(false);
                yield break;
            }

            yield return StartCoroutine(EjecutarAccionJugador(potenciaEfecto, nombreAtaque, tipoAtaque));

            // Si el enemigo muere, termina el turno y el combate con victoria
            if (enemigoActual.stats.vidaActual <= 0)
            {
                FinDelCombate(true);
                yield break;
            }
        }

        // Si ambos siguen vivos al terminar el turno, se vuelve a dar el turno al jugador
        DeterminarTurnoJugador();
    }

    // ACCIÓN DEL JUGADOR: Mbappé ataca (fase de lanzamiento y daño en el enemigo)
    IEnumerator EjecutarAccionJugador(float potenciaEfecto, string nombreAtaque, int tipoAtaque)
    {
        // 1. FASE DE LANZAMIENTO (1.5 segundos)
        if (panelDialogoFin != null) panelDialogoFin.SetActive(true);
        if (textoDialogoFin != null) textoDialogoFin.text = $"<b>Mbappé</b> ha usado <b><color=#FFFF00>{nombreAtaque}</color></b>!";

        PlaySFX(sfxAtaqueMbappe);

        if (spriteRendererMbappe != null && mbappe != null && mbappe.spriteAtaque != null)
        {
            spriteRendererMbappe.sprite = mbappe.spriteAtaque;
        }

        // Calcular daño base en segundo plano
        float danio = Mathf.Max(1, (mbappe.stats.ataque * potenciaEfecto) - enemigoActual.stats.defensa);

        // --- REGLAS DE DAÑO ESPECÍFICAS DE COMBATE ---
        if (enemigoActual is JugadorRebelde && tipoAtaque == 2)
        {
            // Vinicius muere instantáneamente si Mbappé usa el ataque de 50% de ego
            danio = enemigoActual.stats.vidaActual;
        }
        else if (enemigoActual is DiglettDeMbappe)
        {
            // Cualquier ataque a Diglett le hace one shot
            danio = enemigoActual.stats.vidaActual;
        }
        else if (enemigoActual is Dictador)
        {
            // Reglas contra Florentino Pérez
            if (tipoAtaque == 4) danio = enemigoActual.stats.vidaActual; // One shot
            else if (tipoAtaque == 3) danio = Mathf.Round(enemigoActual.stats.vidaMax * 0.33f);
            else if (tipoAtaque == 2) danio = Mathf.Round(enemigoActual.stats.vidaMax * 0.25f);
            else if (tipoAtaque == 1) danio = Mathf.Round(enemigoActual.stats.vidaMax * 0.10f);
        }

        enemigoActual.RecibirDanio(danio);

        yield return new WaitForSeconds(duracionAtaqueMbappe); // Espera leyendo el ataque

        // Restaurar sprite atacante
        if (spriteRendererMbappe != null && mbappe != null && mbappe.spriteIdle != null)
        {
            spriteRendererMbappe.sprite = mbappe.spriteIdle;
        }

        // 2. FASE DE IMPACTO Y DAÑO (1.5 segundos)
        string nombreRival = string.IsNullOrEmpty(enemigoActual.nombreEnemigo) ? "El enemigo" : enemigoActual.nombreEnemigo;
        if (textoDialogoFin != null) textoDialogoFin.text = $"¡<b>{nombreRival}</b> ha recibido mucho daño!";

        // Se elimina el cambio al sprite de daño, se parpadea directamente en su sprite de reposo inicial estilo Pokémon
        PlaySFX(sfxRecibirDanio);

        // Parpadeo y descenso de barra en paralelo
        StartCoroutine(ParpadearSprite(spriteRendererEnemigo));
        yield return StartCoroutine(DescenderBarraVida(barraEnemigo, enemigoActual.stats.vidaActual, enemigoActual.stats.vidaMax, false));

        // Comprobación de muerte del enemigo
        if (enemigoActual.stats.vidaActual <= 0)
        {
            if (spriteRendererEnemigo != null && enemigoActual.spriteMuerto != null)
                spriteRendererEnemigo.sprite = enemigoActual.spriteMuerto;
            
            PlaySFX(sfxMuerteUniversal);
            yield return new WaitForSeconds(1.5f);
        }
    }

    // ACCIÓN DEL ENEMIGO: El enemigo ataca (fase de lanzamiento y daño en Mbappé)
    IEnumerator EjecutarAccionEnemigo()
    {
        yield return new WaitForSeconds(0.5f); // Pausa de transición corta

        // 1. FASE DE LANZAMIENTO ENEMIGO (1.5 segundos)
        string nombreRival = string.IsNullOrEmpty(enemigoActual.nombreEnemigo) ? "Enemigo" : enemigoActual.nombreEnemigo;
        string nombreAtaqueRival = string.IsNullOrEmpty(enemigoActual.nombreAtaqueEnemigo) ? "Ataque" : enemigoActual.nombreAtaqueEnemigo;
        
        if (panelDialogoFin != null) panelDialogoFin.SetActive(true);
        if (textoDialogoFin != null) textoDialogoFin.text = $"¡<b>{nombreRival}</b> ha usado <b><color=#FF5555>{nombreAtaqueRival}</color></b>!";

        PlaySFX(sfxAtaqueEnemigo);

        if (spriteRendererEnemigo != null && enemigoActual != null && enemigoActual.spriteAtaque != null)
        {
            spriteRendererEnemigo.sprite = enemigoActual.spriteAtaque;
        }

        // El enemigo ejecuta su lógica y aplica daño a Mbappé
        enemigoActual.EjecutarTurno(mbappe);

        yield return new WaitForSeconds(duracionAtaqueEnemigo); // Espera leyendo el ataque enemigo

        // Restaurar sprite del enemigo
        if (spriteRendererEnemigo != null && enemigoActual.spriteVisual != null)
        {
            spriteRendererEnemigo.sprite = enemigoActual.spriteVisual;
        }

        // 2. FASE DE IMPACTO EN MBAPPÉ (1.5 segundos)
        if (enemigoActual is DiglettDeMbappe)
        {
            if (textoDialogoFin != null) textoDialogoFin.text = "Mbappé se ha descargado";
            // Contra diglett no hay efecto de parpadeo ni sonido de daño ya que no hace daño alguno
            yield return StartCoroutine(DescenderBarraVida(barraMbappe, mbappe.stats.vidaActual, mbappe.stats.vidaMax, true));
        }
        else
        {
            if (textoDialogoFin != null) textoDialogoFin.text = $"¡<b>Mbappé</b> ha recibido mucho daño!";
            
            // Se elimina el cambio al sprite de daño, se parpadea directamente en su sprite de reposo inicial estilo Pokémon
            PlaySFX(sfxRecibirDanio);
            
            // Parpadeo y descenso de barra en paralelo
            StartCoroutine(ParpadearSprite(spriteRendererMbappe));
            yield return StartCoroutine(DescenderBarraVida(barraMbappe, mbappe.stats.vidaActual, mbappe.stats.vidaMax, true));
        }

        // Comprobación de muerte de Mbappé
        if (mbappe.stats.vidaActual <= 0)
        {
            if (spriteRendererMbappe != null && mbappe.spriteMuerto != null)
                spriteRendererMbappe.sprite = mbappe.spriteMuerto;

            PlaySFX(sfxMuerteUniversal);
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            // Restaurar Mbappé a reposo/idle
            if (spriteRendererMbappe != null && mbappe.spriteIdle != null)
            {
                spriteRendererMbappe.sprite = mbappe.spriteIdle;
            }
        }
    }

    // Sub-corrutina para el efecto visual de parpadeo
    IEnumerator ParpadearSprite(SpriteRenderer sr)
    {
        if (sr == null) yield break;
        for (int i = 0; i < 3; i++)
        {
            sr.enabled = false;
            yield return new WaitForSeconds(0.2f);
            sr.enabled = true;
            yield return new WaitForSeconds(0.2f);
        }
    }

    // Sub-corrutina para animar de forma lenta la barra de vida y textos del HUD (bajando de 1 en 1)
    IEnumerator DescenderBarraVida(Image barra, float vidaActualDestino, float vidaMax, bool esMbappe)
    {
        if (barra == null || vidaMax <= 0) yield break;

        float barraInicio = barra.fillAmount;
        // Forzamos el cálculo en float puro
        float barraDestino = Mathf.Clamp01((float)vidaActualDestino / (float)vidaMax); 
        
        float vidaInicio = barraInicio * vidaMax;
        float tiempo = 0f;

        // Si la barra ya está en el destino, no hacemos nada para evitar bloqueos
        if (Mathf.Approximately(barraInicio, barraDestino)) yield break;

        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * 0.66f; // Duración aproximada de 1.5s
            
            float fillActual = Mathf.Lerp(barraInicio, barraDestino, tiempo);
            barra.fillAmount = fillActual;

            // Calculamos el valor numérico que se va mostrando en el texto paso a paso
            float vidaInterpolada = Mathf.Lerp(vidaInicio, vidaActualDestino, tiempo);

            // Actualizamos el HUD pasando de forma correcta el valor animado y el valor estático del otro personaje
            if (esMbappe)
            {
                // Animamos a Mbappé, el enemigo se queda con su vida actual de sus stats
                ActualizarTextosHUDConValores(vidaInterpolada, enemigoActual.stats.vidaActual);
            }
            else
            {
                // Animamos al enemigo, Mbappé se queda con su vida actual de sus stats
                ActualizarTextosHUDConValores(mbappe.stats.vidaActual, vidaInterpolada);
            }

            ControlarAlarmaVidaBaja();
            yield return null;
        }

        // Aseguramos el valor final exacto al terminar la animación
        barra.fillAmount = barraDestino;
        ActualizarTextosHUD();
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
        if (fuenteAlarmaVidaBaja != null) fuenteAlarmaVidaBaja.Stop();
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
        if (mbappe == null || enemigoActual == null) return;
        ActualizarTextosHUDConValores(mbappe.stats.vidaActual, enemigoActual.stats.vidaActual);
        
        // Sincronizar también las barras de vida físicas del HUD con los valores reales
        if (barraMbappe != null && mbappe.stats != null && mbappe.stats.vidaMax > 0)
        {
            barraMbappe.fillAmount = Mathf.Clamp01((float)mbappe.stats.vidaActual / (float)mbappe.stats.vidaMax);
        }
        if (barraEnemigo != null && enemigoActual.stats != null && enemigoActual.stats.vidaMax > 0)
        {
            barraEnemigo.fillAmount = Mathf.Clamp01((float)enemigoActual.stats.vidaActual / (float)enemigoActual.stats.vidaMax);
        }
    }

    void ActualizarTextosHUDConValores(float vidaMostrarMbappe, float vidaMostrarEnemigo)
    {
        if (textoMbappe != null && mbappe != null && mbappe.stats != null)
        {
            textoMbappe.text = $"Mbappé: {Mathf.Max(0, Mathf.RoundToInt(vidaMostrarMbappe))} / {mbappe.stats.vidaMax}";
        }
        
        if (textoEnemigo != null && enemigoActual != null && enemigoActual.stats != null)
        {
            string nombre = string.IsNullOrEmpty(enemigoActual.nombreEnemigo) ? "Enemigo" : enemigoActual.nombreEnemigo;
            textoEnemigo.text = $"{nombre}: {Mathf.Max(0, Mathf.RoundToInt(vidaMostrarEnemigo))} / {enemigoActual.stats.vidaMax}";
        }
    }

    void FinDelCombate(bool ganoMbappe)
    {
        ganoJugadorEnEsteCombate = ganoMbappe;
        estado = ganoMbappe ? EstadoCombate.VICTORIA : EstadoCombate.DERROTA;
        
        DesactivarInteractividadBotones();
        ApagarAlarma();

        if (ganoMbappe && fuenteMusica != null) 
            fuenteMusica.Stop();

        if (panelDialogoFin != null)
        {
            panelDialogoFin.SetActive(true);
            pasoConversacion = 0; 
            conversacionActiva = true;

            if (ganoMbappe) ReproducirMusica(musicaVictoria);

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
                haPulsadoClick = true;

            if (UnityEngine.InputSystem.Keyboard.current != null && 
               (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame || 
                UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame))
                haPulsadoTeclado = true;

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
            if (pasoConversacion == 0)
            {
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
            if (pasoConversacion == 0)
            {
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

        if (!ganoJugadorEnEsteCombate && fuenteMusica != null) 
            fuenteMusica.Stop();

        // Si el combate concluyó con victoria, actualizamos el paso en la escena de origen correspondiente, de lo contrario reiniciamos su progreso
        if (GameManager.Instance != null)
        {
            if (ganoJugadorEnEsteCombate)
            {
                string enemigoVencido = GameManager.Instance.tipoEnemigoActual.Trim().ToLower();
                if (enemigoVencido == "dictador" || enemigoVencido == "florentino")
                {
                    GameManager.Instance.pasoDespacho = 1; // Pasamos al paso 1 (diálogo final tras derrotar a Florentino)
                }
                else if (enemigoVencido == "jugador" || enemigoVencido == "vinicius" || enemigoVencido == "jugadorrebelde")
                {
                    GameManager.Instance.pasoRecibidor = 2; // Pasamos al paso 2 (diálogo post-combate en el Recibidor)
                }
                else if (enemigoVencido == "entrenador" || enemigoVencido == "xabi" || enemigoVencido == "xabi alonso")
                {
                    GameManager.Instance.pasoValdebebas = 2; // Pasamos al paso 2 (diálogo post-combate en Valdebebas)
                }
                else if (enemigoVencido == "tercero" || enemigoVencido == "diglett" || enemigoVencido == "diglettdembappe")
                {
                    GameManager.Instance.pasoPasillo = 2; // Pasamos al paso 2 (diálogo post-combate en el Pasillo)
                }
            }
            else
            {
                // Si el jugador pierde, reiniciamos el progreso de la escena de origen a 0 y cancelamos el retorno posicional
                GameManager.Instance.usarPosicionRetornoCombate = false;

                string escenaOrigen = GameManager.Instance.escenaOrigenCombate;
                if (escenaOrigen == "Valdebebas")
                {
                    GameManager.Instance.pasoValdebebas = 0;
                }
                else if (escenaOrigen == "Recibidor")
                {
                    GameManager.Instance.pasoRecibidor = 0;
                }
                else if (escenaOrigen == "Pasillo")
                {
                    GameManager.Instance.pasoPasillo = 0;
                }
                else if (escenaOrigen == "Despacho")
                {
                    GameManager.Instance.pasoDespacho = 0;
                }
            }
        }

        // Si el combate se inició desde una pantalla previa, regresamos a ella de forma dinámica
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.escenaOrigenCombate))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameManager.Instance.escenaOrigenCombate);
        }
        else
        {
            // Fallback por defecto si pruebas el combate suelto en el Editor de Unity
            Debug.LogWarning("[CombatManager] GameManager o escena de origen nulos. Volviendo a 'Despacho' por defecto.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Despacho");
        }
    }
}

[System.Serializable]
public class DatosEnemigoEditor
{
    public string nombreIdentificador; // Identificador del tipo de enemigo (ej: "Entrenador", "Jugador", "Tercero", "Dictador")
    public Sprite spriteVisual;        // Sprite visual en estado de reposo (Idle)
    public Sprite spriteAtaque;        // Sprite visual al realizar un ataque
    public Sprite spriteMuerto;        // Sprite visual al ser derrotado
    public Vector3 escalaVisual = Vector3.one; // Escala personalizada para este enemigo (dejar en 0,0,0 para aplicar escala base automática)
    public Sprite spriteFondo;         // Fondo de combate específico para este enemigo/escena
    public AudioClip musicaCombate;    // Música de combate específica para este enemigo/escena
    public Vector3 posicionOffset = Vector3.zero; // Offset de posición personalizado para este enemigo en el combate
    public Vector3 posicionFondoOffset = Vector3.zero; // Offset de posición personalizado para el fondo de combate
}