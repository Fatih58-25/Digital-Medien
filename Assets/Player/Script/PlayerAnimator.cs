using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Animator Assignment")]
    [SerializeField] private Animator animator; // Zuweisung per Drag & Drop im Inspector

    // Animation Parameter Hashes (für bessere Performance)
    private int hashSpeed = Animator.StringToHash("Speed");
    private int hashIsSprinting = Animator.StringToHash("IsSprinting");
    private int hashIsDucking = Animator.StringToHash("IsDucking");
    private int hashIsGrounded = Animator.StringToHash("IsGrounded");
    private int hashJump = Animator.StringToHash("Jump");
    private int hashAttack = Animator.StringToHash("Attack");
    private int hashParry = Animator.StringToHash("Parry");

    // Korrektur: Im Animator heißt es "AttackTyp" (ohne e!)
    private int hashAttackType = Animator.StringToHash("AttackTyp");

    private void Start()
    {
        // Falls im Inspector vergessen, automatisch suchen
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    public void SetMovementSpeed(float speed)
    {
        if (animator == null) return;
        animator.SetFloat(hashSpeed, speed);
    }

    public void SetIsSprinting(bool sprinting)
    {
        if (animator == null) return;
        animator.SetBool(hashIsSprinting, sprinting);
    }

    public void SetIsDucking(bool ducking)
    {
        if (animator == null) return;
        animator.SetBool(hashIsDucking, ducking);
    }

    public void SetIsGrounded(bool grounded)
    {
        if (animator == null) return;
        animator.SetBool(hashIsGrounded, grounded);
    }

    public void SetJump(bool jump)
    {
        if (animator == null) return;
        animator.SetBool(hashJump, jump);
    }

    public void ResetJump()
    {
        if (animator == null) return;
        animator.SetBool(hashJump, false);
    }

    public void PlayAttack(int attackType = 1)
    {
        if (animator == null) return;
        animator.SetInteger(hashAttackType, attackType);
        animator.SetTrigger(hashAttack);
    }

    public void PlayParry()
    {
        if (animator == null) return;
        animator.SetTrigger(hashParry);
    }

    // Animation Events (Leere Methoden fangen Fehler ab)
    public void OnAttackHit() { }
    public void OnAttackEnd() { }
    public void OnParryEnd() { }
}