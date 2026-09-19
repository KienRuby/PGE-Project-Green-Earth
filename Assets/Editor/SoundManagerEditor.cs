#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Giao diện Inspector tùy biến chuyên nghiệp cho SoundManager.
/// Cung cấp thanh trượt âm lượng riêng cho từng âm thanh, nút nghe thử (Preview Audio)
/// trực tiếp trong Editor, và các công cụ cân bằng/đồng bộ âm thanh tự động.
/// </summary>
[CustomEditor(typeof(SoundManager))]
public class SoundManagerEditor : Editor
{
    private bool bgmFold = true;
    private bool sfxFold = true;
    private bool vfxFold = true;
    private bool uiFold = true;
    private bool customFold = false;
    private bool masterFold = true;

    private SoundManager sm;

    private void OnEnable()
    {
        sm = (SoundManager)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Banner Header
        EditorGUILayout.Space(6);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🔊 PGE - TRÌNH QUẢN LÝ & CÂN BẰNG ÂM THANH", titleStyle);
        EditorGUILayout.HelpBox("Điều chỉnh thanh âm lượng riêng của từng âm thanh để đạt độ cân bằng hoàn hảo. Nhấn nút '▶' để nghe thử trực tiếp âm lượng trong Editor.", MessageType.Info);
        EditorGUILayout.Space(4);

        // Quick Action Buttons
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("⚖️ Cân Bằng Mặc Định (Balanced Defaults)", GUILayout.Height(30)))
        {
            Undo.RecordObject(sm, "Apply Balanced Audio Defaults");
            sm.ApplyBalancedDefaults();
            EditorUtility.SetDirty(sm);
            serializedObject.Update();
        }

        GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
        if (GUILayout.Button("🔄 Đồng Bộ Sang Database", GUILayout.Height(30)))
        {
            sm.SyncToDatabase();
        }

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("⏹️ Dừng Nghe", GUILayout.Height(30), GUILayout.Width(90)))
        {
            StopAllPreviewClips();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // 0. Auto Play
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoPlayBgmOnStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("database"));
        EditorGUILayout.Space(6);

        // 1. BGM Section
        bgmFold = EditorGUILayout.BeginFoldoutHeaderGroup(bgmFold, "🎵 Nhạc Nền (Background Music - BGM)");
        if (bgmFold)
        {
            EditorGUI.indentLevel++;
            DrawSoundRow("Menu Chính", "bgmMenu", "bgmMenuVolume");
            DrawSoundRow("Trong Trận (Gameplay)", "bgmGameplay", "bgmGameplayVolume");
            DrawSoundRow("Đánh Boss", "bgmBoss", "bgmBossVolume");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // 2. SFX Section
        sfxFold = EditorGUILayout.BeginFoldoutHeaderGroup(sfxFold, "💥 Hiệu Ứng Chiến Đấu (Gameplay SFX)");
        if (sfxFold)
        {
            EditorGUI.indentLevel++;
            DrawSoundRow("Bắn Súng Cơ Bản", "sfxGunShot", "sfxGunShotVolume");
            DrawSoundRow("Súng Săn Shotgun", "sfxShotgun", "sfxShotgunVolume");
            DrawSoundRow("Tiếng Nổ (Boom/Explosion)", "sfxExplosion", "sfxExplosionVolume");
            DrawSoundRow("Nắm Đấm (Rocket Punch)", "sfxPunch", "sfxPunchVolume");
            DrawSoundRow("Quái Nổ Boomer / Đĩa Cưa", "sfxBoomer", "sfxBoomerVolume");
            DrawSoundRow("Người Chơi Bị Đau", "sfxPlayerHurt", "sfxPlayerHurtVolume");
            DrawSoundRow("Người Chơi Chết", "sfxPlayerDeath", "sfxPlayerDeathVolume");
            DrawSoundRow("Quái Vật Bị Tiêu Diệt", "sfxEnemyDeath", "sfxEnemyDeathVolume");
            DrawSoundRow("Nhặt Vật Phẩm / EXP", "sfxItemPickup", "sfxItemPickupVolume");
            DrawSoundRow("Nhân Vật Lên Cấp", "sfxLevelUp", "sfxLevelUpVolume");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // 3. VFX Section
        vfxFold = EditorGUILayout.BeginFoldoutHeaderGroup(vfxFold, "✨ Kỹ Xảo Chiêu Thức (VFX) - (Chờ mở rộng khi có file mới)");
        if (vfxFold)
        {
            EditorGUILayout.HelpBox("💡 Các ô bên dưới là vị trí chờ sẵn cho các kỹ xảo nguyên tố (Laser, Lửa, Băng, Sét, Khiên). Hiện dự án chỉ có 10 file âm thanh cơ bản nên các ô này tạm để trống. Khi bạn có thêm file âm thanh mới, chỉ cần kéo thả vào ô tương ứng.", MessageType.Info);
            EditorGUI.indentLevel++;
            DrawSoundRow("Tia Laser (Beam)", "vfxLaserBeam", "vfxLaserBeamVolume");
            DrawSoundRow("Bùng Lửa / Phun Lửa", "vfxFireBurst", "vfxFireBurstVolume");
            DrawSoundRow("Băng Vỡ / Đóng Băng", "vfxIceShatter", "vfxIceShatterVolume");
            DrawSoundRow("Sấm Sét / Giật Điện", "vfxLightningStrike", "vfxLightningStrikeVolume");
            DrawSoundRow("Khiên Năng Lượng", "vfxShieldActivate", "vfxShieldActivateVolume");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // 4. UI Section
        uiFold = EditorGUILayout.BeginFoldoutHeaderGroup(uiFold, "🖥️ Giao Diện Người Dùng (UI Audio)");
        if (uiFold)
        {
            EditorGUI.indentLevel++;
            DrawSoundRow("Bấm Nút UI (Click)", "uiButtonClick", "uiButtonClickVolume");
            DrawSoundRow("Mở Bảng Popup", "uiPopupOpen", "uiPopupOpenVolume");
            DrawSoundRow("Đóng Bảng Popup", "uiPopupClose", "uiPopupCloseVolume");
            DrawSoundRow("Nhận Thưởng / Mở Quà", "uiRewardClaim", "uiRewardClaimVolume");
            DrawSoundRow("Báo Lỗi / Thao Tác Sai", "uiError", "uiErrorVolume");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // 5. Custom Lists Section
        customFold = EditorGUILayout.BeginFoldoutHeaderGroup(customFold, "📂 Âm Thanh Tùy Chỉnh Thêm (Custom Lists)");
        if (customFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customBgmList"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customSfxList"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customVfxList"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customUiList"), true);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // 6. Master Volume Section
        masterFold = EditorGUILayout.BeginFoldoutHeaderGroup(masterFold, "🎚️ Âm Lượng Nhóm (Category Master Volumes)");
        if (masterFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isBgmEnabled"), new GUIContent("Bật BGM"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bgmVolume"), new GUIContent("Âm lượng tổng BGM"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isSfxEnabled"), new GUIContent("Bật SFX / VFX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sfxVolume"), new GUIContent("Âm lượng tổng SFX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vfxVolume"), new GUIContent("Âm lượng tổng VFX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uiVolume"), new GUIContent("Âm lượng tổng UI"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (serializedObject.ApplyModifiedProperties())
        {
            sm.SyncToDatabase();
        }
    }

    private void DrawSoundRow(string label, string clipPropName, string volPropName)
    {
        SerializedProperty clipProp = serializedObject.FindProperty(clipPropName);
        SerializedProperty volProp = serializedObject.FindProperty(volPropName);

        if (clipProp == null || volProp == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        // Label
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(170));

        // Clip field (Using ObjectField directly avoids Unity rendering serialized property headers inside horizontal layout)
        clipProp.objectReferenceValue = EditorGUILayout.ObjectField(clipProp.objectReferenceValue, typeof(AudioClip), false, GUILayout.MinWidth(120));

        // Preview button
        GUI.enabled = clipProp.objectReferenceValue != null;
        if (GUILayout.Button("▶", GUILayout.Width(28), GUILayout.Height(18)))
        {
            PlayPreviewClip((AudioClip)clipProp.objectReferenceValue, volProp.floatValue);
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        // Individual Volume Slider
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Âm lượng riêng:", GUILayout.Width(110));
        volProp.floatValue = EditorGUILayout.Slider(volProp.floatValue, 0f, 1f);
        EditorGUILayout.LabelField($"{Mathf.RoundToInt(volProp.floatValue * 100f)}%", GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // =========================================================================
    // AUDIO PREVIEW UTILITIES (REFLECTION)
    // =========================================================================
    private static void PlayPreviewClip(AudioClip clip, float volume)
    {
        if (clip == null) return;

        StopAllPreviewClips();

        try
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            // PlayPreviewClip(AudioClip clip, int startSample, bool loop)
            MethodInfo playMethod = audioUtilClass.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

            if (playMethod != null)
            {
                playMethod.Invoke(null, new object[] { clip, 0, false });
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SoundManagerEditor] Không thể phát thử clip: {ex.Message}");
        }
    }

    private static void StopAllPreviewClips()
    {
        try
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo stopMethod = audioUtilClass.GetMethod("StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);
            stopMethod?.Invoke(null, null);
        }
        catch
        {
            // Ignore if reflection fails
        }
    }
}
#endif
