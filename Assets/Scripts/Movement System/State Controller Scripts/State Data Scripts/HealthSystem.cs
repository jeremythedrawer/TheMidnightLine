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
        TakeDamage();
    }
    public void TakeDamage()
    {

    }
}
