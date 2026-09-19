using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý việc đưa duy nhất 1 Pet Companion vào trận đấu (GamePlay).
/// - Tự động lắng nghe sự kiện nạp scene GamePlay.
/// - Kiểm tra PetService.GetActiveBattlePetId().
/// - Nếu có Pet được trang bị (ID >= 0), triệu hồi đúng 1 Pet đồng hành bay cùng Player.
/// - Nếu không trang bị, không spawn gì cả.
/// - Đảm bảo dọn dẹp sạch sẽ khi chuyển Scene hoặc khởi động lại trận.
/// </summary>
public class PetCombatManager : MonoBehaviour
{
    public static PetCombatManager Instance { get; private set; }

    [Header("Spawned Pet Companion")]
    [SerializeField] private PetCompanionFollower activeCompanion;

    public PetCompanionFollower ActiveCompanion => activeCompanion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadListener()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals("GamePlay", StringComparison.OrdinalIgnoreCase) && Instance == null)
        {
            GameObject host = new GameObject("[PetCombatManager]");
            host.AddComponent<PetCombatManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SpawnEquippedPet();
    }

    /// <summary>
    /// Kiểm tra và triệu hồi con Pet duy nhất được trang bị vào trận đấu.
    /// </summary>
    public void SpawnEquippedPet()
    {
        ClearActiveCompanion();

        int petId = PetService.GetActiveBattlePetId();
        if (petId < 0)
        {
            return;
        }

        PetData petData = PetService.GetPetData(petId);
        string petName = petData != null ? petData.petName : $"Pet_{petId}";


        // Tìm Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Transform playerTransform = playerObj != null ? playerObj.transform : null;

        // Tạo GameObject cho Pet
        GameObject petGo = new GameObject($"[Pet_{petName}]");
        if (playerTransform != null)
        {
            petGo.transform.position = playerTransform.position + new Vector3(-0.6f, 0.4f, 0f);
        }

        // Gán Sprite
        Sprite petSprite = PetService.GetPetSprite(petId);
        SpriteRenderer sr = petGo.AddComponent<SpriteRenderer>();
        sr.sprite = petSprite;
        sr.sortingOrder = 14; // Nằm sát phía sau Player (Player order thường là 15 hoặc 20)

        // Gán hành vi Follower
        PetCompanionFollower follower = petGo.AddComponent<PetCompanionFollower>();
        follower.Initialize(playerTransform, petId, petSprite);

        activeCompanion = follower;
    }

    public void ClearActiveCompanion()
    {
        if (activeCompanion != null)
        {
            Destroy(activeCompanion.gameObject);
            activeCompanion = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        ClearActiveCompanion();
    }
}
