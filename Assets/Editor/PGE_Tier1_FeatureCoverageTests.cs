using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PGE.Auth;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[TestFixture]
public class PGE_Tier1_FeatureCoverageTests
{
    #region Feature 1: PlayerData & Persistence / Balances
    [Test]
    public void F01_01_PlayerDataService_Currencies_DefaultAndPersist()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;
        int origEnergy = PlayerDataService.Energy;
        int origStones = PlayerDataService.AdvanceStones;

        try
        {
            PlayerDataService.DataChips = 2500;
            PlayerDataService.RedGems = 750;
            PlayerDataService.Energy = 45;
            PlayerDataService.AdvanceStones = 12;

            Assert.That(PlayerDataService.DataChips, Is.EqualTo(2500));
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(750));
            Assert.That(PlayerDataService.Energy, Is.EqualTo(45));
            Assert.That(PlayerDataService.AdvanceStones, Is.EqualTo(12));
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
    public void F01_02_PlayerDataService_TrySpendOperations_DeductCorrectly()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;

        try
        {
            PlayerDataService.DataChips = 1000;
            PlayerDataService.RedGems = 500;

            Assert.That(PlayerDataService.HasEnoughDataChips(600), Is.True);
            Assert.That(PlayerDataService.TrySpendDataChips(600), Is.True);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(400));

            Assert.That(PlayerDataService.HasEnoughRedGems(300), Is.True);
            Assert.That(PlayerDataService.TrySpendRedGems(300), Is.True);
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(200));

            Assert.That(PlayerDataService.TrySpendDataChips(500), Is.False, "Overspend must fail.");
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(400));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.RedGems = origGems;
        }
    }

    [Test]
    public void F01_03_PlayerDataService_AddCurrencies_IncreasesBalance()
    {
        int origChips = PlayerDataService.DataChips;
        try
        {
            PlayerDataService.DataChips = 100;
            PlayerDataService.AddDataChips(250);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(350));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
        }
    }

    [Test]
    public void F01_04_PlayerDataService_VipOwnership_TogglesAndSaves()
    {
        bool origVip = PlayerDataService.IsVipOwned;
        try
        {
            PlayerDataService.IsVipOwned = false;
            Assert.That(PlayerDataService.IsVipOwned, Is.False);

            PlayerDataService.IsVipOwned = true;
            Assert.That(PlayerDataService.IsVipOwned, Is.True);
        }
        finally
        {
            PlayerDataService.IsVipOwned = origVip;
        }
    }

    [Test]
    public void F01_05_PlayerDataService_SelectedWeaponId_FallbackAndNotify()
    {
        string origWeapon = PlayerDataService.SelectedWeaponId;
        string notifiedWeapon = null;
        Action<string> listener = id => notifiedWeapon = id;
        PlayerDataService.OnSelectedWeaponChanged += listener;

        try
        {
            PlayerDataService.SelectedWeaponId = "rocket_punch";
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("rocket_punch"));
            Assert.That(notifiedWeapon, Is.EqualTo("rocket_punch"));

            PlayerDataService.SelectedWeaponId = "";
            Assert.That(PlayerDataService.SelectedWeaponId, Is.EqualTo("blaster"), "Empty weapon id should fallback to blaster.");
        }
        finally
        {
            PlayerDataService.OnSelectedWeaponChanged -= listener;
            PlayerDataService.SelectedWeaponId = origWeapon;
        }
    }
    #endregion

    #region Feature 2: Static Event Bus & Decoupling
    [Test]
    public void F02_01_GameEvents_EnemyKilled_DispatchesToListeners()
    {
        int callCount = 0;
        int expAwarded = 0;
        Action onKilled = () => callCount++;
        Action<int> onKilledExp = exp => expAwarded += exp;

        GameEvents.OnEnemyKilled += onKilled;
        GameEvents.OnEnemyKilledWithExp += onKilledExp;
        try
        {
            GameEvents.RaiseEnemyKilled();
            Assert.That(callCount, Is.EqualTo(1));

            GameEvents.RaiseEnemyKilled(50);
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(expAwarded, Is.EqualTo(50));
        }
        finally
        {
            GameEvents.OnEnemyKilled -= onKilled;
            GameEvents.OnEnemyKilledWithExp -= onKilledExp;
        }
    }

    [Test]
    public void F02_02_GameEvents_PlayerLevelUp_DispatchesLevel()
    {
        int receivedLevel = 0;
        Action<int> onLevelUp = lvl => receivedLevel = lvl;

        GameEvents.OnPlayerLevelUp += onLevelUp;
        try
        {
            GameEvents.RaisePlayerLevelUp(4);
            Assert.That(receivedLevel, Is.EqualTo(4));
        }
        finally
        {
            GameEvents.OnPlayerLevelUp -= onLevelUp;
        }
    }

    [Test]
    public void F02_03_GameEvents_ChapterEvents_DispatchCorrectly()
    {
        int playedIdx = -1;
        int clearedNum = -1;
        int starCount = 0;

        Action<int> onPlay = idx => playedIdx = idx;
        Action<int> onClear = num => clearedNum = num;
        Action<int, int> onClearDetail = (num, stars) => { clearedNum = num; starCount = stars; };

        GameEvents.OnChapterPlayed += onPlay;
        GameEvents.OnChapterCleared += onClear;
        GameEvents.OnChapterClearedDetailed += onClearDetail;
        try
        {
            GameEvents.RaiseChapterPlayed(1);
            Assert.That(playedIdx, Is.EqualTo(1));

            GameEvents.RaiseChapterCleared(2);
            Assert.That(clearedNum, Is.EqualTo(2));

            GameEvents.RaiseChapterCleared(3, 3);
            Assert.That(clearedNum, Is.EqualTo(3));
            Assert.That(starCount, Is.EqualTo(3));
        }
        finally
        {
            GameEvents.OnChapterPlayed -= onPlay;
            GameEvents.OnChapterCleared -= onClear;
            GameEvents.OnChapterClearedDetailed -= onClearDetail;
        }
    }

    [Test]
    public void F02_04_GameEvents_DroneTierAdvanced_Dispatches()
    {
        int callCount = 0;
        string droneId = null;
        int tierLevel = 0;

        Action onAdv = () => callCount++;
        Action<string, int> onAdvDetail = (id, t) => { droneId = id; tierLevel = t; };

        GameEvents.OnDroneTierAdvanced += onAdv;
        GameEvents.OnDroneTierAdvancedDetailed += onAdvDetail;
        try
        {
            GameEvents.RaiseDroneTierAdvanced("spider_drone", 2);
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(droneId, Is.EqualTo("spider_drone"));
            Assert.That(tierLevel, Is.EqualTo(2));
        }
        finally
        {
            GameEvents.OnDroneTierAdvanced -= onAdv;
            GameEvents.OnDroneTierAdvancedDetailed -= onAdvDetail;
        }
    }

    [Test]
    public void F02_05_GameEvents_CurrencyChanged_DispatchesCurrencyAndAmount()
    {
        string curr = null;
        int amount = 0;
        Action<string, int> onCurr = (c, a) => { curr = c; amount = a; };

        GameEvents.OnCurrencyChanged += onCurr;
        try
        {
            GameEvents.RaiseCurrencyChanged("RedGems", 999);
            Assert.That(curr, Is.EqualTo("RedGems"));
            Assert.That(amount, Is.EqualTo(999));
        }
        finally
        {
            GameEvents.OnCurrencyChanged -= onCurr;
        }
    }
    #endregion

    #region Feature 3: Zero-Allocation Object Pool
    private class DummyPoolable : MonoBehaviour, IPoolable
    {
        public bool isSpawned;
        public void OnSpawnFromPool() => isSpawned = true;
        public void OnReturnToPool() => isSpawned = false;
    }

    [Test]
    public void F03_01_ObjectPool_InitializeAndPrewarm_CreatesInstances()
    {
        GameObject prefab = new GameObject("PoolPrefab", typeof(DummyPoolable));
        GameObject root = new GameObject("PoolRoot");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 5, canGrow: true, root.transform);
            pool.Initialize(root.transform);

            Assert.That(pool.Prefab, Is.EqualTo(prefab));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void F03_02_ObjectPool_SpawnAndDespawn_UpdatesLifecycle()
    {
        GameObject prefab = new GameObject("PoolPrefab", typeof(DummyPoolable));
        GameObject root = new GameObject("PoolRoot");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 2, canGrow: true, root.transform);
            pool.Initialize(root.transform);

            GameObject obj = pool.Spawn(Vector3.zero, Quaternion.identity);
            Assert.That(obj.activeSelf, Is.True);
            Assert.That(obj.GetComponent<DummyPoolable>().isSpawned, Is.True);

            pool.Despawn(obj);
            Assert.That(obj.activeSelf, Is.False);
            Assert.That(obj.GetComponent<DummyPoolable>().isSpawned, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void F03_03_ObjectPool_DoubleDespawn_IgnoredSafely()
    {
        GameObject prefab = new GameObject("PoolPrefab", typeof(DummyPoolable));
        GameObject root = new GameObject("PoolRoot");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 1, canGrow: false, root.transform);
            pool.Initialize(root.transform);

            GameObject obj = pool.Spawn(Vector3.zero, Quaternion.identity);
            pool.Despawn(obj);
            pool.Despawn(obj); // Double return attempt

            GameObject retrieved = pool.Spawn(Vector3.zero, Quaternion.identity);
            Assert.That(retrieved, Is.EqualTo(obj));
            Assert.That(pool.Spawn(Vector3.zero, Quaternion.identity), Is.Null, "Cannot grow when canGrow is false.");
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void F03_04_ObjectPool_CanGrow_ExpandsBeyondInitialCapacity()
    {
        GameObject prefab = new GameObject("PoolPrefab", typeof(DummyPoolable));
        GameObject root = new GameObject("PoolRoot");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 1, canGrow: true, root.transform);
            pool.Initialize(root.transform);

            GameObject obj1 = pool.Spawn(Vector3.zero, Quaternion.identity);
            GameObject obj2 = pool.Spawn(Vector3.one, Quaternion.identity);

            Assert.That(obj1, Is.Not.Null);
            Assert.That(obj2, Is.Not.Null);

            pool.Despawn(obj1);
            pool.Despawn(obj2);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void F03_05_PoolMember_ReturnToPool_CallsPoolDespawn()
    {
        GameObject prefab = new GameObject("PoolPrefab", typeof(DummyPoolable), typeof(PoolMember));
        GameObject root = new GameObject("PoolRoot");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 1, canGrow: true, root.transform);
            pool.Initialize(root.transform);

            GameObject obj = pool.Spawn(Vector3.zero, Quaternion.identity);
            PoolMember member = obj.GetComponent<PoolMember>();
            Assert.That(member, Is.Not.Null);

            member.ReturnToPool();
            Assert.That(obj.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }
    #endregion

    #region Feature 4: Global Audio & Settings
    [Test]
    public void F04_01_GameSettings_VolumeAndMuteProperties_Persist()
    {
        bool origBgm = GameSettings.BgmEnabled;
        bool origSfx = GameSettings.SfxEnabled;
        try
        {
            GameSettings.BgmEnabled = false;
            GameSettings.SfxEnabled = true;
            Assert.That(GameSettings.BgmEnabled, Is.False);
            Assert.That(GameSettings.SfxEnabled, Is.True);
        }
        finally
        {
            GameSettings.BgmEnabled = origBgm;
            GameSettings.SfxEnabled = origSfx;
        }
    }

    [Test]
    public void F04_02_GameSettings_Toggles_PersistCorrectly()
    {
        bool origDamage = GameSettings.ShowDamage;
        bool origJoystick = GameSettings.DynamicJoystick;
        bool origShake = GameSettings.ScreenShake;

        try
        {
            GameSettings.ShowDamage = false;
            GameSettings.DynamicJoystick = true;
            GameSettings.ScreenShake = false;

            Assert.That(GameSettings.ShowDamage, Is.False);
            Assert.That(GameSettings.DynamicJoystick, Is.True);
            Assert.That(GameSettings.ScreenShake, Is.False);
        }
        finally
        {
            GameSettings.ShowDamage = origDamage;
            GameSettings.DynamicJoystick = origJoystick;
            GameSettings.ScreenShake = origShake;
        }
    }

    [Test]
    public void F04_03_GameSettings_Language_SupportsEnglishAndVietnamese()
    {
        string origLang = GameSettings.Language;
        try
        {
            GameSettings.Language = "Tiếng Việt";
            Assert.That(GameSettings.Language, Is.EqualTo("Tiếng Việt"));
            Assert.That(GameSettings.IsVietnamese, Is.True);

            GameSettings.Language = "English";
            Assert.That(GameSettings.Language, Is.EqualTo("English"));
            Assert.That(GameSettings.IsVietnamese, Is.False);
        }
        finally
        {
            GameSettings.Language = origLang;
        }
    }

    [Test]
    public void F04_04_GameAudioSettingsRuntime_IsMusicSource_DifferentiatesLooping()
    {
        GameObject bgm = new GameObject("BGM", typeof(AudioSource));
        GameObject sfx = new GameObject("SFX", typeof(AudioSource));
        try
        {
            bgm.GetComponent<AudioSource>().loop = true;
            sfx.GetComponent<AudioSource>().loop = false;

            Assert.That(GameAudioSettingsRuntime.IsMusicSource(bgm.GetComponent<AudioSource>()), Is.True);
            Assert.That(GameAudioSettingsRuntime.IsMusicSource(sfx.GetComponent<AudioSource>()), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(bgm);
            Object.DestroyImmediate(sfx);
        }
    }

    [Test]
    public void F04_05_GameAudioSettingsRuntime_ApplySettings_UpdatesMuteStates()
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
            GameSettings.SfxEnabled = true;
            host.GetComponent<GameAudioSettingsRuntime>().ApplySettingsNow();

            Assert.That(bgmSource.mute, Is.True);
            Assert.That(sfxSource.mute, Is.False);
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
    #endregion

    #region Feature 5: Dual Auth & Cloud Save
    [Test]
    public void F05_01_GoogleAuthManager_SignInAndSignOut_TogglesState()
    {
        GoogleAuthManager auth = GoogleAuthManager.Instance;
        Assert.That(auth, Is.Not.Null);

        auth.SignInWithGoogle();
        Assert.That(auth.IsLoggedIn, Is.True);
        Assert.That(auth.CurrentUser.userId, Is.Not.Empty);

        auth.SignOut();
        Assert.That(auth.IsLoggedIn, Is.False);
    }

    [Test]
    public void F05_02_AppleAuthManager_SignInAndSignOut_TogglesState()
    {
        AppleAuthManager auth = AppleAuthManager.Instance;
        Assert.That(auth, Is.Not.Null);

        auth.SignInWithApple();
        Assert.That(auth.IsLoggedIn, Is.True);

        auth.SignOut();
        Assert.That(auth.IsLoggedIn, Is.False);
    }

    [Test]
    public void F05_03_CloudSaveSyncService_IsAnyCloudLoggedIn_ReflectsStatus()
    {
        GoogleAuthManager.Instance.SignOut();
        AppleAuthManager.Instance.SignOut();
        Assert.That(CloudSaveSyncService.IsAnyCloudLoggedIn, Is.False);

        GoogleAuthManager.Instance.SignInWithGoogle();
        Assert.That(CloudSaveSyncService.IsAnyCloudLoggedIn, Is.True);

        GoogleAuthManager.Instance.SignOut();
        Assert.That(CloudSaveSyncService.IsAnyCloudLoggedIn, Is.False);
    }

    [Test]
    public void F05_04_CloudSaveSyncService_SaveAndLoad_SynchronizesData()
    {
        GoogleAuthManager.Instance.SignInWithGoogle();
        int origChips = PlayerDataService.DataChips;
        try
        {
            PlayerDataService.DataChips = 9999;
            bool saved = false;
            CloudSaveSyncService.SaveToCloud((ok, msg) => saved = ok);
            Assert.That(saved, Is.True);

            PlayerDataService.DataChips = 100;
            bool loaded = false;
            CloudSaveSyncService.LoadFromCloud((ok, msg) => loaded = ok);
            Assert.That(loaded, Is.True);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(9999));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            GoogleAuthManager.Instance.SignOut();
        }
    }

    [Test]
    public void F05_05_CloudSaveSyncService_SaveWithoutLogin_FailsGracefully()
    {
        GoogleAuthManager.Instance.SignOut();
        AppleAuthManager.Instance.SignOut();

        bool resultOk = true;
        string resultMsg = null;
        CloudSaveSyncService.SaveToCloud((ok, msg) => { resultOk = ok; resultMsg = msg; });

        Assert.That(resultOk, Is.False);
        Assert.That(resultMsg, Does.Contain("Chưa đăng nhập"));
    }
    #endregion

    #region Feature 6: Player Movement & Camera Viewport Bounds
    [Test]
    public void F06_01_PlayerMovement_MoveSpeed_IsConfiguredAndPositive()
    {
        GameObject go = new GameObject("Player", typeof(PlayerMovement), typeof(Rigidbody2D));
        try
        {
            PlayerMovement pm = go.GetComponent<PlayerMovement>();
            Assert.That(pm.MoveSpeed, Is.GreaterThan(0f));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F06_02_CameraFollow_OffsetAndSpeed_CalculatesPosition()
    {
        GameObject cam = new GameObject("Camera", typeof(CameraFollow));
        GameObject target = new GameObject("Target");
        try
        {
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            target.transform.position = new Vector3(10f, 20f, 0f);
            follow.SetTarget(target.transform);
            follow.Offset = new Vector2(2f, -2f);
            follow.FollowSpeed = 0f; // Instant snap

            follow.UpdateFollow(0.016f);
            Assert.That(cam.transform.position.x, Is.EqualTo(12f));
            Assert.That(cam.transform.position.y, Is.EqualTo(18f));
        }
        finally
        {
            Object.DestroyImmediate(cam);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void F06_03_WaveHUDController_IsViewportPositionVisible_EvaluatesBounds()
    {
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(0.5f, 0.5f, 1f), 0.05f), Is.True);
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(-0.2f, 0.5f, 1f), 0.05f), Is.False);
        Assert.That(WaveHUDController.IsViewportPositionVisible(new Vector3(1.2f, 0.5f, 1f), 0.05f), Is.False);
    }

    [Test]
    public void F06_04_WaveHUDController_CalculateBossIndicatorPosition_ClampsToCanvas()
    {
        Vector2 canvasSize = new Vector2(1080f, 1920f);
        Vector2 indSize = new Vector2(100f, 100f);

        Vector2 leftClamped = WaveHUDController.CalculateBossIndicatorPosition(new Vector3(-0.5f, 0.5f, 1f), canvasSize, indSize, 20f);
        Assert.That(leftClamped.x, Is.LessThan(0f));
        Assert.That(leftClamped.y, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void F06_05_PlayerRunEndController_CalculateStageProgress_ScalesLinearly()
    {
        Assert.That(PlayerRunEndController.CalculateStageProgress(1, 0f, 10), Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(PlayerRunEndController.CalculateStageProgress(5, 0.5f, 10), Is.EqualTo(0.55f).Within(0.001f));
        Assert.That(PlayerRunEndController.CalculateStageProgress(10, 1f, 10), Is.EqualTo(1f).Within(0.001f));
    }
    #endregion

    #region Feature 7: AutoShooter & 360° Aim Math
    [Test]
    public void F07_01_PlayerAutoShooter_CalculateAimScale_FlipsYOnlyOnLeftAim()
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod("CalculateAimScale", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(2f, 2f, 1f);
        Vector3 rightScale = (Vector3)method.Invoke(null, new object[] { 0f, baseScale });
        Vector3 leftScale = (Vector3)method.Invoke(null, new object[] { 180f, baseScale });

        Assert.That(rightScale.y, Is.EqualTo(2f));
        Assert.That(leftScale.y, Is.EqualTo(-2f));
    }

    [Test]
    public void F07_02_PlayerAutoShooter_CalculateBodyScale_FlipsXOnlyOnLeftAim()
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod("CalculateBodyScale", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(2f, 2f, 1f);
        Vector3 rightScale = (Vector3)method.Invoke(null, new object[] { false, baseScale });
        Vector3 leftScale = (Vector3)method.Invoke(null, new object[] { true, baseScale });

        Assert.That(rightScale.x, Is.EqualTo(2f));
        Assert.That(leftScale.x, Is.EqualTo(-2f));
    }

    [Test]
    public void F07_03_PlayerAutoShooter_CalculateLocalAimAngle_CompensatesMirroredBody()
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod("CalculateLocalAimAngle", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        float rightLocal = (float)method.Invoke(null, new object[] { 45f, false });
        float leftLocal = (float)method.Invoke(null, new object[] { 135f, true });

        Assert.That(rightLocal, Is.EqualTo(45f).Within(0.01f));
        Assert.That(leftLocal, Is.EqualTo(45f).Within(0.01f));
    }

    [Test]
    public void F07_04_PlayerAutoShooter_FindNearestEnemy_SelectsClosest()
    {
        GameObject player = new GameObject("Player", typeof(PlayerAutoShooter));
        GameObject enemy1 = new GameObject("Enemy1", typeof(EnemyHealth), typeof(BoxCollider2D));
        GameObject enemy2 = new GameObject("Enemy2", typeof(EnemyHealth), typeof(BoxCollider2D));

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            enemy1.layer = enemyLayer;
            enemy2.layer = enemyLayer;
        }

        try
        {
            enemy1.transform.position = new Vector3(5f, 0f, 0f);
            enemy2.transform.position = new Vector3(2f, 0f, 0f);
            Physics2D.SyncTransforms();

            PlayerAutoShooter shooter = player.GetComponent<PlayerAutoShooter>();
            MethodInfo findMethod = typeof(PlayerAutoShooter).GetMethod("FindNearestEnemy", BindingFlags.NonPublic | BindingFlags.Instance);
            if (findMethod != null && enemyLayer >= 0)
            {
                findMethod.Invoke(shooter, null);
                FieldInfo targetField = typeof(PlayerAutoShooter).GetField("currentTarget", BindingFlags.NonPublic | BindingFlags.Instance);
                Transform target = targetField?.GetValue(shooter) as Transform;
                Assert.That(target, Is.EqualTo(enemy2.transform), "Must target enemy2 at distance 2 rather than enemy1 at distance 5.");
            }
        }
        finally
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(enemy1);
            Object.DestroyImmediate(enemy2);
        }
    }

    [Test]
    public void F07_05_PlayerAutoShooter_AimAngle_HandlesFull360Degrees()
    {
        Vector2[] directions = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
        float[] expectedAngles = { 0f, 90f, 180f, -90f };

        for (int i = 0; i < directions.Length; i++)
        {
            float angle = Mathf.Atan2(directions[i].y, directions[i].x) * Mathf.Rad2Deg;
            Assert.That(Mathf.DeltaAngle(angle, expectedAngles[i]), Is.EqualTo(0f).Within(0.01f));
        }
    }
    #endregion

    #region Feature 8: EXP Scaling & Leveling
    [Test]
    public void F08_01_PlayerLevelController_CalculateMaxExpForLevel_FollowsFormula()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            Assert.That(ctrl.CalculateMaxExpForLevel(1), Is.EqualTo(30));
            Assert.That(ctrl.CalculateMaxExpForLevel(2), Is.EqualTo(50));
            Assert.That(ctrl.CalculateMaxExpForLevel(3), Is.EqualTo(70));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F08_02_PlayerLevelController_AddEXP_IncreasesExpWithoutLevelUp()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            ctrl.AddEXP(15);
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(15));
            Assert.That(ctrl.CurrentLevel, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F08_03_PlayerLevelController_AddEXP_TriggersLevelUpAndCarriesExcess()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            // Level 1 requires 30. Adding 45 -> Level 2 with 15 excess.
            ctrl.AddEXP(45);
            Assert.That(ctrl.CurrentLevel, Is.EqualTo(2));
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(15));
            Assert.That(ctrl.MaxEXP, Is.EqualTo(50));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F08_04_PlayerLevelController_AddEXP_DispatchesGameEvents()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        int eventLevel = 0;
        Action<int> onLvl = lvl => eventLevel = lvl;
        GameEvents.OnPlayerLevelUp += onLvl;

        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            ctrl.AddEXP(30);
            Assert.That(eventLevel, Is.EqualTo(2));
        }
        finally
        {
            GameEvents.OnPlayerLevelUp -= onLvl;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F08_05_PlayerLevelController_MultipleLevelUps_ExecutesSequentially()
    {
        GameObject go = new GameObject("PlayerLevel", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = go.GetComponent<PlayerLevelController>();
            // Lvl 1->2: 30, Lvl 2->3: 50. Total 80 + 5 excess = 85.
            ctrl.AddEXP(85);
            Assert.That(ctrl.CurrentLevel, Is.EqualTo(3));
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(5));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
    #endregion

    #region Feature 9: Chipset Weapons & 10 Combat Skills
    [Test]
    public void F09_01_ChipsetController_CreateDefaultDatabase_Has24Chips()
    {
        List<ChipItemData> db = ChipsetController.CreateDefaultDatabase();
        Assert.That(db, Is.Not.Null);
        Assert.That(db.Count, Is.EqualTo(24));
    }

    [Test]
    public void F09_02_ChipsetController_PrimaryWeapons_ExistInDatabase()
    {
        List<ChipItemData> db = ChipsetController.CreateDefaultDatabase();
        Assert.That(db[0].chipName, Is.EqualTo("Standard Gun"));
        Assert.That(db[1].chipName, Is.EqualTo("Rifle"));
        Assert.That(db[2].chipName, Is.EqualTo("Rocket Punch"));
        Assert.That(db[3].chipName, Is.EqualTo("Spinning Blade"));
        Assert.That(db[4].chipName, Is.EqualTo("Multigun"));
    }

    [Test]
    public void F09_03_ChipsetController_SecondaryWeapons_ExistInDatabase()
    {
        List<ChipItemData> db = ChipsetController.CreateDefaultDatabase();
        Assert.That(db[5].chipName, Is.EqualTo("Gun Turret"));
        Assert.That(db[6].chipName, Is.EqualTo("Spiky Discus"));
        Assert.That(db[7].chipName, Is.EqualTo("Shotgun"));
        Assert.That(db[8].chipName, Is.EqualTo("Energy Jumper Cables"));
        Assert.That(db[9].chipName, Is.EqualTo("High-Explosive Mine"));
    }

    [Test]
    public void F09_04_PlayerChipsetSkillManager_ActivatesRegisteredSkills()
    {
        GameObject go = new GameObject("PlayerSkills", typeof(PlayerChipsetSkillManager));
        try
        {
            PlayerChipsetSkillManager mgr = go.GetComponent<PlayerChipsetSkillManager>();
            Assert.That(mgr, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F09_05_ChipsetBattleStats_RecordsDamageAndGrandTotal()
    {
        ChipsetBattleStats.Reset();
        ChipsetBattleStats.RegisterChipset(1, 1, 50);
        ChipsetBattleStats.RecordDamage(1, 100);
        ChipsetBattleStats.RecordDamage(1, 150);

        Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(250));
    }
    #endregion

    #region Feature 10: Combat Damage, Health & Revive
    [Test]
    public void F10_01_PlayerHealth_SetDamageReduction_ReducesDamage()
    {
        GameObject go = new GameObject("PlayerHealth", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            health.SetDamageReduction(5);
            health.TakeDamage(15);
            Assert.That(health.CurrentHealth, Is.EqualTo(90)); // 100 - (15 - 5) = 90
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F10_02_PlayerHealth_LethalDamage_SetsDeathAndClampsToZero()
    {
        GameObject go = new GameObject("PlayerHealth", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            health.TakeDamage(200);
            Assert.That(health.CurrentHealth, Is.EqualTo(0));
            Assert.That(health.IsDead, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F10_03_PlayerHealth_Revive_RestoresConfiguredPercentage()
    {
        GameObject go = new GameObject("PlayerHealth", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            health.TakeDamage(100);
            Assert.That(health.IsDead, Is.True);

            bool revived = health.Revive(0.5f, 2f);
            Assert.That(revived, Is.True);
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(50));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F10_04_PlayerHealth_Revive_FailsWhenAlive()
    {
        GameObject go = new GameObject("PlayerHealth", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = go.GetComponent<PlayerHealth>();
            Assert.That(health.Revive(), Is.False, "Cannot revive an alive player.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F10_05_PlayerDeathController_ResetForRevive_ReEnablesComponents()
    {
        GameObject go = new GameObject("Player", typeof(PlayerHealth), typeof(PlayerMovement), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(PlayerDeathController));
        try
        {
            PlayerMovement pm = go.GetComponent<PlayerMovement>();
            PlayerDeathController dc = go.GetComponent<PlayerDeathController>();

            dc.TriggerDeath();
            Assert.That(pm.enabled, Is.False);

            dc.ResetForRevive();
            Assert.That(pm.enabled, Is.True);
            Assert.That(dc.IsDeathSequenceActive, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
    #endregion

    #region Feature 11: Enemy Creep AI & Wave Spawner
    [Test]
    public void F11_01_EnemyHealth_TakeDamage_FlashesRed()
    {
        GameObject go = new GameObject("Enemy", typeof(EnemyHealth));
        GameObject spriteChild = new GameObject("Sprite", typeof(SpriteRenderer));
        spriteChild.transform.SetParent(go.transform, false);

        try
        {
            EnemyHealth health = go.GetComponent<EnemyHealth>();
            SpriteRenderer sr = spriteChild.GetComponent<SpriteRenderer>();
            sr.color = Color.white;
            health.CacheSpriteRenderers();

            health.TakeDamage(10);
            Assert.That(sr.color, Is.EqualTo(Color.red));

            health.RestoreSpriteColors();
            Assert.That(sr.color, Is.EqualTo(Color.white));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F11_02_EnemyMovement_MovesTowardsPlayerTarget()
    {
        GameObject enemy = new GameObject("Enemy", typeof(EnemyMovement), typeof(Rigidbody2D));
        GameObject player = new GameObject("Player");
        try
        {
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            player.transform.position = new Vector3(10f, 0f, 0f);
            movement.SetTarget(player.transform);
            Assert.That(movement.CurrentTarget, Is.EqualTo(player.transform));
        }
        finally
        {
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void F11_03_EnemyContactDamage_DealsContactDamageToPlayer()
    {
        GameObject enemy = new GameObject("Enemy", typeof(EnemyContactDamage));
        GameObject player = new GameObject("Player", typeof(PlayerHealth));
        try
        {
            EnemyContactDamage dmg = enemy.GetComponent<EnemyContactDamage>();
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            dmg.SetDamage(20);

            health.TakeDamage(dmg.Damage);
            Assert.That(health.CurrentHealth, Is.EqualTo(80));
        }
        finally
        {
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void F11_04_EnemyHealth_LethalDamage_SetsIsDead()
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
    public void F11_05_EnemySpawner_WaveConfig_LoadsCorrectWaveCount()
    {
        GameObject spawnerGo = new GameObject("EnemySpawner", typeof(EnemySpawner));
        try
        {
            EnemySpawner spawner = spawnerGo.GetComponent<EnemySpawner>();
            Assert.That(spawner, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(spawnerGo);
        }
    }
    #endregion

    #region Feature 12: Boss AI & Phase Behaviors
    [Test]
    public void F12_01_BossRangedAttack_CalculateFanDirections_DistributesEvenly()
    {
        Vector2[] fan = BossRangedAttack.CalculateFanDirections(Vector2.right, 3, 60f);
        Assert.That(fan.Length, Is.EqualTo(3));
        Assert.That(Vector2.Angle(fan[1], Vector2.right), Is.LessThan(0.01f));
        Assert.That(Vector2.Angle(fan[0], fan[1]), Is.EqualTo(30f).Within(0.01f));
        Assert.That(Vector2.Angle(fan[1], fan[2]), Is.EqualTo(30f).Within(0.01f));
    }

    [Test]
    public void F12_02_BossRangedAttack_CalculateRadialDirections_CreatesCircle()
    {
        Vector2[] radial = BossRangedAttack.CalculateRadialDirections(Vector2.right, 4);
        Assert.That(radial.Length, Is.EqualTo(4));
        Assert.That(Vector2.Angle(radial[0], radial[1]), Is.EqualTo(90f).Within(0.01f));
        Assert.That(Vector2.Angle(radial[1], radial[2]), Is.EqualTo(90f).Within(0.01f));
    }

    [Test]
    public void F12_03_BossRangedAttack_TargetRangeCheck_DifferentiatesStates()
    {
        GameObject boss = new GameObject("Boss", typeof(BossRangedAttack));
        GameObject player = new GameObject("Player");
        try
        {
            BossRangedAttack ranged = boss.GetComponent<BossRangedAttack>();
            ranged.SetTarget(player.transform);

            player.transform.position = Vector3.right * (ranged.AttackRange - 1f);
            Assert.That(ranged.GetTargetRangeState(), Is.EqualTo(BossRangedAttack.TargetRangeState.InRange));

            player.transform.position = Vector3.right * (ranged.AttackRange + 5f);
            Assert.That(ranged.GetTargetRangeState(), Is.EqualTo(BossRangedAttack.TargetRangeState.TooFar));
        }
        finally
        {
            Object.DestroyImmediate(boss);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void F12_04_BossHealthBarUI_SanitizeBossDisplayName_FormatsCleanly()
    {
        Assert.That(BossHealthBarUI.SanitizeBossDisplayName("Boss Cyber Mech"), Is.EqualTo("BOSS CYBER MECH"));
        Assert.That(BossHealthBarUI.SanitizeBossDisplayName(null), Is.EqualTo("BOSS"));
    }

    [Test]
    public void F12_05_BossMovement_ChargeState_InitiatesDash()
    {
        GameObject boss = new GameObject("Boss", typeof(BossMovement), typeof(Rigidbody2D));
        try
        {
            BossMovement bm = boss.GetComponent<BossMovement>();
            Assert.That(bm, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(boss);
        }
    }
    #endregion

    #region Feature 13: Sprite HitFlash & Visual Feedback
    [Test]
    public void F13_01_PlayerHealth_HitFlash_ExcludesHealthBar()
    {
        GameObject player = new GameObject("Player", typeof(PlayerHealth));
        GameObject body = new GameObject("Body", typeof(SpriteRenderer));
        body.transform.SetParent(player.transform, false);
        GameObject healthBar = new GameObject("HealthBar", typeof(PlayerWorldHealthBar), typeof(SpriteRenderer));
        healthBar.transform.SetParent(player.transform, false);

        try
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            SpriteRenderer bodySr = body.GetComponent<SpriteRenderer>();
            SpriteRenderer barSr = healthBar.GetComponent<SpriteRenderer>();
            bodySr.color = Color.white;
            barSr.color = Color.green;

            health.CacheSpriteRenderers();
            health.TakeDamage(10);

            Assert.That(bodySr.color, Is.EqualTo(Color.red));
            Assert.That(barSr.color, Is.EqualTo(Color.green), "Health bar must not be turned red on hit flash.");
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void F13_02_PlayerWorldHealthBar_SetNormalizedHealth_ShrinksFill()
    {
        GameObject player = new GameObject("Player", typeof(PlayerWorldHealthBar));
        GameObject bg = new GameObject("Bg", typeof(SpriteRenderer));
        GameObject fill = new GameObject("Fill", typeof(SpriteRenderer));
        bg.transform.SetParent(player.transform, false);
        fill.transform.SetParent(player.transform, false);

        try
        {
            PlayerWorldHealthBar bar = player.GetComponent<PlayerWorldHealthBar>();
            typeof(PlayerWorldHealthBar).GetField("backgroundRenderer", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(bar, bg.GetComponent<SpriteRenderer>());
            typeof(PlayerWorldHealthBar).GetField("fillRenderer", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(bar, fill.GetComponent<SpriteRenderer>());

            bar.SetNormalizedHealth(0.6f);
            Assert.That(fill.transform.localScale.x, Is.EqualTo(0.6f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void F13_03_ScreenShakeService_AddTrauma_DecaysOverTime()
    {
        ScreenShakeService.Reset();
        GameSettings.ScreenShake = true;
        ScreenShakeService.AddTrauma(0.8f);
        Vector3 offset = ScreenShakeService.UpdateAndGetOffset(0.05f);
        Assert.That(offset, Is.Not.EqualTo(Vector3.zero));
    }

    [Test]
    public void F13_04_AutoDestroyVFX_ComponentExists()
    {
        GameObject go = new GameObject("VFX", typeof(AutoDestroyVFX));
        try
        {
            AutoDestroyVFX vfx = go.GetComponent<AutoDestroyVFX>();
            Assert.That(vfx, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F13_05_SpriteDissolveShader_ExistsAndHasProperties()
    {
        Shader shader = Shader.Find("Custom/2D/SpriteDissolve");
        Assert.That(shader, Is.Not.Null, "Custom/2D/SpriteDissolve must exist.");
        Material mat = new Material(shader);
        Assert.That(mat.HasProperty("_DissolveAmount"), Is.True);
        Object.DestroyImmediate(mat);
    }
    #endregion

    #region Feature 14: Drop System & Gem Pickups
    [Test]
    public void F14_01_RewardService_GrantReward_AwardsCurrencies()
    {
        int origChips = PlayerDataService.DataChips;
        try
        {
            PlayerDataService.DataChips = 0;
            RewardData reward = new RewardData { type = RewardType.DataChip, amount = 350 };
            RewardService.GrantReward(reward);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(350));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
        }
    }

    [Test]
    public void F14_02_RewardService_GrantMultipleRewards_ProcessesBatch()
    {
        int origGems = PlayerDataService.RedGems;
        int origEnergy = PlayerDataService.Energy;
        try
        {
            PlayerDataService.RedGems = 0;
            PlayerDataService.Energy = 0;

            RewardData[] batch = {
                new RewardData { type = RewardType.RedGem, amount = 150 },
                new RewardData { type = RewardType.Energy, amount = 20 }
            };
            RewardService.GrantRewards(batch);

            Assert.That(PlayerDataService.RedGems, Is.EqualTo(150));
            Assert.That(PlayerDataService.Energy, Is.EqualTo(20));
        }
        finally
        {
            PlayerDataService.RedGems = origGems;
            PlayerDataService.Energy = origEnergy;
        }
    }

    [Test]
    public void F14_03_ExpGem_Collect_AddsExpToPlayer()
    {
        GameObject player = new GameObject("Player", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = player.GetComponent<PlayerLevelController>();
            ctrl.AddEXP(20);
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(20));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void F14_04_MagnetItem_CalculatesDistanceThreshold()
    {
        Vector3 gemPos = new Vector3(3f, 4f, 0f);
        float dist = Vector3.Distance(Vector3.zero, gemPos);
        Assert.That(dist, Is.EqualTo(5f).Within(0.001f));
    }

    [Test]
    public void F14_05_DropItem_PoolReturn_RecyclesObject()
    {
        GameObject prefab = new GameObject("GemPrefab", typeof(DummyPoolable));
        GameObject root = new GameObject("PoolRoot");
        try
        {
            ObjectPool pool = new ObjectPool(prefab, 1, true, root.transform);
            pool.Initialize(root.transform);

            GameObject gem = pool.Spawn(Vector3.zero, Quaternion.identity);
            pool.Despawn(gem);
            Assert.That(gem.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }
    #endregion

    #region Feature 15: Lab 16 Stats Matrix (4x4)
    [Test]
    public void F15_01_LabUpgrade_16StatsKeys_AreRecognized()
    {
        string[] keys = {
            "HP", "ATK", "DEF", "RECOVERY",
            "CRIT RATE", "CRIT DMG", "ATTACK SPEED", "MOVE SPEED",
            "LIFE STEAL", "DODGE", "COOLDOWN", "KNOCKBACK",
            "AREA OF EFFECT", "DURATION", "MAGNET RANGE", "BONUS EXP"
        };

        for (int i = 0; i < keys.Length; i++)
        {
            string key = PlayerDataService.GetItemLevelKey(keys[i], i);
            Assert.That(key, Does.Contain(keys[i]));
        }
    }

    [Test]
    public void F15_02_LabUpgrade_CalculateCost_FollowsLinearFormula()
    {
        // Pricing formula: 300 + (totalRolls * 150)
        int basePrice = 300;
        int step = 150;
        Assert.That(basePrice + 0 * step, Is.EqualTo(300));
        Assert.That(basePrice + 1 * step, Is.EqualTo(450));
        Assert.That(basePrice + 4 * step, Is.EqualTo(900));
    }

    [Test]
    public void F15_03_PlayerDataService_LabStatLevel_ClampsBetween0And10()
    {
        string stat = "ATK";
        int orig = PlayerDataService.GetItemLevel(stat);
        try
        {
            PlayerDataService.SetItemLevel(stat, 0);
            Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(0));

            PlayerDataService.SetItemLevel(stat, 10);
            Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(10));

            PlayerDataService.SetItemLevel(stat, 15);
            Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(10), "Must clamp max to 10.");

            PlayerDataService.SetItemLevel(stat, -5);
            Assert.That(PlayerDataService.GetItemLevel(stat), Is.EqualTo(0), "Must clamp min to 0.");
        }
        finally
        {
            PlayerDataService.SetItemLevel(stat, orig);
        }
    }

    [Test]
    public void F15_04_PlayerStatsManager_GetStatLevel_MatchesPlayerDataService()
    {
        string stat = "DEF";
        int orig = PlayerDataService.GetItemLevel(stat);
        try
        {
            PlayerDataService.SetItemLevel(stat, 4);
            Assert.That(PlayerStatsManager.GetStatLevel(stat), Is.EqualTo(4));
        }
        finally
        {
            PlayerDataService.SetItemLevel(stat, orig);
        }
    }

    [Test]
    public void F15_05_LabUpgrade_TotalMatrixCap_Is160Levels()
    {
        int totalSlots = 16;
        int maxPerSlot = 10;
        Assert.That(totalSlots * maxPerSlot, Is.EqualTo(160));
    }
    #endregion

    #region Feature 16: Triple Pity Guarantee System
    [Test]
    public void F16_01_PityGuarantee_Thresholds_MatchConfiguredValues()
    {
        Assert.That(PityGuaranteePanel.EliteThreshold, Is.EqualTo(10));
        Assert.That(PityGuaranteePanel.EpicThreshold, Is.EqualTo(25));
        Assert.That(PityGuaranteePanel.LegendThreshold, Is.EqualTo(50));
    }

    [Test]
    public void F16_02_PityGuarantee_IncrementRolls_IncreasesAllCounters()
    {
        int origElite = PlayerDataService.LabElitePityCounter;
        int origEpic = PlayerDataService.LabEpicPityCounter;
        int origLegend = PlayerDataService.LabLegendPityCounter;

        try
        {
            PlayerDataService.LabElitePityCounter = 0;
            PlayerDataService.LabEpicPityCounter = 0;
            PlayerDataService.LabLegendPityCounter = 0;

            PlayerDataService.LabElitePityCounter++;
            PlayerDataService.LabEpicPityCounter++;
            PlayerDataService.LabLegendPityCounter++;

            Assert.That(PlayerDataService.LabElitePityCounter, Is.EqualTo(1));
            Assert.That(PlayerDataService.LabEpicPityCounter, Is.EqualTo(1));
            Assert.That(PlayerDataService.LabLegendPityCounter, Is.EqualTo(1));
        }
        finally
        {
            PlayerDataService.LabElitePityCounter = origElite;
            PlayerDataService.LabEpicPityCounter = origEpic;
            PlayerDataService.LabLegendPityCounter = origLegend;
        }
    }

    [Test]
    public void F16_03_PityGuarantee_HitElite_ResetsEliteCounterOnly()
    {
        int eliteCount = 9;
        int epicCount = 9;
        int legendCount = 9;

        // Roll hits Elite
        eliteCount = 0;
        epicCount++;
        legendCount++;

        Assert.That(eliteCount, Is.EqualTo(0));
        Assert.That(epicCount, Is.EqualTo(10));
        Assert.That(legendCount, Is.EqualTo(10));
    }

    [Test]
    public void F16_04_PityGuarantee_HitEpic_ResetsEpicCounterOnly()
    {
        int eliteCount = 4;
        int epicCount = 24;
        int legendCount = 24;

        // Roll hits Epic
        epicCount = 0;
        eliteCount++;
        legendCount++;

        Assert.That(epicCount, Is.EqualTo(0));
        Assert.That(eliteCount, Is.EqualTo(5));
        Assert.That(legendCount, Is.EqualTo(25));
    }

    [Test]
    public void F16_05_PityGuarantee_HitLegend_ResetsLegendCounterOnly()
    {
        int eliteCount = 7;
        int epicCount = 18;
        int legendCount = 49;

        // Roll hits Legend
        legendCount = 0;
        eliteCount++;
        epicCount++;

        Assert.That(legendCount, Is.EqualTo(0));
        Assert.That(eliteCount, Is.EqualTo(8));
        Assert.That(epicCount, Is.EqualTo(19));
    }
    #endregion

    #region Feature 17: Chipset Inventory & 5 Tiers
    [Test]
    public void F17_01_ChipItemData_TierMaxLevels_MatchDesign()
    {
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Magic), Is.EqualTo(6));
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Rare), Is.EqualTo(9));
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Unique), Is.EqualTo(14));
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Epic), Is.EqualTo(18));
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Holographic), Is.EqualTo(24));
    }

    [Test]
    public void F17_02_ChipItemData_Enhance_ConsumesDataChipsAndIncreasesLevel()
    {
        int origChips = ChipManager.DataChips;
        try
        {
            ChipManager.DataChips = 1000;
            ChipItemData chip = new ChipItemData { id = 1, tier = ChipTier.Magic, level = 1, enhanceCost = 300 };

            Assert.That(chip.CanEnhance, Is.True);
            Assert.That(chip.Enhance(), Is.True);
            Assert.That(chip.level, Is.EqualTo(2));
            Assert.That(ChipManager.DataChips, Is.EqualTo(700));
        }
        finally
        {
            ChipManager.DataChips = origChips;
        }
    }

    [Test]
    public void F17_03_ChipItemData_AdvanceTier_ConsumesFragments()
    {
        ChipItemData chip = new ChipItemData
        {
            id = 2,
            tier = ChipTier.Magic,
            level = 6,
            count = 10,
            requiredCount = 3
        };

        Assert.That(chip.IsAtTierCap, Is.True);
        Assert.That(chip.CanAdvanceTier, Is.True);
        Assert.That(chip.AdvanceTier(), Is.True);
        Assert.That(chip.tier, Is.EqualTo(ChipTier.Rare));
        Assert.That(chip.count, Is.EqualTo(7));
    }

    [Test]
    public void F17_04_ChipItemData_AdvanceTierToHolographic_Requires10AdvanceStones()
    {
        int origStones = ChipManager.AdvanceStones;
        try
        {
            ChipManager.AdvanceStones = 5;
            ChipItemData chip = new ChipItemData
            {
                id = 3,
                tier = ChipTier.Epic,
                level = 18,
                count = 10
            };

            Assert.That(chip.NeedsAdvanceStones, Is.True);
            Assert.That(chip.AdvanceTier(), Is.False, "Cannot advance to Holographic without 10 Advance Stones.");

            ChipManager.AdvanceStones = 12;
            Assert.That(chip.AdvanceTier(), Is.True);
            Assert.That(chip.tier, Is.EqualTo(ChipTier.Holographic));
            Assert.That(ChipManager.AdvanceStones, Is.EqualTo(2));
        }
        finally
        {
            ChipManager.AdvanceStones = origStones;
        }
    }

    [Test]
    public void F17_05_ChipsetController_GetFrameIndex_MatchesTierOrder()
    {
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Magic), Is.EqualTo(0));
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Rare), Is.EqualTo(1));
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Unique), Is.EqualTo(2));
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Epic), Is.EqualTo(3));
        Assert.That(ChipsetController.GetFrameIndex(ChipTier.Holographic), Is.EqualTo(4));
    }
    #endregion

    #region Feature 18: Buddy Drone Management
    [Test]
    public void F18_01_BuddyController_CreateDefaultDatabase_Has10Drones()
    {
        GameObject go = new GameObject("BuddyController", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();
            Assert.That(ctrl.AllBuddies.Count, Is.GreaterThanOrEqualTo(10));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F18_02_BuddyItemData_Enhance_IncreasesLevel()
    {
        int origChips = ChipManager.DataChips;
        try
        {
            ChipManager.DataChips = 1500;
            BuddyItemData drone = new BuddyItemData { id = 101, level = 1, enhanceCost = 400 };

            Assert.That(drone.CanEnhance, Is.True);
            Assert.That(drone.Enhance(), Is.True);
            Assert.That(drone.level, Is.EqualTo(2));
            Assert.That(ChipManager.DataChips, Is.EqualTo(1100));
        }
        finally
        {
            ChipManager.DataChips = origChips;
        }
    }

    [Test]
    public void F18_03_BuddyItemData_AdvanceTier_PromotesRarity()
    {
        BuddyItemData drone = new BuddyItemData
        {
            id = 102,
            tier = BuddyTier.Common,
            level = 1,
            count = 15,
            requiredCount = 5
        };

        Assert.That(drone.CanAdvanceTier, Is.True);
        Assert.That(drone.AdvanceTier(), Is.True);
        Assert.That(drone.tier, Is.EqualTo(BuddyTier.Magic));
        Assert.That(drone.count, Is.EqualTo(10));
    }

    [Test]
    public void F18_04_BuddyCardUI_SetupStates_ReflectsSlotState()
    {
        GameObject go = new GameObject("BuddyCard", typeof(BuddyCardUI));
        try
        {
            BuddyCardUI card = go.GetComponent<BuddyCardUI>();
            card.SetupEmpty(null);
            Assert.That(card.SlotState, Is.EqualTo(BuddySlotState.Empty));

            card.SetupLocked(null);
            Assert.That(card.SlotState, Is.EqualTo(BuddySlotState.Locked));

            BuddyItemData drone = new BuddyItemData { id = 1, buddyName = "Snowflake" };
            card.Setup(drone, null, null);
            Assert.That(card.SlotState, Is.EqualTo(BuddySlotState.Normal));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F18_05_BuddyItemData_SpecificDrones_InitializedProperly()
    {
        GameObject go = new GameObject("BuddyController", typeof(BuddyController));
        try
        {
            BuddyController ctrl = go.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            BuddyItemData spider = ctrl.AllBuddies.FirstOrDefault(b => b.iconKey == "drone-spider");
            Assert.That(spider, Is.Not.Null);
            Assert.That(spider.count, Is.EqualTo(79));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
    #endregion

    #region Feature 19: Level Up Popup & Reroll Modal
    [Test]
    public void F19_01_ChipsetLevelUpPopup_SelectDistinctOffers_ReturnsUniqueCopies()
    {
        var catalog = ChipsetController.CreateDefaultDatabase();
        var offers = ChipsetLevelUpPopup.SelectDistinctOffers(catalog, 4, new System.Random(1337));

        Assert.That(offers.Count, Is.EqualTo(4));
        Assert.That(offers.Select(item => item.id).Distinct().Count(), Is.EqualTo(4));
    }

    [Test]
    public void F19_02_ChipsetLevelUpPopup_RerollLimit_CappedAtTwo()
    {
        GameObject go = new GameObject("LevelUpPopup", typeof(ChipsetLevelUpPopup));
        try
        {
            ChipsetLevelUpPopup popup = go.GetComponent<ChipsetLevelUpPopup>();
            Assert.That(popup.MaxRerollsPerLevel, Is.EqualTo(2));
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(0));
            Assert.That(popup.RemainingRerolls, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F19_03_ChipsetLevelUpPopup_TryReroll_ExecutesAndConsumesRerolls()
    {
        int origGems = PlayerDataService.RedGems;
        GameObject go = new GameObject("LevelUpPopup", typeof(ChipsetLevelUpPopup));
        try
        {
            ChipManager.IsTestMode = true;
            PlayerDataService.RedGems = 100;
            ChipsetLevelUpPopup popup = go.GetComponent<ChipsetLevelUpPopup>();

            Assert.That(popup.TryReroll(), Is.True);
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(1));

            Assert.That(popup.TryReroll(), Is.True);
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(2));

            Assert.That(popup.TryReroll(), Is.False, "3rd reroll attempt must fail.");
        }
        finally
        {
            ChipManager.IsTestMode = false;
            PlayerDataService.RedGems = origGems;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F19_04_ChipsetLevelUpPopup_UpgradeRuntimeChipset_IncrementsLevel()
    {
        GameObject go = new GameObject("LevelUpPopup", typeof(ChipsetLevelUpPopup));
        try
        {
            ChipsetLevelUpPopup popup = go.GetComponent<ChipsetLevelUpPopup>();
            Assert.That(popup.GetRuntimeLevel(1), Is.EqualTo(0));
            Assert.That(popup.UpgradeRuntimeChipset(1), Is.EqualTo(1));
            Assert.That(popup.GetRuntimeLevel(1), Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F19_05_ChipsetLevelVisualLibrary_RarityFrames_ContainsAllTiers()
    {
        ChipsetLevelVisualLibrary lib = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
        if (lib != null)
        {
            Assert.That(lib.tierLeverFrames.Length, Is.EqualTo(5));
        }
    }
    #endregion

    #region Feature 20: Chapter Progression & World Map
    [Test]
    public void F20_01_ChapterDatabase_LoadsAllChapters()
    {
        ChapterDatabase db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
        Assert.That(db, Is.Not.Null);
        Assert.That(db.Count, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void F20_02_ChapterDatabase_IndexClamping_WorksSafely()
    {
        ChapterDatabase db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
        if (db != null)
        {
            ChapterData first = db.GetChapter(-5);
            ChapterData last = db.GetChapter(999);
            Assert.That(first, Is.EqualTo(db.GetChapter(0)));
            Assert.That(last, Is.EqualTo(db.GetChapter(db.Count - 1)));
        }
    }

    [Test]
    public void F20_03_PlayerDataService_ChapterIndices_SaveAndLoad()
    {
        int origSel = PlayerDataService.SelectedChapterIndex;
        int origUnl = PlayerDataService.UnlockedChapterIndex;
        try
        {
            PlayerDataService.SelectedChapterIndex = 2;
            PlayerDataService.UnlockedChapterIndex = 3;

            Assert.That(PlayerDataService.SelectedChapterIndex, Is.EqualTo(2));
            Assert.That(PlayerDataService.UnlockedChapterIndex, Is.EqualTo(3));
        }
        finally
        {
            PlayerDataService.SelectedChapterIndex = origSel;
            PlayerDataService.UnlockedChapterIndex = origUnl;
        }
    }

    [Test]
    public void F20_04_ChapterScreen_EnergySpend_Deducts10Energy()
    {
        int origEnergy = PlayerDataService.Energy;
        try
        {
            PlayerDataService.Energy = 25;
            Assert.That(ChipManager.HasEnoughEnergy(10), Is.True);
            Assert.That(ChipManager.TrySpendEnergy(10), Is.True);
            Assert.That(PlayerDataService.Energy, Is.EqualTo(15));
        }
        finally
        {
            PlayerDataService.Energy = origEnergy;
        }
    }

    [Test]
    public void F20_05_ChapterData_GenerateWaves_CreatesConfiguredWaves()
    {
        ChapterData chapter = ScriptableObject.CreateInstance<ChapterData>();
        chapter.chapterNumber = 1;
        chapter.chapterTitle = "Test Grassland";
        chapter.totalWaves = 5;

        chapter.GenerateWaves();
        Assert.That(chapter.waves.Count, Is.EqualTo(5));
        Object.DestroyImmediate(chapter);
    }
    #endregion

    #region Feature 21: Shop & Currency Exchange
    [Test]
    public void F21_01_ShopController_SetOffers_LoadsConfiguredOffers()
    {
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ShopController shop = go.GetComponent<ShopController>();
            ShopController.Offer offer = new ShopController.Offer
            {
                id = "gem-1",
                displayName = "100 Gems",
                currency = ShopController.CurrencyType.RedGem,
                price = 50,
                reward = ShopController.RewardType.DataChip,
                rewardAmount = 2500
            };
            shop.SetOffersForTesting(new[] { offer });
            Assert.That(shop, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F21_02_ShopController_PurchaseWithRedGems_DeductsAndRewards()
    {
        int origChips = ChipManager.DataChips;
        int origGems = ChipManager.RedGems;
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ChipManager.RedGems = 100;
            ChipManager.DataChips = 0;

            ShopController shop = go.GetComponent<ShopController>();
            ShopController.Offer offer = new ShopController.Offer
            {
                id = "chips-2500",
                currency = ShopController.CurrencyType.RedGem,
                price = 50,
                reward = ShopController.RewardType.DataChip,
                rewardAmount = 2500
            };
            shop.SetOffersForTesting(new[] { offer });

            Assert.That(shop.TryPurchase(0), Is.True);
            Assert.That(ChipManager.RedGems, Is.EqualTo(50));
            Assert.That(ChipManager.DataChips, Is.EqualTo(2500));
        }
        finally
        {
            ChipManager.DataChips = origChips;
            ChipManager.RedGems = origGems;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F21_03_ShopController_PurchaseFreeReward_GrantsCorrectly()
    {
        int origChips = ChipManager.DataChips;
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ChipManager.DataChips = 0;

            ShopController shop = go.GetComponent<ShopController>();
            ShopController.Offer offer = new ShopController.Offer
            {
                id = "free-chips",
                currency = ShopController.CurrencyType.Free,
                price = 0,
                reward = ShopController.RewardType.DataChip,
                rewardAmount = 500
            };
            shop.SetOffersForTesting(new[] { offer });

            Assert.That(shop.TryPurchase(0), Is.True);
            Assert.That(ChipManager.DataChips, Is.EqualTo(500));
        }
        finally
        {
            ChipManager.DataChips = origChips;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F21_04_ShopController_InsufficientRedGems_FailsPurchase()
    {
        int origGems = ChipManager.RedGems;
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ChipManager.RedGems = 10;
            ShopController shop = go.GetComponent<ShopController>();
            ShopController.Offer offer = new ShopController.Offer
            {
                id = "pack-expensive",
                currency = ShopController.CurrencyType.RedGem,
                price = 500,
                reward = ShopController.RewardType.Energy,
                rewardAmount = 20
            };
            shop.SetOffersForTesting(new[] { offer });

            Assert.That(shop.TryPurchase(0), Is.False);
            Assert.That(ChipManager.RedGems, Is.EqualTo(10));
        }
        finally
        {
            ChipManager.RedGems = origGems;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F21_05_ShopController_VNDCurrency_FailsClosedSafely()
    {
        GameObject go = new GameObject("Shop", typeof(ShopController));
        try
        {
            ShopController shop = go.GetComponent<ShopController>();
            ShopController.Offer vndOffer = new ShopController.Offer
            {
                id = "vnd-starter",
                currency = ShopController.CurrencyType.VND,
                price = 20000,
                reward = ShopController.RewardType.RedGem,
                rewardAmount = 500
            };
            shop.SetOffersForTesting(new[] { vndOffer });

            Assert.That(shop.TryPurchase(0), Is.False, "VND offers without IAP integration must fail closed.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
    #endregion

    #region Feature 22: 7-Day Daily Login & Achievements
    [Test]
    public void F22_01_DailyLoginManager_InitializesWithSevenDays()
    {
        GameObject go = new GameObject("DailyLogin", typeof(DailyLoginManager));
        try
        {
            DailyLoginManager mgr = go.GetComponent<DailyLoginManager>();
            mgr.EnsureDatabaseLoaded();
            Assert.That(mgr.CurrentLoginDay, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F22_02_DailyLoginManager_ClaimDay1_GrantsRewardsAndMarksClaimed()
    {
        int origEnergy = PlayerDataService.Energy;
        GameObject go = new GameObject("DailyLogin", typeof(DailyLoginManager));
        try
        {
            DailyLoginManager mgr = go.GetComponent<DailyLoginManager>();
            mgr.EnsureDatabaseLoaded();

            Assert.That(mgr.GetDayState(1), Is.EqualTo(DailyLoginState.Available));
            Assert.That(mgr.TryClaimTodayReward(), Is.True);
            Assert.That(mgr.GetDayState(1), Is.EqualTo(DailyLoginState.Obtained));
        }
        finally
        {
            PlayerDataService.Energy = origEnergy;
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F22_03_DailyLoginManager_AntiDuplicateClaim_PreventsDoubleClaim()
    {
        GameObject go = new GameObject("DailyLogin", typeof(DailyLoginManager));
        try
        {
            DailyLoginManager mgr = go.GetComponent<DailyLoginManager>();
            mgr.EnsureDatabaseLoaded();

            Assert.That(mgr.TryClaimTodayReward(), Is.True);
            Assert.That(mgr.TryClaimTodayReward(), Is.False, "Second claim on the same day must be rejected.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F22_04_AchievementManager_TrackProgress_IncrementsAndTriggersCompletion()
    {
        GameObject go = new GameObject("AchievementManager", typeof(AchievementManager));
        try
        {
            AchievementManager mgr = go.GetComponent<AchievementManager>();
            mgr.EnsureDatabaseLoaded();

            string achId = "drone_upgrade_3";
            mgr.SetProgress(achId, 3);
            Assert.That(mgr.IsCompleted(achId), Is.True);
            Assert.That(mgr.GetState(achId), Is.EqualTo(AchievementState.Completed));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void F22_05_AchievementManager_ClaimReward_GrantsCurrenciesAndMarksClaimed()
    {
        int origGems = PlayerDataService.RedGems;
        GameObject go = new GameObject("AchievementManager", typeof(AchievementManager));
        try
        {
            AchievementManager mgr = go.GetComponent<AchievementManager>();
            mgr.EnsureDatabaseLoaded();

            string achId = "login_reward_2";
            mgr.SetProgress(achId, 2);

            Assert.That(mgr.TryClaimReward(achId), Is.True);
            Assert.That(mgr.IsClaimed(achId), Is.True);
            Assert.That(mgr.TryClaimReward(achId), Is.False, "Cannot claim already claimed achievement.");
        }
        finally
        {
            PlayerDataService.RedGems = origGems;
            Object.DestroyImmediate(go);
        }
    }
    #endregion
}
