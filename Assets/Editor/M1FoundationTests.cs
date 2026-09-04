using System;
using System.Collections.Generic;
using NUnit.Framework;
using PGE.Auth;
using UnityEngine;
using Object = UnityEngine.Object;

public class M1FoundationTests
{
    private class TestPoolableComponent : MonoBehaviour, IPoolable
    {
        public int spawnCount;
        public int returnCount;

        public void OnSpawnFromPool()
        {
            spawnCount++;
        }

        public void OnReturnToPool()
        {
            returnCount++;
        }
    }

    [Test]
    public void PlayerDataService_FreshInstall_InitializesCorrectDefaults()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;
        int origEnergy = PlayerDataService.Energy;
        int origStones = PlayerDataService.AdvanceStones;

        try
        {
            PlayerPrefs.DeleteKey(PlayerDataService.DataChipsKey);
            PlayerPrefs.DeleteKey(PlayerDataService.RedGemsKey);
            PlayerPrefs.DeleteKey(PlayerDataService.EnergyKey);
            PlayerPrefs.DeleteKey(PlayerDataService.AdvanceStonesKey);

            Assert.That(PlayerDataService.DataChips, Is.EqualTo(1000), "Default DataChips must be 1000.");
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(1000), "Default RedGems must be 1000.");
            Assert.That(PlayerDataService.Energy, Is.EqualTo(100), "Default Energy must be 100.");
            Assert.That(PlayerDataService.AdvanceStones, Is.EqualTo(0), "Default AdvanceStones must be 0.");
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.RedGems = origGems;
            PlayerDataService.Energy = origEnergy;
            PlayerDataService.AdvanceStones = origStones;
        }
    }

    [Test]
    public void PlayerDataService_SpendAndAddOperations_AreSafeAndClamped()
    {
        int origChips = PlayerDataService.DataChips;
        try
        {
            PlayerDataService.DataChips = 500;

            // Negative spend must fail and not modify balance
            Assert.That(PlayerDataService.TrySpendDataChips(-100), Is.False);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(500));

            // Overspend must fail and not modify balance
            Assert.That(PlayerDataService.TrySpendDataChips(600), Is.False);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(500));

            // Valid spend
            Assert.That(PlayerDataService.TrySpendDataChips(200), Is.True);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(300));

            // Zero spend
            Assert.That(PlayerDataService.TrySpendDataChips(0), Is.True);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(300));

            // Add negative should do nothing
            PlayerDataService.AddDataChips(-50);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(300));

            // Add positive
            PlayerDataService.AddDataChips(200);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(500));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
        }
    }

    [Test]
    public void PlayerDataService_LabStats_ClampsBetween0And10_AndHandlesNullKeys()
    {
        string[] statNames = { "HP", "ATK", "DEF", "RECOVERY", "CRIT RATE", null, "" };

        foreach (string stat in statNames)
        {
            int orig = PlayerDataService.GetItemLevel(stat);
            try
            {
                PlayerDataService.SetItemLevel(stat, 5);
                Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(5));

                PlayerDataService.SetItemLevel(stat, 15);
                Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(10), "Stat must clamp to 10 max.");

                PlayerDataService.SetItemLevel(stat, -3);
                Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(0), "Stat must clamp to 0 min.");

                PlayerDataService.IncrementItemLevel(stat, 3);
                Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(3));

                PlayerDataService.IncrementItemLevel(stat, 20);
                Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(10));
            }
            finally
            {
                PlayerDataService.SetItemLevel(stat, orig);
            }
        }
    }

    [Test]
    public void PlayerDataService_DeckAndChipsetData_SerializesAndRecoversSafely()
    {
        int[] testDeck = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int deckIdx = 1;

        PlayerDataService.SaveChipsetDeck(deckIdx, testDeck);
        int[] loaded = PlayerDataService.LoadChipsetDeck(deckIdx, new int[10]);

        Assert.That(loaded.Length, Is.EqualTo(10));
        for (int i = 0; i < 10; i++)
        {
            Assert.That(loaded[i], Is.EqualTo(testDeck[i]));
        }

        // Test Chipset item data
        int chipId = 99;
        PlayerDataService.SaveChipsetItemData(chipId, level: 3, tier: 2, count: 5, reqCount: 10, hasStar: true);
        PlayerDataService.SaveChipsetTierEnhanceCount(chipId, 2);
        PlayerDataService.SaveChipsetEnhanceCost(chipId, 450);

        Assert.That(PlayerDataService.HasChipsetItemData(chipId), Is.True);
        bool success = PlayerDataService.LoadChipsetItemData(chipId, out int lvl, out int tier, out int count, out int req, out bool star);
        Assert.That(success, Is.True);
        Assert.That(lvl, Is.EqualTo(3));
        Assert.That(tier, Is.EqualTo(2));
        Assert.That(count, Is.EqualTo(5));
        Assert.That(req, Is.EqualTo(10));
        Assert.That(star, Is.True);
        Assert.That(PlayerDataService.LoadChipsetTierEnhanceCount(chipId), Is.EqualTo(2));
        Assert.That(PlayerDataService.LoadChipsetEnhanceCost(chipId, 100), Is.EqualTo(450));
        Assert.That(PlayerDataService.GetChipTier(chipId), Is.EqualTo(ChipTier.Rare));
    }

    [Test]
    public void ChipManager_TestMode_ProvidesInfiniteTransactionsWhenEnabled()
    {
        ChipManager cm = ChipManager.Instance;
        Assert.That(cm, Is.Not.Null);

        bool origTest = ChipManager.IsTestMode;
        try
        {
            ChipManager.IsTestMode = true;
            Assert.That(ChipManager.HasEnoughDataChips(999999999), Is.True);
            Assert.That(ChipManager.TrySpendDataChips(999999999), Is.True);
            Assert.That(ChipManager.TrySpendRedGems(999999999), Is.True);
            Assert.That(ChipManager.TrySpendEnergy(999999999), Is.True);
            Assert.That(ChipManager.TrySpendAdvanceStones(999999999), Is.True);

            ChipManager.IsTestMode = false;
            PlayerDataService.DataChips = 50;
            Assert.That(ChipManager.HasEnoughDataChips(100), Is.False);
            Assert.That(ChipManager.TrySpendDataChips(100), Is.False);
        }
        finally
        {
            ChipManager.IsTestMode = origTest;
        }
    }

    [Test]
    public void GameEvents_DecoupledBus_NotifiesSubscribersCleanly()
    {
        int killCount = 0;
        int lastExp = 0;
        int droneTier = 0;
        string lastDrone = null;
        int lastChapterPlayed = -1;
        int lastChapterCleared = -1;
        int lastStars = 0;
        int lastLevelUp = 0;
        string lastCurr = null;
        int lastCurrAmt = 0;

        void OnKilled() => killCount++;
        void OnKilledExp(int exp) => lastExp = exp;
        void OnDrone() => droneTier++;
        void OnDroneDetail(string id, int t) { lastDrone = id; }
        void OnChapPlay(int idx) => lastChapterPlayed = idx;
        void OnChapClear(int num) => lastChapterCleared = num;
        void OnChapClearDetail(int num, int stars) => lastStars = stars;
        void OnLvlUp(int lvl) => lastLevelUp = lvl;
        void OnCurr(string c, int a) { lastCurr = c; lastCurrAmt = a; }

        GameEvents.OnEnemyKilled += OnKilled;
        GameEvents.OnEnemyKilledWithExp += OnKilledExp;
        GameEvents.OnDroneTierAdvanced += OnDrone;
        GameEvents.OnDroneTierAdvancedDetailed += OnDroneDetail;
        GameEvents.OnChapterPlayed += OnChapPlay;
        GameEvents.OnChapterCleared += OnChapClear;
        GameEvents.OnChapterClearedDetailed += OnChapClearDetail;
        GameEvents.OnPlayerLevelUp += OnLvlUp;
        GameEvents.OnCurrencyChanged += OnCurr;

        try
        {
            GameEvents.RaiseEnemyKilled();
            Assert.That(killCount, Is.EqualTo(1));

            GameEvents.RaiseEnemyKilled(45);
            Assert.That(killCount, Is.EqualTo(2));
            Assert.That(lastExp, Is.EqualTo(45));

            GameEvents.RaiseDroneTierAdvanced("drone_01", 3);
            Assert.That(droneTier, Is.EqualTo(1));
            Assert.That(lastDrone, Is.EqualTo("drone_01"));

            GameEvents.RaiseChapterPlayed(2);
            Assert.That(lastChapterPlayed, Is.EqualTo(2));

            GameEvents.RaiseChapterCleared(1, 3);
            Assert.That(lastChapterCleared, Is.EqualTo(1));
            Assert.That(lastStars, Is.EqualTo(3));

            GameEvents.RaisePlayerLevelUp(5);
            Assert.That(lastLevelUp, Is.EqualTo(5));

            GameEvents.RaiseCurrencyChanged("DataChips", 1500);
            Assert.That(lastCurr, Is.EqualTo("DataChips"));
            Assert.That(lastCurrAmt, Is.EqualTo(1500));
        }
        finally
        {
            GameEvents.OnEnemyKilled -= OnKilled;
            GameEvents.OnEnemyKilledWithExp -= OnKilledExp;
            GameEvents.OnDroneTierAdvanced -= OnDrone;
            GameEvents.OnDroneTierAdvancedDetailed -= OnDroneDetail;
            GameEvents.OnChapterPlayed -= OnChapPlay;
            GameEvents.OnChapterCleared -= OnChapClear;
            GameEvents.OnChapterClearedDetailed -= OnChapClearDetail;
            GameEvents.OnPlayerLevelUp -= OnLvlUp;
            GameEvents.OnCurrencyChanged -= OnCurr;
        }
    }

    [Test]
    public void ObjectPool_FullLifecycle_PrewarmSpawnReturnAndDoubleReturnSafety()
    {
        GameObject prefab = new GameObject("PoolLifecyclePrefab");
        prefab.AddComponent<TestPoolableComponent>();
        GameObject container = new GameObject("PoolContainer");

        try
        {
            ObjectPool pool = new ObjectPool(prefab, 2, canGrow: true, container.transform);
            pool.Initialize(container.transform);

            // Spawn 1
            GameObject obj1 = pool.Spawn(Vector3.zero, Quaternion.identity);
            Assert.That(obj1, Is.Not.Null);
            Assert.That(obj1.activeSelf, Is.True);
            TestPoolableComponent comp1 = obj1.GetComponent<TestPoolableComponent>();
            Assert.That(comp1.spawnCount, Is.EqualTo(1));

            // Spawn 2
            GameObject obj2 = pool.Spawn(Vector3.one, Quaternion.identity);
            Assert.That(obj2, Is.Not.Null);

            // Return obj1
            pool.Despawn(obj1);
            Assert.That(obj1.activeSelf, Is.False);
            Assert.That(comp1.returnCount, Is.EqualTo(1));

            // Double return of obj1 (should be ignored safely via HashSet)
            pool.Despawn(obj1);
            Assert.That(comp1.returnCount, Is.EqualTo(1), "Duplicate return must not re-trigger OnReturnToPool.");

            // Spawn again (should get obj1)
            GameObject reused = pool.Spawn(Vector3.zero, Quaternion.identity);
            Assert.That(reused, Is.EqualTo(obj1));
            Assert.That(comp1.spawnCount, Is.EqualTo(2));

            pool.Despawn(reused);
            pool.Despawn(obj2);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(container);
        }
    }

    [Test]
    public void GameAudio_CategorizationAndSettings_ControlsMuteState()
    {
        GameObject host = new GameObject("AudioTestHost", typeof(GameAudioSettingsRuntime));
        GameObject musicGo = new GameObject("BGM_Track", typeof(AudioSource));
        GameObject sfxGo = new GameObject("SFX_Shoot", typeof(AudioSource));

        try
        {
            AudioSource music = musicGo.GetComponent<AudioSource>();
            AudioSource sfx = sfxGo.GetComponent<AudioSource>();
            music.loop = true;
            sfx.loop = false;

            Assert.That(GameAudioSettingsRuntime.IsMusicSource(music), Is.True);
            Assert.That(GameAudioSettingsRuntime.IsMusicSource(sfx), Is.False);

            GameSettings.BgmEnabled = false;
            GameSettings.SfxEnabled = true;
            host.GetComponent<GameAudioSettingsRuntime>().ApplySettingsNow();

            Assert.That(music.mute, Is.True);
            Assert.That(sfx.mute, Is.False);

            GameSettings.BgmEnabled = true;
            GameSettings.SfxEnabled = false;
            host.GetComponent<GameAudioSettingsRuntime>().ApplySettingsNow();

            Assert.That(music.mute, Is.False);
            Assert.That(sfx.mute, Is.True);
        }
        finally
        {
            GameSettings.BgmEnabled = true;
            GameSettings.SfxEnabled = true;
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(musicGo);
            Object.DestroyImmediate(sfxGo);
        }
    }

    [Test]
    public void AuthAndCloudSave_FullWorkflow_SignInSaveAndLoad()
    {
        // 1. Google Sign In
        GoogleAuthManager.Instance.SignInWithGoogle();
        Assert.That(GoogleAuthManager.Instance.IsLoggedIn, Is.True);
        Assert.That(CloudSaveSyncService.IsAnyCloudLoggedIn, Is.True);

        int origChips = PlayerDataService.DataChips;
        int origEnergy = PlayerDataService.Energy;

        try
        {
            PlayerDataService.DataChips = 8888;
            PlayerDataService.Energy = 95;

            bool saved = false;
            CloudSaveSyncService.SaveToCloud((ok, msg) => saved = ok);
            Assert.That(saved, Is.True);

            // Simulate local data reduction
            PlayerDataService.DataChips = 100;
            PlayerDataService.Energy = 10;

            bool loaded = false;
            CloudSaveSyncService.LoadFromCloud((ok, msg) => loaded = ok);
            Assert.That(loaded, Is.True);

            // Merged data should take the max between cloud and local
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(8888));
            Assert.That(PlayerDataService.Energy, Is.EqualTo(95));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.Energy = origEnergy;
            GoogleAuthManager.Instance.SignOut();
            Assert.That(GoogleAuthManager.Instance.IsLoggedIn, Is.False);
        }
    }
}
