using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class PetCombatSystemTests
{
    [SetUp]
    public void SetUp()
    {
        PetService.ResetPetDataForTesting();
    }

    [TearDown]
    public void TearDown()
    {
        PetService.ResetPetDataForTesting();

        // Dọn dẹp các GameObject tạm sinh ra trong test
        GameObject managerObj = GameObject.Find("[PetCombatManager]");
        if (managerObj != null) Object.DestroyImmediate(managerObj);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) Object.DestroyImmediate(playerObj);

        var followers = Object.FindObjectsOfType<PetCompanionFollower>();
        foreach (var f in followers)
        {
            if (f != null) Object.DestroyImmediate(f.gameObject);
        }
    }

    [Test]
    public void GetActiveBattlePetId_WhenNoPetEquipped_ReturnsMinusOne()
    {
        int battlePetId = PetService.GetActiveBattlePetId();
        Assert.AreEqual(-1, battlePetId, "Khi chưa trang bị Pet nào, ID phải là -1.");
    }

    [Test]
    public void GetActiveBattlePetId_WhenPetEquippedInActiveSlot_ReturnsEquippedPetId()
    {
        // Giả lập đã chế tạo và sở hữu Pink Bat (ID: 0)
        PetService.SetPetOwned(0, true);
        PetService.ActiveSlotIndex = 0;
        PetService.EquipPet(0, 0);

        int battlePetId = PetService.GetActiveBattlePetId();
        Assert.AreEqual(0, battlePetId, "Phải trả về đúng ID của Pet trong active slot.");
    }

    [Test]
    public void GetActiveBattlePetId_EnforcesAtMostOnePet_BasedOnActiveSlot()
    {
        // Sở hữu Pink Bat (0) và Cydog (4)
        PetService.SetPetOwned(0, true);
        PetService.SetPetOwned(4, true);

        PetService.EquipPet(0, 0); // Slot 0: Pink Bat
        PetService.EquipPet(1, 4); // Slot 1: Cydog

        // Khi ActiveSlot = 0 => chỉ mang Pink Bat
        PetService.ActiveSlotIndex = 0;
        Assert.AreEqual(0, PetService.GetActiveBattlePetId(), "Chỉ mang tối đa 1 Pet (Slot 0)");

        // Khi ActiveSlot = 1 => chỉ mang Cydog
        PetService.ActiveSlotIndex = 1;
        Assert.AreEqual(4, PetService.GetActiveBattlePetId(), "Chỉ mang tối đa 1 Pet (Slot 1)");
    }

    [Test]
    public void GetActiveBattlePetId_FallsBackToFirstAvailableSlot_IfActiveSlotEmpty()
    {
        PetService.SetPetOwned(2, true); // Cyber Spider
        PetService.EquipPet(1, 2);       // Trang bị vào Slot 1
        PetService.ActiveSlotIndex = 0;   // Slot 0 đang trống

        int battlePetId = PetService.GetActiveBattlePetId();
        Assert.AreEqual(2, battlePetId, "Nếu slot active trống, tự động fallback sang slot có trang bị.");
    }

    [Test]
    public void AllSevenPetSprites_AreAccessibleAndNotNull()
    {
        string[] expectedNames = { "Pet_Bat", "Pet_Slime", "Pet_Spider", "Pet_Snake", "Pet_Dog", "Pet_Turtle", "Pet_Snail" };

        for (int i = 0; i < 7; i++)
        {
            Sprite s = PetService.GetPetSprite(i);
            Assert.IsNotNull(s, $"Sprite của Pet ID {i} ({expectedNames[i]}) không được null.");
            Assert.IsTrue(s.name.Contains(expectedNames[i]), $"Sprite của Pet ID {i} phải đúng tên {expectedNames[i]}. Tên hiện tại: {s.name}");
        }
    }

    [Test]
    public void PetCompanionFollower_IsCompletelyPassive_HasNoCollidersOrWeapons()
    {
        GameObject petGo = new GameObject("TestPet");
        var follower = petGo.AddComponent<PetCompanionFollower>();

        GameObject mockPlayer = new GameObject("Player");
        mockPlayer.tag = "Player";

        follower.Initialize(mockPlayer.transform, 0, PetService.GetPetSprite(0));

        // Pet tuyệt đối không có Collider2D hoặc Collider thường để không cản đạn và không va chạm quái
        Assert.IsNull(petGo.GetComponent<Collider2D>(), "Pet không được có Collider2D.");
        Assert.IsNull(petGo.GetComponent<Collider>(), "Pet không được có Collider 3D.");
        Assert.IsNull(petGo.GetComponent<Rigidbody2D>(), "Pet không được có Rigidbody2D.");

        // Layer phải là Default
        Assert.AreEqual(0, petGo.layer, "Pet phải ở Layer Default.");

        Object.DestroyImmediate(mockPlayer);
        Object.DestroyImmediate(petGo);
    }

    [Test]
    public void PetCombatManager_SpawnsAtMostOnePetCompanion()
    {
        // Chuẩn bị Mock Player
        GameObject playerObj = new GameObject("Player");
        playerObj.tag = "Player";
        playerObj.transform.position = Vector3.zero;

        // Trang bị 1 Pet
        PetService.SetPetOwned(1, true); // Green Slime
        PetService.ActiveSlotIndex = 0;
        PetService.EquipPet(0, 1);

        // Tạo PetCombatManager
        GameObject mgrGo = new GameObject("[PetCombatManager]");
        PetCombatManager mgr = mgrGo.AddComponent<PetCombatManager>();
        mgr.SpawnEquippedPet();

        Assert.IsNotNull(mgr.ActiveCompanion, "Phải triệu hồi thành công Pet Companion.");
        Assert.AreEqual(1, mgr.ActiveCompanion.PetId, "Companion phải có PetId = 1 (Green Slime).");

        // Gọi SpawnEquippedPet lần nữa -> Vẫn chỉ có đúng 1 Pet duy nhất
        mgr.SpawnEquippedPet();
        var allCompanions = Object.FindObjectsOfType<PetCompanionFollower>();
        Assert.AreEqual(1, allCompanions.Length, "Số lượng Pet trong trận đấu không bao giờ vượt quá 1.");

        Object.DestroyImmediate(mgrGo);
        Object.DestroyImmediate(playerObj);
    }

    [Test]
    public void PetCompanionFollower_AlwaysTrailsBehindPlayer()
    {
        GameObject mockPlayer = new GameObject("Player");
        mockPlayer.tag = "Player";
        mockPlayer.transform.position = Vector3.zero;

        GameObject petGo = new GameObject("TestPet");
        var follower = petGo.AddComponent<PetCompanionFollower>();
        follower.Initialize(mockPlayer.transform, 6, PetService.GetPetSprite(6)); // Snail

        // 1. Khi Player di chuyển sang phải (dx > 0)
        mockPlayer.transform.position = new Vector3(10f, 0f, 0f);
        Vector3 targetPosRight = follower.CalculateTargetPosition(0.016f);
        // Vị trí sau lưng Player (phía sau bên trái của Player): targetPos.x < player.x
        Assert.Less(targetPosRight.x, mockPlayer.transform.position.x, "Khi Player chạy sang phải, Pet phải ở sau lưng (bên trái).");

        // 2. Khi Player di chuyển sang trái (dx < 0)
        mockPlayer.transform.position = new Vector3(5f, 0f, 0f);
        Vector3 targetPosLeft = follower.CalculateTargetPosition(0.016f);
        // Vị trí sau lưng Player (phía sau bên phải của Player): targetPos.x > player.x
        Assert.Greater(targetPosLeft.x, mockPlayer.transform.position.x, "Khi Player chạy sang trái, Pet phải ở sau lưng (bên phải).");

        Object.DestroyImmediate(mockPlayer);
        Object.DestroyImmediate(petGo);
    }

    [Test]
    public void PetCompanionFollower_SpriteFlipX_FacesRightNatively()
    {
        GameObject mockPlayer = new GameObject("Player");
        mockPlayer.tag = "Player";
        mockPlayer.transform.position = Vector3.zero;

        GameObject petGo = new GameObject("TestPet");
        var follower = petGo.AddComponent<PetCompanionFollower>();
        follower.Initialize(mockPlayer.transform, 6, PetService.GetPetSprite(6)); // Snail

        // Vì toàn bộ 7 Sprite Pet gốc đều quay mặt sang PHẢI:
        // Ban đầu flipX phải là false (nhìn sang phải)
        Assert.IsFalse(follower.PetSpriteRenderer.flipX, "Pet mặc định quay mặt sang phải (flipX = false).");

        Object.DestroyImmediate(mockPlayer);
        Object.DestroyImmediate(petGo);
    }
}
