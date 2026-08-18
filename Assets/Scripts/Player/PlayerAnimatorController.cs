using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[DefaultExecutionOrder(100)]
public class PlayerAnimatorController : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    [Header("Player References")]
    [Tooltip("Script cung cấp hướng input di chuyển hiện tại của Player.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Tooltip("Script bắn tự động, cung cấp trạng thái có mục tiêu trong tầm và đang bắn.")]
    [SerializeField] private PlayerAutoShooter autoShooter;

    [Header("Animation Settings")]
    [Tooltip("Ngưỡng input tối thiểu để Animator chuyển từ Idle sang Run.")]
    [SerializeField, Min(0f)] private float movementThreshold = 0.01f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (autoShooter == null)
        {
            autoShooter = GetComponent<PlayerAutoShooter>();
        }
    }

    private void Update()
    {
        // Execution order 100 bảo đảm Movement và AutoShooter đã cập nhật frame này.
        float thresholdSqr = movementThreshold * movementThreshold;
        bool isMoving =
            playerMovement != null &&
            playerMovement.MoveDirection.sqrMagnitude > thresholdSqr;

        bool isAttacking = autoShooter != null && autoShooter.IsAttacking;

        animator.SetBool(IsMovingHash, isMoving);
        animator.SetBool(IsAttackingHash, isAttacking);
    }

    private void OnDisable()
    {
        if (animator == null)
            return;

        animator.SetBool(IsMovingHash, false);
        animator.SetBool(IsAttackingHash, false);
    }
}
