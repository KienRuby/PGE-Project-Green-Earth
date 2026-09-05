using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PGEGameLogicTests
{
    [Test]
    public void LabUpgrade_KeyGeneration_IsConsistent()
    {
        string key1 = LabUpgradeController.GetItemLevelKey("ATK", 1);
        string key2 = LabUpgradeController.GetItemLevelKey("atk", 1);
        string key3 = LabUpgradeController.GetItemLevelKey("DEF", 0);

        Assert.That(key1, Is.EqualTo("PGE.Lab.ItemLevel.ATK"));
        Assert.That(key2, Is.EqualTo("PGE.Lab.ItemLevel.ATK"));
        Assert.That(key3, Is.EqualTo("PGE.Lab.ItemLevel.DEF"));
    }

    [Test]
    public void PlayerStatsManager_GetStatLevel_ReadsCorrectly()
    {
        PlayerPrefs.SetInt("PGE.Lab.ItemLevel.HP", 5);
        PlayerPrefs.SetInt("PGE.Lab.ItemLevel.SPD", 3);
        PlayerPrefs.Save();

        int hpLevel = PlayerStatsManager.GetStatLevel("HP");
        int spdLevel = PlayerStatsManager.GetStatLevel("SPD");

        Assert.That(hpLevel, Is.EqualTo(5));
        Assert.That(spdLevel, Is.EqualTo(3));
    }

    [Test]
    public void LabUpgrade_PlayerDataService_CapsLevelAt10()
    {
        PlayerDataService.SetItemLevel("ATK", 15);
        Assert.That(PlayerDataService.GetItemLevel("ATK"), Is.EqualTo(10), "Level must be capped at MaxLabItemLevel (10).");

        PlayerDataService.SetItemLevel("ATK", 9);
        PlayerDataService.IncrementItemLevel("ATK", 5);
        Assert.That(PlayerDataService.GetItemLevel("ATK"), Is.EqualTo(10), "Incrementing past 10 must clamp to 10.");

        PlayerPrefs.SetInt("PGE.Lab.ItemLevel.OVERFLOW_TEST", 99);
        PlayerPrefs.Save();
        Assert.That(PlayerStatsManager.GetStatLevel("OVERFLOW_TEST"), Is.EqualTo(10), "PlayerStatsManager must read capped value of 10.");
    }

    [Test]
    public void LabUpgradeController_DefaultMaxLevel_IsTen()
    {
        Assert.That(LabUpgradeController.DefaultMaxLevel, Is.EqualTo(10));
        Assert.That(PlayerDataService.MaxLabItemLevel, Is.EqualTo(10));
    }

    [Test]
    public void ApkFreshInstall_AllLabItemsStartLocked_AndReleaseBalancesAreCorrect()
    {
        const string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            scenePath,
            UnityEditor.SceneManagement.OpenSceneMode.Single);

        LabUpgradeController controller = Object.FindObjectOfType<LabUpgradeController>(true);
        Assert.That(controller, Is.Not.Null, "MainMenu must contain a LabUpgradeController.");
        Assert.That(controller.gameObject.scene, Is.EqualTo(scene));

        FieldInfo itemsField = typeof(LabUpgradeController).GetField(
            "items",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(itemsField, Is.Not.Null);

        LabUpgradeController.ItemEntry[] items =
            (LabUpgradeController.ItemEntry[])itemsField.GetValue(controller);
        Assert.That(items, Is.Not.Null);
        Assert.That(items.Length, Is.EqualTo(16), "The fresh-install contract covers all 16 Lab stats.");

        string[] balanceKeys =
        {
            PlayerDataService.DataChipsKey,
            PlayerDataService.RedGemsKey,
            PlayerDataService.EnergyKey,
            PlayerDataService.AdvanceStonesKey
        };
        bool[] hadBalance = new bool[balanceKeys.Length];
        int[] savedBalance = new int[balanceKeys.Length];
        string[] itemKeys = new string[items.Length];
        bool[] hadItemLevel = new bool[items.Length];
        int[] savedItemLevel = new int[items.Length];

        for (int i = 0; i < balanceKeys.Length; i++)
        {
            hadBalance[i] = PlayerPrefs.HasKey(balanceKeys[i]);
            savedBalance[i] = PlayerPrefs.GetInt(balanceKeys[i]);
        }

        for (int i = 0; i < items.Length; i++)
        {
            Assert.That(items[i], Is.Not.Null, $"Lab item {i} must be configured.");
            itemKeys[i] = LabUpgradeController.GetItemLevelKey(items[i].itemName, i);
            hadItemLevel[i] = PlayerPrefs.HasKey(itemKeys[i]);
            savedItemLevel[i] = PlayerPrefs.GetInt(itemKeys[i]);
        }

        try
        {
            foreach (string key in balanceKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            foreach (string key in itemKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();

            Assert.That(PlayerDataService.DataChips, Is.EqualTo(1000));
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(1000));
            Assert.That(PlayerDataService.Energy, Is.EqualTo(100));
            Assert.That(PlayerDataService.AdvanceStones, Is.EqualTo(0));

            for (int i = 0; i < items.Length; i++)
            {
                LabUpgradeController.ItemEntry item = items[i];
                Assert.That(item.startsUnlocked, Is.False,
                    $"{item.itemName} must be locked on a clean APK install.");
                Assert.That(PlayerDataService.GetItemLevel(item.itemName), Is.EqualTo(0),
                    $"{item.itemName} must use level 0 as its locked fresh-install state.");
            }
        }
        finally
        {
            for (int i = 0; i < balanceKeys.Length; i++)
            {
                if (hadBalance[i])
                {
                    PlayerPrefs.SetInt(balanceKeys[i], savedBalance[i]);
                }
                else
                {
                    PlayerPrefs.DeleteKey(balanceKeys[i]);
                }
            }

            for (int i = 0; i < itemKeys.Length; i++)
            {
                if (hadItemLevel[i])
                {
                    PlayerPrefs.SetInt(itemKeys[i], savedItemLevel[i]);
                }
                else
                {
                    PlayerPrefs.DeleteKey(itemKeys[i]);
                }
            }

            PlayerPrefs.Save();
        }
    }

    [Test]
    public void PlayerHealth_DamageReduction_ReducesDamageCorrectly()
    {
        GameObject go = new GameObject("PlayerHealthTest");
        PlayerHealth health = go.AddComponent<PlayerHealth>();

        health.SetDamageReduction(5);
        int initialHp = health.CurrentHealth;

        health.TakeDamage(12);
        // Effective damage should be 12 - 5 = 7
        Assert.That(health.CurrentHealth, Is.EqualTo(initialHp - 7));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PlayerHealth_LethalDamage_TriggersDeathAndDoesNotHealToFull()
    {
        GameObject go = new GameObject("PlayerLethalHealthTest");
        PlayerHealth health = go.AddComponent<PlayerHealth>();

        bool deathTriggered = false;
        health.OnPlayerDeath += () => deathTriggered = true;

        // Deal fatal damage
        health.TakeDamage(150);

        Assert.That(health.CurrentHealth, Is.EqualTo(0), "Máu sau khi nhận sát thương chí tử phải bằng 0.");
        Assert.That(health.IsDead, Is.True, "Player phải ở trạng thái IsDead = true.");
        Assert.That(deathTriggered, Is.True, "Sự kiện OnPlayerDeath phải được phát khi máu về 0.");

        // Subsequent damage or heals must not revive the player
        health.TakeDamage(50);
        Assert.That(health.CurrentHealth, Is.EqualTo(0));

        health.Heal(50);
        Assert.That(health.CurrentHealth, Is.EqualTo(0));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void EnemyHealth_TakeDamage_FlashesRedImmediately()
    {
        GameObject go = new GameObject("EnemyFlashTest");
        EnemyHealth health = go.AddComponent<EnemyHealth>();

        GameObject childSprite = new GameObject("SpriteChild");
        childSprite.transform.SetParent(go.transform, false);
        SpriteRenderer sr = childSprite.AddComponent<SpriteRenderer>();
        sr.color = Color.white;

        health.CacheSpriteRenderers();

        // Ban đầu màu trắng
        Assert.That(sr.color, Is.EqualTo(Color.white));

        // Nhận damage -> Phải nháy đỏ ngay lập tức
        health.TakeDamage(10);
        Assert.That(sr.color, Is.EqualTo(Color.red), "Sprite quái vật phải chuyển sang màu đỏ ngay khi nhận sát thương.");

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);
        Assert.That(mpb.GetFloat("_FlashAmount"), Is.EqualTo(1f), "Shader FlashAmount phải bằng 1 khi nhận damage.");

        // Phục hồi lại màu ban đầu
        health.RestoreSpriteColors();
        Assert.That(sr.color, Is.EqualTo(Color.white), "Sprite quái vật phải khôi phục lại màu ban đầu.");

        sr.GetPropertyBlock(mpb);
        Assert.That(mpb.GetFloat("_FlashAmount"), Is.EqualTo(0f), "Shader FlashAmount phải trở về 0 khi phục hồi.");

        // Nhận đòn kết liễu khiến máu về 0 (50 máu -> trừ 50) -> Vẫn phải nháy đỏ
        health.TakeDamage(health.MaxHealth);
        Assert.That(health.CurrentHealth, Is.EqualTo(0));
        Assert.That(health.IsDead, Is.True);
        Assert.That(sr.color, Is.EqualTo(Color.red), "Phát bắn kết liễu (máu về 0) VẪN PHẢI nháy đỏ.");
        sr.GetPropertyBlock(mpb);
        Assert.That(mpb.GetFloat("_FlashAmount"), Is.EqualTo(1f), "Shader FlashAmount vẫn phải bằng 1 khi nhận đòn kết liễu.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PlayerHealth_TakeDamage_FlashesRedImmediately_AndExcludesHealthBar()
    {
        GameObject go = new GameObject("PlayerFlashTest");
        PlayerHealth health = go.AddComponent<PlayerHealth>();

        // Body Sprite
        GameObject bodyObject = new GameObject("Body");
        bodyObject.transform.SetParent(go.transform, false);
        SpriteRenderer bodySr = bodyObject.AddComponent<SpriteRenderer>();
        bodySr.color = Color.white;

        // Health Bar (Không được bị đổi màu đỏ)
        GameObject healthBarObj = new GameObject("PlayerWorldHealthBar");
        healthBarObj.transform.SetParent(go.transform, false);
        healthBarObj.AddComponent<PlayerWorldHealthBar>();
        SpriteRenderer barSr = healthBarObj.AddComponent<SpriteRenderer>();
        barSr.color = Color.green;

        health.CacheSpriteRenderers();

        // Ban đầu
        Assert.That(bodySr.color, Is.EqualTo(Color.white));
        Assert.That(barSr.color, Is.EqualTo(Color.green));

        // Nhận damage -> Body phải nháy đỏ, thanh máu vẫn giữ nguyên màu xanh lá
        health.TakeDamage(10);
        Assert.That(bodySr.color, Is.EqualTo(Color.red), "Body của Player phải chuyển sang màu đỏ ngay khi nhận damage.");
        Assert.That(barSr.color, Is.EqualTo(Color.green), "Thanh máu không được bị đổi màu đỏ khi Player nhận damage.");

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        bodySr.GetPropertyBlock(mpb);
        Assert.That(mpb.GetFloat("_FlashAmount"), Is.EqualTo(1f), "Shader FlashAmount của Player phải bằng 1 khi nhận damage.");

        // Phục hồi lại màu ban đầu
        health.RestoreSpriteColors();
        Assert.That(bodySr.color, Is.EqualTo(Color.white), "Body của Player phải khôi phục lại màu ban đầu.");

        bodySr.GetPropertyBlock(mpb);
        Assert.That(mpb.GetFloat("_FlashAmount"), Is.EqualTo(0f), "Shader FlashAmount phải trở về 0 khi phục hồi.");

        // Nhận damage lần 2 -> Tiếp tục chớp đỏ riêng biệt cho lần 2
        typeof(PlayerHealth).GetField("invincibleTimer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(health, 0f);
        health.TakeDamage(10);
        Assert.That(bodySr.color, Is.EqualTo(Color.red), "Body của Player phải chớp đỏ lần 2 tương ứng với đòn đánh thứ 2.");

        // Nhận đòn chí tử khiến máu về 0 -> Vẫn phải nháy đỏ
        typeof(PlayerHealth).GetField("invincibleTimer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(health, 0f);
        health.TakeDamage(health.MaxHealth * 2);
        Assert.That(health.CurrentHealth, Is.EqualTo(0));
        Assert.That(health.IsDead, Is.True);
        Assert.That(bodySr.color, Is.EqualTo(Color.red), "Đòn đánh chí tử (máu về 0) VẪN PHẢI nháy đỏ.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PlayerHealth_HitFlashMaterial_AppliedToAllPlayerRenderers()
    {
        GameObject go = new GameObject("PlayerFlashMaterialTest");
        PlayerHealth health = go.AddComponent<PlayerHealth>();

        Shader shader = Shader.Find("Custom/2D/SpriteHitFlash");
        if (shader != null)
        {
            Material testMat = new Material(shader);
            health.HitFlashMaterial = testMat;

            GameObject body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            SpriteRenderer bodySr = body.AddComponent<SpriteRenderer>();

            GameObject gun = new GameObject("Gun");
            gun.transform.SetParent(go.transform, false);
            SpriteRenderer gunSr = gun.AddComponent<SpriteRenderer>();

            health.CacheSpriteRenderers();

            Assert.That(bodySr.sharedMaterial, Is.EqualTo(testMat), "Body renderer phải được gán HitFlashMaterial.");
            Assert.That(gunSr.sharedMaterial, Is.EqualTo(testMat), "Gun renderer phải được gán HitFlashMaterial.");

            health.TakeDamage(10);
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            bodySr.GetPropertyBlock(mpb);
            Assert.That(mpb.GetFloat("_FlashAmount"), Is.EqualTo(1f));
            gunSr.GetPropertyBlock(mpb);
            Assert.That(mpb.GetFloat("_FlashAmount"), Is.EqualTo(1f));
        }

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PlayerAutoShooter_FindNearestEnemy_UsesClosestColliderPoint()
    {
        GameObject player = new GameObject("NearestEnemyPlayer");
        PlayerAutoShooter shooter = player.AddComponent<PlayerAutoShooter>();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Assert.That(enemyLayer, Is.GreaterThanOrEqualTo(0), "Project must define the Enemy layer.");

        // Pivot is farther away, but this large enemy's collider reaches x = 1.
        GameObject largeEnemy = new GameObject("LargeEnemy");
        largeEnemy.layer = enemyLayer;
        largeEnemy.transform.position = new Vector3(8f, 0f, 0f);
        largeEnemy.AddComponent<EnemyHealth>();
        BoxCollider2D largeCollider = largeEnemy.AddComponent<BoxCollider2D>();
        largeCollider.size = new Vector2(14f, 2f);

        // Pivot and collider are both at x = 3.
        GameObject smallEnemy = new GameObject("SmallEnemy");
        smallEnemy.layer = enemyLayer;
        smallEnemy.transform.position = new Vector3(3f, 0f, 0f);
        smallEnemy.AddComponent<EnemyHealth>();
        smallEnemy.AddComponent<BoxCollider2D>();

        typeof(PlayerAutoShooter).GetField("detectionShape", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(shooter, PlayerAutoShooter.DetectionShape.Circle);
        typeof(PlayerAutoShooter).GetField("detectionRadius", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(shooter, 20f);
        typeof(PlayerAutoShooter).GetField("currentAttackRange", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(shooter, 20f);

        Physics2D.SyncTransforms();

        MethodInfo findNearest = typeof(PlayerAutoShooter).GetMethod(
            "FindNearestEnemy",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.That(findNearest, Is.Not.Null);
        findNearest.Invoke(shooter, null);

        Transform selected = typeof(PlayerAutoShooter)
            .GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(shooter) as Transform;

        Assert.That(selected, Is.EqualTo(largeEnemy.transform),
            "Nearest enemy must be selected by its closest collider point, not by a distant root pivot.");

        Object.DestroyImmediate(smallEnemy);
        Object.DestroyImmediate(largeEnemy);
        Object.DestroyImmediate(player);
    }

    [Test]
    public void PlayerWorldHealthBar_ShrinksOnlyFillFromLeftEdge()
    {
        GameObject player = new GameObject("PlayerWorldHealthBarTest");
        player.AddComponent<PlayerHealth>();
        PlayerWorldHealthBar healthBar = player.AddComponent<PlayerWorldHealthBar>();

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(player.transform, false);
        SpriteRenderer background = backgroundObject.AddComponent<SpriteRenderer>();

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(player.transform, false);
        SpriteRenderer fill = fillObject.AddComponent<SpriteRenderer>();

        typeof(PlayerWorldHealthBar).GetField("backgroundRenderer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(healthBar, background);
        typeof(PlayerWorldHealthBar).GetField("fillRenderer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(healthBar, fill);

        Vector3 backgroundScale = background.transform.localScale;
        healthBar.SetNormalizedHealth(0.5f);

        Assert.That(background.transform.localScale, Is.EqualTo(backgroundScale), "Nền tối không được giảm theo HP.");
        Assert.That(fill.transform.localScale.x, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(fill.transform.localPosition.x, Is.EqualTo(-0.25f).Within(0.001f),
            "Fill phải giữ cạnh trái và co dần từ phải sang trái.");

        Object.DestroyImmediate(player);
    }

    [Test]
    public void GamePlay_PlayerWorldHealthBar_UsesProvidedSprites()
    {
        string sceneYaml = File.ReadAllText("Assets/Scenes/GamePlay.unity");
        string healthBarScript = File.ReadAllText("Assets/Scripts/Player/PlayerWorldHealthBar.cs");
        Assert.That(sceneYaml, Does.Contain("guid: 25b79f251ebc4d51b761051e9940d0cc"));
        Assert.That(sceneYaml, Does.Contain("fileID: -1459868381, guid: f70775b4373cdc64ba1cbe8e8bd22ee4"),
            "Thanh máu Player phải dùng BrHpPlayer làm nền.");
        Assert.That(sceneYaml, Does.Contain("fileID: 362662875, guid: f70775b4373cdc64ba1cbe8e8bd22ee4"),
            "Thanh máu Player phải dùng HpPlayer làm Fill.");
        Assert.That(healthBarScript, Does.Not.Contain("barRoot.localPosition ="),
            "Script không được ghi đè vị trí Transform mà người dùng đã chỉnh.");
        Assert.That(healthBarScript, Does.Not.Contain("barRoot.localScale ="),
            "Script không được ghi đè kích thước Transform mà người dùng đã chỉnh.");
    }

    [Test]
    public void BossRangedAttack_DirectionPatterns_AreCenteredAndEvenlySpaced()
    {
        Vector2[] fan = BossRangedAttack.CalculateFanDirections(Vector2.right, 3, 60f);
        Assert.That(fan, Has.Length.EqualTo(3));
        Assert.That(Vector2.Angle(fan[1], Vector2.right), Is.LessThan(0.01f));
        Assert.That(Vector2.Angle(fan[0], fan[1]), Is.EqualTo(30f).Within(0.01f));
        Assert.That(Vector2.Angle(fan[1], fan[2]), Is.EqualTo(30f).Within(0.01f));

        Vector2[] radial = BossRangedAttack.CalculateRadialDirections(Vector2.right, 4);
        Assert.That(radial, Has.Length.EqualTo(4));
        Assert.That(Vector2.Angle(radial[0], radial[1]), Is.EqualTo(90f).Within(0.01f));
        Assert.That(Vector2.Angle(radial[1], radial[2]), Is.EqualTo(90f).Within(0.01f));
    }

    [Test]
    public void BossPrefab_HasConfiguredRangedSkillsAndEnemyProjectile()
    {
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss.prefab");
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BossProjectile.prefab");

        Assert.That(bossPrefab, Is.Not.Null);
        Assert.That(projectilePrefab, Is.Not.Null);

        BossRangedAttack rangedAttack = bossPrefab.GetComponent<BossRangedAttack>();
        EnemyProjectile enemyProjectile = projectilePrefab.GetComponent<EnemyProjectile>();
        Assert.That(rangedAttack, Is.Not.Null, "Boss.prefab phải có BossRangedAttack.");
        Assert.That(rangedAttack.ProjectilePrefab, Is.EqualTo(enemyProjectile));
        Assert.That(rangedAttack.SkillCount, Is.EqualTo(3));
        Assert.That(rangedAttack.AttackRange, Is.GreaterThan(0f));
        Assert.That(enemyProjectile, Is.Not.Null, "BossProjectile.prefab phải có EnemyProjectile.");

        AnimationClip bossDie = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaton/Enemy/Boss/Die.anim");
        Assert.That(bossDie, Is.Not.Null);
        Assert.That(bossDie.isLooping, Is.False, "Animation Die của Boss phải chạy một lần rồi giữ frame cuối.");
    }

    [Test]
    public void BossRangedAttack_RangeCheck_UsesConfiguredRadius()
    {
        GameObject boss = new GameObject("BossRangeTest");
        boss.AddComponent<EnemyHealth>();
        BossRangedAttack rangedAttack = boss.AddComponent<BossRangedAttack>();
        GameObject player = new GameObject("PlayerRangeTest");
        player.AddComponent<PlayerHealth>();

        rangedAttack.SetTarget(player.transform);
        player.transform.position = boss.transform.position;
        Assert.That(rangedAttack.GetTargetRangeState(), Is.EqualTo(BossRangedAttack.TargetRangeState.InRange));
        Assert.That(rangedAttack.IsTargetInRange(), Is.True,
            "Boss phải đứng yên và vẫn bắn khi Player tiến sát.");

        player.transform.position = Vector3.right * (rangedAttack.AttackRange - 0.1f);
        Assert.That(rangedAttack.GetTargetRangeState(), Is.EqualTo(BossRangedAttack.TargetRangeState.InRange));
        Assert.That(rangedAttack.IsTargetInRange(), Is.True);

        player.transform.position = Vector3.right * (rangedAttack.AttackRange + 0.1f);
        Assert.That(rangedAttack.GetTargetRangeState(), Is.EqualTo(BossRangedAttack.TargetRangeState.TooFar));
        Assert.That(rangedAttack.IsTargetInRange(), Is.False);

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(boss);
    }

    [Test]
    public void BlasterWeapon_UsesCurrentGunVisualConfiguration()
    {
        WeaponData blaster = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapons/BlasterGun.asset");
        Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Character/khẩu súng.png");

        Assert.That(blaster, Is.Not.Null, "Không tìm thấy dữ liệu súng Blaster.");
        Assert.That(expectedSprite, Is.Not.Null, "Không tìm thấy sprite khẩu súng mới.");
        Assert.That(blaster.gunSprite, Is.EqualTo(expectedSprite),
            "Sprite trong BlasterGun.asset phải khớp sprite mới vì PlayerAutoShooter sẽ dùng dữ liệu này khi Play.");
        Assert.That(blaster.firePointOffset, Is.EqualTo(new Vector2(3.59f, -0.99f)),
            "FirePoint trong BlasterGun.asset phải khớp vị trí đã căn theo sprite mới.");
    }

    [Test]
    public void PlayerHealth_Revive_RestoresHalfHealthOnlyAfterDeath()
    {
        GameObject go = new GameObject("PlayerReviveTest");
        PlayerHealth health = go.AddComponent<PlayerHealth>();

        Assert.That(health.Revive(), Is.False, "Player còn sống không được gọi Revive.");
        health.TakeDamage(health.MaxHealth);

        bool revived = health.Revive(0.5f, 2f);

        Assert.That(revived, Is.True);
        Assert.That(health.IsDead, Is.False);
        Assert.That(health.CurrentHealth, Is.EqualTo(Mathf.CeilToInt(health.MaxHealth * 0.5f)));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void PlayerDataService_VipOwnership_DefaultsLockedAndPersists()
    {
        bool hadValue = PlayerPrefs.HasKey(PlayerDataService.VipOwnedKey);
        int originalValue = PlayerPrefs.GetInt(PlayerDataService.VipOwnedKey, 0);

        try
        {
            PlayerPrefs.DeleteKey(PlayerDataService.VipOwnedKey);
            Assert.That(PlayerDataService.IsVipOwned, Is.False);

            PlayerDataService.IsVipOwned = true;
            Assert.That(PlayerDataService.IsVipOwned, Is.True);
        }
        finally
        {
            if (hadValue) PlayerPrefs.SetInt(PlayerDataService.VipOwnedKey, originalValue);
            else PlayerPrefs.DeleteKey(PlayerDataService.VipOwnedKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void PlayerRunEndController_StageProgress_UsesWaveAndTimerProgress()
    {
        Assert.That(PlayerRunEndController.CalculateStageProgress(1, 0.5f, 10), Is.EqualTo(0.15f).Within(0.001f));
        Assert.That(PlayerRunEndController.CalculateStageProgress(99, 1f, 10), Is.EqualTo(1f));
        Assert.That(PlayerRunEndController.CalculateStageProgress(0, 0f, 0), Is.EqualTo(0f));
    }

    [Test]
    public void WaveHUD_BossIndicator_ShowsOnlyOffscreenAndClampsToCanvasEdge()
    {
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(0.5f, 0.5f, 1f), 0.02f), Is.True);
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(-0.1f, 0.5f, 1f), 0.02f), Is.False);
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(0.5f, 0.5f, -1f), 0.02f), Is.False);

        Vector2 left = WaveHUDController.CalculateBossIndicatorPosition(
            new Vector3(-0.2f, 0.5f, 1f),
            new Vector2(1080f, 1920f),
            new Vector2(150f, 150f),
            30f);
        Assert.That(left.x, Is.EqualTo(-435f).Within(0.01f));
        Assert.That(left.y, Is.EqualTo(0f).Within(0.01f));

        Vector2 topRight = WaveHUDController.CalculateBossIndicatorPosition(
            new Vector3(1.2f, 1.2f, 1f),
            new Vector2(1080f, 1920f),
            new Vector2(150f, 150f),
            30f);
        Assert.That(Mathf.Abs(topRight.x), Is.LessThanOrEqualTo(435.01f));
        Assert.That(Mathf.Abs(topRight.y), Is.LessThanOrEqualTo(855.01f));
    }

    [Test]
    public void BossHealthBar_NameSanitizer_RemovesUnsupportedWarningEmojiGlyphs()
    {
        string result = BossHealthBarUI.SanitizeBossDisplayName("⚠️ Boss Fight ⚠️");

        Assert.That(result, Is.EqualTo("BOSS FIGHT"));
        Assert.That(result.Contains("\u26A0"), Is.False);
        Assert.That(result.Contains("\uFE0F"), Is.False);
        Assert.That(BossHealthBarUI.SanitizeBossDisplayName(null), Is.EqualTo("BOSS"));
    }

    [Test]
    public void ObjectPool_DoubleReturn_DoesNotDuplicateInQueue()
    {
        GameObject prefab = new GameObject("PoolTestPrefab");
        GameObject container = new GameObject("PoolContainer");

        ObjectPool pool = new ObjectPool(prefab, 1, false, container.transform);
        pool.Initialize(container.transform);

        GameObject instance = pool.Get(Vector3.zero, Quaternion.identity);
        Assert.That(instance, Is.Not.Null);

        // Return first time
        pool.Return(instance);

        // Return second time (attempt double enqueue)
        pool.Return(instance);

        // Get instance once
        GameObject firstGet = pool.Get(Vector3.zero, Quaternion.identity);
        Assert.That(firstGet, Is.EqualTo(instance));

        // Get instance second time (should be null since pool has size 1 and canGrow = false)
        GameObject secondGet = pool.Get(Vector3.zero, Quaternion.identity);
        Assert.That(secondGet, Is.Null);

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(container);
    }

    [Test]
    public void PlayerDeathController_TriggersDeath_DisablesMovementAndInvokesEvents()
    {
        GameObject playerGo = new GameObject("PlayerTest");
        PlayerHealth health = playerGo.AddComponent<PlayerHealth>();
        PlayerMovement movement = playerGo.AddComponent<PlayerMovement>();
        PlayerDeathController deathCtrl = playerGo.AddComponent<PlayerDeathController>();

        bool deathStartedInvoked = false;
        deathCtrl.OnDeathStarted += () => { deathStartedInvoked = true; };

        deathCtrl.TriggerDeath();

        Assert.That(deathCtrl.IsDeathSequenceActive, Is.True);
        Assert.That(deathStartedInvoked, Is.True);
        Assert.That(movement.enabled, Is.False);

        Object.DestroyImmediate(playerGo);
    }

    [Test]
    public void VFXBoomPrefab_ExistsAndHasRequiredComponents()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX Boom.prefab");
        Assert.That(prefab, Is.Not.Null, "VFX Boom.prefab phải tồn tại trong thư mục Assets/Prefabs.");

        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        Assert.That(sr, Is.Not.Null, "VFX Boom.prefab phải có SpriteRenderer.");
        Assert.That(sr.sortingOrder, Is.GreaterThanOrEqualTo(20), "VFX Boom SpriteRenderer sorting order phải >= 20 để hiển thị trên cùng.");

        Animator anim = prefab.GetComponent<Animator>();
        Assert.That(anim, Is.Not.Null, "VFX Boom.prefab phải có Animator.");

        AutoDestroyVFX autoDestroy = prefab.GetComponent<AutoDestroyVFX>();
        Assert.That(autoDestroy, Is.Not.Null, "VFX Boom.prefab phải có AutoDestroyVFX để tự hủy sau khi nổ xong.");
        Assert.That(autoDestroy.UseUnscaledTime, Is.True, "VFX Boom phải dùng unscaled time để vẫn chạy khi gameplay tạm dừng.");

        Assert.That(anim.updateMode, Is.EqualTo(AnimatorUpdateMode.UnscaledTime), "Animator VFX Boom phải chạy bằng unscaled time.");

        AnimationClip boomClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaton/VFX/PlayerBoom.anim");
        Assert.That(boomClip, Is.Not.Null, "PlayerBoom.anim phải tồn tại.");
        Assert.That(boomClip.isLooping, Is.False, "PlayerBoom.anim không được loop (isLooping = false).");
    }

    [Test]
    public void PlayerDeathController_ExplosionVfx_KeepsPlayerVisibleUntilSequenceCompletes()
    {
        GameObject playerGo = new GameObject("PlayerExplosionDeathTest");
        PlayerHealth health = playerGo.AddComponent<PlayerHealth>();
        PlayerMovement movement = playerGo.AddComponent<PlayerMovement>();
        SpriteRenderer playerSr = playerGo.AddComponent<SpriteRenderer>();
        PlayerDeathController deathCtrl = playerGo.AddComponent<PlayerDeathController>();

        Assert.That(deathCtrl.UseExplosionVfx, Is.True, "UseExplosionVfx mặc định phải là true.");
        Assert.That(deathCtrl.ExplosionDuration, Is.GreaterThan(0f), "ExplosionDuration phải > 0.");

        deathCtrl.TriggerDeath();

        Assert.That(deathCtrl.IsDeathSequenceActive, Is.True);
        Assert.That(movement.enabled, Is.False);
        Assert.That(playerSr.enabled, Is.True, "Player phải còn hiển thị trong animation Die và chỉ được ẩn sau khi VFX Boom hoàn tất.");

        Object.DestroyImmediate(playerGo);
    }

    [Test]
    public void PlayerDeathController_ResetForRevive_RestoresPlayerComponents()
    {
        GameObject playerGo = new GameObject("PlayerReviveRestoreTest");
        playerGo.AddComponent<PlayerHealth>();
        PlayerMovement movement = playerGo.AddComponent<PlayerMovement>();
        SpriteRenderer renderer = playerGo.AddComponent<SpriteRenderer>();
        BoxCollider2D collider = playerGo.AddComponent<BoxCollider2D>();
        Rigidbody2D rigidbody = playerGo.AddComponent<Rigidbody2D>();
        PlayerDeathController deathCtrl = playerGo.AddComponent<PlayerDeathController>();

        movement.enabled = false;
        renderer.enabled = false;
        collider.enabled = false;
        rigidbody.simulated = false;

        deathCtrl.ResetForRevive();

        Assert.That(movement.enabled, Is.True);
        Assert.That(renderer.enabled, Is.True);
        Assert.That(collider.enabled, Is.True);
        Assert.That(rigidbody.simulated, Is.True);
        Assert.That(deathCtrl.IsDeathSequenceActive, Is.False);

        Object.DestroyImmediate(playerGo);
    }

    [Test]
    public void DieAnimation_HasNoRootPositionCurves_ToPreventAnimatorTransformLock()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaton/Player/Die.anim");
        Assert.That(clip, Is.Not.Null);

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (string.IsNullOrEmpty(binding.path) && binding.propertyName.StartsWith("m_LocalPosition"))
            {
                Assert.Fail($"Die.anim contains root position curve: {binding.propertyName}, which locks Player movement in Animator!");
            }
        }
    }

    [Test]
    public void BigCreep_Animations_HaveNoRootTransformLocks()
    {
        AnimationClip dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaton/Enemy/Creep/DieBig.anim");
        Assert.That(dieClip, Is.Not.Null, "DieBig.anim must exist.");

        EditorCurveBinding[] dieBindings = AnimationUtility.GetCurveBindings(dieClip);
        foreach (var binding in dieBindings)
        {
            if (string.IsNullOrEmpty(binding.path) && binding.propertyName.StartsWith("m_LocalPosition"))
            {
                Assert.Fail($"DieBig.anim contains root position curve: {binding.propertyName}, which causes BigCreep teleport on death!");
            }
            if (string.IsNullOrEmpty(binding.path) && binding.propertyName.StartsWith("m_LocalScale"))
            {
                Assert.Fail($"DieBig.anim contains root scale curve: {binding.propertyName}, which overrides BigCreep death facing!");
            }
        }

        AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaton/Enemy/Creep/runbig.anim");
        Assert.That(runClip, Is.Not.Null, "runbig.anim must exist.");

        EditorCurveBinding[] runBindings = AnimationUtility.GetCurveBindings(runClip);
        foreach (var binding in runBindings)
        {
            if (string.IsNullOrEmpty(binding.path) && binding.propertyName.StartsWith("m_LocalScale"))
            {
                Assert.Fail($"runbig.anim contains root scale curve: {binding.propertyName}, which overrides BigCreep sprite facing!");
            }
        }
    }

    [Test]
    public void BigCreepPrefab_Configuration_MatchesStandardEnemyRequirements()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BigCreep.prefab");
        Assert.That(prefab, Is.Not.Null, "BigCreep.prefab must exist in Assets/Prefabs.");

        Rigidbody2D rb = prefab.GetComponent<Rigidbody2D>();
        Assert.That(rb, Is.Not.Null, "BigCreep must have Rigidbody2D.");
        Assert.That(rb.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic), "BigCreep Rigidbody2D must be Dynamic.");

        Collider2D col = prefab.GetComponent<Collider2D>();
        Assert.That(col, Is.Not.Null, "BigCreep must have Collider2D.");
        Assert.That(col.isTrigger, Is.False, "BigCreep Collider2D must be solid (isTrigger = false).");

        EnemyHealth health = prefab.GetComponent<EnemyHealth>();
        Assert.That(health, Is.Not.Null, "BigCreep must have EnemyHealth.");

        EnemyMovement movement = prefab.GetComponent<EnemyMovement>();
        Assert.That(movement, Is.Not.Null, "BigCreep must have EnemyMovement.");
    }

    [Test]
    public void TargetFrameRate_IsConfiguredForSmoothGameplay()
    {
        PlayerDataService.InitializeApplicationSettings();
        Assert.That(Application.targetFrameRate, Is.GreaterThanOrEqualTo(60));
    }

    [Test]
    public void AAAGoldenStarParticleDissolveShader_LoadsAndContainsRequiredProperties()
    {
        Shader shader = Shader.Find("Custom/2D/SpriteDissolve");
        Assert.That(shader, Is.Not.Null, "Custom/2D/SpriteDissolve shader must exist in the project.");

        Material mat = new Material(shader);
        Assert.That(mat.HasProperty("_DissolveAmount"), Is.True);
        Assert.That(mat.HasProperty("_DissolveDirectionMode"), Is.True);
        Assert.That(mat.HasProperty("_ParticleShapeMode"), Is.True);
        Assert.That(mat.HasProperty("_ParticleGridSize"), Is.True);
        Assert.That(mat.HasProperty("_DisperseSpeed"), Is.True);
        Assert.That(mat.HasProperty("_RadialBurstSpread"), Is.True);
        Assert.That(mat.HasProperty("_UpwardDrift"), Is.True);
        Assert.That(mat.HasProperty("_SwirlStrength"), Is.True);
        Assert.That(mat.HasProperty("_DisperseChaos"), Is.True);
        Assert.That(mat.HasProperty("_ParticleShrink"), Is.True);
        Assert.That(mat.HasProperty("_Gravity"), Is.True);
        Assert.That(mat.HasProperty("_EdgeColor"), Is.True);
        Assert.That(mat.HasProperty("_InnerEdgeColor"), Is.True);
        Assert.That(mat.HasProperty("_EdgeIntensity"), Is.True);
        Assert.That(mat.HasProperty("_SupernovaFlash"), Is.True);
        Assert.That(mat.HasProperty("_StarSparkleSpeed"), Is.True);
        Assert.That(mat.HasProperty("_PrismaticShimmer"), Is.True);
        Assert.That(mat.HasProperty("_HaloGlowIntensity"), Is.True);
        Assert.That(mat.HasProperty("_SpriteUVRect"), Is.True);
        Object.DestroyImmediate(mat);
    }

    [Test]
    public void PlayerDataService_CurrencyManagement_WorksCorrectly()
    {
        PlayerDataService.DataChips = 5000;
        PlayerDataService.RedGems = 1000;

        Assert.That(PlayerDataService.HasEnoughDataChips(3000), Is.True);
        Assert.That(PlayerDataService.TrySpendDataChips(2000), Is.True);
        Assert.That(PlayerDataService.DataChips, Is.EqualTo(3000));

        PlayerDataService.AddDataChips(1500);
        Assert.That(PlayerDataService.DataChips, Is.EqualTo(4500));

        Assert.That(PlayerDataService.HasEnoughRedGems(1500), Is.False);
        Assert.That(PlayerDataService.TrySpendRedGems(500), Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(500));
    }

    [Test]
    public void ChipManager_TestMode_ProvidesUnlimitedChipsAndEnergy()
    {
        ChipManager.IsTestMode = false;
        ChipManager.DataChips = 100;
        Assert.That(ChipManager.HasEnoughDataChips(200), Is.False);

        // Turn on Test Mode
        ChipManager.IsTestMode = true;
        Assert.That(ChipManager.IsTestMode, Is.True);
        Assert.That(ChipManager.IsInfiniteInTest, Is.True);
        Assert.That(ChipManager.HasEnoughDataChips(999999), Is.True);
        Assert.That(ChipManager.TrySpendDataChips(500000), Is.True);
        Assert.That(ChipManager.HasEnoughRedGems(999999), Is.True);
        Assert.That(ChipManager.TrySpendRedGems(500000), Is.True);
        Assert.That(ChipManager.HasEnoughEnergy(9999), Is.True);
        Assert.That(ChipManager.TrySpendEnergy(500), Is.True);
        Assert.That(ChipManager.HasEnoughAdvanceStones(9999), Is.True);
        Assert.That(ChipManager.TrySpendAdvanceStones(500), Is.True);

        // Turn off Test Mode
        ChipManager.IsTestMode = false;
        Assert.That(ChipManager.DataChips, Is.EqualTo(100));
    }

    [Test]
    public void PlayerDataService_SelectedWeaponId_FallbackAndSet_WorksCorrectly()
    {
        string original = PlayerPrefs.GetString(PlayerDataService.SelectedWeaponIdKey, "blaster");
        string changedWeapon = null;
        System.Action<string> handler = id => changedWeapon = id;
        PlayerDataService.OnSelectedWeaponChanged += handler;

        try
        {
            // Null or whitespace fallback to "blaster"
            PlayerDataService.SelectedWeaponId = null;
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("blaster"));
            Assert.That(changedWeapon, Is.EqualTo("blaster"));

            PlayerDataService.SelectedWeaponId = "   ";
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("blaster"));

            // Custom weapon ID
            PlayerDataService.SelectedWeaponId = "laser_blaster";
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("laser_blaster"));
            Assert.That(changedWeapon, Is.EqualTo("laser_blaster"));
        }
        finally
        {
            PlayerDataService.OnSelectedWeaponChanged -= handler;
            PlayerDataService.SelectedWeaponId = original;
        }
    }

    [Test]
    public void CameraFollow_ZeroOrNegativeSpeed_SnapsImmediatelyToDesiredPosition()
    {
        GameObject cameraGo = new GameObject("CameraTest");
        GameObject targetGo = new GameObject("TargetTest");
        targetGo.transform.position = new Vector3(100f, 200f, 0f);
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);

        CameraFollow follow = cameraGo.AddComponent<CameraFollow>();
        follow.SetTarget(targetGo.transform);
        follow.FollowSpeed = 0f;
        follow.Offset = new Vector2(5f, -5f);

        // Update follow with followSpeed = 0
        follow.UpdateFollow(0.016f);

        Assert.That(cameraGo.transform.position.x, Is.EqualTo(105f));
        Assert.That(cameraGo.transform.position.y, Is.EqualTo(195f));
        Assert.That(cameraGo.transform.position.z, Is.EqualTo(-10f));

        Object.DestroyImmediate(cameraGo);
        Object.DestroyImmediate(targetGo);
    }

    [Test]
    public void ShopController_VNDOffer_FailsClosedAndGrantsNoRewards()
    {
        GameObject shopGo = new GameObject("ShopTest");
        ShopController shop = shopGo.AddComponent<ShopController>();

        int initialGems = ChipManager.RedGems;
        ShopController.Offer vndOffer = new ShopController.Offer
        {
            id = "vnd-pack-1",
            displayName = "1000 RED GEMS (VND)",
            currency = ShopController.CurrencyType.VND,
            price = 50000,
            reward = ShopController.RewardType.RedGem,
            rewardAmount = 1000
        };

        shop.SetOffersForTesting(new[] { vndOffer });
        bool result = shop.TryPurchase(0);

        Assert.That(result, Is.False, "Purchases with CurrencyType.VND must fail-closed.");
        Assert.That(ChipManager.RedGems, Is.EqualTo(initialGems), "No rewards must be granted for VND offers without payment integration.");

        Object.DestroyImmediate(shopGo);
    }

    [Test]
    public void MainMenu_Architecture_SingleChapterPanelUnderCanvasContent()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Assert.That(File.Exists(scenePath), Is.True, "MainMenu.unity must exist.");

        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        Assert.That(canvas, Is.Not.Null, "Canvas must exist in MainMenu.unity.");

        Transform contentTr = canvas.transform.Find("Content");
        Assert.That(contentTr, Is.Not.Null, "Canvas/Content must exist in MainMenu.unity.");

        Transform contentChapterPanel = contentTr.Find("ChapterPanel");
        Assert.That(contentChapterPanel, Is.Not.Null, "Canvas/Content/ChapterPanel must exist.");

        Transform rootChapterPanel = canvas.transform.Find("ChapterPanel");
        Assert.That(rootChapterPanel, Is.Null, "Duplicate Canvas/ChapterPanel must NOT exist.");

        // Count all ChapterPanel objects in scene
        int count = 0;
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            foreach (var transform in rootGo.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == "ChapterPanel")
                {
                    count++;
                }
            }
        }
        Assert.That(count, Is.EqualTo(1), "There must be exactly ONE ChapterPanel in MainMenu.unity.");
    }

    [Test]
    public void MainMenu_BottomNavigation_ChapterItem_PointsToContentChapterPanel()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        Transform contentChapterPanel = canvas.transform.Find("Content/ChapterPanel");
        Assert.That(contentChapterPanel, Is.Not.Null);

        BottomNavigationController bottomNav = Object.FindObjectOfType<BottomNavigationController>();
        Assert.That(bottomNav, Is.Not.Null, "BottomNavigationController must exist.");

        SerializedObject navSO = new SerializedObject(bottomNav);
        SerializedProperty itemsProp = navSO.FindProperty("items");
        Assert.That(itemsProp.arraySize, Is.GreaterThanOrEqualTo(3));

        SerializedProperty chapterItem = itemsProp.GetArrayElementAtIndex(2);
        GameObject boundPanel = chapterItem.FindPropertyRelative("panel").objectReferenceValue as GameObject;
        Assert.That(boundPanel, Is.EqualTo(contentChapterPanel.gameObject), "BottomNavigation items[2].panel must point to Canvas/Content/ChapterPanel.");
    }

    [Test]
    public void MainMenu_ShopPanel_HasFunctionalShopControllerAndOffers()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        Transform content = canvas.transform.Find("Content");
        Assert.That(content.GetComponent<ContentSizeFitter>(), Is.Null, "Canvas/Content must not resize full-screen tab panels.");
        Assert.That(content.GetComponent<GridLayoutGroup>(), Is.Null, "Canvas/Content must not lay out full-screen tab panels as grid cells.");

        Transform shopPanel = canvas.transform.Find("Content/ShopPanel");
        Assert.That(shopPanel, Is.Not.Null, "Canvas/Content/ShopPanel must exist.");
        Assert.That(shopPanel.GetComponent<ScrollRect>(), Is.Not.Null, "ShopPanel must be scrollable.");

        ShopController shop = shopPanel.GetComponent<ShopController>();
        Assert.That(shop, Is.Not.Null, "ShopPanel must use ShopController instead of a coming-soon placeholder.");

        SerializedObject shopSO = new SerializedObject(shop);
        Assert.That(shopSO.FindProperty("offers").arraySize, Is.EqualTo(7), "ShopPanel must expose all seven configured offers.");
    }

    [Test]
    public void MainMenu_DefaultNavigationItem_IsTheOnlyActiveContentPanel()
    {
        const string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        BottomNavigationController bottomNav = Object.FindObjectOfType<BottomNavigationController>();
        Assert.That(bottomNav, Is.Not.Null);

        SerializedObject navSO = new SerializedObject(bottomNav);
        SerializedProperty items = navSO.FindProperty("items");
        int defaultSelectedIndex = navSO.FindProperty("defaultSelectedIndex").intValue;
        Assert.That(defaultSelectedIndex, Is.InRange(0, items.arraySize - 1));

        for (int i = 0; i < items.arraySize; i++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            bool selected = i == defaultSelectedIndex;
            GameObject panel = item.FindPropertyRelative("panel").objectReferenceValue as GameObject;
            Assert.That(panel, Is.Not.Null, $"Navigation item {i} must reference a panel.");
            Assert.That(panel.activeSelf, Is.EqualTo(selected), "Exactly the configured default tab must be active in the saved scene.");

            Sprite activeSprite = item.FindPropertyRelative("activeSprite").objectReferenceValue as Sprite;
            Sprite inactiveSprite = item.FindPropertyRelative("inactiveSprite").objectReferenceValue as Sprite;
            Button button = item.FindPropertyRelative("button").objectReferenceValue as Button;
            Assert.That(button, Is.Not.Null, $"Navigation item {i} must reference a Button.");
            Assert.That(activeSprite, Is.Not.Null, $"Navigation item {i} must have activeSprite assigned.");
            Assert.That(inactiveSprite, Is.Not.Null, $"Navigation item {i} must have inactiveSprite assigned.");
        }
    }

    [TestCase(0f, 1f)]
    [TestCase(90f, 1f)]
    [TestCase(180f, -1f)]
    [TestCase(-90f, 1f)]
    public void CalculateAimScale_FlipsOnlyYAxis(float angle, float expectedYSign)
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod(
            "CalculateAimScale",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(2f, 3f, 4f);
        Vector3 result = (Vector3)method.Invoke(null, new object[] { angle, baseScale });

        Assert.That(result.x, Is.EqualTo(baseScale.x));
        Assert.That(result.y, Is.EqualTo(Mathf.Abs(baseScale.y) * expectedYSign));
        Assert.That(result.z, Is.EqualTo(baseScale.z));
    }

    [TestCase(false, 2f)]
    [TestCase(true, -2f)]
    public void CalculateBodyScale_MirrorsOnlyXAxis(
        bool isAimingLeft,
        float expectedX
    )
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod(
            "CalculateBodyScale",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(2f, 3f, 4f);
        Vector3 result = (Vector3)method.Invoke(
            null,
            new object[] { isAimingLeft, baseScale }
        );

        Assert.That(result.x, Is.EqualTo(expectedX));
        Assert.That(result.y, Is.EqualTo(baseScale.y));
        Assert.That(result.z, Is.EqualTo(baseScale.z));
    }

    [TestCase(0f, false, 0f)]
    [TestCase(90f, false, 90f)]
    [TestCase(180f, true, 0f)]
    [TestCase(135f, true, 45f)]
    [TestCase(-135f, true, -45f)]
    public void CalculateLocalAimAngle_CompensatesBodyMirror(
        float worldAngle,
        bool isAimingLeft,
        float expectedLocalAngle
    )
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod(
            "CalculateLocalAimAngle",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(method, Is.Not.Null);

        float result = (float)method.Invoke(
            null,
            new object[] { worldAngle, isAimingLeft }
        );

        Assert.That(Mathf.DeltaAngle(expectedLocalAngle, result), Is.EqualTo(0f));
    }

    [Test]
    public void MainMenu_LabAtlasSpriteReferences_HaveValidLocalIds()
    {
        const string scenePath = "Assets/Scenes/MainMenu.unity";
        const string atlasPath = "Assets/UI/Lab/Generated/lab-icon-atlas.png";
        string atlasGuid = AssetDatabase.AssetPathToGUID(atlasPath);
        Assert.That(atlasGuid, Is.Not.Empty);

        var validLocalIds = new HashSet<long>();
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(atlasPath))
        {
            if (!(asset is Sprite sprite)) continue;

            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string spriteGuid, out long localId), Is.True);
            Assert.That(spriteGuid, Is.EqualTo(atlasGuid));
            validLocalIds.Add(localId);
        }

        MatchCollection references = Regex.Matches(
            File.ReadAllText(scenePath),
            @"fileID:\s*(-?\d+), guid:\s*" + atlasGuid);
        Assert.That(references.Count, Is.GreaterThan(0));

        var missingLocalIds = new HashSet<long>();
        foreach (Match reference in references)
        {
            long localId = long.Parse(reference.Groups[1].Value);
            if (!validLocalIds.Contains(localId)) missingLocalIds.Add(localId);
        }

        Assert.That(missingLocalIds, Is.Empty, "Every Lab atlas sprite reference in MainMenu must resolve to a current sprite local ID.");
    }

    [Test]
    public void MainMenu_ChapterPanel_ImagesHaveNonNullSprites()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Assert.That(scene.IsValid(), Is.True);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        Transform chapterPanel = canvas.transform.Find("Content/ChapterPanel");
        Assert.That(chapterPanel, Is.Not.Null);

        // Verify key sprites in ChapterPanel
        Transform stageBg = chapterPanel.Find("StagePreviewWindow/Viewport/StageBackground");
        Assert.That(stageBg, Is.Not.Null);
        Image stageBgImg = stageBg.GetComponent<Image>();
        Assert.That(stageBgImg.sprite, Is.Not.Null, "StageBackground must have a valid sprite assigned.");

        Transform boss = chapterPanel.Find("StagePreviewWindow/Viewport/BossSilhouette");
        Assert.That(boss, Is.Not.Null);
        Image bossImg = boss.GetComponent<Image>();
        Assert.That(bossImg, Is.Not.Null);
        Assert.That(bossImg.sprite, Is.Not.Null, "BossSilhouette must have a valid sprite assigned.");

        Transform rewardIcon = chapterPanel.Find("SubWidgetsContainer/QuestWidget/RewardBox/RewardIcon");
        Assert.That(rewardIcon, Is.Not.Null);
        Image rewardImg = rewardIcon.GetComponent<Image>();
        Assert.That(rewardImg, Is.Not.Null);
        Assert.That(rewardImg.sprite, Is.Not.Null, "RewardIcon must have a valid sprite assigned.");
    }

    [Test]
    public void ChapterDatabase_AllChapters_HaveValidPreviewAndBossSprites()
    {
        ChapterDatabase db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
        Assert.That(db, Is.Not.Null);
        Assert.That(db.Count, Is.GreaterThanOrEqualTo(4));

        for (int i = 0; i < db.Count; i++)
        {
            ChapterData chapter = db.GetChapter(i);
            Assert.That(chapter, Is.Not.Null, $"Chapter at index {i} must exist.");
            Assert.That(chapter.previewBackground, Is.Not.Null, $"Chapter {chapter.chapterNumber} must have previewBackground assigned.");
            Assert.That(chapter.bossSilhouette, Is.Not.Null, $"Chapter {chapter.chapterNumber} must have bossSilhouette assigned.");
        }
    }

    [Test]
    public void PauseModalController_OpenAndClose_TogglesTimeScaleAndActiveState()
    {
        GameObject modalRoot = new GameObject("PauseModalRoot");
        GameObject statsPnl = new GameObject("StatsPanel");
        GameObject chipPnl = new GameObject("ChipPanel");
        GameObject artPnl = new GameObject("ArtPanel");
        GameObject defPnl = new GameObject("DefPanel");
        GameObject atkPnl = new GameObject("AtkPanel");
        GameObject othPnl = new GameObject("OthPanel");

        GameObject resumeBtnGo = new GameObject("ResumeBtn");
        Button resumeBtn = resumeBtnGo.AddComponent<Button>();

        GameObject homeBtnGo = new GameObject("HomeBtn");
        Button homeBtn = homeBtnGo.AddComponent<Button>();

        GameObject hpTxtGo = new GameObject("HpText");
        TMP_Text hpTxt = hpTxtGo.AddComponent<TextMeshProUGUI>();

        GameObject defTxtGo = new GameObject("DefText");
        TMP_Text defTxt = defTxtGo.AddComponent<TextMeshProUGUI>();

        GameObject lvlTxtGo = new GameObject("LvlText");
        TMP_Text lvlTxt = lvlTxtGo.AddComponent<TextMeshProUGUI>();

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        pauseCtrl.SetReferencesForTesting(
            modalRoot, resumeBtn, homeBtn,
            null, null, null,
            statsPnl, chipPnl, artPnl,
            null, null, null,
            defPnl, atkPnl, othPnl,
            hpTxt, defTxt, lvlTxt
        );

        float originalTimeScale = Time.timeScale;
        try
        {
            pauseCtrl.OpenPauseModal();
            Assert.That(pauseCtrl.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(modalRoot.activeSelf, Is.True);

            pauseCtrl.ResumeGame();
            Assert.That(pauseCtrl.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(modalRoot.activeSelf, Is.False);
        }
        finally
        {
            Time.timeScale = originalTimeScale;
            Object.DestroyImmediate(modalRoot);
            Object.DestroyImmediate(statsPnl);
            Object.DestroyImmediate(chipPnl);
            Object.DestroyImmediate(artPnl);
            Object.DestroyImmediate(defPnl);
            Object.DestroyImmediate(atkPnl);
            Object.DestroyImmediate(othPnl);
            Object.DestroyImmediate(resumeBtnGo);
            Object.DestroyImmediate(homeBtnGo);
            Object.DestroyImmediate(hpTxtGo);
            Object.DestroyImmediate(defTxtGo);
            Object.DestroyImmediate(lvlTxtGo);
        }
    }

    [Test]
    public void PauseModalController_TabSwitching_UpdatesPanels()
    {
        GameObject modalRoot = new GameObject("PauseModalRoot");
        GameObject statsPnl = new GameObject("StatsPanel");
        GameObject chipPnl = new GameObject("ChipPanel");
        GameObject artPnl = new GameObject("ArtPanel");
        GameObject defPnl = new GameObject("DefPanel");
        GameObject atkPnl = new GameObject("AtkPanel");
        GameObject othPnl = new GameObject("OthPanel");

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        pauseCtrl.SetReferencesForTesting(
            modalRoot, null, null,
            null, null, null,
            statsPnl, chipPnl, artPnl,
            null, null, null,
            defPnl, atkPnl, othPnl,
            null, null, null
        );

        pauseCtrl.SelectMainTab(1); // CHIPSET
        Assert.That(pauseCtrl.CurrentMainTab, Is.EqualTo(1));
        Assert.That(statsPnl.activeSelf, Is.False);
        Assert.That(chipPnl.activeSelf, Is.True);
        Assert.That(artPnl.activeSelf, Is.False);

        pauseCtrl.SelectMainTab(2); // ARTIFACT
        Assert.That(pauseCtrl.CurrentMainTab, Is.EqualTo(2));
        Assert.That(statsPnl.activeSelf, Is.False);
        Assert.That(chipPnl.activeSelf, Is.False);
        Assert.That(artPnl.activeSelf, Is.True);

        pauseCtrl.SelectMainTab(0); // STATS
        Assert.That(pauseCtrl.CurrentMainTab, Is.EqualTo(0));
        Assert.That(statsPnl.activeSelf, Is.True);
        Assert.That(chipPnl.activeSelf, Is.False);
        Assert.That(artPnl.activeSelf, Is.False);

        // Sub-Tabs
        pauseCtrl.SelectSubTab(1); // Attack
        Assert.That(pauseCtrl.CurrentSubTab, Is.EqualTo(1));
        Assert.That(defPnl.activeSelf, Is.False);
        Assert.That(atkPnl.activeSelf, Is.True);
        Assert.That(othPnl.activeSelf, Is.False);

        pauseCtrl.SelectSubTab(2); // Other
        Assert.That(pauseCtrl.CurrentSubTab, Is.EqualTo(2));
        Assert.That(defPnl.activeSelf, Is.False);
        Assert.That(atkPnl.activeSelf, Is.False);
        Assert.That(othPnl.activeSelf, Is.True);

        Object.DestroyImmediate(modalRoot);
        Object.DestroyImmediate(statsPnl);
        Object.DestroyImmediate(chipPnl);
        Object.DestroyImmediate(artPnl);
        Object.DestroyImmediate(defPnl);
        Object.DestroyImmediate(atkPnl);
        Object.DestroyImmediate(othPnl);
    }

    [Test]
    public void PauseModalController_HomeButton_OpensQuitConfirmModal_AndNoCancels()
    {
        GameObject modalRoot = new GameObject("PauseModalRoot");
        GameObject confirmPnl = new GameObject("QuitConfirmDialog");

        GameObject homeBtnGo = new GameObject("HomeBtn");
        Button homeBtn = homeBtnGo.AddComponent<Button>();

        GameObject noBtnGo = new GameObject("NoBtn");
        Button noBtn = noBtnGo.AddComponent<Button>();

        GameObject okBtnGo = new GameObject("OkBtn");
        Button okBtn = okBtnGo.AddComponent<Button>();

        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        pauseCtrl.SetReferencesForTesting(
            modalRoot, null, homeBtn,
            null, null, null,
            null, null, null,
            null, null, null,
            null, null, null,
            null, null, null,
            confirmPnl, noBtn, okBtn
        );

        confirmPnl.SetActive(false);

        // Click Home -> Confirmation dialog opens
        pauseCtrl.OnHomeButtonClicked();
        Assert.That(confirmPnl.activeSelf, Is.True);

        // Click No -> Confirmation dialog closes
        pauseCtrl.OnConfirmNoClicked();
        Assert.That(confirmPnl.activeSelf, Is.False);

        Object.DestroyImmediate(modalRoot);
        Object.DestroyImmediate(confirmPnl);
        Object.DestroyImmediate(homeBtnGo);
        Object.DestroyImmediate(noBtnGo);
        Object.DestroyImmediate(okBtnGo);
    }

    [Test]
    public void Chipset_All24Chips_AreProperlyConfigured()
    {
        GameObject go = new GameObject("ChipsetControllerTest");
        ChipsetController controller = go.AddComponent<ChipsetController>();
        controller.InitializeDatabase();

        Assert.That(controller.AllChips.Count, Is.EqualTo(24), "Database phải chứa đúng 24 loại chip set.");

        string[] expectedNames = {
            "Standard Gun", "Rifle", "Rocket Punch", "Spinning Blade", "Multigun",
            "Gun Turret", "Spiky Discus", "Shotgun", "Energy Jumper Cables", "High-Explosive Mine",
            "Aiming Lens", "Plasma Field", "Laser Eye", "Biochemical Mine", "Tesla Coil",
            "ATK Module", "Black Hole Mine", "Sonic Boom", "Big Battery", "Turret Module",
            "Ice Turret", "Invincible Shield", "Healing Turret", "Flamethrower"
        };

        for (int i = 0; i < 24; i++)
        {
            var chip = controller.AllChips[i];
            Assert.That(chip.id, Is.EqualTo(i + 1));
            Assert.That(chip.chipName, Is.EqualTo(expectedNames[i]));
            Assert.That(string.IsNullOrWhiteSpace(chip.iconKey), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(chip.baseStatsSummary), Is.False, $"Base stats của {chip.chipName} không được rỗng.");
            Assert.That(string.IsNullOrWhiteSpace(chip.magicBonus), Is.False, $"Magic bonus của {chip.chipName} không được rỗng.");
            Assert.That(string.IsNullOrWhiteSpace(chip.rareBonus), Is.False, $"Rare bonus của {chip.chipName} không được rỗng.");
            Assert.That(string.IsNullOrWhiteSpace(chip.uniqueBonus), Is.False, $"Unique bonus của {chip.chipName} không được rỗng.");
            Assert.That(string.IsNullOrWhiteSpace(chip.epicBonus), Is.False, $"Epic bonus của {chip.chipName} không được rỗng.");
        }

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_PrimaryTenStats_MatchDesignTable()
    {
        List<ChipItemData> chips = ChipsetController.CreateDefaultDatabase();
        string[] expectedBaseStats = {
            "ATK <color=#FFCB49>53.13</color>\n<color=#FFCB49>Fast</color> ATK Speed",
            "ATK <color=#FFCB49>10.5</color>\n<color=#FFCB49>Fast</color> ATK Speed",
            "ATK <color=#FFCB49>70</color> / AoE ATK <color=#FFCB49>37</color>\n<color=#FFCB49>Slow</color> ATK Speed",
            "ATK <color=#FFCB49>36</color>\n<color=#FFCB49>Fast</color> ATK Speed",
            "ATK <color=#FFCB49>19</color> | 4 shells\n<color=#FFCB49>Slow</color> ATK Speed",
            "ATK <color=#FFCB49>27</color> | Duration 14.4s | CD 8.4s\n<color=#FFCB49>Fast</color> ATK Speed",
            "ATK <color=#FFCB49>30</color>\n<color=#FFCB49>Normal</color> Spin Speed",
            "ATK <color=#FFCB49>86</color>\n<color=#FFCB49>Slow</color> ATK Speed",
            "Life Steal <color=#FFCB49>2.3%</color>",
            "Mine AoE ATK <color=#FFCB49>27</color>\nCooldown: 5.55s"
        };

        string[][] expectedTierBonuses = {
            new[] { "ATK +15%", "ATK Speed +15%", "+5% Life Steal", "Adds Penetration Skill" },
            new[] { "ATK +25%", "ATK Speed +20%", "ATK +80%", "ATK Speed +35%" },
            new[] { "ATK +40%", "ATK Speed +40%", "AoE ATK Range +40%", "ATK +180%" },
            new[] { "ATK Speed +9%", "ATK Speed +18%", "Spin Speed +36%", "ATK Speed +36%" },
            new[] { "Adds +1 shells", "Adds +1 shells", "Adds +3 shells", "Adds +4 shells" },
            new[] { "Turret Duration +20%", "Turret Cooldown -30%", "Turret Duration +20%", "Turret Duration +30%" },
            new[] { "+1 Discus", "Spin Speed +30%", "+1 Discus", "Spin Speed +35%" },
            new[] { "ATK +15%", "ATK +15%", "Adds Penetration Skill", "Fires two times in a row" },
            new[] { "All Weapons' +1% Life Steal", "All Weapons' +1% Life Steal", "All Weapons' +1% Life Steal", "All Weapons' +2% Life Steal" },
            new[] { "ATK +20%", "Cooldown -20%", "ATK +55%", "ATK +144%" }
        };

        for (int i = 0; i < 10; i++)
        {
            ChipItemData chip = chips[i];
            Assert.That(chip.baseStatsSummary, Is.EqualTo(expectedBaseStats[i]), $"Base stats của {chip.chipName} phải khớp bảng thiết kế.");
            Assert.That(
                new[] { chip.magicBonus, chip.rareBonus, chip.uniqueBonus, chip.epicBonus },
                Is.EqualTo(expectedTierBonuses[i]),
                $"Tier bonus của {chip.chipName} phải khớp bảng thiết kế.");
        }
    }

    [Test]
    public void Chipset_TierLevelCaps_FollowProgressionRules()
    {
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Magic), Is.EqualTo(6), "Tier 1 (Magic) max level phải là 6.");
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Rare), Is.EqualTo(9), "Tier 2 (Rare) max level phải là 9.");
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Unique), Is.EqualTo(14), "Tier 3 (Unique) max level phải là 14.");
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Epic), Is.EqualTo(18), "Tier 4 (Epic) max level phải là 18.");
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Holographic), Is.EqualTo(24), "Advance Tier (Tier 5 / Holographic) max level phải là 24.");
    }

    [Test]
    public void Chipset_AdvanceTier_Requires10Stones_ToReachAdvanceTier()
    {
        ChipItemData epicChip = new ChipItemData
        {
            id = 1,
            chipName = "Standard Gun",
            tier = ChipTier.Epic,
            level = 18,
            count = 10,
            requiredCount = 0
        };

        Assert.That(epicChip.IsAtTierCap, Is.True);
        Assert.That(epicChip.CanAdvanceTier, Is.True);
        Assert.That(epicChip.NeedsAdvanceStones, Is.True);
        Assert.That(epicChip.AdvanceStoneCost, Is.EqualTo(10));

        // Test with 0 Advance Stones
        ChipManager.AdvanceStones = 0;
        bool advancedWithoutStones = epicChip.AdvanceTier();
        Assert.That(advancedWithoutStones, Is.False, "Không thể đột phá lên Advance Tier nếu không đủ 10 Đá Tiến Bậc.");
        Assert.That(epicChip.tier, Is.EqualTo(ChipTier.Epic));

        // Test with 15 Advance Stones
        ChipManager.AdvanceStones = 15;
        bool advancedWithStones = epicChip.AdvanceTier();
        Assert.That(advancedWithStones, Is.True, "Đột phá thành công khi có đủ 10 Đá Tiến Bậc.");
        Assert.That(epicChip.tier, Is.EqualTo(ChipTier.Holographic));
        Assert.That(epicChip.MaxLevel, Is.EqualTo(24));
        Assert.That(ChipManager.AdvanceStones, Is.EqualTo(5), "Phải trừ đúng 10 Đá Tiến Bậc.");
    }

    [Test]
    public void Chipset_Progression_FromTier1ToTier5()
    {
        ChipItemData chip = new ChipItemData
        {
            id = 24,
            chipName = "Flamethrower",
            tier = ChipTier.Magic,
            level = 1,
            count = 500,
            requiredCount = 3
        };

        // Tier 1: upgrade to level 6
        while (chip.level < 6)
        {
            Assert.That(chip.CanUpgrade, Is.True);
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(6));
        Assert.That(chip.IsAtTierCap, Is.True);
        Assert.That(chip.CanUpgrade, Is.False);
        Assert.That(chip.CanAdvanceTier, Is.True);

        // Advance to Tier 2 (Rare, max 9)
        bool adv2 = chip.AdvanceTier();
        Assert.That(adv2, Is.True);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Rare));
        Assert.That(chip.MaxLevel, Is.EqualTo(9));

        // Tier 2: upgrade to level 9
        while (chip.level < 9)
        {
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(9));
        Assert.That(chip.IsAtTierCap, Is.True);

        // Advance to Tier 3 (Unique, max 14)
        bool adv3 = chip.AdvanceTier();
        Assert.That(adv3, Is.True);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Unique));
        Assert.That(chip.MaxLevel, Is.EqualTo(14));

        // Tier 3: upgrade to level 14
        while (chip.level < 14)
        {
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(14));
        Assert.That(chip.IsAtTierCap, Is.True);

        // Advance to Tier 4 (Epic, max 18)
        bool adv4 = chip.AdvanceTier();
        Assert.That(adv4, Is.True);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Epic));
        Assert.That(chip.MaxLevel, Is.EqualTo(18));

        // Tier 4: upgrade to level 18
        while (chip.level < 18)
        {
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(18));
        Assert.That(chip.IsAtTierCap, Is.True);
        Assert.That(chip.NeedsAdvanceStones, Is.True);

        // Advance to Tier 5 (Holographic, max 24) with 10 stones
        ChipManager.AdvanceStones = 20;
        bool adv5 = chip.AdvanceTier();
        Assert.That(adv5, Is.True);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Holographic));
        Assert.That(chip.MaxLevel, Is.EqualTo(24));

        // Tier 5: upgrade to level 24
        while (chip.level < 24)
        {
            chip.Upgrade();
        }
        Assert.That(chip.level, Is.EqualTo(24));
        Assert.That(chip.IsMaxOverall, Is.True);
        Assert.That(chip.CanUpgrade, Is.False);
        Assert.That(chip.CanAdvanceTier, Is.False);
    }

    [Test]
    public void Buddy_Database_InitializesWithAllDrones()
    {
        GameObject go = new GameObject("BuddyControllerTest");
        BuddyController controller = go.AddComponent<BuddyController>();
        controller.InitializeDatabase();

        Assert.That(controller.AllBuddies.Count, Is.GreaterThanOrEqualTo(10), "Buddy database must have all 10+ drones initialized.");

        BuddyItemData snowflake = null;
        BuddyItemData spider = null;
        foreach (var b in controller.AllBuddies)
        {
            if (b.iconKey == "drone-snowflake") snowflake = b;
            if (b.iconKey == "drone-spider") spider = b;
        }

        Assert.That(snowflake, Is.Not.Null, "Snowflake Drone must exist in database.");
        Assert.That(snowflake.count, Is.EqualTo(65), "Snowflake Drone count matches 65 in screenshot.");
        Assert.That(spider, Is.Not.Null, "Spider Drone must exist in database.");
        Assert.That(spider.count, Is.EqualTo(79), "Spider Drone count matches 79 in screenshot.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Buddy_EnhanceAndAdvanceTier_ConsumesResources_And_IncreasesProgression()
    {
        BuddyItemData drone = new BuddyItemData
        {
            id = 101,
            buddyName = "Test Drone",
            iconKey = "drone-spider",
            tier = BuddyTier.Common,
            level = 1,
            count = 10,
            requiredCount = 3,
            enhanceCost = 500
        };

        // Advance Tier with fragments
        Assert.That(drone.CanAdvanceTier, Is.True);
        bool adv = drone.AdvanceTier();
        Assert.That(adv, Is.True);
        Assert.That(drone.tier, Is.EqualTo(BuddyTier.Magic));
        Assert.That(drone.count, Is.EqualTo(7));
        Assert.That(drone.requiredCount, Is.GreaterThan(3));

        // Enhance level with Data Chips
        ChipManager.DataChips = 2000;
        Assert.That(drone.CanEnhance, Is.True);
        bool enh = drone.Enhance();
        Assert.That(enh, Is.True);
        Assert.That(drone.level, Is.EqualTo(2));
        Assert.That(drone.enhanceCost, Is.GreaterThan(500));
    }

    [Test]
    public void Buddy_CardUI_StateTransitions()
    {
        GameObject go = new GameObject("BuddyCardUITest");
        BuddyCardUI card = go.AddComponent<BuddyCardUI>();

        // Setup empty
        card.SetupEmpty(null);
        Assert.That(card.SlotState, Is.EqualTo(BuddySlotState.Empty));
        Assert.That(card.BoundData, Is.Null);

        // Setup locked
        card.SetupLocked(null);
        Assert.That(card.SlotState, Is.EqualTo(BuddySlotState.Locked));
        Assert.That(card.BoundData, Is.Null);

        // Setup normal
        BuddyItemData drone = new BuddyItemData { id = 1, buddyName = "Snowflake", level = 1, count = 65, requiredCount = 3 };
        card.Setup(drone, null, null);
        Assert.That(card.SlotState, Is.EqualTo(BuddySlotState.Normal));
        Assert.That(card.BoundData, Is.EqualTo(drone));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_Database_InitializesWithSonicBoomAndStats()
    {
        GameObject go = new GameObject("ChipsetControllerTest");
        ChipsetController controller = go.AddComponent<ChipsetController>();
        controller.InitializeDatabase();

        Assert.That(controller.AllChips.Count, Is.EqualTo(24), "Chipset database must have all 24 chips initialized.");

        ChipItemData sonicBoom = null;
        foreach (var c in controller.AllChips)
        {
            if (c.id == 18 || c.chipName == "Sonic Boom") sonicBoom = c;
        }

        Assert.That(sonicBoom, Is.Not.Null, "Sonic Boom must exist in database.");
        Assert.That(sonicBoom.count, Is.EqualTo(439), "Sonic Boom count must be 439 matching screenshot.");
        Assert.That(sonicBoom.requiredCount, Is.EqualTo(3), "Sonic Boom requiredCount must be 3.");
        Assert.That(sonicBoom.description, Does.Contain("Sonic attack"), "Description should describe Sonic attack.");
        Assert.That(sonicBoom.magicBonus, Does.Contain("ATK +15%"), "Magic bonus should be ATK +15%.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_Enhance_ConsumesDataChips_And_IncreasesLevel()
    {
        ChipItemData chip = new ChipItemData
        {
            id = 200,
            chipName = "Test Chip",
            tier = ChipTier.Magic,
            level = 1,
            enhanceCost = 500
        };

        ChipManager.DataChips = 1000;
        Assert.That(chip.CanEnhance, Is.True);
        bool success = chip.Enhance();

        Assert.That(success, Is.True);
        Assert.That(chip.level, Is.EqualTo(2));
        Assert.That(ChipManager.DataChips, Is.EqualTo(500));
        Assert.That(chip.enhanceCost, Is.GreaterThan(500));
    }

    [Test]
    public void Chipset_CardUI_EmptyAndNormalStates()
    {
        GameObject go = new GameObject("ChipsetCardUITest");
        ChipsetCardUI card = go.AddComponent<ChipsetCardUI>();

        card.SetupEmpty(null);
        Assert.That(card.SlotState, Is.EqualTo(ChipSlotState.Empty));
        Assert.That(card.BoundData, Is.Null);

        ChipItemData chip = new ChipItemData { id = 1, chipName = "Standard Gun", level = 1, count = 22, requiredCount = 3 };
        card.Setup(chip, null, null);
        Assert.That(card.SlotState, Is.EqualTo(ChipSlotState.Normal));
        Assert.That(card.BoundData, Is.EqualTo(chip));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_TierUnlock_RequiresTenEnhancesAndConsumesFragments()
    {
        bool originalTestMode = ChipManager.IsTestMode;
        int originalDataChips = ChipManager.DataChips;
        int originalStoredDataChips = PlayerDataService.DataChips;
        ChipItemData chip = new ChipItemData
        {
            id = 201,
            chipName = "Tier Test",
            tier = ChipTier.Magic,
            level = 1,
            count = 8,
            enhanceCost = 1
        };
        chip.ConfigureTierUnlockRules(10, 3, 5, 7, 11);
        try
        {
            ChipManager.IsTestMode = false;
            ChipManager.DataChips = 100000;

            for (int i = 0; i < 10; i++)
            {
                Assert.That(chip.Enhance(), Is.True, $"Enhance lần {i + 1} phải thành công.");
            }

            Assert.That(chip.tierEnhanceCount, Is.EqualTo(10));
            Assert.That(chip.IsTierUnlockReady, Is.True);
            Assert.That(chip.CanEnhance, Is.False);
            Assert.That(chip.CanAdvanceTier, Is.True);
            Assert.That(chip.AdvanceTier(), Is.True);
            Assert.That(chip.tier, Is.EqualTo(ChipTier.Rare));
            Assert.That(chip.count, Is.EqualTo(5), "Green -> Blue phải trừ đúng 3 mảnh chipset.");
            Assert.That(chip.tierEnhanceCount, Is.Zero, "Sang khung mới phải reset tiến độ Enhance.");
        }
        finally
        {
            PlayerDataService.DataChips = originalStoredDataChips;
            ChipManager.IsTestMode = originalTestMode;
            ChipManager.DataChips = originalDataChips;
        }
    }

    [Test]
    public void Chipset_YellowToRed_ConsumesInspectorConfiguredRedDataChips()
    {
        bool originalTestMode = ChipManager.IsTestMode;
        int originalRedGems = ChipManager.RedGems;
        int originalStoredRedGems = PlayerDataService.RedGems;
        ChipItemData chip = new ChipItemData
        {
            id = 202,
            chipName = "Red Frame Test",
            tier = ChipTier.Epic,
            level = 18,
            count = 999,
            tierEnhanceCount = 10
        };
        chip.ConfigureTierUnlockRules(10, 3, 5, 7, 11);
        try
        {
            ChipManager.IsTestMode = false;
            ChipManager.RedGems = 11;

            Assert.That(chip.UsesRedDataChipForAdvance, Is.True);
            Assert.That(chip.CanAdvanceTier, Is.True);
            Assert.That(chip.AdvanceTier(), Is.True);
            Assert.That(chip.tier, Is.EqualTo(ChipTier.Holographic));
            Assert.That(ChipManager.RedGems, Is.Zero, "Yellow -> Red phải trừ đúng Data Chip đỏ đã cấu hình.");
            Assert.That(chip.count, Is.EqualTo(999), "Yellow -> Red không được trừ mảnh chipset thường.");
        }
        finally
        {
            PlayerDataService.RedGems = originalStoredRedGems;
            ChipManager.IsTestMode = originalTestMode;
            ChipManager.RedGems = originalRedGems;
        }
    }

    [Test]
    public void Chipset_FrameAndPerkMapping_FollowsGreenBluePurpleYellowRedOrder()
    {
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Magic), Is.EqualTo(0));
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Rare), Is.EqualTo(1));
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Unique), Is.EqualTo(2));
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Epic), Is.EqualTo(3));
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Holographic), Is.EqualTo(4));

        Assert.That(ChipsetController.IsTierPerkUnlocked(ChipTier.Magic, 0), Is.False);
        Assert.That(ChipsetController.IsTierPerkUnlocked(ChipTier.Rare, 0), Is.True);
        Assert.That(ChipsetController.IsTierPerkUnlocked(ChipTier.Unique, 1), Is.True);
        Assert.That(ChipsetController.IsTierPerkUnlocked(ChipTier.Epic, 2), Is.True);
        Assert.That(ChipsetController.IsTierPerkUnlocked(ChipTier.Holographic, 3), Is.True);
    }

    [Test]
    public void Chipset_MainMenuFrameLibrary_ContainsFiveDistinctTierFrames()
    {
        ChipsetLevelVisualLibrary library = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");

        Assert.That(library, Is.Not.Null);
        Assert.That(library.mainMenuTierFrames, Has.Length.EqualTo(5));
        Assert.That(library.mainMenuTierFrames, Has.All.Not.Null);
        Assert.That(new HashSet<Sprite>(library.mainMenuTierFrames).Count, Is.EqualTo(5));
        CollectionAssert.AreEqual(
            new[] { "ChipsetGreen", "ChipsetBlue", "ChipsetPurple", "ChipsetYelloe", "ChipsetRed" },
            System.Array.ConvertAll(library.mainMenuTierFrames, sprite => sprite.name));
    }

    [Test]
    public void Chipset_DetailBottomBar_UsesSelectedReferenceLayout()
    {
        GameObject go = new GameObject("DetailBottomBarTest", typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();

        ChipsetCardUI.ApplyDetailBottomBarLayout(rect);

        Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.08f, 0.10f)));
        Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0.92f, 0.25f)));
        Assert.That(rect.offsetMin, Is.EqualTo(new Vector2(0f, 28f)));
        Assert.That(rect.offsetMax, Is.EqualTo(new Vector2(0f, 28f)));
        Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(rect.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(rect.localScale, Is.EqualTo(new Vector3(0.966f, 0.975f, 1f)));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_LevelLabel_AutoShrinksStatusSuffixInsideFrame()
    {
        GameObject go = new GameObject("ChipsetLevelLabelTest", typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text text = go.GetComponent<TMP_Text>();

        ChipsetCardUI.ConfigureLevelLabel(text, true);

        Assert.That(text.enableAutoSizing, Is.True);
        Assert.That(text.fontSizeMin, Is.EqualTo(10f));
        Assert.That(text.fontSizeMax, Is.EqualTo(18f));
        Assert.That(text.enableWordWrapping, Is.False);
        Assert.That(text.margin, Is.EqualTo(new Vector4(2f, 0f, 2f, 0f)));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_FragmentBarColor_MatchesUnlockedFrameTier()
    {
        Assert.That(ChipsetCardUI.GetTierProgressColor(ChipTier.Magic), Is.EqualTo(new Color32(74, 222, 128, 255)));
        Assert.That(ChipsetCardUI.GetTierProgressColor(ChipTier.Rare), Is.EqualTo(new Color32(56, 189, 248, 255)));
        Assert.That(ChipsetCardUI.GetTierProgressColor(ChipTier.Unique), Is.EqualTo(new Color32(192, 132, 252, 255)));
        Assert.That(ChipsetCardUI.GetTierProgressColor(ChipTier.Epic), Is.EqualTo(new Color32(250, 204, 21, 255)));
        Assert.That(ChipsetCardUI.GetTierProgressColor(ChipTier.Holographic), Is.EqualTo(new Color32(255, 77, 45, 255)));
    }

    [Test]
    public void Chipset_CalculateFillRatio_MatchesQuantityRatio()
    {
        // 0/3 -> 0%
        Assert.That(ChipsetCardUI.CalculateFillRatio(0, 3), Is.EqualTo(0f));
        // 3/3 -> 100%
        Assert.That(ChipsetCardUI.CalculateFillRatio(3, 3), Is.EqualTo(1.0f));
        // 1/3 -> ~33.3%
        Assert.That(ChipsetCardUI.CalculateFillRatio(1, 3), Is.EqualTo(1f / 3f).Within(0.001f));
        // 2/3 -> ~66.7%
        Assert.That(ChipsetCardUI.CalculateFillRatio(2, 3), Is.EqualTo(2f / 3f).Within(0.001f));
        // 22/3 -> 100% (clamped)
        Assert.That(ChipsetCardUI.CalculateFillRatio(22, 3), Is.EqualTo(1.0f));
        // other quantities: e.g. 5/10 -> 50%
        Assert.That(ChipsetCardUI.CalculateFillRatio(5, 10), Is.EqualTo(0.5f));
        // Max level overall -> 100%
        Assert.That(ChipsetCardUI.CalculateFillRatio(15, 0, isMaxOverall: true), Is.EqualTo(1.0f));
        // Required 0 with count > 0 -> 100%
        Assert.That(ChipsetCardUI.CalculateFillRatio(5, 0), Is.EqualTo(1.0f));
        // Required 0 with count 0 -> 0%
        Assert.That(ChipsetCardUI.CalculateFillRatio(0, 0), Is.EqualTo(0f));
    }

    [Test]
    public void Chipset_ParseFillRatioFromProgressText_ParsesCorrectly()
    {
        Assert.That(ChipsetCardUI.ParseFillRatioFromProgressText("0/3"), Is.EqualTo(0f));
        Assert.That(ChipsetCardUI.ParseFillRatioFromProgressText("3/3"), Is.EqualTo(1.0f));
        Assert.That(ChipsetCardUI.ParseFillRatioFromProgressText("1/3"), Is.EqualTo(1f / 3f).Within(0.001f));
        Assert.That(ChipsetCardUI.ParseFillRatioFromProgressText("2/3"), Is.EqualTo(2f / 3f).Within(0.001f));
        Assert.That(ChipsetCardUI.ParseFillRatioFromProgressText("22/3"), Is.EqualTo(1.0f));
        Assert.That(ChipsetCardUI.ParseFillRatioFromProgressText("MAX"), Is.EqualTo(1.0f));
        Assert.That(ChipsetCardUI.ParseFillRatioFromProgressText(""), Is.EqualTo(0f));
        Assert.That(ChipsetCardUI.ParseFillRatioFromProgressText(null), Is.EqualTo(0f));
    }

    [Test]
    public void Chipset_UpdateProgressBar_SetsFillAnchorAndVisibility()
    {
        GameObject cardObj = new GameObject("TestCard", typeof(RectTransform), typeof(ChipsetCardUI));
        GameObject bottomBarObj = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
        bottomBarObj.transform.SetParent(cardObj.transform, false);

        ChipsetCardUI card = cardObj.GetComponent<ChipsetCardUI>();
        card.EnsureProgressBar();

        Assert.That(card.ProgressFillRect, Is.Not.Null, "EnsureProgressBar must create or locate ProgressFillRect.");
        Assert.That(card.ProgressFillImage, Is.Not.Null, "EnsureProgressBar must create or locate ProgressFillImage.");

        // Test 0/3 -> fill is 0, image disabled
        card.UpdateProgressBar(0f);
        Assert.That(card.ProgressFillRect.anchorMax.x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(card.ProgressFillImage.enabled, Is.False);

        // Test 3/3 -> fill is 1.0, image enabled
        card.UpdateProgressBar(1.0f);
        Assert.That(card.ProgressFillRect.anchorMax.x, Is.EqualTo(1.0f).Within(0.001f));
        Assert.That(card.ProgressFillImage.enabled, Is.True);

        // Test 1/3 -> fill is 0.333, image enabled
        card.UpdateProgressBar(1f / 3f);
        Assert.That(card.ProgressFillRect.anchorMax.x, Is.EqualTo(1f / 3f).Within(0.001f));
        Assert.That(card.ProgressFillImage.enabled, Is.True);

        // Test with ChipItemData
        ChipItemData chip0 = new ChipItemData { id = 1, chipName = "Standard Gun", count = 0, requiredCount = 3 };
        card.Setup(chip0, null, null);
        Assert.That(card.ProgressFillRect.anchorMax.x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(card.ProgressFillImage.enabled, Is.False);

        ChipItemData chip3 = new ChipItemData { id = 1, chipName = "Standard Gun", count = 3, requiredCount = 3 };
        card.Setup(chip3, null, null);
        Assert.That(card.ProgressFillRect.anchorMax.x, Is.EqualTo(1.0f).Within(0.001f));
        Assert.That(card.ProgressFillImage.enabled, Is.True);

        Object.DestroyImmediate(cardObj);
    }

    [Test]
    public void Buddy_PurifyingDrone_MatchesStats()
    {
        GameObject go = new GameObject("BuddyControllerTest");
        BuddyController controller = go.AddComponent<BuddyController>();
        controller.InitializeDatabase();

        BuddyItemData purifying = null;
        foreach (var b in controller.AllBuddies)
        {
            if (b.id == 10 || b.buddyName == "Purifying Drone") purifying = b;
        }

        Assert.That(purifying, Is.Not.Null, "Purifying Drone must exist in database.");
        Assert.That(purifying.count, Is.EqualTo(38), "Purifying Drone count must be 38.");
        Assert.That(purifying.requiredCount, Is.EqualTo(3), "Purifying Drone requiredCount must be 3.");
        Assert.That(purifying.description, Does.Contain("Ailment Resistance"), "Description matches screenshot.");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_RealtimeCurrencySync_UpdatesTopBarOnEvents()
    {
        GameObject go = new GameObject("ChipsetTopBarTest");
        ChipsetController ctrl = go.AddComponent<ChipsetController>();

        TMP_Text energyText = new GameObject("Energy").AddComponent<TextMeshProUGUI>();
        TMP_Text chipText = new GameObject("Chip").AddComponent<TextMeshProUGUI>();
        TMP_Text redText = new GameObject("Red").AddComponent<TextMeshProUGUI>();
        TMP_Text stonesText = new GameObject("Stones").AddComponent<TextMeshProUGUI>();

        var type = typeof(ChipsetController);
        type.GetField("energyText", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, energyText);
        type.GetField("chipCurrencyText", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, chipText);
        type.GetField("redCurrencyText", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, redText);
        type.GetField("advanceStonesText", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, stonesText);

        ctrl.InitializeDatabase();
        ctrl.enabled = true;

        ChipManager.DataChips = 54321;
        ChipManager.RedGems = 12345;
        ChipManager.Energy = 42;
        ChipManager.AdvanceStones = 99;

        Assert.That(chipText.text, Is.EqualTo("54,321"));
        Assert.That(redText.text, Is.EqualTo("12,345"));
        Assert.That(energyText.text, Is.EqualTo($"42/{ChipManager.MaxEnergy}"));
        Assert.That(stonesText.text, Is.EqualTo("99"));

        Object.DestroyImmediate(energyText.gameObject);
        Object.DestroyImmediate(chipText.gameObject);
        Object.DestroyImmediate(redText.gameObject);
        Object.DestroyImmediate(stonesText.gameObject);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Chipset_InventoryCards_CanBeClickedAndOpenModal()
    {
        GameObject controllerGo = new GameObject("ChipsetControllerTest");
        ChipsetController ctrl = controllerGo.AddComponent<ChipsetController>();

        GameObject invContentGo = new GameObject("InventoryContent");
        GameObject detailModalGo = new GameObject("DetailModal");
        detailModalGo.SetActive(false);

        GameObject cardPrefabGo = new GameObject("CardTemplate");
        cardPrefabGo.AddComponent<Image>();
        cardPrefabGo.AddComponent<Button>();
        cardPrefabGo.AddComponent<ChipsetCardUI>();
        cardPrefabGo.transform.SetParent(invContentGo.transform, false);
        cardPrefabGo.SetActive(false);

        // Pre-create 3 existing cards in scene (like InvCard_00 .. InvCard_02)
        ChipsetCardUI[] existingCards = new ChipsetCardUI[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject cardGo = new GameObject($"InvCard_{i:00}");
            cardGo.AddComponent<Image>();
            cardGo.AddComponent<Button>();
            existingCards[i] = cardGo.AddComponent<ChipsetCardUI>();
            cardGo.transform.SetParent(invContentGo.transform, false);
        }

        var type = typeof(ChipsetController);
        type.GetField("inventoryContent", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, invContentGo.transform);
        type.GetField("cardPrefab", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, cardPrefabGo);
        type.GetField("detailModal", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, detailModalGo);

        ctrl.InitializeDatabase();
        ctrl.RefreshInventory();

        // Check that existing card 0 has bound data and button click listener
        Assert.That(existingCards[0].BoundData, Is.Not.Null, "Existing inventory card 0 must have bound chip data.");
        Assert.That(existingCards[0].BoundData.id, Is.GreaterThan(0));

        // Trigger button click on existing card 0
        Button btn = existingCards[0].GetComponent<Button>();
        Assert.That(btn, Is.Not.Null);
        btn.onClick.Invoke();

        // Check that detailModal is now opened
        Assert.That(detailModalGo.activeSelf, Is.True, "Clicking an inventory chip card must open the DetailModal.");

        Object.DestroyImmediate(invContentGo);
        Object.DestroyImmediate(detailModalGo);
        Object.DestroyImmediate(controllerGo);
    }

    [Test]
    public void Chipset_DetailModal_WithUninitializedDeck_DoesNotThrow()
    {
        GameObject controllerGo = new GameObject("ChipsetNullDeckTest");
        ChipsetController controller = controllerGo.AddComponent<ChipsetController>();
        GameObject detailModal = new GameObject("DetailModal");
        detailModal.SetActive(false);

        var type = typeof(ChipsetController);
        type.GetField("detailModal", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(controller, detailModal);
        type.GetField("deckEquippedIds", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(controller, new int[3][]);

        ChipItemData chip = ChipsetController.CreateDefaultDatabase()[0];
        Assert.DoesNotThrow(() => controller.OpenDetailModal(chip));
        Assert.That(detailModal.activeSelf, Is.True);

        Object.DestroyImmediate(detailModal);
        Object.DestroyImmediate(controllerGo);
    }

    [Test]
    public void Buddy_InventoryCards_CanBeClickedAndOpenModal()
    {
        GameObject controllerGo = new GameObject("BuddyControllerTest");
        BuddyController ctrl = controllerGo.AddComponent<BuddyController>();

        GameObject invContentGo = new GameObject("BuddyInventoryContent");
        GameObject detailModalGo = new GameObject("BuddyDetailModal");
        detailModalGo.SetActive(false);

        GameObject cardPrefabGo = new GameObject("BuddyCardTemplate");
        cardPrefabGo.AddComponent<Image>();
        cardPrefabGo.AddComponent<Button>();
        cardPrefabGo.AddComponent<BuddyCardUI>();
        cardPrefabGo.transform.SetParent(invContentGo.transform, false);
        cardPrefabGo.SetActive(false);

        // Pre-create 3 existing cards in scene
        BuddyCardUI[] existingCards = new BuddyCardUI[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject cardGo = new GameObject($"InvCard_{i:00}");
            cardGo.AddComponent<Image>();
            cardGo.AddComponent<Button>();
            existingCards[i] = cardGo.AddComponent<BuddyCardUI>();
            cardGo.transform.SetParent(invContentGo.transform, false);
        }

        var type = typeof(BuddyController);
        type.GetField("inventoryContent", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, invContentGo.transform);
        type.GetField("cardPrefab", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, cardPrefabGo);
        type.GetField("detailModal", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ctrl, detailModalGo);

        ctrl.InitializeDatabase();
        ctrl.RefreshInventory();

        // Check that existing card 0 has bound data and button click listener
        Assert.That(existingCards[0].BoundData, Is.Not.Null, "Existing buddy inventory card 0 must have bound data.");

        // Trigger button click on existing card 0
        Button btn = existingCards[0].GetComponent<Button>();
        Assert.That(btn, Is.Not.Null);
        btn.onClick.Invoke();

        // Check that detailModal is now opened
        Assert.That(detailModalGo.activeSelf, Is.True, "Clicking an inventory buddy card must open the DetailModal.");

        Object.DestroyImmediate(invContentGo);
        Object.DestroyImmediate(detailModalGo);
        Object.DestroyImmediate(controllerGo);
    }

    [Test]
    public void Chipset_TierAdvanceCosts_MatchesRequestedRules()
    {
        // Kiểm tra đúng yêu cầu:
        // Xanh lá -> Xanh dương: 5 mảnh
        // Xanh dương -> Tím: 10 mảnh
        // Tím -> Vàng: 15 mảnh
        // Vàng -> Đỏ: 20 mảnh + 100 đá đỏ
        bool originalTestMode = ChipManager.IsTestMode;
        int originalRedGems = ChipManager.RedGems;
        int originalStoredRedGems = PlayerDataService.RedGems;

        try
        {
            ChipManager.IsTestMode = false;

            // 1. Magic (Xanh lá) -> Rare (Xanh dương): 5 mảnh
            ChipItemData greenChip = new ChipItemData
            {
                id = 301,
                chipName = "Green Advance Test",
                tier = ChipTier.Magic,
                level = 6,
                count = 5,
                tierEnhanceCount = 10
            };
            greenChip.ConfigureTierUnlockRules(10, 5, 10, 15, 100, 20);
            Assert.That(greenChip.CurrentAdvanceCost, Is.EqualTo(5));
            Assert.That(greenChip.requiredCount, Is.EqualTo(5));
            Assert.That(greenChip.CanAdvanceTier, Is.True);
            Assert.That(greenChip.AdvanceTier(), Is.True);
            Assert.That(greenChip.tier, Is.EqualTo(ChipTier.Rare));
            Assert.That(greenChip.count, Is.Zero);
            Assert.That(greenChip.requiredCount, Is.EqualTo(10), "Sang phẩm Rare, requiredCount phải là 10 mảnh.");

            // 2. Rare (Xanh dương) -> Unique (Tím): 10 mảnh
            ChipItemData blueChip = new ChipItemData
            {
                id = 302,
                chipName = "Blue Advance Test",
                tier = ChipTier.Rare,
                level = 9,
                count = 10,
                tierEnhanceCount = 10
            };
            blueChip.ConfigureTierUnlockRules(10, 5, 10, 15, 100, 20);
            Assert.That(blueChip.CurrentAdvanceCost, Is.EqualTo(10));
            Assert.That(blueChip.requiredCount, Is.EqualTo(10));
            Assert.That(blueChip.CanAdvanceTier, Is.True);
            Assert.That(blueChip.AdvanceTier(), Is.True);
            Assert.That(blueChip.tier, Is.EqualTo(ChipTier.Unique));
            Assert.That(blueChip.count, Is.Zero);
            Assert.That(blueChip.requiredCount, Is.EqualTo(15), "Sang phẩm Unique, requiredCount phải là 15 mảnh.");

            // 3. Unique (Tím) -> Epic (Vàng): 15 mảnh
            ChipItemData purpleChip = new ChipItemData
            {
                id = 303,
                chipName = "Purple Advance Test",
                tier = ChipTier.Unique,
                level = 14,
                count = 15,
                tierEnhanceCount = 10
            };
            purpleChip.ConfigureTierUnlockRules(10, 5, 10, 15, 100, 20);
            Assert.That(purpleChip.CurrentAdvanceCost, Is.EqualTo(15));
            Assert.That(purpleChip.requiredCount, Is.EqualTo(15));
            Assert.That(purpleChip.CanAdvanceTier, Is.True);
            Assert.That(purpleChip.AdvanceTier(), Is.True);
            Assert.That(purpleChip.tier, Is.EqualTo(ChipTier.Epic));
            Assert.That(purpleChip.count, Is.Zero);
            Assert.That(purpleChip.requiredCount, Is.EqualTo(20), "Sang phẩm Epic, requiredCount phải là 20 mảnh.");

            // 4. Epic (Vàng) -> Holographic (Đỏ): 20 mảnh + 100 đá đỏ
            ChipItemData yellowChip = new ChipItemData
            {
                id = 304,
                chipName = "Yellow Advance Test",
                tier = ChipTier.Epic,
                level = 18,
                count = 20,
                tierEnhanceCount = 10
            };
            yellowChip.ConfigureTierUnlockRules(10, 5, 10, 15, 100, 20);
            Assert.That(yellowChip.CurrentAdvanceCost, Is.EqualTo(20));
            Assert.That(yellowChip.YellowToRedDataChipCost, Is.EqualTo(100));
            Assert.That(yellowChip.requiredCount, Is.EqualTo(20));

            // Thiếu đá đỏ (chỉ có 50 đá đỏ) -> Không đột phá được
            ChipManager.RedGems = 50;
            Assert.That(yellowChip.HasAdvanceCurrency, Is.False);
            Assert.That(yellowChip.CanAdvanceTier, Is.False);
            Assert.That(yellowChip.AdvanceTier(), Is.False);

            // Thiếu mảnh (chỉ có 19 mảnh, đủ 100 đá đỏ) -> Không đột phá được
            ChipManager.RedGems = 100;
            yellowChip.count = 19;
            Assert.That(yellowChip.HasAdvanceCurrency, Is.False);
            Assert.That(yellowChip.CanAdvanceTier, Is.False);
            Assert.That(yellowChip.AdvanceTier(), Is.False);

            // Đủ cả 20 mảnh VÀ 100 đá đỏ -> Đột phá thành công lên Holographic (Đỏ)
            yellowChip.count = 25;
            ChipManager.RedGems = 150;
            Assert.That(yellowChip.HasAdvanceCurrency, Is.True);
            Assert.That(yellowChip.CanAdvanceTier, Is.True);
            Assert.That(yellowChip.AdvanceTier(), Is.True);
            Assert.That(yellowChip.tier, Is.EqualTo(ChipTier.Holographic));
            Assert.That(yellowChip.count, Is.EqualTo(5), "Vàng -> Đỏ phải trừ đúng 20 mảnh (còn lại 5 mảnh).");
            Assert.That(ChipManager.RedGems, Is.EqualTo(50), "Vàng -> Đỏ phải trừ đúng 100 đá đỏ (còn lại 50).");
        }
        finally
        {
            PlayerDataService.RedGems = originalStoredRedGems;
            ChipManager.IsTestMode = originalTestMode;
            ChipManager.RedGems = originalRedGems;
        }
    }
}



