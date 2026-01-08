using UnityEngine;
using TMPro;  

public class Nota : MonoBehaviour
{
    public string mensaje;  
    public TextMeshProUGUI textoInteractuar;  
    private bool cercaDelJugador = false;  
    private PlayerMovementQ jugador;


    void Start()
    {
        jugador = FindObjectOfType<PlayerMovementQ>();  
    }
    private void Update()
    {
        if (cercaDelJugador && Input.GetKeyDown(KeyCode.E))
        {
            RecogerNota();  
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))  
        {
            cercaDelJugador = true;
            if (textoInteractuar != null) 
            {
                textoInteractuar.enabled = true;  
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))  
        {
            cercaDelJugador = false;
            if (textoInteractuar != null)  
            {
                textoInteractuar.enabled = false;  
            }
        }
    }

    public void RecogerNota()
    {
        Debug.Log("Nota recogida: " + mensaje);

        if (jugador != null)
        {
            jugador.notasRecogidas++;  
            jugador.currentHealth = jugador.maxHealth;
            Debug.Log("Notes: " + jugador.notasRecogidas);  
        }

        if (textoInteractuar != null)  
        {
            textoInteractuar.enabled = false;  
        }
        Destroy(gameObject);  
    }
}
