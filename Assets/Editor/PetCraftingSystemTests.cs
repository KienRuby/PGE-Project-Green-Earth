#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class PetCraftingSystemTests
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
    }

    [Test]
    public void Test_01_DefaultState_NoOwnedPets()
    {
        // Yêu cầu 1: Mặc định ban đầu người chơi CHƯA sở hữu bất kỳ con Pet nào
        List<int> owned = PetService.GetOwnedPetIds();
        Assert.IsNotNull(owned, "Danh sách owned không được null");
        Assert.AreEqual(0, owned.Count, "Danh sách Pet sở hữu ban đầu bắt buộc phải rỗng (0 Pet)!");

        for (int i = 0; i < 7; i++)
        {
            Assert.IsFalse(PetService.IsPetOwned(i), $"Pet ID {i} mặc định không được đánh dấu là đã sở hữu!");
        }

        int[] equipped = PetService.LoadEquippedSlots();
        Assert.AreEqual(3, equipped.Length, "Phải có đúng 3 Slot trang bị");
        Assert.AreEqual(-1, equipped[0], "Slot 1 ban đầu phải trống (-1)");
        Assert.AreEqual(-1, equipped[1], "Slot 2 ban đầu phải trống (-1)");
        Assert.AreEqual(-1, equipped[2], "Slot 3 ban đầu phải trống (-1)");
    }

    [Test]
    public void Test_02_CanCraft_ValidatesIngredientsAndOwnership()
    {
        // Cydog (ID 4) yêu cầu Spiky Discus (ID 7) x2 và Shotgun (ID 8) x2
        PetData cydog = PetService.GetPetData(4);
        Assert.IsNotNull(cydog, "Phải tìm thấy dữ liệu Cydog ID 4");
        Assert.AreEqual("Cydog", cydog.petName);
        Assert.AreEqual(2, cydog.ingredients.Count, "Cydog phải có đúng 2 nguyên liệu");

        int ing1Id = cydog.ingredients[0].chipsetId;
        int ing2Id = cydog.ingredients[1].chipsetId;

        // Đặt số lượng nguyên liệu về 0
        PlayerDataService.SaveChipsetItemData(ing1Id, 1, 1, 0, 5, false);
        PlayerDataService.SaveChipsetItemData(ing2Id, 1, 1, 0, 5, false);

        // Chưa đủ nguyên liệu => CanCraft phải trả về false
        Assert.IsFalse(PetService.CanCraft(4), "Chưa đủ mảnh chipset thì CanCraft phải trả về false!");

        // Chỉ đủ 1 loại nguyên liệu => vẫn phải trả về false
        PlayerDataService.SaveChipsetItemData(ing1Id, 1, 1, 10, 5, false);
        Assert.IsFalse(PetService.CanCraft(4), "Chỉ đủ 1 trong 2 loại nguyên liệu thì CanCraft vẫn phải trả về false!");

        // Đủ cả 2 loại nguyên liệu => CanCraft phải trả về true
        PlayerDataService.SaveChipsetItemData(ing2Id, 1, 1, 10, 5, false);
        Assert.IsTrue(PetService.CanCraft(4), "Đủ cả 2 loại nguyên liệu thì CanCraft phải trả về true!");
    }

    [Test]
    public void Test_03_CraftPet_DeductsIngredientsAndUnlocks()
    {
        PetData cydog = PetService.GetPetData(4);
        int ing1Id = cydog.ingredients[0].chipsetId;
        int ing2Id = cydog.ingredients[1].chipsetId;
        int req1 = cydog.ingredients[0].requiredCount;
        int req2 = cydog.ingredients[1].requiredCount;

        // Cấp 10 mảnh cho mỗi loại
        PlayerDataService.SaveChipsetItemData(ing1Id, 1, 1, 10, 5, false);
        PlayerDataService.SaveChipsetItemData(ing2Id, 1, 1, 10, 5, false);

        bool eventFired = false;
        int craftedPetId = -1;
        PetService.OnPetCrafted += (id) =>
        {
            eventFired = true;
            craftedPetId = id;
        };

        // Thực hiện chế tạo Cydog
        bool craftSuccess = PetService.CraftPet(4);
        Assert.IsTrue(craftSuccess, "Chế tạo Pet phải thành công khi đủ điều kiện!");
        Assert.IsTrue(eventFired, "Sự kiện OnPetCrafted phải được kích hoạt sau khi craft!");
        Assert.AreEqual(4, craftedPetId, "ID trong sự kiện craft phải là 4 (Cydog)!");

        // Xác nhận trạng thái sở hữu
        Assert.IsTrue(PetService.IsPetOwned(4), "Cydog phải được đánh dấu đã sở hữu!");
        Assert.IsTrue(PetService.GetOwnedPetIds().Contains(4), "Cydog phải có trong danh sách GetOwnedPetIds()!");
        Assert.AreEqual(1, PetService.GetOwnedPetIds().Count, "Danh sách Pet sở hữu hiện tại phải có đúng 1 Pet!");

        // Xác nhận nguyên liệu trong kho đã bị trừ đúng số lượng
        Assert.AreEqual(10 - req1, PlayerDataService.GetChipsetPieceCount(ing1Id), $"Nguyên liệu {ing1Id} phải bị trừ đúng {req1} mảnh!");
        Assert.AreEqual(10 - req2, PlayerDataService.GetChipsetPieceCount(ing2Id), $"Nguyên liệu {ing2Id} phải bị trừ đúng {req2} mảnh!");

        // Không thể craft lại Pet đã sở hữu
        Assert.IsFalse(PetService.CanCraft(4), "Pet đã sở hữu rồi thì CanCraft không được phép trả về true nữa!");
        Assert.IsFalse(PetService.CraftPet(4), "Không thể craft lại Pet đã sở hữu!");
    }

    [Test]
    public void Test_04_EquipPet_OnlyAllowsOwnedPets()
    {
        // 1. Thử trang bị Pet chưa sở hữu (Pink Bat ID 0) => Phải thất bại
        Assert.IsFalse(PetService.IsPetOwned(0));
        bool equipUnowned = PetService.EquipPet(0, 0);
        Assert.IsFalse(equipUnowned, "Tuyệt đối không được phép trang bị Pet chưa sở hữu!");
        Assert.AreEqual(-1, PetService.GetEquippedPetId(0), "Slot 1 vẫn phải trống");

        // 2. Mở khóa Cydog (ID 4)
        PetService.SetPetOwned(4, true);
        Assert.IsTrue(PetService.IsPetOwned(4));

        // 3. Trang bị Cydog vào Slot 1 (index 0)
        bool equipSuccess = PetService.EquipPet(0, 4);
        Assert.IsTrue(equipSuccess, "Trang bị Pet đã sở hữu vào Slot 1 phải thành công!");
        Assert.AreEqual(4, PetService.GetEquippedPetId(0), "Slot 1 phải đang giữ Cydog (ID 4)!");
        Assert.IsTrue(PetService.IsPetEquipped(4), "IsPetEquipped(4) phải trả về true!");
        Assert.AreEqual(0, PetService.GetEquippedSlotIndex(4), "Vị trí trang bị của Cydog phải là slot index 0!");

        // 4. Chuyển Cydog sang Slot 2 (index 1) => Slot 1 tự động gỡ để tránh trùng lặp
        PetService.EquipPet(1, 4);
        Assert.AreEqual(-1, PetService.GetEquippedPetId(0), "Slot 1 phải được gỡ bỏ khi Cydog chuyển sang Slot 2!");
        Assert.AreEqual(4, PetService.GetEquippedPetId(1), "Slot 2 phải đang giữ Cydog!");

        // 5. Gỡ bỏ trang bị
        PetService.UnequipPet(1);
        Assert.AreEqual(-1, PetService.GetEquippedPetId(1), "Slot 2 phải trống sau khi Unequip!");
        Assert.IsFalse(PetService.IsPetEquipped(4), "Cydog không còn trang bị ở slot nào!");
    }

    [Test]
    public void Test_05_PetIngredients_IconsAndFramesMatchChipsets()
    {
        var allPets = PetService.AllPets;
        Assert.AreEqual(7, allPets.Count, "Phải có đúng 7 loại Pet trong hệ thống!");

        foreach (var pet in allPets)
        {
            Assert.AreEqual(2, pet.ingredients.Count, $"Pet {pet.petName} phải có 2 nguyên liệu chipset!");

            foreach (var ing in pet.ingredients)
            {
                Assert.IsTrue(ing.chipsetId >= 1 && ing.chipsetId <= 10, $"Chipset ID {ing.chipsetId} phải từ 1 đến 10");
                Assert.IsNotNull(ing.iconSprite, $"Pet {pet.petName} - Nguyên liệu {ing.chipsetName} (ID {ing.chipsetId}) bắt buộc phải có Icon!");
                Assert.IsNotNull(ing.frameSprite, $"Pet {pet.petName} - Nguyên liệu {ing.chipsetName} (ID {ing.chipsetId}) bắt buộc phải có Frame!");

                // Kiểm tra tên sprite icon chứa từ khóa phù hợp với chipsetName
                string spriteName = ing.iconSprite.name.ToLowerInvariant();
                string chipName = ing.chipsetName.ToLowerInvariant();

                bool nameMatches = false;
                if (chipName.Contains("turret") && (spriteName.Contains("turret") || spriteName.Contains("tháp"))) nameMatches = true;
                else if (chipName.Contains("mine") && (spriteName.Contains("mine") || spriteName.Contains("mìn"))) nameMatches = true;
                else if (chipName.Contains("punch") && (spriteName.Contains("punch") || spriteName.Contains("đấm"))) nameMatches = true;
                else if (chipName.Contains("blade") && (spriteName.Contains("blade") || spriteName.Contains("dao"))) nameMatches = true;
                else if (chipName.Contains("multi") && (spriteName.Contains("multi") || spriteName.Contains("tia"))) nameMatches = true;
                else if (chipName.Contains("discus") && (spriteName.Contains("discus") || spriteName.Contains("gai"))) nameMatches = true;
                else if (chipName.Contains("shotgun") && (spriteName.Contains("shotgun") || spriteName.Contains("săn"))) nameMatches = true;
                else if (chipName.Contains("cable") && (spriteName.Contains("cable") || spriteName.Contains("cáp"))) nameMatches = true;
                else if (chipName.Contains("rifle") && (spriteName.Contains("rifle") || spriteName.Contains("trường"))) nameMatches = true;
                else if (chipName.Contains("standard") && (spriteName.Contains("standard") || spriteName.Contains("tiêu chuẩn"))) nameMatches = true;

                Assert.IsTrue(nameMatches, $"Icon sprite '{ing.iconSprite.name}' không khớp với tên chipset '{ing.chipsetName}' của pet '{pet.petName}'!");
            }
        }
    }

    [Test]
    public void Test_06_PetCardsAndIcons_AreDistinctAndCorrect()
    {
        var allPets = PetService.AllPets;
        Assert.AreEqual(7, allPets.Count, "Phải có đúng 7 loại Pet trong hệ thống!");

        string[] expectedIcons = { "Pet_Bat", "Pet_Slime", "Pet_Spider", "Pet_Snake", "Pet_Dog", "Pet_Turtle", "Pet_Snail" };
        HashSet<Sprite> uniqueIcons = new HashSet<Sprite>();

        for (int i = 0; i < allPets.Count; i++)
        {
            var pet = allPets[i];
            Assert.IsNotNull(pet.petIcon, $"Pet ID {i} ({pet.petName}) bắt buộc phải có petIcon!");
            Assert.AreEqual(expectedIcons[i], pet.petIcon.name, $"Pet ID {i} ({pet.petName}) phải có icon '{expectedIcons[i]}', không được dùng sai icon!");
            Assert.IsTrue(uniqueIcons.Add(pet.petIcon), $"Pet ID {i} ({pet.petName}) không được dùng trùng icon với Pet khác!");

            if (i == 0)
            {
                Assert.IsNotNull(pet.cardSprite, "Pink Bat (ID 0) phải có Card_Pet_Bat");
                Assert.AreEqual("Card_Pet_Bat", pet.cardSprite.name);
            }
            else if (i == 4)
            {
                Assert.IsNotNull(pet.cardSprite, "Cydog (ID 4) phải có Card_Pet_Dog");
                Assert.AreEqual("Card_Pet_Dog", pet.cardSprite.name);
            }
            else
            {
                // Slime, Spider, Snake, Turtle, Snail KHÔNG được gán Card_Pet_Bat hay Card_Pet_Dog
                Assert.IsTrue(pet.cardSprite == null || pet.cardSprite.name.Contains("Empty"),
                    $"Pet ID {i} ({pet.petName}) không được dùng Card_Pet_Bat hay Card_Pet_Dog! Phải dùng Card_Slot_Empty + petIcon riêng.");
            }
        }
    }
}
#endif
