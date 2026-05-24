using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MbappeController : MonoBehaviour
{
    public float moveSpeed;

    public bool isMoving;

    // variable para registrar si hay movimiento del personaje
    public Vector2 input;
    
    //Función para updatear la posición del personaje
    private void Update()
    {
        if (!isMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            //Debug para ver si detecta que pulso las letras
            if (input != Vector2.zero) Debug.Log("Estoy pulsando una tecla: " + input);

            // Si se pulsan ambas direcciones, ignoramos la Y para evitar diagonales
            if (input.x != 0) input.y = 0;

            //Vector2.zero es para indicar que la posición en (0, 0)
            if (input != Vector2.zero)
            {
                var targetPos = transform.position;
                //*0.1f es para reducir la distancia de movimiento al pulsar una tecla de movimiento
                targetPos.x += input.x * 0.1f; 
                targetPos.y += input.y * 0.1f;

                StartCoroutine(Move(targetPos));
            }
        }
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

