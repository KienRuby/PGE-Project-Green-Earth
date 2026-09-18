#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerSkinApplier))]
public class PlayerSkinApplierEditor : Editor
{
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

    private void OnEnable()
    {
        if (currentHandleMode != HandleEditMode.Disabled)
        {
            Tools.hidden = true;
        }
    }

    private void OnDisable()
    {
        Tools.hidden = false;
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
        if (GUILayout.Button("🔄 Đồng bộ / Khởi tạo Visual Slots", GUILayout.Height(28)))
        {
            Undo.RecordObject(applier.gameObject, "Ensure Visual Slots");
            applier.AutoEnsureVisualSlots();
            applier.ApplySkin(applier.previewSkinIndex);
            EditorUtility.SetDirty(applier);
            Debug.Log("[PlayerSkinApplierEditor] Đã kiểm tra và đồng bộ Visual Slots thành công!");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // 2. Skin Preview Switcher Buttons (Có nút MẶC ĐỊNH để đối chiếu)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("👁️ CHỌN XEM TRƯỚC VÀ HIỆU CHỈNH SKIN / MẶC ĐỊNH:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        // Nút Mặc Định (Gốc) để đối chiếu
        bool isDefault = applier.isShowingDefault || applier.previewSkinIndex == 0;
        GUI.backgroundColor = isDefault ? new Color(1f, 0.78f, 0.15f, 1f) : Color.white;
        if (GUILayout.Button("↺ MẶC ĐỊNH\n(Slot 1)", GUILayout.Height(42), GUILayout.Width(100)))
        {
            Undo.RecordObject(applier, "Switch to Default Visuals");
            applier.previewSkinIndex = 0;
            applier.ApplyDefaultVisuals();
            EditorUtility.SetDirty(applier);
        }

        for (int i = 1; i <= 4; i++)
        {
            bool isCurrent = (!applier.isShowingDefault && applier.previewSkinIndex == i);
            GUI.backgroundColor = isCurrent ? new Color(0.2f, 0.85f, 0.4f, 1f) : Color.white;

            string skinLabel = i == 1 ? "Skin 1\n(Unit-1)" :
                               i == 2 ? "Skin 2\n(Unit-2)" :
                               i == 3 ? "Skin 3\n(Unit-3)" : "Skin 4\n(Unit-4)";

            if (GUILayout.Button(skinLabel, GUILayout.Height(42)))
            {
                Undo.RecordObject(applier, $"Preview Skin {i}");
                applier.previewSkinIndex = i;
                applier.ApplySkin(i);
                EditorUtility.SetDirty(applier);
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (applier.isShowingDefault || applier.previewSkinIndex == 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "🔍 ĐANG XEM TRƯỚC & HIỆU CHỈNH BẢN MẶC ĐỊNH GỐC (BASE CHARACTER):\n" +
                "• Bạn có thể dùng chuột kéo thả trực tiếp các Visual Slot (BodyVisual, GunVisual,...) trên Scene View hoặc chỉnh số ở bảng dưới.\n" +
                "• Bấm nút [💾 LƯU VỊ TRÍ SCENE HIỆN TẠI VÀO BẢN MẶC ĐỊNH] để ghi nhận tọa độ mới!\n" +
                "• Bấm [Skin 1], [Skin 2], [Skin 3] hoặc [Skin 4] ở trên để chuyển sang tùy biến skin.",
                MessageType.Info
            );

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📦 SPRITE MẶC ĐỊNH GỐC (TỰ NẠP THỦ CÔNG):", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Sprite dBody = (Sprite)EditorGUILayout.ObjectField("Thân mặc định:", applier.defaultBodySprite, typeof(Sprite), false);
            Sprite dGun = (Sprite)EditorGUILayout.ObjectField("Súng mặc định:", applier.defaultGunSprite, typeof(Sprite), false);
            Sprite dArm = (Sprite)EditorGUILayout.ObjectField("Tay mặc định:", applier.defaultArmSprite, typeof(Sprite), false);
            Sprite dLeg1 = (Sprite)EditorGUILayout.ObjectField("Chân 1 mặc định:", applier.defaultLeg1Sprite, typeof(Sprite), false);
            Sprite dLeg2 = (Sprite)EditorGUILayout.ObjectField("Chân 2 mặc định:", applier.defaultLeg2Sprite, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(applier, "Change Default Sprites");
                applier.defaultBodySprite = dBody;
                applier.defaultGunSprite = dGun;
                applier.defaultArmSprite = dArm;
                applier.defaultLeg1Sprite = dLeg1;
                applier.defaultLeg2Sprite = dLeg2;
                applier.ApplyDefaultVisuals();
                EditorUtility.SetDirty(applier);
            }
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.Space(4);
            int newIndex = EditorGUILayout.IntSlider("Skin Slider:", applier.previewSkinIndex, 0, 4);
            if (newIndex != applier.previewSkinIndex)
            {
                applier.previewSkinIndex = newIndex;
                applier.ApplySkin(newIndex);
                EditorUtility.SetDirty(applier);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // Khôi phục khung xương gốc nếu bị lỡ tay kéo nhầm
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🦴 CÔNG CỤ KHÔI PHỤC KHUNG XƯƠNG GỐC:", EditorStyles.boldLabel);
        if (GUILayout.Button("⚡ Đặt Lại Xương Cha Về Chuẩn (Reset Parent Bones)", GUILayout.Height(26)))
        {
            Undo.RecordObject(applier.transform, "Reset Parent Bones");
            applier.ResetParentBonesToDefault();
            if (applier.isShowingDefault)
            {
                applier.ApplyDefaultVisuals();
            }
            else
            {
                applier.ApplySkin(applier.previewSkinIndex);
            }
            EditorUtility.SetDirty(applier);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(applier.gameObject.scene);
            Debug.Log("[PlayerSkinApplierEditor] Đã căn lại toàn bộ xương cha về tọa độ hoạt hình mặc định!");
        }
        EditorGUILayout.HelpBox("💡 Bấm nút này nếu bạn lỡ dùng phím W kéo nhầm các Xương cha ('thân', 'GunSprite', 'Tay', 'Chan 1', 'chan 2') làm lệch cả nhân vật.", MessageType.None);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 3. One-Click Save Current Scene Transforms & Sprites
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (applier.isShowingDefault)
        {
            GUI.backgroundColor = new Color(1f, 0.78f, 0.15f, 1f);
            if (GUILayout.Button("💾 LƯU VỊ TRÍ SCENE HIỆN TẠI VÀO BẢN MẶC ĐỊNH", GUILayout.Height(38)))
            {
                Undo.RecordObject(applier, "Capture Transforms for Default Skin");
                applier.CaptureCurrentSceneTransformsForDefault();
                EditorUtility.SetDirty(applier);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(applier.gameObject.scene);
                Debug.Log("[PlayerSkinApplierEditor] ✅ Đã lưu toàn bộ tọa độ, tỷ lệ và Sprite trên Scene vào Bản Mặc Định (Default)!");
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📋 Sao Chép Sang Skin 1 (Unit-1)", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Đồng bộ Skin 1", "Bạn có muốn sao chép toàn bộ tọa độ & Sprite của Bản Mặc Định sang Skin 1 (AD Unit-1) không?", "Đồng ý", "Hủy"))
                {
                    Undo.RecordObject(applier, "Copy Default to Skin 1");
                    applier.CopyDefaultTransformsToSkin1();
                    EditorUtility.SetDirty(applier);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(applier.gameObject.scene);
                    Debug.Log("[PlayerSkinApplierEditor] ✅ Đã đồng bộ tọa độ Bản Mặc Định sang Skin 1 (AD Unit-1)!");
                }
            }

            if (GUILayout.Button("↺ Đặt Lại Về (0, 0, 0) Gốc", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Khôi phục tọa độ Mặc định", "Bạn có chắc muốn đặt lại toàn bộ Offset của Bản Mặc Định về (0,0,0) không?", "Đồng ý", "Hủy"))
                {
                    Undo.RecordObject(applier, "Reset Default Transforms");
                    applier.ResetDefaultTransformsToZero();
                    EditorUtility.SetDirty(applier);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(applier.gameObject.scene);
                    Debug.Log("[PlayerSkinApplierEditor] Đã đặt lại tọa độ Bản Mặc Định về (0,0,0)!");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("👉 MẸO: Bạn có thể chọn trực tiếp BodyVisual, GunVisual, Leg1Visual trong Scene, dùng công cụ W (Move) và R (Scale) để chỉnh, rồi bấm nút trên để LƯU LẠI VÀO BẢN MẶC ĐỊNH!", MessageType.None);
        }
        else
        {
            int configIndex = applier.previewSkinIndex - 1;
            string skinName = (applier.skins != null && configIndex >= 0 && configIndex < applier.skins.Length)
                ? applier.skins[configIndex].skinName
                : $"Skin {applier.previewSkinIndex}";

            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f, 1f);
            if (GUILayout.Button($"💾 LƯU VỊ TRÍ SCENE HIỆN TẠI VÀO SKIN {applier.previewSkinIndex} ({skinName})", GUILayout.Height(36)))
            {
                Undo.RecordObject(applier, $"Capture Transforms for Skin {applier.previewSkinIndex}");
                applier.CaptureCurrentSceneTransforms(applier.previewSkinIndex);
                EditorUtility.SetDirty(applier);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(applier.gameObject.scene);
                Debug.Log($"[PlayerSkinApplierEditor] ✅ Đã lưu toàn bộ tọa độ, tỷ lệ và Sprite trên Scene vào Skin {applier.previewSkinIndex} ({skinName})!");
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("👉 MẸO: Bạn có thể chọn trực tiếp BodyVisual, GunVisual, Leg1Visual trong Scene, dùng công cụ W (Move) và R (Scale) để chỉnh, rồi bấm nút trên để LƯU LẠI!", MessageType.None);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 4. Scene Handle Controls
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎯 CHẾ ĐỘ HIỂN THỊ TAY NẮM (HANDLES) TRÊN SCENE VIEW:", EditorStyles.boldLabel);
        currentHandleMode = (HandleEditMode)EditorGUILayout.EnumPopup("Chế độ Handles:", currentHandleMode);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 5. Detailed Part Controls for Active Skin or Default
        if (applier.isShowingDefault)
        {
            if (applier.defaultBody == null) applier.defaultBody = new BodyPartTransformConfig();
            if (applier.defaultGun == null) applier.defaultGun = new BodyPartTransformConfig();
            if (applier.defaultArm == null) applier.defaultArm = new BodyPartTransformConfig();
            if (applier.defaultLeg1 == null) applier.defaultLeg1 = new BodyPartTransformConfig();
            if (applier.defaultLeg2 == null) applier.defaultLeg2 = new BodyPartTransformConfig();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showPartDetails = EditorGUILayout.Foldout(showPartDetails, "⚙️ THÔNG SỐ CHI TIẾT & TỌA ĐỘ: BẢN MẶC ĐỊNH (BASE CHARACTER)", true);

            if (showPartDetails)
            {
                EditorGUI.indentLevel++;

                // Súng
                DrawPartSection(applier, "Khẩu súng (Gun)", applier.defaultGun, applier.defaultGunSprite, s => applier.defaultGunSprite = s, () =>
                {
                    applier.defaultGun = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                    applier.ApplyDefaultVisuals();
                });

                // Nòng súng (FirePoint)
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("🎯 Nòng súng (FirePoint Offset):", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                Vector2 newFp = EditorGUILayout.Vector2Field("Tọa độ nòng súng:", applier.defaultFirePointOffset);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(applier, "Change FirePoint Offset");
                    applier.defaultFirePointOffset = newFp;
                    applier.ApplyDefaultVisuals();
                    EditorUtility.SetDirty(applier);
                }

                // Thân
                DrawPartSection(applier, "Thân (Body)", applier.defaultBody, applier.defaultBodySprite, s => applier.defaultBodySprite = s, () =>
                {
                    applier.defaultBody = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                    applier.ApplyDefaultVisuals();
                });

                // Cánh tay
                DrawPartSection(applier, "Cánh tay (Arm)", applier.defaultArm, applier.defaultArmSprite, s => applier.defaultArmSprite = s, () =>
                {
                    applier.defaultArm = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                    applier.ApplyDefaultVisuals();
                });

                // Chân 1
                DrawPartSection(applier, "Chân trái (Chan 1)", applier.defaultLeg1, applier.defaultLeg1Sprite, s => applier.defaultLeg1Sprite = s, () =>
                {
                    applier.defaultLeg1 = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                    applier.ApplyDefaultVisuals();
                });

                // Chân 2
                DrawPartSection(applier, "Chân phải (chan 2)", applier.defaultLeg2, applier.defaultLeg2Sprite, s => applier.defaultLeg2Sprite = s, () =>
                {
                    applier.defaultLeg2 = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                    applier.ApplyDefaultVisuals();
                });

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
        else if (applier.skins != null && applier.previewSkinIndex > 0 && (applier.previewSkinIndex - 1) < applier.skins.Length)
        {
            PlayerSkinConfig activeSkin = applier.skins[applier.previewSkinIndex - 1];
            if (activeSkin != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                showPartDetails = EditorGUILayout.Foldout(showPartDetails, $"⚙️ THÔNG SỐ CHI TIẾT & SPRITE: {activeSkin.skinName.ToUpper()}", true);

                if (showPartDetails)
                {
                    EditorGUI.indentLevel++;

                    // Súng
                    DrawPartSection(applier, "Khẩu súng (Gun)", activeSkin.gun, activeSkin.gunSprite, s => activeSkin.gunSprite = s, () =>
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
                    DrawPartSection(applier, "Thân (Body)", activeSkin.body, activeSkin.bodySprite, s => activeSkin.bodySprite = s, () =>
                    {
                        activeSkin.body = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                        applier.ApplySkin(applier.previewSkinIndex);
                    });

                    // Cánh tay
                    DrawPartSection(applier, "Cánh tay (Arm)", activeSkin.arm, activeSkin.armSprite, s => activeSkin.armSprite = s, () =>
                    {
                        activeSkin.arm = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                        applier.ApplySkin(applier.previewSkinIndex);
                    });

                    // Chân 1
                    DrawPartSection(applier, "Chân trái (Chan 1)", activeSkin.leg1, activeSkin.leg1Sprite, s => activeSkin.leg1Sprite = s, () =>
                    {
                        activeSkin.leg1 = new BodyPartTransformConfig(Vector2.zero, Vector2.one);
                        applier.ApplySkin(applier.previewSkinIndex);
                    });

                    // Chân 2
                    DrawPartSection(applier, "Chân phải (chan 2)", activeSkin.leg2, activeSkin.leg2Sprite, s => activeSkin.leg2Sprite = s, () =>
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

    private void DrawPartSection(PlayerSkinApplier applier, string partName, BodyPartTransformConfig config, Sprite currentSprite, Action<Sprite> onSpriteChanged, Action onReset)
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

        if (onSpriteChanged != null)
        {
            EditorGUI.BeginChangeCheck();
            Sprite newSprite = (Sprite)EditorGUILayout.ObjectField("  Sprite (Thủ công):", currentSprite, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(applier, $"Change {partName} Sprite");
                onSpriteChanged(newSprite);
                if (applier.isShowingDefault)
                    applier.ApplyDefaultVisuals();
                else
                    applier.ApplySkin(applier.previewSkinIndex);
                EditorUtility.SetDirty(applier);
            }
        }

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
            if (applier.isShowingDefault)
                applier.ApplyDefaultVisuals();
            else
                applier.ApplySkin(applier.previewSkinIndex);
            EditorUtility.SetDirty(applier);
        }
    }

    private void OnSceneGUI()
    {
        if (currentHandleMode == HandleEditMode.Disabled)
        {
            Tools.hidden = false;
            return;
        }

        PlayerSkinApplier applier = (PlayerSkinApplier)target;
        if (applier == null)
        {
            Tools.hidden = false;
            return;
        }

        // Ẩn công cụ mặc định của Unity (phím W/E/R) để người dùng không bấm nhầm vào Xương cha
        Tools.hidden = true;

        if (applier.isShowingDefault)
        {
            if (applier.defaultBody == null) applier.defaultBody = new BodyPartTransformConfig();
            if (applier.defaultGun == null) applier.defaultGun = new BodyPartTransformConfig();
            if (applier.defaultArm == null) applier.defaultArm = new BodyPartTransformConfig();
            if (applier.defaultLeg1 == null) applier.defaultLeg1 = new BodyPartTransformConfig();
            if (applier.defaultLeg2 == null) applier.defaultLeg2 = new BodyPartTransformConfig();

            // 1. Gun Handle
            if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.GunOnly)
            {
                DrawPartHandle(applier, applier.defaultGun, applier.gunVisual, "🔴 Súng (Mặc định)", Color.red);
            }

            // 2. FirePoint Handle
            if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.FirePointOnly || currentHandleMode == HandleEditMode.GunOnly)
            {
                DrawDefaultFirePointHandle(applier);
            }

            // 3. Body Handle
            if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.BodyOnly)
            {
                DrawPartHandle(applier, applier.defaultBody, applier.bodyVisual, "🔵 Thân (Mặc định)", Color.cyan);
            }

            // 4. Arm Handle
            if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.GunOnly)
            {
                DrawPartHandle(applier, applier.defaultArm, applier.armVisual, "🟡 Tay (Mặc định)", Color.yellow);
            }

            // 5. Legs Handles
            if (currentHandleMode == HandleEditMode.All || currentHandleMode == HandleEditMode.LegsOnly)
            {
                DrawPartHandle(applier, applier.defaultLeg1, applier.leg1Visual, "🟢 Chân 1 (Mặc định)", Color.green);
                DrawPartHandle(applier, applier.defaultLeg2, applier.leg2Visual, "🟣 Chân 2 (Mặc định)", new Color(0.8f, 0.4f, 1f));
            }
            return;
        }

        if (applier.skins == null || applier.skins.Length == 0)
        {
            Tools.hidden = false;
            return;
        }

        // Ẩn công cụ mặc định của Unity (phím W/E/R) để người dùng không bấm nhầm vào Xương cha
        Tools.hidden = true;

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

    private void DrawDefaultFirePointHandle(PlayerSkinApplier applier)
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
        Handles.Label(fpWorldPos + Vector3.up * 0.15f, "🎯 Nòng súng (Mặc định)");

        EditorGUI.BeginChangeCheck();
        Vector3 newFpWorldPos = Handles.PositionHandle(fpWorldPos, fp.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(applier, "Adjust Default FirePoint in Scene");
            Vector3 newLocalPos = parentTr.InverseTransformPoint(newFpWorldPos);
            applier.defaultFirePointOffset = new Vector2(newLocalPos.x, newLocalPos.y);
            fp.localPosition = new Vector3(newLocalPos.x, newLocalPos.y, fp.localPosition.z);
            EditorUtility.SetDirty(applier);
        }
    }
}
#endif
