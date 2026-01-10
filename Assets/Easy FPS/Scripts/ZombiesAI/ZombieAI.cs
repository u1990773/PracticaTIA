using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class ZombieAI : MonoBehaviour
{
    public NavMeshAgent navAgent;

    public enum ZombieState { Idle, Chase, Attack, Dead }

    public Animator animator;

    public ZombieState currentState = ZombieState.Idle;

    public Transform player;

    public float chaseDistance = 10f;

    public float attackDistance = 2f;

    public float attackCooldown = 2f;

    public float attackDelay = 1.5f;

    public int damage = 10;

    public int health = 100;

    private CapsuleCollider capsuleCollider;

    private bool isAttacking;

    private float lastAttackTime;


    public GameObject bloodScreenEffect;

    private GameObject instantiatedObject;

    void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.Log("Player not found!");
        }
        capsuleCollider = GetComponent<CapsuleCollider>();
        navAgent = GetComponent<NavMeshAgent>();
        lastAttackTime = -attackCooldown;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        switch (currentState)
        {
            case ZombieState.Idle:
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsAttacking", false);
                if (Vector3.Distance(transform.position, player.position) <= chaseDistance)
                    currentState = ZombieState.Chase;
                break;
            case ZombieState.Chase:
                animator.SetBool("IsWalking", true);
                animator.SetBool("IsAttacking", false);
                navAgent.SetDestination(player.position);
                if (Vector3.Distance(transform.position, player.position) <= attackDistance)
                    currentState = ZombieState.Attack;
                break;
            case ZombieState.Attack:
                animator.SetBool("IsAttacking", true);
                navAgent.SetDestination(transform.position);
                if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
                {

                    StartCoroutine(AttackWithDelay());
                    Debug.Log("Attack Player");
                    StartCoroutine(ActiveBloodScreenEffect());

                }

                if (Vector3.Distance(transform.position, player.position) > attackDistance)
                    currentState = ZombieState.Chase;
                break;
            case ZombieState.Dead:
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsDead", true);
                navAgent.enabled = false;
                capsuleCollider.enabled = false;
                enabled = false;
                GameManager.instance.currentScore += 1;
                Debug.Log("Dead");
                break;

        }
    }

    private IEnumerator AttackWithDelay()
    {
        isAttacking = true;

        PlayerMovementQ playerMovement = player.GetComponent<PlayerMovementQ>();
        if (playerMovement != null)
        {
            playerMovement.TakeDamage(damage);
        }

        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
        lastAttackTime = Time.time;

    }
    private IEnumerator ActiveBloodScreenEffect()
    {
        InstantiateObject();
        yield return new WaitForSeconds(attackDelay);
        DeleteObject();
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentState == ZombieState.Dead)
            return;

        health -= damageAmount;

        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    private void Die()
    {
        currentState = ZombieState.Dead;
    }
    void InstantiateObject()
    {
        instantiatedObject = Instantiate(bloodScreenEffect);
    }

    void DeleteObject()
    {
        if (instantiatedObject != null)
        {
            Destroy(instantiatedObject);

            instantiatedObject = null;
        }
    }
}