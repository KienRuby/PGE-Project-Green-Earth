#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerSkinApplier))]
[InitializeOnLoad]
public class PlayerSkinApplierEditor : Editor
{
    private static readonly string[] SkinFolderNames = new string[] { "1", "2", "3", "4" };

    public enum HandleEditMode
    {
        All,
        GunOnly,
        BodyOnly,
        LegsOnly,
        FirePointOnly,
        Disabled
    }

    private static HandleEditMode currentHandleMode = HandleEditMode.All;
    private static bool showPartDetails = true;

    static PlayerSkinApplierEditor()
    {
        EditorApplication.delayCall += CheckAndSetupIfMissing;
    }

    private static void CheckAndSetupIfMissing()
    {
        if (Application.isPlaying) return;
        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (activeScene.path != "Assets/Scenes/GamePlay.unity") return;

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            PlayerSkinApplier applier = player.GetComponent<PlayerSkinApplier>();
            if (applier == null)
            {
                SetupPlayerSkinApplierInGamePlayScene();
            }
        }
    }

    public override void OnInspectorGUI()
    {
        PlayerSkinApplier applier = (PlayerSkinApplier)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🎮 BỘ ĐIỀU KHIỂN & CHỈNH SỬA SKIN PLAYER (VISUAL SLOTS)", titleStyle);
        EditorGUILayout.HelpBox(
            "💡 KIẾN TRÚC VISUAL SLOT:\n" +
            "• Animator điều khiển các Xương cha (thân, GunPivot, Chan 1, chan 2, Tay).\n" +
            "• SpriteRenderer nằm trên các Visual Slot con độc lập, KHÔNG BAO GIỜ bị Animator ghi đè!\n" +
            "• Bạn có thể dùng chuột kéo thả trực tiếp trên Scene View hoặc chỉnh số ở bảng dưới mà Animation vẫn chạy 100%!",
            MessageType.Info
        );
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 1. Action Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 Khởi tạo Visual Slots", GUILayout.Height(28)))
        {
            Undo.RecordObject(applier.gameObject, "Ensure Visual Slots");
            applier.AutoEnsureVisualSlots();
            applier.ApplySkin(applier.previewSkinIndex);
            EditorUtility.SetDirty(applier);
            Debug.Log("[PlayerSkinApplierEditor] Đã kiểm tra và đồng bộ Visual Slots thành công!");
        }

        if (GUILayout.Button("📥 Tự động nạp 4 bộ Skin", GUILayout.Height(28)))
        {
            Undo.RecordObject(applier, "Auto Load Skin Sprites");
            AutoLoadAllSkinSprites(applier);
            applier.ApplySkin(applier.previewSkinIndex);
            EditorUtility.SetDirty(applier);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // 2. Skin Preview Switcher Buttons
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("👁️ CHỌN XEM TRƯỚC VÀ HIỆU CHỈNH SKIN:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        for (int i = 0; i < 4; i++)
        {
            bool isCurrent = (applier.previewSkinIndex == i);
            GUI.backgroundColor = isCurrent ? new Color(0.2f, 0.85f, 0.4f, 1f) : Color.white;

            string skinLabel = i == 0 ? "Skin 1\n(Unit-1)" :
                               i == 1 ? "Skin 2\n(Unit-2)" :
                               i == 2 ? "Skin 3\n(Unit-3)" : "Skin 4\n(Unit-4)";

            if (GUILayout.Button(skinLabel, GUILayout.Height(40)))
            {
                applier.previewSkinIndex = i;
                applier.ApplySkin(i);
                EditorUtility.SetDirty(applier);
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        int newIndex = EditorGUILayout.IntSlider("Skin Slider:", applier.previewSkinIndex, 0, 3);
        if (newIndex != applier.previewSkinIndex)
        {
            applier.previewSkinIndex = newIndex;
            applier.ApplySkin(newIndex);
            EditorUtility.SetDirty(applier);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 3. One-Click Save Current Scene Transforms
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f, 1f);
        if (GUILayout.Button($"💾 LƯU VỊ TRÍ SCENE HIỆN TẠI VÀO SKIN {applier.previewSkinIndex + 1}", GUILayout.Height(36)))
        {
            Undo.RecordObject(applier, $"Capture Transforms for Skin {applier.previewSkinIndex + 1}");
            applier.CaptureCurrentSceneTransforms(applier.previewSkinIndex);
            EditorUtility.SetDirty(applier);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(applier.gameObject.scene);
            Debug.Log($"[PlayerSkinApplierEditor] ✅ Đã lưu toàn bộ tọa độ và tỷ lệ trên Scene vào Skin {applier.previewSkinIndex + 1} ({applier.skins[applier.previewSkinIndex].skinName})!");
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.HelpBox("👉 MẸO: Bạn có thể chọn trực tiếp BodyVisual, GunVisual, Leg1Visual trong Scene, dùng công cụ W (Move) và R (Scale) để chỉnh, rồi bấm nút trên để LƯU LẠI!", MessageType.None);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 4. Scene Handle Controls
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎯 CHẾ ĐỘ HIỂN THỊ TAY NẮM (HANDLES) TRÊN SCENE VIEW:", EditorStyles.boldLabel);
        currentHandleMode = (HandleEditMode)EditorGUILayout.EnumPopup("Chế độ Handles:", currentHandleMode);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 5. Detailed Part Controls for Active Skin
        if (applier.skins != null && applier.previewSkinIndex >= 0 && applier.previewSkinIndex < applier.skins.Length)
        {
            PlayerSkinConfig activeSkin = applier.skins[applier.previewSkinIndex];
            if (activeSkin != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                showPartDetails = EditorGUILayout.Foldout(showPartDetails, $"⚙️ THÔNG SỐ CHI TIẾT: {activeSkin.skinName.ToUpper()}", true);

                if (showPartDetails)
                {
                    EditorGUI.indentLevel++;

                    // Súng
                    DrawPartSection(applier, "Khẩu súng (Gun)", activeSkin.gun, () =>
                    {
                        activeSkin.gun = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                        applier.ApplySkin(applier.previewSkinIndex);
                    });

                    // Nòng súng (FirePoint)
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("🎯 Nòng súng (FirePoint Offset):", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    Vector2 newFp = EditorGUILayout.Vector2Field("Tọa độ nòng súng:", activeSkin.firePointOffset);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(applier, "Change FirePoint Offset");
                        activeSkin.firePointOffset = newFp;
                        applier.ApplySkin(applier.previewSkinIndex);
                        EditorUtility.SetDirty(applier);
                    }

                    // Thân
                    DrawPartSection(applier, "Thân (Body)", activeSkin.body, () =>
                    {
                        activeSkin.body = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                        applier.ApplySkin(applier.previewSkinIndex);
                    });

                    // Cánh tay
                    DrawPartSection(applier, "Cánh tay (Arm)", activeSkin.arm, () =>
                    {
                        activeSkin.arm = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                        applier.ApplySkin(applier.previewSkinIndex);
                    });

                    // Chân 1
                    DrawPartSection(applier, "Chân trái (Chan 1)", activeSkin.leg1, () =>
                    {
                        activeSkin.leg1 = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                        applier.ApplySkin(applier.previewSkinIndex);
                    });

                    // Chân 2
                    DrawPartSection(applier, "Chân phải (chan 2)", activeSkin.leg2, () =>
                    {
                        activeSkin.leg2 = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                        applier.ApplySkin(applier.previewSkinIndex);
                    });

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.Space(10);
        base.OnInspectorGUI();
    }

    private void DrawPartSection(PlayerSkinApplier applier, string partName, BodyPartTransformConfig config, Action onReset)
    {
        if (config == null) return;

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"🔹 {partName}:", EditorStyles.boldLabel);
        if (GUILayout.Button("Reset", GUILayout.Width(50), GUILayout.Height(18)))
        {
            Undo.RecordObject(applier, $"Reset {partName}");
            onReset?.Invoke();
            EditorUtility.SetDirty(applier);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        Vector2 pos = EditorGUILayout.Vector2Field("  Vị trí (Offset):", config.positionOffset);
        Vector2 scale = EditorGUILayout.Vector2Field("  Tỷ lệ (Scale):", config.scaleMultiplier);
        float rot = EditorGUILayout.FloatField("  Góc xoay (Độ):", config.rotationOffset);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(applier, $"Edit {partName}");
            config.positionOffset = pos;
            config.scaleMultiplier = scale;
            config.rotationOffset = rot;
            applier.ApplySkin(applier.previewSkinIndex);
            EditorUtility.SetDirty(applier);
        }
    }

    private void OnSceneGUI()
    {
        if (currentHandleMode == HandleEditMode.Disabled) return;

        PlayerSkinApplier applier = (PlayerSkinApplier)target;
        if (applier == null || applier.skins == null || applier.skins.Length == 0) return;

        int skinIdx = Mathf.Clamp(applier.previewSkinIndex, 0, applier.skins.Length - 1);
        PlayerSkinConfig skin = applier.skins[skinIdx];
        if (skin == null) return;

        // 1. Gun Handle
        if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.GunOnly)
        {
            DrawPartHandle(applier, skin.gun, applier.gunVisual, "🔴 Súng", Color.red);
        }

        // 2. FirePoint Handle
        if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.FirePointOnly || currentHandleMode == HandleEditMode.GunOnly)
        {
            DrawFirePointHandle(applier, skin);
        }

        // 3. Body Handle
        if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.BodyOnly)
        {
            DrawPartHandle(applier, skin.body, applier.bodyVisual, "🔵 Thân", Color.cyan);
        }

        // 4. Arm Handle
        if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.GunOnly)
        {
            DrawPartHandle(applier, skin.arm, applier.armVisual, "🟡 Tay", Color.yellow);
        }

        // 5. Legs Handles
        if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.LegsOnly)
        {
            DrawPartHandle(applier, skin.leg1, applier.leg1Visual, "🟢 Chân 1", Color.green);
            DrawPartHandle(applier, skin.leg2, applier.leg2Visual, "🟣 Chân 2", new Color(0.8f, 0.4f, 1f));
        }
    }

    private void DrawPartHandle(PlayerSkinApplier applier, BodyPartTransformConfig config, Transform visualTransform, string label, Color color)
    {
        if (config == null || visualTransform == null || visualTransform.parent == null) return;

        Transform parentBone = visualTransform.parent;
        Vector3 worldPos = visualTransform.position;

        Handles.color = color;
        Handles.Label(worldPos + Vector3.up * 0.15f, label);

        EditorGUI.BeginChangeCheck();
        Vector3 newWorldPos = Handles.PositionHandle(worldPos, visualTransform.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(applier, $"Move {label}");
            Vector3 localPos = parentBone.InverseTransformPoint(newWorldPos);
            config.positionOffset = new Vector2(localPos.x, localPos.y);
            visualTransform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            EditorUtility.SetDirty(applier);
        }
    }

    private void DrawFirePointHandle(PlayerSkinApplier applier, PlayerSkinConfig skin)
    {
        if (applier.firePoint == null) return;

        Transform fp = applier.firePoint;
        Transform parentTr = fp.parent;
        if (parentTr == null) return;

        Vector3 fpWorldPos = fp.position;

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(fpWorldPos, Vector3.forward, 0.12f);
        Handles.color = Color.red;
        Handles.DrawLine(fpWorldPos, fpWorldPos + fp.right * 0.8f);
        Handles.Label(fpWorldPos + Vector3.up * 0.15f, "🎯 Nòng súng");

        EditorGUI.BeginChangeCheck();
        Vector3 newFpWorldPos = Handles.PositionHandle(fpWorldPos, fp.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(applier, "Adjust FirePoint in Scene");
            Vector3 newLocalPos = parentTr.InverseTransformPoint(newFpWorldPos);
            skin.firePointOffset = new Vector2(newLocalPos.x, newLocalPos.y);
            fp.localPosition = new Vector3(newLocalPos.x, newLocalPos.y, fp.localPosition.z);
            EditorUtility.SetDirty(applier);
        }
    }

    public static void AutoLoadAllSkinSprites(PlayerSkinApplier applier)
    {
        if (applier == null) return;

        for (int i = 0; i < 4; i++)
        {
            string folderNum = SkinFolderNames[i];
            PlayerSkinConfig skin = applier.skins[i];
            if (skin == null)
            {
                skin = new PlayerSkinConfig();
                applier.skins[i] = skin;
            }

            skin.skinName = i == 0 ? "AD Unit-1 (Basic Body)" :
                            i == 1 ? "AD Unit-2" :
                            i == 2 ? "AD Unit-3" : "AD Unit-4";

            string folderPath = $"Assets/Sprites/Character/Skin/{folderNum}";
            Sprite[] allSprites = LoadAllSpritesFromFolder(folderPath);

            Sprite body = FindBestSprite(allSprites, "thân", "body", "than");
            Sprite arm = FindBestSprite(allSprites, "Tay", "cánh tay trên", "arm", "vai");
            Sprite gun = FindBestSprite(allSprites, "Gun", "súng", "sung");
            Sprite leg1 = FindBestSprite(allSprites, "Chan 1", "chân 1", "chan 1", "leg 1");
            Sprite leg2 = FindBestSprite(allSprites, "chan 2", "chân 2", "leg 2");

            if (body != null) skin.bodySprite = body;
            if (arm != null) skin.armSprite = arm;
            if (gun != null) skin.gunSprite = gun;
            if (leg1 != null) skin.leg1Sprite = leg1;
            if (leg2 != null) skin.leg2Sprite = leg2;
        }

        Debug.Log("[PlayerSkinApplierEditor] Successfully auto-loaded all sliced sprites for 4 player skins!");
    }

    [MenuItem("Tools/PGE/Setup Player Visual Slots & Skins")]
    public static void SetupPlayerSkinApplierInGamePlayScene()
    {
        string scenePath = "Assets/Scenes/GamePlay.unity";
        var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

        if (currentScene.path != scenePath)
        {
            currentScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            var match = currentScene.GetRootGameObjects();
            foreach (var go in match)
            {
                if (go.name.Equals("Player", StringComparison.OrdinalIgnoreCase))
                {
                    player = go;
                    break;
                }
            }
        }

        if (player == null)
        {
            Debug.LogError("[PlayerSkinApplierEditor] Cannot find Player GameObject in GamePlay.unity!");
            return;
        }

        PlayerSkinApplier applier = player.GetComponent<PlayerSkinApplier>();
        if (applier == null)
        {
            applier = Undo.AddComponent<PlayerSkinApplier>(player);
        }

        AutoLoadAllSkinSprites(applier);
        applier.AutoEnsureVisualSlots();
        applier.ApplySkin(applier.previewSkinIndex);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(currentScene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currentScene);

        Debug.Log("[PlayerSkinApplierEditor] PlayerSkinApplier successfully attached and configured on Player in GamePlay.unity!");
    }

    private static Sprite[] LoadAllSpritesFromFolder(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { folderPath });
        System.Collections.Generic.List<Sprite> spriteList = new System.Collections.Generic.List<Sprite>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in subAssets)
            {
                if (asset is Sprite s)
                {
                    spriteList.Add(s);
                }
            }
        }

        return spriteList.ToArray();
    }

    private static Sprite FindBestSprite(Sprite[] sprites, params string[] searchKeywords)
    {
        if (sprites == null || sprites.Length == 0) return null;

        foreach (string keyword in searchKeywords)
        {
            foreach (Sprite s in sprites)
            {
                if (s == null) continue;
                if (s.name.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
        }

        foreach (string keyword in searchKeywords)
        {
            foreach (Sprite s in sprites)
            {
                if (s == null) continue;
                if (s.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return s;
            }
        }

        return null;
    }
}
#endif
