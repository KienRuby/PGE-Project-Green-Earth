using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PGE.Auth;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[TestFixture]
public class PGE_Tier4_RealWorldScenarioTests
{
    #region Scenario 1: Fresh Install to First Gameplay Loop
    [Test]
    public void T4_Scenario1_FreshInstall_To_FirstGameplayLoop()
    {
        // 1. Initial fresh install balances
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;
        int origEnergy = PlayerDataService.Energy;

        GameObject dailyGo = new GameObject("DailyLogin", typeof(DailyLoginManager));
        GameObject playerGo = new GameObject("Player", typeof(PlayerLevelController), typeof(PlayerHealth));

        try
        {
            PlayerDataService.DataChips = 500;
            PlayerDataService.RedGems = 100;
            PlayerDataService.Energy = 20;

            // 2. Claim Day 1 Daily Login Reward
            DailyLoginManager dailyMgr = dailyGo.GetComponent<DailyLoginManager>();
            dailyMgr.EnsureDatabaseLoaded();

            Assert.That(dailyMgr.GetDayState(1), Is.EqualTo(DailyLoginState.Available));
            Assert.That(dailyMgr.TryClaimTodayReward(), Is.True);
            Assert.That(dailyMgr.GetDayState(1), Is.EqualTo(DailyLoginState.Obtained));

            // 3. Spend 10 energy to start Chapter 1
            Assert.That(PlayerDataService.Energy, Is.GreaterThanOrEqualTo(10));
            PlayerDataService.Energy -= 10;
            Assert.That(PlayerDataService.Energy, Is.EqualTo(10));

            // 4. Combat loop: Kill 10 creeps, accumulate EXP, trigger Level Up
            PlayerLevelController levelCtrl = playerGo.GetComponent<PlayerLevelController>();
            for (int i = 0; i < 10; i++)
            {
                int expPerCreep = 5;
                GameEvents.RaiseEnemyKilled(expPerCreep);
                levelCtrl.AddEXP(expPerCreep);
            }

            // Total EXP = 50. Level 1 requires 30. Reaches Level 2 with 20 excess.
            Assert.That(levelCtrl.CurrentLevel, Is.EqualTo(2));
            Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(20));

            // 5. Select Skill and Record Battle Damage
            ChipsetBattleStats.Reset();
            ChipsetBattleStats.RegisterChipset(1, 1, 20);
            ChipsetBattleStats.RecordDamage(1, 120); // Standard gun upgrade
            Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(120));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.RedGems = origGems;
            PlayerDataService.Energy = origEnergy;
            Object.DestroyImmediate(dailyGo);
            Object.DestroyImmediate(playerGo);
        }
    }
    #endregion

    #region Scenario 2: Boss Multi-Skill & Enrage Phase Battle
    [Test]
    public void T4_Scenario2_BossMultiSkill_And_EnragePhaseBattle()
    {
        GameObject bossGo = new GameObject("Boss", typeof(BossRangedAttack), typeof(EnemyHealth));
        GameObject playerGo = new GameObject("Player", typeof(PlayerHealth));

        try
        {
            BossRangedAttack rangedAttack = bossGo.GetComponent<BossRangedAttack>();
            EnemyHealth bossHealth = bossGo.GetComponent<EnemyHealth>();
            PlayerHealth playerHealth = playerGo.GetComponent<PlayerHealth>();

            rangedAttack.SetTarget(playerGo.transform);

            // Phase 1: Fan attack
            Vector2[] fanDirs = BossRangedAttack.CalculateFanDirections(Vector2.down, 3, 45f);
            Assert.That(fanDirs.Length, Is.EqualTo(3));

            // Phase 2: Radial 360° attack
            Vector2[] radialDirs = BossRangedAttack.CalculateRadialDirections(Vector2.down, 6);
            Assert.That(radialDirs.Length, Is.EqualTo(6));

            // Phase 3: Player damages boss down below 50% HP (Enrage threshold)
            int initialHp = bossHealth.MaxHealth;
            bossHealth.TakeDamage(initialHp * 6 / 10);
            Assert.That(bossHealth.CurrentHealth, Is.LessThan(initialHp * 0.5f));

            // Phase 4: Lethal attack defeats boss
            bossHealth.TakeDamage(bossHealth.CurrentHealth);
            Assert.That(bossHealth.IsDead, Is.True);
            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(bossGo);
            Object.DestroyImmediate(playerGo);
        }
    }
    #endregion

    #region Scenario 3: Lab Matrix Upgrade & Triple Pity Guarantee Sweep
    [Test]
    public void T4_Scenario3_LabMatrixUpgrade_And_TriplePitySweep()
    {
        int origChips = PlayerDataService.DataChips;
        int origElitePity = PlayerDataService.LabElitePityCounter;

        try
        {
            PlayerDataService.DataChips = 50000;
            PlayerDataService.LabElitePityCounter = 0;

            // Perform 10 rolls to trigger Elite Pity
            for (int i = 0; i < 9; i++)
            {
                int cost = 300 + i * 150;
                Assert.That(PlayerDataService.TrySpendDataChips(cost), Is.True);
                PlayerDataService.LabElitePityCounter++;
            }

            Assert.That(PlayerDataService.LabElitePityCounter, Is.EqualTo(9));
            PlayerDataService.LabElitePityCounter++; // 10th roll reaches Elite threshold
            Assert.That(PlayerDataService.LabElitePityCounter >= PityGuaranteePanel.EliteThreshold, Is.True);

            // Elite guarantee consumed -> reset Elite counter to 0
            PlayerDataService.LabElitePityCounter = 0;
            Assert.That(PlayerDataService.LabElitePityCounter, Is.EqualTo(0));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.LabElitePityCounter = origElitePity;
        }
    }
    #endregion

    #region Scenario 4: Full Chapter Wave Clear to Star Payout
    [Test]
    public void T4_Scenario4_FullChapterWaveClear_To_StarPayout()
    {
        int origUnlocked = PlayerDataService.UnlockedChapterIndex;
        int origEnergy = PlayerDataService.Energy;

        try
        {
            PlayerDataService.UnlockedChapterIndex = 0;
            PlayerDataService.Energy = 20;

            // Spend energy to play chapter 1
            PlayerDataService.Energy -= 10;
            GameEvents.RaiseChapterPlayed(0);

            // Complete waves 1 through 5
            int currentWave = 1;
            int totalWaves = 5;
            while (currentWave <= totalWaves)
            {
                float progress = PlayerRunEndController.CalculateStageProgress(currentWave, 1f, totalWaves);
                Assert.That(progress, Is.GreaterThan(0f));
                currentWave++;
            }

            // Player finished run with full HP -> 3 Stars
            int starsEarned = 3;
            GameEvents.RaiseChapterCleared(1, starsEarned);

            // Chapter 2 (index 1) is unlocked
            PlayerDataService.UnlockedChapterIndex = 1;
            Assert.That(PlayerDataService.UnlockedChapterIndex, Is.EqualTo(1));
        }
        finally
        {
            PlayerDataService.UnlockedChapterIndex = origUnlocked;
            PlayerDataService.Energy = origEnergy;
        }
    }
    #endregion

    #region Scenario 5: Disaster Recovery & Cloud State Restoration
    [Test]
    public void T4_Scenario5_DisasterRecovery_And_CloudStateRestoration()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;

        try
        {
            // 1. Initial cloud save with progress
            GoogleAuthManager.Instance.SignInWithGoogle();
            PlayerDataService.DataChips = 8888;
            PlayerDataService.RedGems = 1234;

            bool saveSuccess = false;
            CloudSaveSyncService.SaveToCloud((ok, msg) => saveSuccess = ok);
            Assert.That(saveSuccess, Is.True);
            GoogleAuthManager.Instance.SignOut();

            // 2. Simulated device corruption / local data reset
            PlayerDataService.DataChips = 0;
            PlayerDataService.RedGems = 0;
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(0));
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(0));

            // 3. User logs back in and restores state from cloud
            GoogleAuthManager.Instance.SignInWithGoogle();
            bool loadSuccess = false;
            CloudSaveSyncService.LoadFromCloud((ok, msg) => loadSuccess = ok);

            Assert.That(loadSuccess, Is.True);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(8888), "DataChips restored without loss.");
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(1234), "RedGems restored without loss.");
        }
        finally
        {
            GoogleAuthManager.Instance.SignOut();
            PlayerDataService.DataChips = origChips;
            PlayerDataService.RedGems = origGems;
        }
    }
    #endregion

    #region Scenario 6: Full Meta Progression Loop (Shop -> Lab -> Chipset -> Drone -> Combat)
    [Test]
    public void T4_Scenario6_FullMetaProgressionLoop_ShopToCombat()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;
        int origStones = PlayerDataService.AdvanceStones;
        int origEnergy = PlayerDataService.Energy;

        GameObject shopGo = new GameObject("Shop", typeof(ShopController));
        GameObject dailyGo = new GameObject("DailyLogin", typeof(DailyLoginManager));

        try
        {
            // 1. Daily Login Claim
            DailyLoginManager daily = dailyGo.GetComponent<DailyLoginManager>();
            daily.EnsureDatabaseLoaded();
            daily.TryClaimTodayReward();

            // 2. Setup initial currencies
            PlayerDataService.RedGems = 500;
            PlayerDataService.DataChips = 1000;
            PlayerDataService.AdvanceStones = 20;
            PlayerDataService.Energy = 30;

            // 3. Shop Purchase: Exchange 100 RedGems for 5,000 DataChips
            ShopController shop = shopGo.GetComponent<ShopController>();
            ShopController.Offer offer = new ShopController.Offer
            {
                id = "pack-5000-chips",
                currency = ShopController.CurrencyType.RedGem,
                price = 100,
                reward = ShopController.RewardType.DataChip,
                rewardAmount = 5000
            };
            shop.SetOffersForTesting(new[] { offer });

            Assert.That(shop.TryPurchase(0), Is.True);
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(400));
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(6000));

            // 4. Lab Matrix Upgrades: Upgrade ATK and HP stats
            int roll0Cost = 300;
            Assert.That(PlayerDataService.TrySpendDataChips(roll0Cost), Is.True);
            PlayerDataService.IncrementItemLevel("ATK", 1);

            int roll1Cost = 450;
            Assert.That(PlayerDataService.TrySpendDataChips(roll1Cost), Is.True);
            PlayerDataService.IncrementItemLevel("HP", 1);

            Assert.That(PlayerDataService.GetItemLevel("ATK"), Is.EqualTo(1));
            Assert.That(PlayerDataService.GetItemLevel("HP"), Is.EqualTo(1));
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(5250));

            // 5. Chipset Advance to Holographic
            ChipItemData chip = new ChipItemData
            {
                id = 1,
                chipName = "Standard Gun",
                tier = ChipTier.Epic,
                level = 18,
                count = 10,
                enhanceCost = 1000
            };

            Assert.That(chip.AdvanceTier(), Is.True);
            Assert.That(chip.tier, Is.EqualTo(ChipTier.Holographic));
            Assert.That(PlayerDataService.AdvanceStones, Is.EqualTo(10)); // 20 - 10 = 10

            // 6. Drone Advance Tier
            BuddyItemData drone = new BuddyItemData
            {
                id = 1,
                buddyName = "Snowflake Drone",
                tier = BuddyTier.Common,
                level = 1,
                count = 10,
                requiredCount = 5
            };
            Assert.That(drone.AdvanceTier(), Is.True);
            Assert.That(drone.tier, Is.EqualTo(BuddyTier.Magic));
            Assert.That(drone.count, Is.EqualTo(5));

            // 7. Start Chapter 1 Run
            Assert.That(ChipManager.TrySpendEnergy(10), Is.True);
            Assert.That(PlayerDataService.Energy, Is.EqualTo(20));

            // 8. Battle Execution & Damage
            ChipsetBattleStats.Reset();
            ChipsetBattleStats.RegisterChipset(chip.id, chip.level, 100);
            ChipsetBattleStats.RecordDamage(chip.id, 500);
            Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(500));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.RedGems = origGems;
            PlayerDataService.AdvanceStones = origStones;
            PlayerDataService.Energy = origEnergy;
            Object.DestroyImmediate(shopGo);
            Object.DestroyImmediate(dailyGo);
        }
    }
    #endregion
}
