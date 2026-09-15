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
        bool isDefault = applier.isShowingDefault;
        GUI.backgroundColor = isDefault ? new Color(1f, 0.78f, 0.15f, 1f) : Color.white;
        if (GUILayout.Button("↺ MẶC ĐỊNH\n(Đối chiếu)", GUILayout.Height(42), GUILayout.Width(100)))
        {
            Undo.RecordObject(applier, "Switch to Default Visuals");
            applier.ApplyDefaultVisuals();
            EditorUtility.SetDirty(applier);
        }

        for (int i = 0; i < 4; i++)
        {
            bool isCurrent = (!applier.isShowingDefault && applier.previewSkinIndex == i);
            GUI.backgroundColor = isCurrent ? new Color(0.2f, 0.85f, 0.4f, 1f) : Color.white;

            string skinLabel = i == 0 ? "Skin 1\n(Unit-1)" :
                               i == 1 ? "Skin 2\n(Unit-2)" :
                               i == 2 ? "Skin 3\n(Unit-3)" : "Skin 4\n(Unit-4)";

            if (GUILayout.Button(skinLabel, GUILayout.Height(42)))
            {
                Undo.RecordObject(applier, $"Preview Skin {i + 1}");
                applier.previewSkinIndex = i;
                applier.ApplySkin(i);
                EditorUtility.SetDirty(applier);
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (applier.isShowingDefault)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "🔍 ĐANG ĐỐI CHIẾU VỚI BẢN MẶC ĐỊNH GỐC (BASE CHARACTER):\n" +
                "• Đang hiển thị 5 Sprite mặc định ban đầu ở vị trí (0, 0, 0) chuẩn.\n" +
                "• Bạn có thể gán thủ công các Sprite mặc định ở bảng bên dưới.\n" +
                "• Bấm [Skin 1], [Skin 2], [Skin 3] hoặc [Skin 4] ở trên để quay lại tùy biến skin!",
                MessageType.Warning
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
            int newIndex = EditorGUILayout.IntSlider("Skin Slider:", applier.previewSkinIndex, 0, 3);
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

        // 3. One-Click Save Current Scene Transforms & Sprites (chỉ khi đang chọn skin)
        if (!applier.isShowingDefault)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f, 1f);
            if (GUILayout.Button($"💾 LƯU VỊ TRÍ SCENE HIỆN TẠI VÀO SKIN {applier.previewSkinIndex + 1}", GUILayout.Height(36)))
            {
                Undo.RecordObject(applier, $"Capture Transforms for Skin {applier.previewSkinIndex + 1}");
                applier.CaptureCurrentSceneTransforms(applier.previewSkinIndex);
                EditorUtility.SetDirty(applier);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(applier.gameObject.scene);
                Debug.Log($"[PlayerSkinApplierEditor] ✅ Đã lưu toàn bộ tọa độ, tỷ lệ và Sprite trên Scene vào Skin {applier.previewSkinIndex + 1} ({applier.skins[applier.previewSkinIndex].skinName})!");
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("👉 MẸO: Bạn có thể chọn trực tiếp BodyVisual, GunVisual, Leg1Visual trong Scene, dùng công cụ W (Move) và R (Scale) để chỉnh, rồi bấm nút trên để LƯU LẠI!", MessageType.None);
            EditorGUILayout.EndVertical();
        }

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
        if (applier == null || applier.skins == null || applier.skins.Length == 0)
        {
            Tools.hidden = false;
            return;
        }

        // Khi đang ở chế độ Mặc Định (Đối chiếu), không vẽ Handles để tránh thao tác kéo nhầm vào Skin
        if (applier.isShowingDefault)
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
}
#endif
