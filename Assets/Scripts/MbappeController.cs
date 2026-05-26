using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MbappeController : MonoBehaviour
{
    public float moveSpeed;

    public bool isMoving;

    // variable para registrar si hay movimiento del personaje
    public Vector2 input;

    private Animator animator;

    // 1. NUEVO: Variable para guardar el componente visual del Sprite
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // 2. NUEVO: Buscamos el SpriteRenderer del personaje al iniciar el juego
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    //Función para updatear la posición del personaje
    private void Update()
    {
        // 1. NUEVO/MODIFICADO: Registramos la pulsación al vuelo para el espejo
        // Usamos Input.GetAxisRaw para saber hacia dónde quiere mirar AL INSTANTE
        float direccionHorizontal = Input.GetAxisRaw("Horizontal");

        if (direccionHorizontal < 0) 
        {
            spriteRenderer.flipX = true;  // Si pulsa izquierda, activamos espejo (mira a la izquierda)
        }
        else if (direccionHorizontal > 0) 
        {
            spriteRenderer.flipX = false; // Si pulsa derecha, quitamos espejo (mira a la derecha)
        }
        if (!isMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            Debug.Log("This is input.x" + input.x);
            Debug.Log("This is input.y" + input.y);

            

            //Debug para ver si detecta que pulso las letras
            if (input != Vector2.zero) Debug.Log("Estoy pulsando una tecla: " + input);

            // Si se pulsan ambas direcciones, ignoramos la Y para evitar diagonales
            if (input.x != 0) input.y = 0;

            //Vector2.zero es para indicar que la posición en (0, 0)
            if (input != Vector2.zero)
            {
                // 3. NUEVO: Control del efecto espejo según la dirección
                if (input.x < 0) 
                {
                    spriteRenderer.flipX = true;  // Activa el espejo (mira a la izquierda)
                }
                else if (input.x > 0) 
                {
                    spriteRenderer.flipX = false; // Desactiva el espejo (mira original a la derecha)
                }

                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);

                var targetPos = transform.position;
                //*0.1f es para reducir la distancia de movimiento al pulsar una tecla de movimiento
                targetPos.x += input.x * 0.1f; 
                targetPos.y += input.y * 0.1f;

                StartCoroutine(Move(targetPos));
            }
        }

        animator.SetBool("isMoving", isMoving); 
    }

    //Función de movimiento
    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;
        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;
    }

}

