using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHUDManager : MonoBehaviour
{
    public static BossHUDManager Instance { get; private set; }

    [Header("Boss UI Elements")]
    [SerializeField] private GameObject bossPanel;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Image hpMainFill;
    [SerializeField] private Image hpDamageFill;

    [Header("Elden Ring Damage Bar Settings")]
    [SerializeField] private float damageBufferDelay = 0.6f;
    [SerializeField] private float shrinkSpeed = 1.5f;

    private float targetHpFill = 1f;
    private float delayTimer = 0f;
    private EnemyBase activeBoss;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (bossPanel != null) bossPanel.SetActive(false);
    }

    private void Update()
    {
        HandleDelayedDamageBar();
    }

    public void ShowBossHealthBar(EnemyBase boss)
    {
        activeBoss = boss;

        if (bossNameText != null) bossNameText.text = boss.BossName;

        targetHpFill = boss.GetHealthPercentage;
        if (hpMainFill != null) hpMainFill.fillAmount = targetHpFill;
        if (hpDamageFill != null) hpDamageFill.fillAmount = targetHpFill;

        // Event'lere abone ol
        activeBoss.OnHealthChanged += UpdateBossHPBar;
        activeBoss.OnDied += HideBossHealthBar;

        if (bossPanel != null) bossPanel.SetActive(true);
    }

    public void HideBossHealthBar()
    {
        if (activeBoss != null)
        {
            activeBoss.OnHealthChanged -= UpdateBossHPBar;
            activeBoss.OnDied -= HideBossHealthBar;
            activeBoss = null;
        }

        if (bossPanel != null) bossPanel.SetActive(false);
    }

    private void UpdateBossHPBar(int current, int max)
    {
        targetHpFill = Mathf.Clamp01((float)current / max);
        if (hpMainFill != null) hpMainFill.fillAmount = targetHpFill;

        if (hpDamageFill != null && hpDamageFill.fillAmount < targetHpFill)
        {
            hpDamageFill.fillAmount = targetHpFill;
        }

        delayTimer = damageBufferDelay;
    }

    private void HandleDelayedDamageBar()
    {
        if (hpDamageFill == null || !bossPanel.activeSelf) return;

        if (delayTimer > 0)
        {
            delayTimer -= Time.deltaTime;
        }
        else if (hpDamageFill.fillAmount > targetHpFill)
        {
            hpDamageFill.fillAmount = Mathf.Lerp(hpDamageFill.fillAmount, targetHpFill, Time.deltaTime * shrinkSpeed);
            if (Mathf.Abs(hpDamageFill.fillAmount - targetHpFill) < 0.001f)
            {
                hpDamageFill.fillAmount = targetHpFill;
            }
        }
    }
}