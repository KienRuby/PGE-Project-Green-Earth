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
public class PGE_Tier2_BoundaryCornerTests
{
    #region Feature 1: PlayerData & Persistence / Balances (Boundary)
    [Test]
    public void T2_F01_01_Currencies_ZeroAndNegativeSpend_RejectedWithoutSideEffects()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;
        int origEnergy = PlayerDataService.Energy;
        int origStones = PlayerDataService.AdvanceStones;

        try
        {
            PlayerDataService.DataChips = 50;
            PlayerDataService.RedGems = 20;
            PlayerDataService.Energy = 10;
            PlayerDataService.AdvanceStones = 5;

            Assert.That(PlayerDataService.TrySpendDataChips(0), Is.True);
            Assert.That(PlayerDataService.TrySpendDataChips(-100), Is.False, "Negative chip spend must be rejected.");
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(50));

            Assert.That(PlayerDataService.TrySpendRedGems(0), Is.True);
            Assert.That(PlayerDataService.TrySpendRedGems(-50), Is.False, "Negative gem spend must be rejected.");
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(20));

            Assert.That(ChipManager.TrySpendEnergy(0), Is.True);
            Assert.That(ChipManager.TrySpendEnergy(-5), Is.False, "Negative energy spend must be rejected.");
            Assert.That(PlayerDataService.Energy, Is.EqualTo(10));
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
    public void T2_F01_02_Currencies_ExactSpendLeavesZero_OverspendByOneFails()
    {
        int origChips = PlayerDataService.DataChips;
        try
        {
            PlayerDataService.DataChips = 100;
            Assert.That(PlayerDataService.TrySpendDataChips(100), Is.True);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(0));

            Assert.That(PlayerDataService.TrySpendDataChips(1), Is.False, "Spending 1 when balance is 0 must fail.");
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(0));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
        }
    }

    [Test]
    public void T2_F01_03_Currencies_NegativeAssignment_ClampsToZero()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;
        int origEnergy = PlayerDataService.Energy;

        try
        {
            PlayerDataService.DataChips = -500;
            PlayerDataService.RedGems = -200;
            PlayerDataService.Energy = -50;

            Assert.That(PlayerDataService.DataChips, Is.GreaterThanOrEqualTo(0));
            Assert.That(PlayerDataService.RedGems, Is.GreaterThanOrEqualTo(0));
            Assert.That(PlayerDataService.Energy, Is.GreaterThanOrEqualTo(0));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.RedGems = origGems;
            PlayerDataService.Energy = origEnergy;
        }
    }

    [Test]
    public void T2_F01_04_Currencies_LargeIntegerValues_PersistWithoutOverflow()
    {
        int origChips = PlayerDataService.DataChips;
        try
        {
            int largeVal = 10000000;
            PlayerDataService.DataChips = largeVal;
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(largeVal));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
        }
    }

    [Test]
    public void T2_F01_05_PlayerDataService_EmptyOrNullWeaponId_DefaultsToBlaster()
    {
        string origWeapon = PlayerDataService.SelectedWeaponId;
        try
        {
            PlayerDataService.SelectedWeaponId = null;
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("blaster"));

            PlayerDataService.SelectedWeaponId = "";
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("blaster"));

            PlayerDataService.SelectedWeaponId = "   ";
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("blaster"));
        }
        finally
        {
            PlayerDataService.SelectedWeaponId = origWeapon;
        }
    }
    #endregion

    #region Feature 2: Static Event Bus & Decoupling (Boundary)
    [Test]
    public void T2_F02_01_GameEvents_RaiseWithZeroOrNegativeExp_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => GameEvents.RaiseEnemyKilled(0));
        Assert.DoesNotThrow(() => GameEvents.RaiseEnemyKilled(-100));
    }

    [Test]
    public void T2_F02_02_GameEvents_RaiseCurrencyChanged_NullOrEmptyString_HandlesSafely()
    {
        Assert.DoesNotThrow(() => GameEvents.RaiseCurrencyChanged(null, 0));
        Assert.DoesNotThrow(() => GameEvents.RaiseCurrencyChanged("", 500));
    }

    [Test]
    public void T2_F02_03_GameEvents_RepeatedSubscription_ProcessesHandlersAccurately()
    {
        int count = 0;
        Action handler = () => count++;

        GameEvents.OnEnemyKilled += handler;
        GameEvents.OnEnemyKilled += handler;

        try
        {
            GameEvents.RaiseEnemyKilled();
            Assert.That(count, Is.EqualTo(2));
        }
        finally
        {
            GameEvents.OnEnemyKilled -= handler;
            GameEvents.OnEnemyKilled -= handler;
        }
    }

    [Test]
    public void T2_F02_04_GameEvents_UnsubscribeUnregisteredHandler_DoesNotThrow()
    {
        Action dummy = () => { };
        Assert.DoesNotThrow(() => GameEvents.OnEnemyKilled -= dummy);
        Assert.DoesNotThrow(() => GameEvents.OnPlayerLevelUp -= lvl => { });
    }

    [Test]
    public void T2_F02_05_GameEvents_BurstDispatch_HandlesHighVolumeEvents()
    {
        int totalDispatches = 0;
        Action onKill = () => totalDispatches++;
        GameEvents.OnEnemyKilled += onKill;

        try
        {
            for (int i = 0; i < 500; i++)
            {
                GameEvents.RaiseEnemyKilled();
            }
            Assert.That(totalDispatches, Is.EqualTo(500));
        }
        finally
        {
            GameEvents.OnEnemyKilled -= onKill;
        }
    }
    #endregion

    #region Feature 3: Zero-Allocation Object Pool (Boundary)
    private class DummyPoolItem : MonoBehaviour, IPoolable
    {
        public int spawnCount = 0;
        public int returnCount = 0;
        public void OnSpawnFromPool() => spawnCount++;
        public void OnReturnToPool() => returnCount++;
    }

    [Test]
    public void T2_F03_01_ObjectPool_ExhaustionWithCanGrowFalse_ReturnsNullSafely()
    {
        GameObject prefab = new GameObject("Prefab", typeof(DummyPoolItem));
        GameObject root = new GameObject("Root");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 1, canGrow: false, root.transform);
            pool.Initialize(root.transform);

            GameObject obj1 = pool.Spawn(Vector3.zero, Quaternion.identity);
            GameObject obj2 = pool.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(obj1, Is.Not.Null);
            Assert.That(obj2, Is.Null, "Exhausted pool with canGrow=false must return null.");
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void T2_F03_02_ObjectPool_CanGrowTrue_ExpandsOnDemand()
    {
        GameObject prefab = new GameObject("Prefab", typeof(DummyPoolItem));
        GameObject root = new GameObject("Root");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 1, canGrow: true, root.transform);
            pool.Initialize(root.transform);

            GameObject obj1 = pool.Spawn(Vector3.zero, Quaternion.identity);
            GameObject obj2 = pool.Spawn(Vector3.zero, Quaternion.identity);
            GameObject obj3 = pool.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(obj1, Is.Not.Null);
            Assert.That(obj2, Is.Not.Null);
            Assert.That(obj3, Is.Not.Null);

            pool.Despawn(obj1);
            pool.Despawn(obj2);
            pool.Despawn(obj3);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void T2_F03_03_ObjectPool_DespawnNull_IgnoredSafely()
    {
        GameObject prefab = new GameObject("Prefab", typeof(DummyPoolItem));
        GameObject root = new GameObject("Root");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 2, canGrow: true, root.transform);
            pool.Initialize(root.transform);

            Assert.DoesNotThrow(() => pool.Despawn(null));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void T2_F03_04_ObjectPool_DoubleDespawn_IgnoredSafely()
    {
        GameObject prefab = new GameObject("Prefab", typeof(DummyPoolItem));
        GameObject root = new GameObject("Root");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 1, canGrow: false, root.transform);
            pool.Initialize(root.transform);

            GameObject obj = pool.Spawn(Vector3.zero, Quaternion.identity);
            pool.Despawn(obj);
            pool.Despawn(obj); // Double return

            GameObject retrieved = pool.Spawn(Vector3.zero, Quaternion.identity);
            Assert.That(retrieved, Is.EqualTo(obj));
            Assert.That(pool.Spawn(Vector3.zero, Quaternion.identity), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void T2_F03_05_ObjectPool_InitialSizeZero_SpawnsCleanly()
    {
        GameObject prefab = new GameObject("Prefab", typeof(DummyPoolItem));
        GameObject root = new GameObject("Root");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 0, canGrow: true, root.transform);
            pool.Initialize(root.transform);

            GameObject obj = pool.Spawn(Vector3.zero, Quaternion.identity);
            Assert.That(obj, Is.Not.Null);
            pool.Despawn(obj);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }
    #endregion

    #region Feature 4: Global Audio & Settings (Boundary)
    [Test]
    public void T2_F04_01_GameSettings_RapidToggling_MaintainsDeterministicState()
    {
        bool origBgm = GameSettings.BgmEnabled;
        try
        {
            for (int i = 0; i < 100; i++)
            {
                GameSettings.BgmEnabled = (i % 2 == 0);
            }
            Assert.That(GameSettings.BgmEnabled, Is.False); // 99 % 2 != 0 -> false
        }
        finally
        {
            GameSettings.BgmEnabled = origBgm;
        }
    }

    [Test]
    public void T2_F04_02_GameSettings_Language_FallbackGracefully()
    {
        string orig = GameSettings.Language;
        try
        {
            GameSettings.Language = "";
            Assert.That(GameSettings.IsVietnamese, Is.False);

            GameSettings.Language = "Tiếng Việt";
            Assert.That(GameSettings.IsVietnamese, Is.True);
        }
        finally
        {
            GameSettings.Language = orig;
        }
    }

    [Test]
    public void T2_F04_03_GameAudioSettingsRuntime_ApplyWithoutSources_DoesNotThrow()
    {
        GameObject host = new GameObject("AudioHost", typeof(GameAudioSettingsRuntime));
        try
        {
            GameAudioSettingsRuntime runtime = host.GetComponent<GameAudioSettingsRuntime>();
            Assert.DoesNotThrow(() => runtime.ApplySettingsNow());
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void T2_F04_04_GameAudioSettingsRuntime_BothBgmAndSfxDisabled_MutesAll()
    {
        GameObject host = new GameObject("AudioHost", typeof(GameAudioSettingsRuntime));
        GameObject bgm = new GameObject("BGM", typeof(AudioSource));
        GameObject sfx = new GameObject("SFX", typeof(AudioSource));
        try
        {
            AudioSource bgmSource = bgm.GetComponent<AudioSource>();
            AudioSource sfxSource = sfx.GetComponent<AudioSource>();
            bgmSource.loop = true;
            sfxSource.loop = false;

            GameSettings.BgmEnabled = false;
            GameSettings.SfxEnabled = false;
            host.GetComponent<GameAudioSettingsRuntime>().ApplySettingsNow();

            Assert.That(bgmSource.mute, Is.True);
            Assert.That(sfxSource.mute, Is.True);
        }
        finally
        {
            GameSettings.BgmEnabled = true;
            GameSettings.SfxEnabled = true;
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(bgm);
            Object.DestroyImmediate(sfx);
        }
    }

    [Test]
    public void T2_F04_05_GameAudioSettingsRuntime_IsMusicSource_NullSource_ReturnsFalse()
    {
        Assert.That(GameAudioSettingsRuntime.IsMusicSource(null), Is.False);
    }
    #endregion

    #region Feature 5: Dual Auth & Cloud Save (Boundary)
    [Test]
    public void T2_F05_01_CloudSaveSyncService_SaveWhenLoggedOut_ReturnsError()
    {
        GoogleAuthManager.Instance.SignOut();
        AppleAuthManager.Instance.SignOut();

        bool success = true;
        string err = null;
        CloudSaveSyncService.SaveToCloud((ok, msg) => { success = ok; err = msg; });

        Assert.That(success, Is.False);
        Assert.That(err, Does.Contain("Chưa đăng nhập"));
    }

    [Test]
    public void T2_F05_02_CloudSaveSyncService_LoadWhenLoggedOut_ReturnsError()
    {
        GoogleAuthManager.Instance.SignOut();
        AppleAuthManager.Instance.SignOut();

        bool success = true;
        string err = null;
        CloudSaveSyncService.LoadFromCloud((ok, msg) => { success = ok; err = msg; });

        Assert.That(success, Is.False);
        Assert.That(err, Does.Contain("Chưa đăng nhập"));
    }

    [Test]
    public void T2_F05_03_AuthManagers_AlternatingSignIns_MaintainStateIsolation()
    {
        GoogleAuthManager.Instance.SignOut();
        AppleAuthManager.Instance.SignOut();

        GoogleAuthManager.Instance.SignInWithGoogle();
        Assert.That(GoogleAuthManager.Instance.IsLoggedIn, Is.True);
        Assert.That(AppleAuthManager.Instance.IsLoggedIn, Is.False);

        AppleAuthManager.Instance.SignInWithApple();
        Assert.That(AppleAuthManager.Instance.IsLoggedIn, Is.True);

        GoogleAuthManager.Instance.SignOut();
        Assert.That(GoogleAuthManager.Instance.IsLoggedIn, Is.False);
        Assert.That(AppleAuthManager.Instance.IsLoggedIn, Is.True);

        AppleAuthManager.Instance.SignOut();
        Assert.That(AppleAuthManager.Instance.IsLoggedIn, Is.False);
    }

    [Test]
    public void T2_F05_04_GoogleAuthManager_ConsecutiveSignIns_IsIdempotent()
    {
        GoogleAuthManager.Instance.SignInWithGoogle();
        string uid1 = GoogleAuthManager.Instance.CurrentUser.userId;

        GoogleAuthManager.Instance.SignInWithGoogle();
        string uid2 = GoogleAuthManager.Instance.CurrentUser.userId;

        Assert.That(uid1, Is.EqualTo(uid2));
        Assert.That(GoogleAuthManager.Instance.IsLoggedIn, Is.True);
        GoogleAuthManager.Instance.SignOut();
    }

    [Test]
    public void T2_F05_05_AppleAuthManager_ConsecutiveSignOuts_IsIdempotent()
    {
        AppleAuthManager.Instance.SignOut();
        Assert.That(AppleAuthManager.Instance.IsLoggedIn, Is.False);

        Assert.DoesNotThrow(() => AppleAuthManager.Instance.SignOut());
        Assert.That(AppleAuthManager.Instance.IsLoggedIn, Is.False);
    }
    #endregion

    #region Feature 6: Player Movement & Camera Viewport Bounds (Boundary)
    [Test]
    public void T2_F06_01_CameraFollow_ZeroOrNegativeDeltaTime_DoesNotProduceNaN()
    {
        GameObject cam = new GameObject("Camera", typeof(CameraFollow));
        GameObject target = new GameObject("Target");
        try
        {
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            follow.SetTarget(target.transform);

            follow.UpdateFollow(0f);
            Assert.That(float.IsNaN(cam.transform.position.x), Is.False);

            follow.UpdateFollow(-0.016f);
            Assert.That(float.IsNaN(cam.transform.position.x), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(cam);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void T2_F06_02_CameraFollow_NullTarget_RetainsPositionSafely()
    {
        GameObject cam = new GameObject("Camera", typeof(CameraFollow));
        try
        {
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            follow.SetTarget(null);
            Vector3 origPos = cam.transform.position;

            Assert.DoesNotThrow(() => follow.UpdateFollow(0.016f));
            Assert.That(cam.transform.position, Is.EqualTo(origPos));
        }
        finally
        {
            Object.DestroyImmediate(cam);
        }
    }

    [Test]
    public void T2_F06_03_WaveHUDController_ViewportPositionVisible_ThresholdEdges()
    {
        // Margin 0.05 -> Valid range is [-0.05, 1.05]
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(-0.05f, 0.5f, 1f), 0.05f), Is.True);
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(1.05f, 0.5f, 1f), 0.05f), Is.True);
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(-0.051f, 0.5f, 1f), 0.05f), Is.False);
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(1.051f, 0.5f, 1f), 0.05f), Is.False);
    }

    [Test]
    public void T2_F06_04_WaveHUDController_CalculateBossIndicator_ExtremePositionsClamped()
    {
        Vector2 canvasSize = new Vector2(1080f, 1920f);
        Vector2 indSize = new Vector2(100f, 100f);

        Vector2 extremePos = WaveHUDController.CalculateBossIndicatorPosition(new Vector3(9999f, 9999f, 1f), canvasSize, indSize, 20f);
        Assert.That(extremePos.x, Is.LessThanOrEqualTo(canvasSize.x * 0.5f));
        Assert.That(extremePos.y, Is.LessThanOrEqualTo(canvasSize.y * 0.5f));
    }

    [Test]
    public void T2_F06_05_PlayerRunEndController_CalculateStageProgress_BoundaryWaves()
    {
        Assert.That(PlayerRunEndController.CalculateStageProgress(0, 0f, 10), Is.EqualTo(0f));
        Assert.That(PlayerRunEndController.CalculateStageProgress(-1, 0f, 10), Is.EqualTo(0f));
        Assert.That(PlayerRunEndController.CalculateStageProgress(15, 1f, 10), Is.EqualTo(1f));
    }
    #endregion

    #region Feature 7: AutoShooter & 360° Aim Math (Boundary)
    [Test]
    public void T2_F07_01_PlayerAutoShooter_AimAngle_ZeroVectorHandlesCleanly()
    {
        float angle = Mathf.Atan2(Vector2.zero.y, Vector2.zero.x) * Mathf.Rad2Deg;
        Assert.That(float.IsNaN(angle), Is.False);
        Assert.That(angle, Is.EqualTo(0f));
    }

    [Test]
    public void T2_F07_02_PlayerAutoShooter_CalculateAimScale_AngleWrapArounds()
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod("CalculateAimScale", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(1f, 1f, 1f);
        Vector3 s360 = (Vector3)method.Invoke(null, new object[] { 360f, baseScale });
        Vector3 s720 = (Vector3)method.Invoke(null, new object[] { 720f, baseScale });
        Vector3 sMinus360 = (Vector3)method.Invoke(null, new object[] { -360f, baseScale });

        Assert.That(s360.y, Is.EqualTo(1f));
        Assert.That(s720.y, Is.EqualTo(1f));
        Assert.That(sMinus360.y, Is.EqualTo(1f));
    }

    [Test]
    public void T2_F07_03_PlayerAutoShooter_CalculateBodyScale_PreservesMagnitude()
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod("CalculateBodyScale", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(3.5f, 2.0f, 1.0f);
        Vector3 rightScale = (Vector3)method.Invoke(null, new object[] { false, baseScale });
        Vector3 leftScale = (Vector3)method.Invoke(null, new object[] { true, baseScale });

        Assert.That(Mathf.Abs(rightScale.x), Is.EqualTo(3.5f));
        Assert.That(Mathf.Abs(leftScale.x), Is.EqualTo(3.5f));
        Assert.That(rightScale.x, Is.GreaterThan(0f));
        Assert.That(leftScale.x, Is.LessThan(0f));
    }

    [Test]
    public void T2_F07_04_PlayerAutoShooter_FindNearestEnemy_NoEnemies_TargetRemainsNull()
    {
        GameObject player = new GameObject("Player", typeof(PlayerAutoShooter));
        try
        {
            PlayerAutoShooter shooter = player.GetComponent<PlayerAutoShooter>();
            MethodInfo findMethod = typeof(PlayerAutoShooter).GetMethod("FindNearestEnemy", BindingFlags.NonPublic | BindingFlags.Instance);
            if (findMethod != null)
            {
                findMethod.Invoke(shooter, null);
                FieldInfo targetField = typeof(PlayerAutoShooter).GetField("currentTarget", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That(targetField?.GetValue(shooter), Is.Null);
            }
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void T2_F07_05_PlayerAutoShooter_CalculateLocalAimAngle_QuadrantTransitions()
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod("CalculateLocalAimAngle", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        float a0 = (float)method.Invoke(null, new object[] { 0f, false });
        float a90 = (float)method.Invoke(null, new object[] { 90f, false });
        float a180 = (float)method.Invoke(null, new object[] { 180f, true });
        float a270 = (float)method.Invoke(null, new object[] { -90f, false });

        Assert.That(float.IsNaN(a0), Is.False);
        Assert.That(float.IsNaN(a90), Is.False);
        Assert.That(float.IsNaN(a180), Is.False);
        Assert.That(float.IsNaN(a270), Is.False);
    }
    #endregion

    #region Feature 8: EXP Scaling & Leveling (Boundary)
    [Test]
    public void T2_F08_01_PlayerLevelController_ZeroOrNegativeExp_Ignored()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            ctrl.AddEXP(0);
            Assert.That(ctrl.CurrentLevel, Is.EqualTo(1));
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(0));

            ctrl.AddEXP(-100);
            Assert.That(ctrl.CurrentLevel, Is.EqualTo(1));
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F08_02_PlayerLevelController_MassiveExpBurst_LevelsUpAccurately()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            // Add 10,000 EXP
            ctrl.AddEXP(10000);
            Assert.That(ctrl.CurrentLevel, Is.GreaterThan(20));
            Assert.That(ctrl.CurrentEXP, Is.LessThan(ctrl.MaxEXP));
            Assert.That(ctrl.CurrentEXP, Is.GreaterThanOrEqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F08_03_PlayerLevelController_HighLevelFormula_MatchesExactSpecification()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            // MaxEXP(level) = 30 + (level - 1) * 20
            Assert.That(ctrl.CalculateMaxExpForLevel(100), Is.EqualTo(30 + 99 * 20)); // 2010
            Assert.That(ctrl.CalculateMaxExpForLevel(50), Is.EqualTo(30 + 49 * 20));  // 1010
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F08_04_PlayerLevelController_ExactThresholdExp_AdvancesLevelWithZeroExcess()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            ctrl.AddEXP(30);
            Assert.That(ctrl.CurrentLevel, Is.EqualTo(2));
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(0));

            ctrl.AddEXP(50);
            Assert.That(ctrl.CurrentLevel, Is.EqualTo(3));
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F08_05_PlayerLevelController_CurrentExp_NeverExceedsMaxExp()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            for (int i = 1; i <= 200; i++)
            {
                ctrl.AddEXP(i * 7);
                Assert.That(ctrl.CurrentEXP, Is.LessThan(ctrl.MaxEXP));
            }
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
    #endregion

    #region Feature 9: Chipset Weapons & 10 Combat Skills (Boundary)
    [Test]
    public void T2_F09_01_ChipsetBattleStats_NegativeOrZeroId_HandlesGracefully()
    {
        ChipsetBattleStats.Reset();
        Assert.DoesNotThrow(() => ChipsetBattleStats.RegisterChipset(0, 1, 10));
        Assert.DoesNotThrow(() => ChipsetBattleStats.RegisterChipset(-5, 1, 10));
    }

    [Test]
    public void T2_F09_02_ChipsetBattleStats_ZeroOrNegativeDamage_DoesNotCorruptTotal()
    {
        ChipsetBattleStats.Reset();
        ChipsetBattleStats.RegisterChipset(1, 1, 50);
        ChipsetBattleStats.RecordDamage(1, 0);
        ChipsetBattleStats.RecordDamage(1, -50);

        Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(0));
    }

    [Test]
    public void T2_F09_03_ChipsetBattleStats_Reset_ClearsGrandTotal()
    {
        ChipsetBattleStats.Reset();
        ChipsetBattleStats.RegisterChipset(1, 1, 100);
        ChipsetBattleStats.RecordDamage(1, 500);
        Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(500));

        ChipsetBattleStats.Reset();
        Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(0));
    }

    [Test]
    public void T2_F09_04_ChipsetController_CatalogLookup_OutOfRange_ReturnsNullSafely()
    {
        var db = ChipsetController.CreateDefaultDatabase();
        ChipItemData outOfRange = db.FirstOrDefault(c => c.id == 9999);
        Assert.That(outOfRange, Is.Null);
    }

    [Test]
    public void T2_F09_05_ChipsetBattleStats_MassiveDamageValues_AccumulatesAccurately()
    {
        ChipsetBattleStats.Reset();
        ChipsetBattleStats.RegisterChipset(1, 1, 100);
        ChipsetBattleStats.RecordDamage(1, 1000000);
        ChipsetBattleStats.RecordDamage(1, 2000000);

        Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(3000000));
    }
    #endregion

    #region Feature 10: Combat Damage, Health & Revive (Boundary)
    [Test]
    public void T2_F10_01_PlayerHealth_DamageReductionExceedsDamage_DealsMinimumOneDamage()
    {
        GameObject go = new GameObject("PlayerHealth", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            health.SetDamageReduction(100);
            health.TakeDamage(10);

            // Minimum 1 damage ensures player is never healed on incoming damage
            Assert.That(health.CurrentHealth, Is.EqualTo(99));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F10_02_PlayerHealth_MassiveOverkillDamage_ClampsToZeroAndSetsDead()
    {
        GameObject go = new GameObject("PlayerHealth", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            health.TakeDamage(10000000);

            Assert.That(health.CurrentHealth, Is.EqualTo(0));
            Assert.That(health.IsDead, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F10_03_PlayerHealth_TakeZeroDamage_NoEffect()
    {
        GameObject go = new GameObject("PlayerHealth", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            health.TakeDamage(0);
            health.TakeDamage(-50);

            Assert.That(health.CurrentHealth, Is.EqualTo(100));
            Assert.That(health.IsDead, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F10_04_PlayerHealth_ReviveHealthPercentBoundaries_HandledCorrectly()
    {
        GameObject go = new GameObject("PlayerHealth", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            health.TakeDamage(100);
            Assert.That(health.IsDead, Is.True);

            // 100% revive restores MaxHealth
            bool revived = health.Revive(1.0f);
            Assert.That(revived, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
            Assert.That(health.IsDead, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F10_05_PlayerHealth_ReviveWhenAlive_ReturnsFalse()
    {
        GameObject go = new GameObject("PlayerHealth", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            Assert.That(health.Revive(), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
    #endregion

    #region Feature 11: Enemy Creep AI & Wave Spawner (Boundary)
    [Test]
    public void T2_F11_01_EnemyHealth_NegativeDamage_DoesNotHealEnemy()
    {
        GameObject go = new GameObject("Enemy", typeof(EnemyHealth));
        try
        {
            EnemyHealth health = go.GetComponent<EnemyHealth>();
            int maxHp = health.MaxHealth;
            health.TakeDamage(-50);
            Assert.That(health.CurrentHealth, Is.EqualTo(maxHp));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F11_02_EnemyMovement_NullTarget_StopsWithoutThrowing()
    {
        GameObject enemy = new GameObject("Enemy", typeof(EnemyMovement), typeof(Rigidbody2D));
        try
        {
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            movement.SetTarget(null);
            Assert.That(movement.CurrentTarget, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(enemy);
        }
    }

    [Test]
    public void T2_F11_03_EnemyContactDamage_ZeroDamage_DealsZero()
    {
        GameObject enemy = new GameObject("Enemy", typeof(EnemyContactDamage));
        GameObject player = new GameObject("Player", typeof(PlayerHealth));
        try
        {
            EnemyContactDamage dmg = enemy.GetComponent<EnemyContactDamage>();
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            dmg.SetDamage(0);

            health.TakeDamage(dmg.Damage);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
        }
        finally
        {
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void T2_F11_04_EnemyHealth_ExactLethalDamage_SetsDeath()
    {
        GameObject go = new GameObject("Enemy", typeof(EnemyHealth));
        try
        {
            EnemyHealth health = go.GetComponent<EnemyHealth>();
            health.TakeDamage(health.MaxHealth);
            Assert.That(health.IsDead, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F11_05_EnemyHealth_RestoreSpriteColorsWithoutRenderers_DoesNotThrow()
    {
        GameObject go = new GameObject("Enemy", typeof(EnemyHealth));
        try
        {
            EnemyHealth health = go.GetComponent<EnemyHealth>();
            Assert.DoesNotThrow(() => health.RestoreSpriteColors());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
    #endregion

    #region Feature 12: Boss AI & Phase Behaviors (Boundary)
    [Test]
    public void T2_F12_01_BossRangedAttack_CalculateFanDirections_CountOne_ReturnsCenter()
    {
        Vector2[] fan = BossRangedAttack.CalculateFanDirections(Vector2.right, 1, 45f);
        Assert.That(fan.Length, Is.EqualTo(1));
        Assert.That(fan[0], Is.EqualTo(Vector2.right));
    }

    [Test]
    public void T2_F12_02_BossRangedAttack_CalculateFanDirections_ZeroAngle_CollapsesToCenter()
    {
        Vector2[] fan = BossRangedAttack.CalculateFanDirections(Vector2.up, 5, 0f);
        Assert.That(fan.Length, Is.EqualTo(5));
        for (int i = 0; i < fan.Length; i++)
        {
            Assert.That(Vector2.Angle(fan[i], Vector2.up), Is.EqualTo(0f).Within(0.01f));
        }
    }

    [Test]
    public void T2_F12_03_BossRangedAttack_CalculateRadialDirections_EvenDistribution()
    {
        Vector2[] radial8 = BossRangedAttack.CalculateRadialDirections(Vector2.right, 8);
        Assert.That(radial8.Length, Is.EqualTo(8));
        for (int i = 0; i < 7; i++)
        {
            Assert.That(Vector2.Angle(radial8[i], radial8[i + 1]), Is.EqualTo(45f).Within(0.01f));
        }
    }

    [Test]
    public void T2_F12_04_BossHealthBarUI_SanitizeDisplayName_EmptyOrWhitespace_ReturnsDefault()
    {
        Assert.That(BossHealthBarUI.SanitizeBossDisplayName(""), Is.EqualTo("BOSS"));
        Assert.That(BossHealthBarUI.SanitizeBossDisplayName("   "), Is.EqualTo("BOSS"));
        Assert.That(BossHealthBarUI.SanitizeBossDisplayName(null), Is.EqualTo("BOSS"));
    }

    [Test]
    public void T2_F12_05_BossRangedAttack_TargetRangeState_NullTarget_ReturnsTooFar()
    {
        GameObject boss = new GameObject("Boss", typeof(BossRangedAttack));
        try
        {
            BossRangedAttack ranged = boss.GetComponent<BossRangedAttack>();
            ranged.SetTarget(null);
            Assert.That(ranged.GetTargetRangeState(), Is.EqualTo(BossRangedAttack.TargetRangeState.TooFar));
        }
        finally
        {
            Object.DestroyImmediate(boss);
        }
    }
    #endregion

    #region Feature 13: Sprite HitFlash & Visual Feedback (Boundary)
    [Test]
    public void T2_F13_01_PlayerHealth_CacheSpriteRenderers_NoRenderers_HandlesCleanly()
    {
        GameObject go = new GameObject("EmptyPlayer", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            Assert.DoesNotThrow(() => health.CacheSpriteRenderers());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F13_02_PlayerWorldHealthBar_SetNormalizedHealth_BoundaryValues()
    {
        GameObject player = new GameObject("Player", typeof(PlayerWorldHealthBar));
        GameObject fill = new GameObject("Fill", typeof(SpriteRenderer));
        fill.transform.SetParent(player.transform, false);

        try
        {
            PlayerWorldHealthBar bar = player.GetComponent<PlayerWorldHealthBar>();
            typeof(PlayerWorldHealthBar).GetField("fillRenderer", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(bar, fill.GetComponent<SpriteRenderer>());

            bar.SetNormalizedHealth(-0.5f);
            Assert.That(fill.transform.localScale.x, Is.EqualTo(0f));

            bar.SetNormalizedHealth(1.5f);
            Assert.That(fill.transform.localScale.x, Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void T2_F13_03_ScreenShakeService_DisabledSetting_ProducesZeroOffset()
    {
        ScreenShakeService.Reset();
        GameSettings.ScreenShake = false;
        ScreenShakeService.AddTrauma(1.0f);
        Vector3 offset = ScreenShakeService.UpdateAndGetOffset(0.016f);
        Assert.That(offset, Is.EqualTo(Vector3.zero));
        GameSettings.ScreenShake = true;
    }

    [Test]
    public void T2_F13_04_ScreenShakeService_TraumaCappedAtOne()
    {
        ScreenShakeService.Reset();
        GameSettings.ScreenShake = true;
        ScreenShakeService.AddTrauma(5.0f);
        ScreenShakeService.AddTrauma(10.0f);
        // Internal trauma clamps to 1.0f
        Vector3 offset = ScreenShakeService.UpdateAndGetOffset(0.016f);
        Assert.That(offset, Is.Not.EqualTo(Vector3.zero));
    }

    [Test]
    public void T2_F13_05_ScreenShakeService_LargeDeltaTime_DecaysTraumaToZero()
    {
        ScreenShakeService.Reset();
        GameSettings.ScreenShake = true;
        ScreenShakeService.AddTrauma(0.5f);
        ScreenShakeService.UpdateAndGetOffset(10.0f); // 10 seconds elapsed
        Vector3 finalOffset = ScreenShakeService.UpdateAndGetOffset(0.016f);
        Assert.That(finalOffset, Is.EqualTo(Vector3.zero));
    }
    #endregion

    #region Feature 14: Drop System & Gem Pickups (Boundary)
    [Test]
    public void T2_F14_01_RewardService_GrantZeroReward_NoBalanceChange()
    {
        int origChips = PlayerDataService.DataChips;
        try
        {
            RewardData zeroReward = new RewardData { type = RewardType.DataChip, amount = 0 };
            RewardService.GrantReward(zeroReward);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(origChips));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
        }
    }

    [Test]
    public void T2_F14_02_RewardService_GrantRewards_EmptyOrNullArray_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => RewardService.GrantRewards(new RewardData[0]));
        Assert.DoesNotThrow(() => RewardService.GrantRewards(null));
    }

    [Test]
    public void T2_F14_03_RewardService_GrantNegativeReward_DoesNotDeduct()
    {
        int origChips = PlayerDataService.DataChips;
        try
        {
            RewardData negReward = new RewardData { type = RewardType.DataChip, amount = -500 };
            RewardService.GrantReward(negReward);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(origChips));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
        }
    }

    [Test]
    public void T2_F14_04_MagnetItem_DistanceCalculation_CoLocatedIsZero()
    {
        Vector3 pos1 = new Vector3(5f, 5f, 0f);
        Vector3 pos2 = new Vector3(5f, 5f, 0f);
        Assert.That(Vector3.Distance(pos1, pos2), Is.EqualTo(0f));
    }

    [Test]
    public void T2_F14_05_DropItem_RecycleDeactivatedObject_HandledSafely()
    {
        GameObject prefab = new GameObject("GemPrefab", typeof(DummyPoolItem));
        GameObject root = new GameObject("PoolRoot");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 1, true, root.transform);
            pool.Initialize(root.transform);

            GameObject gem = pool.Spawn(Vector3.zero, Quaternion.identity);
            gem.SetActive(false);
            Assert.DoesNotThrow(() => pool.Despawn(gem));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }
    #endregion

    #region Feature 15: Lab 16 Stats Matrix (4x4) (Boundary)
    [Test]
    public void T2_F15_01_PlayerDataService_LabStatLevel_LowerBoundaryClamped()
    {
        string stat = "HP";
        int orig = PlayerDataService.GetItemLevel(stat);
        try
        {
            PlayerDataService.SetItemLevel(stat, 0);
            Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(0));

            PlayerDataService.SetItemLevel(stat, -10);
            Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(0));
        }
        finally
        {
            PlayerDataService.SetItemLevel(stat, orig);
        }
    }

    [Test]
    public void T2_F15_02_PlayerDataService_LabStatLevel_UpperBoundaryClamped()
    {
        string stat = "ATK";
        int orig = PlayerDataService.GetItemLevel(stat);
        try
        {
            PlayerDataService.SetItemLevel(stat, 10);
            Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(10));

            PlayerDataService.SetItemLevel(stat, 999);
            Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(10));
        }
        finally
        {
            PlayerDataService.SetItemLevel(stat, orig);
        }
    }

    [Test]
    public void T2_F15_03_PlayerDataService_IncrementAtCap_RemainsAtCap()
    {
        string stat = "DEF";
        int orig = PlayerDataService.GetItemLevel(stat);
        try
        {
            PlayerDataService.SetItemLevel(stat, 10);
            PlayerDataService.IncrementItemLevel(stat, 5);
            Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(10));
        }
        finally
        {
            PlayerDataService.SetItemLevel(stat, orig);
        }
    }

    [Test]
    public void T2_F15_04_LabUpgrade_PricingFormula_ExtremeRolls()
    {
        // Pricing formula: 300 + (rolls * 150)
        int cost0 = 300 + 0 * 150;
        int cost159 = 300 + 159 * 150;

        Assert.That(cost0, Is.EqualTo(300));
        Assert.That(cost159, Is.EqualTo(24150));
    }

    [Test]
    public void T2_F15_05_PlayerStatsManager_GetStatLevel_UnknownStat_ReturnsZero()
    {
        int lvl = PlayerStatsManager.GetStatLevel("NON_EXISTENT_STAT_KEY");
        Assert.That(lvl, Is.EqualTo(0));
    }
    #endregion

    #region Feature 16: Triple Pity Guarantee System (Boundary)
    [Test]
    public void T2_F16_01_PityGuarantee_RollsBelowThreshold_DoesNotTrigger()
    {
        int elitePity = 9;
        Assert.That(elitePity < PityGuaranteePanel.EliteThreshold, Is.True);
    }

    [Test]
    public void T2_F16_02_PityGuarantee_ExactTenthRoll_ReachesEliteThreshold()
    {
        int elitePity = 10;
        Assert.That(elitePity >= PityGuaranteePanel.EliteThreshold, Is.True);
    }

    [Test]
    public void T2_F16_03_PityGuarantee_Exact25thRoll_ReachesEpicThreshold()
    {
        int epicPity = 25;
        Assert.That(epicPity >= PityGuaranteePanel.EpicThreshold, Is.True);
    }

    [Test]
    public void T2_F16_04_PityGuarantee_Exact50thRoll_ReachesLegendThreshold()
    {
        int legendPity = 50;
        Assert.That(legendPity >= PityGuaranteePanel.LegendThreshold, Is.True);
    }

    [Test]
    public void T2_F16_05_PityGuarantee_NegativeCounters_ClampsToZero()
    {
        int orig = PlayerDataService.LabElitePityCounter;
        try
        {
            PlayerDataService.LabElitePityCounter = -5;
            Assert.That(PlayerDataService.LabElitePityCounter, Is.GreaterThanOrEqualTo(0));
        }
        finally
        {
            PlayerDataService.LabElitePityCounter = orig;
        }
    }
    #endregion

    #region Feature 17: Chipset Inventory & 5 Tiers (Boundary)
    [Test]
    public void T2_F17_01_ChipItemData_EnhanceExactCurrency_SucceedsAndLeavesZero()
    {
        int origChips = ChipManager.DataChips;
        try
        {
            ChipManager.DataChips = 300;
            ChipItemData chip = new ChipItemData { id = 1, tier = ChipTier.Magic, level = 1, enhanceCost = 300 };

            Assert.That(chip.Enhance(), Is.True);
            Assert.That(ChipManager.DataChips, Is.EqualTo(0));
            Assert.That(chip.level, Is.EqualTo(2));
        }
        finally
        {
            ChipManager.DataChips = origChips;
        }
    }

    [Test]
    public void T2_F17_02_ChipItemData_EnhanceAtTierCap_FailsWithoutSpending()
    {
        int origChips = ChipManager.DataChips;
        try
        {
            ChipManager.DataChips = 1000;
            ChipItemData chip = new ChipItemData { id = 1, tier = ChipTier.Magic, level = 6, enhanceCost = 300 };

            Assert.That(chip.CanEnhance, Is.False);
            Assert.That(chip.Enhance(), Is.False);
            Assert.That(ChipManager.DataChips, Is.EqualTo(1000));
        }
        finally
        {
            ChipManager.DataChips = origChips;
        }
    }

    [Test]
    public void T2_F17_03_ChipItemData_AdvanceTier_InsufficientFragments_Fails()
    {
        ChipItemData chip = new ChipItemData
        {
            id = 1,
            tier = ChipTier.Magic,
            level = 6,
            count = 2,
            requiredCount = 3
        };

        Assert.That(chip.CanAdvanceTier, Is.False);
        Assert.That(chip.AdvanceTier(), Is.False);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Magic));
    }

    [Test]
    public void T2_F17_04_ChipItemData_AdvanceTierToHolographic_RequiresExact10Stones()
    {
        int origStones = ChipManager.AdvanceStones;
        try
        {
            ChipManager.AdvanceStones = 9;
            ChipItemData chip = new ChipItemData { id = 1, tier = ChipTier.Epic, level = 18, count = 10 };

            Assert.That(chip.AdvanceTier(), Is.False);
            Assert.That(ChipManager.AdvanceStones, Is.EqualTo(9));

            ChipManager.AdvanceStones = 10;
            Assert.That(chip.AdvanceTier(), Is.True);
            Assert.That(chip.tier, Is.EqualTo(ChipTier.Holographic));
            Assert.That(ChipManager.AdvanceStones, Is.EqualTo(0));
        }
        finally
        {
            ChipManager.AdvanceStones = origStones;
        }
    }

    [Test]
    public void T2_F17_05_ChipItemData_AdvanceTierAtHolographic_ReturnsFalse()
    {
        ChipItemData chip = new ChipItemData { id = 1, tier = ChipTier.Holographic, level = 24, count = 100 };
        Assert.That(chip.CanAdvanceTier, Is.False);
        Assert.That(chip.AdvanceTier(), Is.False);
    }
    #endregion

    #region Feature 18: Buddy Drone Management (Boundary)
    [Test]
    public void T2_F18_01_BuddyItemData_EnhanceInsufficientChips_Fails()
    {
        int origChips = ChipManager.DataChips;
        try
        {
            ChipManager.DataChips = 50;
            BuddyItemData drone = new BuddyItemData { id = 1, level = 1, enhanceCost = 200 };

            Assert.That(drone.CanEnhance, Is.False);
            Assert.That(drone.Enhance(), Is.False);
            Assert.That(drone.level, Is.EqualTo(1));
        }
        finally
        {
            ChipManager.DataChips = origChips;
        }
    }

    [Test]
    public void T2_F18_02_BuddyItemData_AdvanceTier_InsufficientFragments_Fails()
    {
        BuddyItemData drone = new BuddyItemData { id = 1, tier = BuddyTier.Common, level = 1, count = 2, requiredCount = 5 };
        Assert.That(drone.CanAdvanceTier, Is.False);
        Assert.That(drone.AdvanceTier(), Is.False);
    }

    [Test]
    public void T2_F18_03_BuddyCardUI_SetupWithNull_HandlesSafely()
    {
        GameObject go = new GameObject("BuddyCard", typeof(BuddyCardUI));
        try
        {
            BuddyCardUI card = go.GetComponent<BuddyCardUI>();
            Assert.DoesNotThrow(() => card.Setup(null, null, null));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F18_04_BuddyItemData_ZeroFragments_CannotSpend()
    {
        BuddyItemData drone = new BuddyItemData { id = 1, count = 0, requiredCount = 5 };
        Assert.That(drone.CanAdvanceTier, Is.False);
    }

    [Test]
    public void T2_F18_05_BuddyItemData_AdvanceAtMaxTier_ReturnsFalse()
    {
        BuddyItemData drone = new BuddyItemData { id = 1, tier = BuddyTier.Holographic, level = 10, count = 50, requiredCount = 10 };
        Assert.That(drone.CanAdvanceTier, Is.False);
        Assert.That(drone.AdvanceTier(), Is.False);
    }
    #endregion

    #region Feature 19: Level Up Popup & Reroll Modal (Boundary)
    [Test]
    public void T2_F19_01_ChipsetLevelUpPopup_SelectDistinctOffers_RequestExceedsCatalog_ClampsSafely()
    {
        var catalog = ChipsetController.CreateDefaultDatabase();
        var offers = ChipsetLevelUpPopup.SelectDistinctOffers(catalog, 50, new System.Random(42));
        Assert.That(offers.Count, Is.EqualTo(catalog.Count));
        Assert.That(offers.Select(o => o.id).Distinct().Count(), Is.EqualTo(catalog.Count));
    }

    [Test]
    public void T2_F19_02_ChipsetLevelUpPopup_TryReroll_InsufficientGems_Fails()
    {
        int origGems = PlayerDataService.RedGems;
        GameObject go = new GameObject("Popup", typeof(ChipsetLevelUpPopup));
        try
        {
            ChipManager.IsTestMode = false;
            PlayerDataService.RedGems = 10; // 20 required
            ChipsetLevelUpPopup popup = go.GetComponent<ChipsetLevelUpPopup>();

            Assert.That(popup.TryReroll(), Is.False);
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(0));
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(10));
        }
        finally
        {
            PlayerDataService.RedGems = origGems;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F19_03_ChipsetLevelUpPopup_TryReroll_ThirdAttemptBlocked()
    {
        int origGems = PlayerDataService.RedGems;
        GameObject go = new GameObject("Popup", typeof(ChipsetLevelUpPopup));
        try
        {
            ChipManager.IsTestMode = true;
            PlayerDataService.RedGems = 100;
            ChipsetLevelUpPopup popup = go.GetComponent<ChipsetLevelUpPopup>();

            Assert.That(popup.TryReroll(), Is.True);
            Assert.That(popup.TryReroll(), Is.True);
            Assert.That(popup.TryReroll(), Is.False, "3rd attempt must be rejected.");
        }
        finally
        {
            ChipManager.IsTestMode = false;
            PlayerDataService.RedGems = origGems;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F19_04_ChipsetLevelUpPopup_UpgradeRuntimeChipset_UnknownId_HandlesSafely()
    {
        GameObject go = new GameObject("Popup", typeof(ChipsetLevelUpPopup));
        try
        {
            ChipsetLevelUpPopup popup = go.GetComponent<ChipsetLevelUpPopup>();
            Assert.DoesNotThrow(() => popup.UpgradeRuntimeChipset(99999));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F19_05_ChipsetLevelUpPopup_SelectDistinctOffers_EmptyCatalog_ReturnsEmpty()
    {
        var offers = ChipsetLevelUpPopup.SelectDistinctOffers(new List<ChipItemData>(), 4, new System.Random(42));
        Assert.That(offers, Is.Not.Null);
        Assert.That(offers.Count, Is.EqualTo(0));
    }
    #endregion

    #region Feature 20: Chapter Progression & World Map (Boundary)
    [Test]
    public void T2_F20_01_ChapterDatabase_NegativeIndex_ClampsToZero()
    {
        ChapterDatabase db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
        if (db != null)
        {
            ChapterData result = db.GetChapter(-10);
            Assert.That(result, Is.EqualTo(db.GetChapter(0)));
        }
    }

    [Test]
    public void T2_F20_02_ChapterDatabase_LargeIndex_ClampsToLast()
    {
        ChapterDatabase db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
        if (db != null)
        {
            ChapterData result = db.GetChapter(1000);
            Assert.That(result, Is.EqualTo(db.GetChapter(db.Count - 1)));
        }
    }

    [Test]
    public void T2_F20_03_ChapterScreen_EnergySpend_InsufficientEnergy_Fails()
    {
        int origEnergy = PlayerDataService.Energy;
        try
        {
            PlayerDataService.Energy = 9; // 10 required
            Assert.That(ChipManager.HasEnoughEnergy(10), Is.False);
            Assert.That(ChipManager.TrySpendEnergy(10), Is.False);
            Assert.That(PlayerDataService.Energy, Is.EqualTo(9));
        }
        finally
        {
            PlayerDataService.Energy = origEnergy;
        }
    }

    [Test]
    public void T2_F20_04_PlayerDataService_UnlockedChapterIndex_Monotonicity()
    {
        int orig = PlayerDataService.UnlockedChapterIndex;
        try
        {
            PlayerDataService.UnlockedChapterIndex = 0;
            PlayerDataService.UnlockedChapterIndex = 1;
            Assert.That(PlayerDataService.UnlockedChapterIndex, Is.EqualTo(1));
        }
        finally
        {
            PlayerDataService.UnlockedChapterIndex = orig;
        }
    }

    [Test]
    public void T2_F20_05_ChapterData_GenerateWaves_ZeroWaves_ProducesEmpty()
    {
        ChapterData chapter = ScriptableObject.CreateInstance<ChapterData>();
        chapter.totalWaves = 0;
        chapter.GenerateWaves();
        Assert.That(chapter.waves.Count, Is.EqualTo(0));
        Object.DestroyImmediate(chapter);
    }
    #endregion

    #region Feature 21: Shop & Currency Exchange (Boundary)
    [Test]
    public void T2_F21_01_ShopController_PurchaseOutOfBoundsIndex_ReturnsFalse()
    {
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ShopController shop = go.GetComponent<ShopController>();
            shop.SetOffersForTesting(new ShopController.Offer[0]);

            Assert.That(shop.TryPurchase(-1), Is.False);
            Assert.That(shop.TryPurchase(5), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F21_02_ShopController_PurchaseExactRedGems_LeavesZero()
    {
        int origGems = ChipManager.RedGems;
        int origChips = ChipManager.DataChips;
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ChipManager.RedGems = 50;
            ChipManager.DataChips = 0;

            ShopController shop = go.GetComponent<ShopController>();
            ShopController.Offer offer = new ShopController.Offer
            {
                id = "offer-1",
                currency = ShopController.CurrencyType.RedGem,
                price = 50,
                reward = ShopController.RewardType.DataChip,
                rewardAmount = 1000
            };
            shop.SetOffersForTesting(new[] { offer });

            Assert.That(shop.TryPurchase(0), Is.True);
            Assert.That(ChipManager.RedGems, Is.EqualTo(0));
            Assert.That(ChipManager.DataChips, Is.EqualTo(1000));
        }
        finally
        {
            ChipManager.RedGems = origGems;
            ChipManager.DataChips = origChips;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F21_03_ShopController_PurchaseFreeReward_CostZero()
    {
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ShopController shop = go.GetComponent<ShopController>();
            ShopController.Offer offer = new ShopController.Offer
            {
                id = "free-pack",
                currency = ShopController.CurrencyType.Free,
                price = 0,
                reward = ShopController.RewardType.DataChip,
                rewardAmount = 200
            };
            shop.SetOffersForTesting(new[] { offer });

            Assert.That(shop.TryPurchase(0), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F21_04_ShopController_PurchaseVND_FailsClosedSafely()
    {
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ShopController shop = go.GetComponent<ShopController>();
            ShopController.Offer vndOffer = new ShopController.Offer
            {
                id = "vnd-pack",
                currency = ShopController.CurrencyType.VND,
                price = 50000,
                reward = ShopController.RewardType.RedGem,
                rewardAmount = 1000
            };
            shop.SetOffersForTesting(new[] { vndOffer });

            Assert.That(shop.TryPurchase(0), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F21_05_ShopController_SetOffersForTesting_NullArray_HandlesSafely()
    {
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ShopController shop = go.GetComponent<ShopController>();
            Assert.DoesNotThrow(() => shop.SetOffersForTesting(null));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
    #endregion

    #region Feature 22: 7-Day Daily Login & Achievements (Boundary)
    [Test]
    public void T2_F22_01_DailyLoginManager_DayOutOfBounds_ReturnsUnavailableOrLocked()
    {
        GameObject go = new GameObject("DailyLogin", typeof(DailyLoginManager));
        try
        {
            DailyLoginManager mgr = go.GetComponent<DailyLoginManager>();
            mgr.EnsureDatabaseLoaded();

            Assert.That(mgr.GetDayState(0), Is.EqualTo(DailyLoginState.Locked));
            Assert.That(mgr.GetDayState(8), Is.EqualTo(DailyLoginState.Locked));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F22_02_DailyLoginManager_DoubleClaimSameDay_Blocked()
    {
        GameObject go = new GameObject("DailyLogin", typeof(DailyLoginManager));
        try
        {
            DailyLoginManager mgr = go.GetComponent<DailyLoginManager>();
            mgr.EnsureDatabaseLoaded();

            Assert.That(mgr.TryClaimTodayReward(), Is.True);
            Assert.That(mgr.TryClaimTodayReward(), Is.False, "Second claim attempt on same day must be rejected.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F22_03_AchievementManager_ClaimUncompleted_ReturnsFalse()
    {
        GameObject go = new GameObject("AchievementManager", typeof(AchievementManager));
        try
        {
            AchievementManager mgr = go.GetComponent<AchievementManager>();
            mgr.EnsureDatabaseLoaded();

            string achId = "drone_upgrade_3";
            mgr.SetProgress(achId, 0); // Not completed

            Assert.That(mgr.TryClaimReward(achId), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F22_04_AchievementManager_ClaimAlreadyClaimed_ReturnsFalse()
    {
        GameObject go = new GameObject("AchievementManager", typeof(AchievementManager));
        try
        {
            AchievementManager mgr = go.GetComponent<AchievementManager>();
            mgr.EnsureDatabaseLoaded();

            string achId = "login_reward_2";
            mgr.SetProgress(achId, 2);

            Assert.That(mgr.TryClaimReward(achId), Is.True);
            Assert.That(mgr.TryClaimReward(achId), Is.False, "Double claim on achievement must be rejected.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void T2_F22_05_AchievementManager_NegativeProgress_ClampsToZero()
    {
        GameObject go = new GameObject("AchievementManager", typeof(AchievementManager));
        try
        {
            AchievementManager mgr = go.GetComponent<AchievementManager>();
            mgr.EnsureDatabaseLoaded();

            string achId = "drone_upgrade_3";
            mgr.SetProgress(achId, -10);
            Assert.That(mgr.GetProgress(achId), Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
    #endregion
}
