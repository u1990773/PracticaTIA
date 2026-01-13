using UnityEngine;

/// <summary>
/// Sistema de vida para VR que mantiene compatibilidad con el sistema legacy.
/// Se sincroniza con PlayerMovementQ pero mantiene la vida independientemente.
/// </summary>
public class VRPlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Legacy Sync")]
    [SerializeField] private bool syncWithLegacyPlayer = true;
    [SerializeField] private string legacyPlayerTag = "Player";

    [Header("VR Feedback")]
    [SerializeField] private bool useHapticOnDamage = true;
    [SerializeField] private float hapticIntensity = 0.8f;
    [SerializeField] private float hapticDuration = 0.2f;

    [Header("Game Over")]
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField] private float delayBeforeGameOver = 2f;

    private PlayerMovementQ legacyPlayer;
    private bool isDead = false;

    private void Start()
    {
        // Intentar encontrar el player legacy
        FindLegacyPlayer();

        // Inicializar vida
        currentHealth = maxHealth;

        // Si encontramos el player legacy, sincronizar la vida inicial
        if (legacyPlayer != null)
        {
            SyncHealthFromLegacy();
        }

        Debug.Log($"[VRPlayerHealth] Sistema de vida inicializado. HP: {currentHealth}/{maxHealth}");
    }

    private void Update()
    {
        // Sincronizar con player legacy si existe y está activo
        if (syncWithLegacyPlayer && legacyPlayer != null)
        {
            SyncHealthFromLegacy();
        }
    }

    private void FindLegacyPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag(legacyPlayerTag);
        if (playerObj != null)
        {
            legacyPlayer = playerObj.GetComponent<PlayerMovementQ>();

            if (legacyPlayer != null)
            {
                Debug.Log("[VRPlayerHealth] Player legacy encontrado y sincronizado.");
            }
        }
    }

    private void SyncHealthFromLegacy()
    {
        if (legacyPlayer == null) return;

        // Usar reflection para acceder al campo 'vida' del PlayerMovementQ
        try
        {
            var vidaField = legacyPlayer.GetType().GetField("vida",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (vidaField != null)
            {
                float legacyHealth = (float)vidaField.GetValue(legacyPlayer);

                // Solo actualizar si cambió
                if (Mathf.Abs(currentHealth - legacyHealth) > 0.1f)
                {
                    float previousHealth = currentHealth;
                    currentHealth = legacyHealth;

                    // Si perdió vida, ejecutar feedback
                    if (currentHealth < previousHealth)
                    {
                        OnDamageTaken(previousHealth - currentHealth);
                    }

                    // Check si murió
                    if (currentHealth <= 0 && !isDead)
                    {
                        Die();
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VRPlayerHealth] No se pudo sincronizar vida: {e.Message}");
        }
    }

    /// <summary>
    /// Recibe daño. Llamar desde zombies o trampas.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[VRPlayerHealth] Daño recibido: {damage}. HP: {currentHealth}/{maxHealth}");

        // Sincronizar con player legacy
        if (legacyPlayer != null)
        {
            try
            {
                var vidaField = legacyPlayer.GetType().GetField("vida",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (vidaField != null)
                {
                    vidaField.SetValue(legacyPlayer, currentHealth);
                }
            }
            catch { }
        }

        OnDamageTaken(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Restaura vida.
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);

        Debug.Log($"[VRPlayerHealth] Curación: {amount}. HP: {currentHealth}/{maxHealth}");

        // Sincronizar con player legacy
        if (legacyPlayer != null)
        {
            try
            {
                var vidaField = legacyPlayer.GetType().GetField("vida",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (vidaField != null)
                {
                    vidaField.SetValue(legacyPlayer, currentHealth);
                }
            }
            catch { }
        }
    }

    private void OnDamageTaken(float damage)
    {
        // Haptic feedback
        if (useHapticOnDamage && VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendHapticBoth(hapticIntensity, hapticDuration);
        }

        // Aquí podrías añadir efectos visuales (pantalla roja, etc)
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("[VRPlayerHealth] Player ha muerto.");

        // Haptic intenso
        if (VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendHapticPulse(
                VRHapticsManager.Instance.GetLeftController(), 5, 1f, 0.2f, 0.1f
            );
            VRHapticsManager.Instance.SendHapticPulse(
                VRHapticsManager.Instance.GetRightController(), 5, 1f, 0.2f, 0.1f
            );
        }

        // Cargar escena de Game Over
        StartCoroutine(LoadGameOverScene());
    }

    private System.Collections.IEnumerator LoadGameOverScene()
    {
        yield return new WaitForSeconds(delayBeforeGameOver);

        // Intentar cargar escena de Game Over
        try
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
        }
        catch
        {
            Debug.LogError($"[VRPlayerHealth] No se pudo cargar escena: {gameOverSceneName}");
        }
    }

    #region Public API

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsDead() => isDead;

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    #endregion
}