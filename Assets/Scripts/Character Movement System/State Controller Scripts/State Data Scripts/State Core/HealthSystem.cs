using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public CharacterStats characterStats;

    public bool isDead {  get; private set; }

    private int currentHealth;

    private void Start()
    {
        currentHealth = characterStats.health;
    }

    private void Update()
    {
    }
    public void TakeDamage(int attackStats)
    {
        currentHealth = Mathf.Max(currentHealth - attackStats, 0);
        characterStats.health = currentHealth;
    }
}
