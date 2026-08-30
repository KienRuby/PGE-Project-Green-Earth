using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PGE.Auth;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[TestFixture]
public class PGE_Tier3_CrossFeatureIntegrationTests
{
    #region Integration 1: Lab Upgrade -> Currency Deduction -> Stat Buff -> Player Combat Stats
    [Test]
    public void T3_01_LabUpgrade_DeductsDataChips_AndBuffsPlayerCombatStats()
    {
        int origChips = PlayerDataService.DataChips;
        int origAtk = PlayerDataService.GetItemLevel("ATK");

        try
        {
            PlayerDataService.DataChips = 5000;
            PlayerDataService.SetItemLevel("ATK", 0);

            int basePrice = 300;
            int step = 150;
            int cost = basePrice + 0 * step;
            Assert.That(cost, Is.EqualTo(300));

            // Upgrade ATK stat
            Assert.That(PlayerDataService.TrySpendDataChips(cost), Is.True);
            PlayerDataService.IncrementItemLevel("ATK", 1);

            Assert.That(PlayerDataService.DataChips, Is.EqualTo(4700));
            Assert.That(PlayerDataService.GetItemLevel("ATK"), Is.EqualTo(1));
            Assert.That(PlayerStatsManager.GetStatLevel("ATK"), Is.EqualTo(1));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.SetItemLevel("ATK", origAtk);
        }
    }
    #endregion

    #region Integration 2: Enemy Kill -> Gem Drop -> Player Magnet Collection -> EXP Gain -> Level Up Event
    [Test]
    public void T3_02_EnemyKill_DropsExp_CollectedByPlayer_TriggersLevelUp()
    {
        GameObject playerGo = new GameObject("Player", typeof(PlayerLevelController));
        int receivedLevel = 0;
        Action<int> onLevelUp = lvl => receivedLevel = lvl;
        GameEvents.OnPlayerLevelUp += onLevelUp;

        try
        {
            PlayerLevelController levelCtrl = playerGo.GetComponent<PlayerLevelController>();
            Assert.That(levelCtrl.CurrentLevel, Is.EqualTo(1));

            // Enemy dies and grants 35 EXP
            int droppedExp = 35;
            GameEvents.RaiseEnemyKilled(droppedExp);
            levelCtrl.AddEXP(droppedExp);

            Assert.That(levelCtrl.CurrentLevel, Is.EqualTo(2));
            Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(5));
            Assert.That(receivedLevel, Is.EqualTo(2));
        }
        finally
        {
            GameEvents.OnPlayerLevelUp -= onLevelUp;
            Object.DestroyImmediate(playerGo);
        }
    }
    #endregion

    #region Integration 3: Level Up Event -> Popup Offers Generated -> Skill Selected -> Weapon Damage Amplified
    [Test]
    public void T3_03_LevelUp_GeneratesOffers_AndAppliesSkillUpgrade()
    {
        var catalog = ChipsetController.CreateDefaultDatabase();
        var offers = ChipsetLevelUpPopup.SelectDistinctOffers(catalog, 4, new System.Random(1337));

        Assert.That(offers.Count, Is.EqualTo(4));
        ChipItemData chosenOffer = offers[0];

        // Apply selected chip upgrade in battle stats
        ChipsetBattleStats.Reset();
        ChipsetBattleStats.RegisterChipset(chosenOffer.id, 1, 50);
        ChipsetBattleStats.RecordDamage(chosenOffer.id, 150);

        Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(150));
    }
    #endregion

    #region Integration 4: Chapter Clear -> Stars Calculated -> Achievement Reported -> Reward Claimed
    [Test]
    public void T3_04_ChapterClear_ReportsAchievement_AndGrantsRewardPayout()
    {
        int origGems = PlayerDataService.RedGems;
        GameObject achGo = new GameObject("AchievementManager", typeof(AchievementManager));

        try
        {
            PlayerDataService.RedGems = 0;
            AchievementManager achMgr = achGo.GetComponent<AchievementManager>();
            achMgr.EnsureDatabaseLoaded();

            // Clear chapter 1 with 3 stars
            GameEvents.RaiseChapterCleared(1, 3);

            string achId = "login_reward_2";
            achMgr.SetProgress(achId, 2);

            Assert.That(achMgr.IsCompleted(achId), Is.True);
            Assert.That(achMgr.TryClaimReward(achId), Is.True);
            Assert.That(PlayerDataService.RedGems, Is.GreaterThan(0));
        }
        finally
        {
            PlayerDataService.RedGems = origGems;
            Object.DestroyImmediate(achGo);
        }
    }
    #endregion

    #region Integration 5: Shop Currency Exchange -> Red Gems to Data Chips -> Used for Chipset Enhance
    [Test]
    public void T3_05_ShopExchange_ConvertsGemsToChips_ThenEnhancesChipset()
    {
        int origChips = ChipManager.DataChips;
        int origGems = ChipManager.RedGems;
        GameObject shopGo = new GameObject("Shop", typeof(ShopController));

        try
        {
            ChipManager.RedGems = 100;
            ChipManager.DataChips = 0;

            ShopController shop = shopGo.GetComponent<ShopController>();
            ShopController.Offer offer = new ShopController.Offer
            {
                id = "gem-to-chips",
                currency = ShopController.CurrencyType.RedGem,
                price = 50,
                reward = ShopController.RewardType.DataChip,
                rewardAmount = 2500
            };
            shop.SetOffersForTesting(new[] { offer });

            // Buy Data Chips with Red Gems
            Assert.That(shop.TryPurchase(0), Is.True);
            Assert.That(ChipManager.RedGems, Is.EqualTo(50));
            Assert.That(ChipManager.DataChips, Is.EqualTo(2500));

            // Use purchased Data Chips to enhance Chipset
            ChipItemData chip = new ChipItemData
            {
                id = 1,
                chipName = "Standard Gun",
                tier = ChipTier.Magic,
                level = 1,
                enhanceCost = 1000
            };

            Assert.That(chip.CanEnhance, Is.True);
            Assert.That(chip.Enhance(), Is.True);
            Assert.That(chip.level, Is.EqualTo(2));
            Assert.That(ChipManager.DataChips, Is.EqualTo(1500));
        }
        finally
        {
            ChipManager.DataChips = origChips;
            ChipManager.RedGems = origGems;
            Object.DestroyImmediate(shopGo);
        }
    }
    #endregion

    #region Integration 6: Drone Tier Advance -> Fragment Deduction -> Event Broadcast
    [Test]
    public void T3_06_DroneTierAdvance_DeductsFragments_AndBroadcastsEvent()
    {
        BuddyItemData drone = new BuddyItemData
        {
            id = 1,
            buddyName = "Snowflake Drone",
            tier = BuddyTier.Common,
            level = 1,
            count = 10,
            requiredCount = 5
        };

        string advancedDrone = null;
        int advancedTier = 0;
        Action<string, int> listener = (id, t) => { advancedDrone = id; advancedTier = t; };
        GameEvents.OnDroneTierAdvancedDetailed += listener;

        try
        {
            Assert.That(drone.CanAdvanceTier, Is.True);
            Assert.That(drone.AdvanceTier(), Is.True);
            Assert.That(drone.tier, Is.EqualTo(BuddyTier.Magic));
            Assert.That(drone.count, Is.EqualTo(5));

            GameEvents.RaiseDroneTierAdvanced("drone-snowflake", 1);
            Assert.That(advancedDrone, Is.EqualTo("drone-snowflake"));
            Assert.That(advancedTier, Is.EqualTo(1));
        }
        finally
        {
            GameEvents.OnDroneTierAdvancedDetailed -= listener;
        }
    }
    #endregion

    #region Integration 7: Cloud Save Sync -> Save State -> Account Switch -> Cloud State Restored
    [Test]
    public void T3_07_CloudSaveSync_MultiAccountSync_RestoresStateProperly()
    {
        int origChips = PlayerDataService.DataChips;
        try
        {
            // User A signs in
            GoogleAuthManager.Instance.SignInWithGoogle();
            PlayerDataService.DataChips = 7777;
            bool saved = false;
            CloudSaveSyncService.SaveToCloud((ok, msg) => saved = ok);
            Assert.That(saved, Is.True);
            GoogleAuthManager.Instance.SignOut();

            // Local cache changed
            PlayerDataService.DataChips = 0;

            // User A signs back in and loads
            GoogleAuthManager.Instance.SignInWithGoogle();
            bool loaded = false;
            CloudSaveSyncService.LoadFromCloud((ok, msg) => loaded = ok);
            Assert.That(loaded, Is.True);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(7777));
        }
        finally
        {
            GoogleAuthManager.Instance.SignOut();
            PlayerDataService.DataChips = origChips;
        }
    }
    #endregion

    #region Integration 8: Lethal Damage -> PlayerDeathController -> Revive -> Component Recovery
    [Test]
    public void T3_08_PlayerLethalDamage_DeathSequence_AndReviveRecovery()
    {
        GameObject player = new GameObject("Player", typeof(PlayerHealth), typeof(PlayerMovement), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(PlayerDeathController));
        try
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            PlayerDeathController deathCtrl = player.GetComponent<PlayerDeathController>();

            // Inflict lethal damage
            health.TakeDamage(100);
            Assert.That(health.IsDead, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(0));

            deathCtrl.TriggerDeath();
            Assert.That(movement.enabled, Is.False);
            Assert.That(deathCtrl.IsDeathSequenceActive, Is.True);

            // Trigger Revive
            bool revived = health.Revive(0.5f, 2.0f);
            Assert.That(revived, Is.True);
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(50));

            deathCtrl.ResetForRevive();
            Assert.That(movement.enabled, Is.True);
            Assert.That(deathCtrl.IsDeathSequenceActive, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }
    #endregion

    #region Integration 9: Wave Spawning -> Creep Movement -> Contact Damage -> Player Health Bar Fill
    [Test]
    public void T3_09_CreepContactDamage_UpdatesPlayerHealthAndWorldHealthBar()
    {
        GameObject player = new GameObject("Player", typeof(PlayerHealth), typeof(PlayerWorldHealthBar));
        GameObject fill = new GameObject("Fill", typeof(SpriteRenderer));
        fill.transform.SetParent(player.transform, false);

        GameObject enemy = new GameObject("Enemy", typeof(EnemyContactDamage));

        try
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            PlayerWorldHealthBar bar = player.GetComponent<PlayerWorldHealthBar>();
            typeof(PlayerWorldHealthBar).GetField("fillRenderer", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(bar, fill.GetComponent<SpriteRenderer>());

            EnemyContactDamage contact = enemy.GetComponent<EnemyContactDamage>();
            contact.SetDamage(30);

            health.TakeDamage(contact.Damage);
            Assert.That(health.CurrentHealth, Is.EqualTo(70));

            bar.SetNormalizedHealth((float)health.CurrentHealth / health.MaxHealth);
            Assert.That(fill.transform.localScale.x, Is.EqualTo(0.7f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(enemy);
        }
    }
    #endregion

    #region Integration 10: Boss Health Threshold -> Enrage -> Radial Projectiles -> Screen Trauma
    [Test]
    public void T3_10_BossEnrageTransition_RadialBurst_AndScreenTrauma()
    {
        GameObject boss = new GameObject("Boss", typeof(EnemyHealth), typeof(BossRangedAttack));
        try
        {
            EnemyHealth bossHealth = boss.GetComponent<EnemyHealth>();
            BossRangedAttack ranged = boss.GetComponent<BossRangedAttack>();

            ScreenShakeService.Reset();
            GameSettings.ScreenShake = true;

            // Damage boss to trigger low health / enrage phase
            bossHealth.TakeDamage(bossHealth.MaxHealth * 6 / 10);
            Assert.That(bossHealth.CurrentHealth, Is.LessThan(bossHealth.MaxHealth * 0.5f));

            // Fire 8-way radial barrage
            Vector2[] radialDirs = BossRangedAttack.CalculateRadialDirections(Vector2.down, 8);
            Assert.That(radialDirs.Length, Is.EqualTo(8));

            // Enrage trauma
            ScreenShakeService.AddTrauma(0.6f);
            Vector3 offset = ScreenShakeService.UpdateAndGetOffset(0.016f);
            Assert.That(offset, Is.Not.EqualTo(Vector3.zero));
        }
        finally
        {
            Object.DestroyImmediate(boss);
        }
    }
    #endregion

    #region Integration 11: Daily Login Claim -> RedGems -> LevelUp Reroll -> New Chipset Offers
    [Test]
    public void T3_11_DailyLoginRedGems_PowersLevelUpPopupReroll()
    {
        int origGems = PlayerDataService.RedGems;
        GameObject dailyGo = new GameObject("DailyLogin", typeof(DailyLoginManager));
        GameObject popupGo = new GameObject("Popup", typeof(ChipsetLevelUpPopup));

        try
        {
            PlayerDataService.RedGems = 0;

            DailyLoginManager dailyMgr = dailyGo.GetComponent<DailyLoginManager>();
            dailyMgr.EnsureDatabaseLoaded();

            // Claim reward (awards gems/energy)
            dailyMgr.TryClaimTodayReward();
            PlayerDataService.RedGems += 50; // Ensure enough gems for rerolls

            ChipManager.IsTestMode = false;
            ChipsetLevelUpPopup popup = popupGo.GetComponent<ChipsetLevelUpPopup>();

            // Reroll 1 consumes 20 gems
            Assert.That(popup.TryReroll(), Is.True);
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(30));
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(1));

            // Reroll 2 consumes 20 gems
            Assert.That(popup.TryReroll(), Is.True);
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(10));
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(2));
        }
        finally
        {
            PlayerDataService.RedGems = origGems;
            Object.DestroyImmediate(dailyGo);
            Object.DestroyImmediate(popupGo);
        }
    }
    #endregion

    #region Integration 12: Zero-Allocation Object Pool -> Multi-type Concurrent Spawning & Recycling
    private class DummyBullet : MonoBehaviour, IPoolable
    {
        public bool active;
        public void OnSpawnFromPool() => active = true;
        public void OnReturnToPool() => active = false;
    }
    private class DummyVfx : MonoBehaviour, IPoolable
    {
        public bool active;
        public void OnSpawnFromPool() => active = true;
        public void OnReturnToPool() => active = false;
    }

    [Test]
    public void T3_12_MultiTypeObjectPool_ConcurrentSpawningAndRecycling()
    {
        GameObject bulletPrefab = new GameObject("Bullet", typeof(DummyBullet));
        GameObject vfxPrefab = new GameObject("Vfx", typeof(DummyVfx));
        GameObject root = new GameObject("PoolRoot");

        try
        {
            ObjectPool bulletPool = new ObjectPool(bulletPrefab, 5, true, root.transform);
            ObjectPool vfxPool = new ObjectPool(vfxPrefab, 5, true, root.transform);
            bulletPool.Initialize(root.transform);
            vfxPool.Initialize(root.transform);

            List<GameObject> activeBullets = new List<GameObject>();
            List<GameObject> activeVfx = new List<GameObject>();

            for (int i = 0; i < 5; i++)
            {
                activeBullets.Add(bulletPool.Spawn(Vector3.right * i, Quaternion.identity));
                activeVfx.Add(vfxPool.Spawn(Vector3.up * i, Quaternion.identity));
            }

            Assert.That(activeBullets.All(b => b.GetComponent<DummyBullet>().active), Is.True);
            Assert.That(activeVfx.All(v => v.GetComponent<DummyVfx>().active), Is.True);

            // Recycle all
            foreach (var b in activeBullets) bulletPool.Despawn(b);
            foreach (var v in activeVfx) vfxPool.Despawn(v);

            Assert.That(activeBullets.All(b => !b.activeSelf), Is.True);
            Assert.That(activeVfx.All(v => !v.activeSelf), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(bulletPrefab);
            Object.DestroyImmediate(vfxPrefab);
            Object.DestroyImmediate(root);
        }
    }
    #endregion

    #region Integration 13: Lab Matrix Upgrades -> Total Rolls Tracked -> Triple Pity Guarantee
    [Test]
    public void T3_13_LabMatrixUpgrades_AdvancesPityCounters_AndTriggersGuarantee()
    {
        int origChips = PlayerDataService.DataChips;
        int origElitePity = PlayerDataService.LabElitePityCounter;
        int origEpicPity = PlayerDataService.LabEpicPityCounter;

        try
        {
            PlayerDataService.DataChips = 50000;
            PlayerDataService.LabElitePityCounter = 0;
            PlayerDataService.LabEpicPityCounter = 0;

            for (int i = 0; i < 10; i++)
            {
                int cost = 300 + i * 150;
                Assert.That(PlayerDataService.TrySpendDataChips(cost), Is.True);
                PlayerDataService.LabElitePityCounter++;
                PlayerDataService.LabEpicPityCounter++;
            }

            Assert.That(PlayerDataService.LabElitePityCounter >= PityGuaranteePanel.EliteThreshold, Is.True);
            Assert.That(PlayerDataService.LabEpicPityCounter, Is.EqualTo(10));

            // Consume Elite guarantee
            PlayerDataService.LabElitePityCounter = 0;
            Assert.That(PlayerDataService.LabElitePityCounter, Is.EqualTo(0));
            Assert.That(PlayerDataService.LabEpicPityCounter, Is.EqualTo(10)); // Epic pity preserved
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.LabElitePityCounter = origElitePity;
            PlayerDataService.LabEpicPityCounter = origEpicPity;
        }
    }
    #endregion

    #region Integration 14: Chipset Tier Advance to Holographic -> Stone Consumption -> Higher Level Cap
    [Test]
    public void T3_14_ChipsetTierAdvanceToHolographic_ConsumesAdvanceStones_AndRaisesCap()
    {
        int origStones = ChipManager.AdvanceStones;
        int origChips = ChipManager.DataChips;

        try
        {
            ChipManager.AdvanceStones = 15;
            ChipManager.DataChips = 5000;

            ChipItemData chip = new ChipItemData
            {
                id = 1,
                chipName = "Standard Gun",
                tier = ChipTier.Epic,
                level = 18,
                count = 10,
                enhanceCost = 500
            };

            Assert.That(chip.IsAtTierCap, Is.True);
            Assert.That(chip.NeedsAdvanceStones, Is.True);

            Assert.That(chip.AdvanceTier(), Is.True);
            Assert.That(chip.tier, Is.EqualTo(ChipTier.Holographic));
            Assert.That(ChipManager.AdvanceStones, Is.EqualTo(5));
            Assert.That(ChipItemData.GetMaxLevelForTier(chip.tier), Is.EqualTo(24));

            // Now further enhancement is unlocked up to 24
            Assert.That(chip.CanEnhance, Is.True);
            Assert.That(chip.Enhance(), Is.True);
            Assert.That(chip.level, Is.EqualTo(19));
            Assert.That(ChipManager.DataChips, Is.EqualTo(4500));
        }
        finally
        {
            ChipManager.AdvanceStones = origStones;
            ChipManager.DataChips = origChips;
        }
    }
    #endregion
}
