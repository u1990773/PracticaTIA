using UnityEngine;

/// <summary>
/// Añadir al Player legacy. Intercepta daño y lo gestiona correctamente en VR.
/// </summary>
public class VRDamageInterceptor : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    private PlayerMovementQ legacyPlayer;
    private bool isDead = false;

    private void Start()
    {
        legacyPlayer = GetComponent<PlayerMovementQ>();
        currentHealth = maxHealth;

        // Forzar vida inicial al script legacy
        if (legacyPlayer != null)
        {
            SetLegacyHealth(currentHealth);
        }

        Debug.Log($"[VRDamageInterceptor] Vida inicial: {currentHealth}");
    }

    /// <summary>
    /// Los zombies llaman este método.
    /// </summary>
    public void RecibirDaño(float cantidad)
    {
        TakeDamage(cantidad);
    }

    /// <summary>
    /// Alternativa que también pueden llamar.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[VRDamageInterceptor] Daño: {amount}. Vida: {currentHealth}/{maxHealth}");

        // Actualizar vida en script legacy
        if (legacyPlayer != null)
        {
            SetLegacyHealth(currentHealth);
        }

        // Haptic feedback
        if (VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendHapticBoth(0.7f, 0.2f);
        }

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("[VRDamageInterceptor] Player muerto!");

        // Haptic fuerte
        if (VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendHapticBoth(1f, 0.5f);
        }

        // Cargar Game Over después de 2 segundos
        Invoke(nameof(LoadGameOver), 2f);
    }

    private void LoadGameOver()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
    }

    private void SetLegacyHealth(float health)
    {
        try
        {
            var vidaField = legacyPlayer.GetType().GetField("vida",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (vidaField != null)
            {
                vidaField.SetValue(legacyPlayer, health);
            }
        }
        catch { }
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
}