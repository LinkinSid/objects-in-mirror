using UnityEngine;

public class Stamina : MonoBehaviour
{
    public float maxStamina = 100f;
    public float sprintDrainRate = 15f;
    public float dashCost = 25f;
    public float rechargeRate = 35f;
    public float rechargeDelay = 0.3f;

    [HideInInspector] public float currentStamina;

    float rechargeDelayTimer;

    public bool canSprint => currentStamina > 0f;
    public bool canDash => currentStamina >= dashCost;

    void Start()
    {
        currentStamina = maxStamina;
    }

    public void DrainSprint(float deltaTime)
    {
        currentStamina = Mathf.Max(currentStamina - sprintDrainRate * deltaTime, 0f);
        rechargeDelayTimer = rechargeDelay;
    }

    public void DrainDash()
    {
        currentStamina = Mathf.Max(currentStamina - dashCost, 0f);
        rechargeDelayTimer = rechargeDelay;
    }

    public void Recharge(float deltaTime)
    {
        if (rechargeDelayTimer > 0f)
        {
            rechargeDelayTimer -= deltaTime;
            return;
        }
        currentStamina = Mathf.Min(currentStamina + rechargeRate * deltaTime, maxStamina);
    }
}
