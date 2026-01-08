using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShootingController : MonoBehaviour{
    

    public Animator animator;
    public Transform firePoint;

    public float fireRate = 0.1f;

    public float fireRange= 10f;

    private float nextFireTime = 0f;

    public bool isAuto = false;

    public int maxAmmo = 30;

    public int currentAmmo;

    public float reloadTime = 1.5f;

    private bool isReloading = false;

    public ParticleSystem muzzleFlash;

    public ParticleSystem bloodEffect;

    public int damagePerShot = 20;

    

    [Header("Sound Effect")]
    public AudioSource soundAudioSource;
    public AudioClip shootingSoundClip;

    public AudioClip reloadSoundClip;

    [Header("UI")]
    public TextMeshProUGUI ammoText;

    void Start(){
        currentAmmo = maxAmmo;
        UpdateAmmoText();
    }

    void Update()
    {
        if(isReloading)
            return;
        if(isAuto == true)
        {
            if(Input.GetButton("Fire1") && Time.time >= nextFireTime){

                nextFireTime = Time.time +1f/ fireRate;
                Debug.Log("el Dani es Gay");
                Shoot();
                
            }
            else{
                animator.SetBool("Shoot", false);
            }
        }
        else{
            if(Input.GetButtonDown("Fire1") && Time.time >= nextFireTime){

                nextFireTime = Time.time +1f/ fireRate;
                Debug.Log("el Dani NO es Gay");
                Shoot();
                
            }
            else{
                animator.SetBool("Shoot", false);
            }
        }

        if(Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
        {
            Debug.Log("bona tarda");
            Reload();
        }
    }

    private void Shoot()
    {
        if(currentAmmo > 0){

        
            RaycastHit hit;
            if(Physics.Raycast(firePoint.position, firePoint.forward, out hit, fireRange))
            {
                Debug.Log(hit.transform.name);
                ZombieAI zombieAI = hit.collider.GetComponent<ZombieAI>();
                if(zombieAI != null)
                {
                    zombieAI.TakeDamage(damagePerShot);
                   
                    ParticleSystem blood = Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(blood.gameObject, blood.main.duration);
                    
                }


                WaypointZombieAI waypointzombieAI = hit.collider.GetComponent<WaypointZombieAI>();
                if(waypointzombieAI != null)
                {
                    waypointzombieAI.TakeDamage(damagePerShot);    

                     ParticleSystem blood = Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
                     Destroy(blood.gameObject, blood.main.duration);
                }
            }
            muzzleFlash.Play();
            animator.SetBool("Shoot", true);
            currentAmmo--;
            UpdateAmmoText();
            Debug.Log(currentAmmo);

            soundAudioSource.PlayOneShot(shootingSoundClip);
        }
        else
        {
            Reload();
        }
    }

    private void Reload()
    {
        if(!isReloading && currentAmmo < maxAmmo)
        {
            Debug.Log("rriiamc");
            animator.SetTrigger("Reload");
            isReloading = true;

            soundAudioSource.PlayOneShot(reloadSoundClip);

            Invoke("FinishReloading", reloadTime);
        }
    }

    private void FinishReloading()
    {
        
        currentAmmo = maxAmmo;
        isReloading= false;
        UpdateAmmoText();
        animator.ResetTrigger("Reload");
    }
    private void UpdateAmmoText()
    {
        ammoText.text = $"{currentAmmo}/{maxAmmo}";
    }
}