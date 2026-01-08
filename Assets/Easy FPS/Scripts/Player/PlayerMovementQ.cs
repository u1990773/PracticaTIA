using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayerMovementQ : MonoBehaviour
{
    public int notasRecogidas = 0;
    public TextMeshProUGUI contadorNotas;
    public TextMeshProUGUI texto_mision;


    [Header("Player Health & Damage")]
    public int maxHealth = 100;

    public int currentHealth;
    private bool isImmortal = false;


    [Header("Player Movement & Gravity")]

    public float movementSpeed = 5f;
    public float jumpForce = 2f;
    private CharacterController controller;
    public float gravity = -9.81f;
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;
    private bool isGrounded;
    private Vector3 velocity;

    [Header("Foot Steps")]
        public AudioSource leftFootAudioSource;
        public AudioSource rightFootAudioSource;
        public AudioClip[] footstepSounds;
        public float footstepInterval;
        private float nextFootstepTime;
        private bool isLeftFootStep = true;

    void Start() //sart Cabron
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        HandleMovement();
        HandleGravity();

        //Handle Footsteps
        if (isGrounded && controller.velocity.magnitude > 0.1f && Time.time >= nextFootstepTime)
        {
            PlayFootstepSound();
            nextFootstepTime = Time.time + footstepInterval;
        }

        //Handle Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {

            velocity.y = Mathf.Sqrt(jumpForce * -2 * gravity);
        }

        controller.Move(velocity * Time.deltaTime);

        if (contadorNotas != null)
        {
            contadorNotas.text = "Notes: " + notasRecogidas;
        }

        if (notasRecogidas == 5)
        {
            texto_mision.text = "You already have the notes. Escape through the principal door!";
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            isImmortal = !isImmortal; 
            if (isImmortal)
            {
                maxHealth = 9999;
                currentHealth = maxHealth;
                Debug.Log("CHEAT ACTIVATED: Immortality ON");
            }
            else
            {
                maxHealth = 100;
                currentHealth = maxHealth;
                Debug.Log("CHEAT DEACTIVATED: Immortality OFF");
            }
        }

    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput =Input.GetAxis("Vertical");

        Vector3 movement = transform.right * horizontalInput + transform.forward * verticalInput;
        movement.y =0;

        controller.Move(movement*movementSpeed * Time.deltaTime);
    }


    void HandleGravity(){
        velocity.y += gravity * Time.deltaTime;
    }

    void PlayFootstepSound(){
        AudioClip footstepClip = footstepSounds[Random.Range(0, footstepSounds.Length)];

        if(isLeftFootStep){
            leftFootAudioSource.PlayOneShot(footstepClip);
        }
        else{
            rightFootAudioSource.PlayOneShot(footstepClip);
        }

        isLeftFootStep = !isLeftFootStep;
    }

    public void TakeDamage(int damageAmount)
    {
        if (!isImmortal) 
    {
        currentHealth -= damageAmount;

        if(currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    }

     void LoadGameOverScene()
    {
        SceneManager.LoadScene("GameOver"); 
        
    }
    private void Die(){

        LoadGameOverScene();
        Debug.Log("Player has died");
    }
}
