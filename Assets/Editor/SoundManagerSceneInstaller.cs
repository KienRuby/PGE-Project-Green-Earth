#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tự động thiết lập và cấu hình GameObject [SoundManager] trong Hierarchy
/// và nạp sẵn toàn bộ 6 file âm thanh trong Assets/Audio vào SoundDatabase & SoundManager.
/// </summary>
public static class SoundManagerSceneInstaller
{
    private const string SoundBackgroundPath = "Assets/Audio/Sound-background.wav";
    private const string GunShotPath = "Assets/Audio/Ban sung player.mp3";
    private const string EnemyDeathPath = "Assets/Audio/Enemy-death.mp3";
    private const string ItemPickupPath = "Assets/Audio/Nhặt item.wav";
    private const string PlayerDeathPath = "Assets/Audio/Player-death.wav";
    private const string UiClickPath = "Assets/Audio/UI.wav";

    private const string ResourcesDatabasePath = "Assets/Resources/SoundDatabase.asset";
    private const string DataDatabasePath = "Assets/Data/Audio/SoundDatabase.asset";

    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";

    [MenuItem("PGE/Audio/Setup SoundManager in Hierarchy")]
    public static void SetupFromMenu()
    {
        SetupAll();
    }

    public static void SetupAll()
    {

        // 1. Nạp các AudioClip từ thư mục Assets/Audio
        AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundBackgroundPath);
        AudioClip gunShotClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GunShotPath);
        AudioClip enemyDeathClip = AssetDatabase.LoadAssetAtPath<AudioClip>(EnemyDeathPath);
        AudioClip itemPickupClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ItemPickupPath);
        AudioClip playerDeathClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PlayerDeathPath);
        AudioClip uiClip = AssetDatabase.LoadAssetAtPath<AudioClip>(UiClickPath);

        // 2. Điền dữ liệu vào SoundDatabase.asset
        PopulateSoundDatabase(ResourcesDatabasePath, bgmClip, gunShotClip, enemyDeathClip, itemPickupClip, playerDeathClip, uiClip);
        PopulateSoundDatabase(DataDatabasePath, bgmClip, gunShotClip, enemyDeathClip, itemPickupClip, playerDeathClip, uiClip);

        // 3. Cài đặt GameObject [SoundManager] vào Scene đang mở
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            InstallIntoScene(activeScene, bgmClip, gunShotClip, enemyDeathClip, itemPickupClip, playerDeathClip, uiClip);
        }

        // 4. Cài đặt vào cả 2 scene chính MainMenu và GamePlay nếu chưa có
        InstallIntoScenePath(MainMenuScenePath, bgmClip, gunShotClip, enemyDeathClip, itemPickupClip, playerDeathClip, uiClip);
        InstallIntoScenePath(GamePlayScenePath, bgmClip, gunShotClip, enemyDeathClip, itemPickupClip, playerDeathClip, uiClip);

        AssetDatabase.SaveAssets();
    }

    private static void PopulateSoundDatabase(string dbPath, AudioClip bgm, AudioClip gunShot, AudioClip enemyDeath, AudioClip itemPickup, AudioClip playerDeath, AudioClip ui)
    {
        SoundDatabase db = AssetDatabase.LoadAssetAtPath<SoundDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<SoundDatabase>();
            string dir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(db, dbPath);
        }

        SerializedObject so = new SerializedObject(db);
        SerializedProperty soundsProp = so.FindProperty("sounds");
        soundsProp.ClearArray();

        void AddSound(string id, AudioCategory category, AudioClip clip, float volume = 1f, float minPitch = 0.95f, float maxPitch = 1.05f, float cooldown = 0.04f, int maxSimultaneous = 8)
        {
            if (clip == null) return;
            int idx = soundsProp.arraySize;
            soundsProp.InsertArrayElementAtIndex(idx);
            SerializedProperty elem = soundsProp.GetArrayElementAtIndex(idx);

            elem.FindPropertyRelative("soundId").stringValue = id;
            elem.FindPropertyRelative("category").enumValueIndex = (int)category;
            elem.FindPropertyRelative("baseVolume").floatValue = volume;
            elem.FindPropertyRelative("pitchRange").vector2Value = new Vector2(minPitch, maxPitch);
            elem.FindPropertyRelative("spatialBlend").floatValue = 0f;
            elem.FindPropertyRelative("cooldown").floatValue = cooldown;
            elem.FindPropertyRelative("maxSimultaneous").intValue = maxSimultaneous;

            SerializedProperty clipsProp = elem.FindPropertyRelative("clips");
            clipsProp.ClearArray();
            clipsProp.InsertArrayElementAtIndex(0);
            clipsProp.GetArrayElementAtIndex(0).objectReferenceValue = clip;
        }

        AddSound(SoundIdConst.BGM_MAIN_MENU, AudioCategory.BGM, bgm, 0.6f, 1f, 1f, 0.5f);
        AddSound(SoundIdConst.BGM_COMBAT, AudioCategory.BGM, bgm, 0.6f, 1f, 1f, 0.5f);
        AddSound(SoundIdConst.SFX_GUN_SHOT_STANDARD, AudioCategory.SFX, gunShot, 0.32f, 0.95f, 1.05f, 0.05f);
        AddSound(SoundIdConst.SFX_ENEMY_DEATH, AudioCategory.SFX, enemyDeath, 0.85f, 0.95f, 1.05f, 0.05f);
        AddSound(SoundIdConst.SFX_PICKUP_ITEM, AudioCategory.SFX, itemPickup, 0.8f, 0.95f, 1.05f, 0.03f);
        AddSound(SoundIdConst.SFX_PICKUP_EXP, AudioCategory.SFX, itemPickup, 0.8f, 0.95f, 1.05f, 0.03f);
        AddSound(SoundIdConst.SFX_PLAYER_DEATH, AudioCategory.SFX, playerDeath, 0.85f, 1f, 1f, 0.5f);
        AddSound(SoundIdConst.SFX_PLAYER_HURT, AudioCategory.SFX, playerDeath, 0.8f, 0.95f, 1.05f, 0.1f);
        AddSound(SoundIdConst.UI_BUTTON_CLICK, AudioCategory.UI, ui, 0.8f, 1f, 1f, 0.03f);
        AddSound(SoundIdConst.UI_TAB_SWITCH, AudioCategory.UI, ui, 0.8f, 1f, 1f, 0.03f);

        AddSound(SoundIdConst.SFX_PUNCH_SPAWN, AudioCategory.SFX, AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Punch.wav"), 0.6f, 1f, 1f, 0f, 0);
        AddSound(SoundIdConst.SFX_PUNCH_HIT, AudioCategory.SFX, AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX BOOM.mp3"), 0.6f, 1f, 1f, 0f, 0);
        AddSound(SoundIdConst.SFX_EXPLOSION_SMALL, AudioCategory.SFX, AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX BOOM.mp3"), 0.6f, 0.95f, 1.05f, 0.05f, 8);
        AddSound(SoundIdConst.SFX_EXPLOSION_LARGE, AudioCategory.SFX, AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX BOOM.mp3"), 0.6f, 0.9f, 1.1f, 0.05f, 8);
        AddSound(SoundIdConst.SFX_BOOMER_HIT, AudioCategory.SFX, AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/boomer.wav"), 0.6f, 1f, 1f, 0f, 0);
        AddSound(SoundIdConst.SFX_GUN_SHOT_SHOTGUN, AudioCategory.SFX, AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/shotgun.wav"), 0.80f, 1f, 1f, 0f, 0);
        AddSound(SoundIdConst.SFX_LEVEL_UP, AudioCategory.SFX, itemPickup, 0.8f, 1.1f, 1.2f, 0.1f, 4);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
    }

    private static void InstallIntoScenePath(string scenePath, AudioClip bgm, AudioClip gunShot, AudioClip enemyDeath, AudioClip itemPickup, AudioClip playerDeath, AudioClip ui)
    {
        if (!File.Exists(scenePath)) return;

        Scene currentActive = SceneManager.GetActiveScene();
        if (currentActive.path == scenePath)
        {
            InstallIntoScene(currentActive, bgm, gunShot, enemyDeath, itemPickup, playerDeath, ui);
            return;
        }

        Scene openedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            InstallIntoScene(openedScene, bgm, gunShot, enemyDeath, itemPickup, playerDeath, ui);
        }
        finally
        {
            EditorSceneManager.CloseScene(openedScene, true);
        }
    }

    private static void InstallIntoScene(Scene scene, AudioClip bgm, AudioClip gunShot, AudioClip enemyDeath, AudioClip itemPickup, AudioClip playerDeath, AudioClip ui)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        // 1. Tìm xem trong Scene đã có [SoundManager] hoặc AudioManager chưa
        GameObject targetObj = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "[SoundManager]" || root.name == "SoundManager" || root.name == "[AudioManager]" || root.name == "AudioManager")
            {
                targetObj = root;
                break;
            }
            if (root.GetComponent<SoundManager>() != null || root.GetComponent<AudioManager>() != null)
            {
                targetObj = root;
                break;
            }
        }

        if (targetObj == null)
        {
            targetObj = new GameObject("[SoundManager]");
            SceneManager.MoveGameObjectToScene(targetObj, scene);
            targetObj.transform.SetAsFirstSibling();
        }

        SoundDatabase db = AssetDatabase.LoadAssetAtPath<SoundDatabase>(ResourcesDatabasePath)
                        ?? AssetDatabase.LoadAssetAtPath<SoundDatabase>(DataDatabasePath);

        // 2. Gắn và cấu hình component SoundManager
        SoundManager sm = targetObj.GetComponent<SoundManager>();
        if (sm == null)
        {
            sm = targetObj.AddComponent<SoundManager>();
        }

        sm.bgmMenu = bgm;
        sm.bgmGameplay = bgm;
        sm.sfxGunShot = gunShot;
        sm.sfxShotgun = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/shotgun.wav");
        sm.sfxExplosion = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX BOOM.mp3");
        sm.sfxPunch = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Punch.wav");
        sm.sfxBoomer = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/boomer.wav");
        sm.sfxEnemyDeath = enemyDeath;
        sm.sfxItemPickup = itemPickup;
        sm.sfxLevelUp = itemPickup;
        sm.sfxPlayerDeath = playerDeath;
        sm.sfxPlayerHurt = playerDeath;
        sm.uiButtonClick = ui;
        sm.uiPopupOpen = ui;
        sm.uiPopupClose = ui;
        sm.autoPlayBgmOnStart = true;
        sm.ApplyBalancedDefaults();

        SerializedObject smSo = new SerializedObject(sm);
        SerializedProperty smDbProp = smSo.FindProperty("database");
        if (smDbProp != null && db != null)
        {
            smDbProp.objectReferenceValue = db;
            smSo.ApplyModifiedProperties();
        }

        // 3. Gắn và cấu hình component AudioManager (đồng bộ 2 hệ thống)
        AudioManager am = targetObj.GetComponent<AudioManager>();
        if (am == null)
        {
            am = targetObj.AddComponent<AudioManager>();
        }

        SerializedObject amSo = new SerializedObject(am);
        SerializedProperty dbProp = amSo.FindProperty("database");
        if (dbProp != null && db != null)
        {
            dbProp.objectReferenceValue = db;
            amSo.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(targetObj);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

    }
}
#endif
