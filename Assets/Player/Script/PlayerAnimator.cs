using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    
    // Animation Parameter Hashes (für bessere Performance)
    private int hashSpeed = Animator.StringToHash("Speed");
    private int hashIsSprinting = Animator.StringToHash("IsSprinting");
    private int hashIsDucking = Animator.StringToHash("IsDucking");
    private int hashIsGrounded = Animator.StringToHash("IsGrounded");
    private int hashJump = Animator.StringToHash("Jump");
    private int hashAttack = Animator.StringToHash("Attack");
    private int hashParry = Animator.StringToHash("Parry");
    private int hashAttackType = Animator.StringToHash("AttackType");
    
    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }
    
    public void SetMovementSpeed(float speed)
    {
        animator.SetFloat(hashSpeed, speed);
    }
    
    public void SetIsSprinting(bool sprinting)
    {
        animator.SetBool(hashIsSprinting, sprinting);
    }
    
    public void SetIsDucking(bool ducking)
    {
        animator.SetBool(hashIsDucking, ducking);
    }
    
    public void SetIsGrounded(bool grounded)
    {
        animator.SetBool(hashIsGrounded, grounded);
    }
    
    public void SetJump(bool jump)
    {
        animator.SetBool(hashJump, jump);
    }
    
    public void ResetJump()
    {
        animator.SetBool(hashJump, false);
    }
    
    public void PlayAttack(int attackType = 1)
    {
        animator.SetInteger(hashAttackType, attackType);
        animator.SetTrigger(hashAttack);
    }
    
    public void PlayParry()
    {
        animator.SetTrigger(hashParry);
    }
    
    // Für Animation Events
    public void OnAttackHit()
    {
        // Wird vom Animator aufgerufen wenn Hit
    }
    
    public void OnAttackEnd()
    {
        // Wird vom Animator aufgerufen wenn Animation endet
    }
    
    public void OnParryEnd()
    {
        // Wird vom Animator aufgerufen wenn Parry Animation endet
    }
}
