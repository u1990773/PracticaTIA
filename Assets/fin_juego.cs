using UnityEngine;
using UnityEngine.SceneManagement;  

public class FinDeJuego : MonoBehaviour
{

    public PlayerMovementQ jugador;
    public int notasNecesarias = 5;

    private void Start()
    {
        if (jugador == null)
        {
            jugador = FindObjectOfType<PlayerMovementQ>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (jugador.notasRecogidas >= notasNecesarias)
            {

                Debug.Log("�Juego Terminado!");

                SceneManager.LoadScene("GameOver");

            }
        }
    }
}
