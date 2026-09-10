import os

files = [
    "Assets/Prefabs/Buddy/Buddy_Sloy.prefab",
    "Assets/Prefabs/Buddy/Buddy_TurretBuffer.prefab",
    "Assets/Prefabs/Buddy/Buddy_PurifyingDrone.prefab",
    "Assets/Prefabs/Buddy/Buddy_RadarEye.prefab",
    "Assets/Prefabs/Buddy/Buddy_AssaultBlaster.prefab"
]

for f in files:
    assert os.path.exists(f), f"Missing {f}"
    meta = f + ".meta"
    assert os.path.exists(meta), f"Missing {meta}"
    with open(f, "r", encoding="utf-8") as fp:
        content = fp.read()
        assert "SpriteRenderer" in content, f"No SpriteRenderer in {f}"
        assert "m_Sprite:" in content, f"No m_Sprite in {f}"
        assert "buddyId:" in content, f"No buddyId in {f}"
        assert "FirePoint" in content, f"No FirePoint in {f}"
    print(f"[OK] {f}")

# Verify GamePlay.unity contains BuddyCombatManager
with open("Assets/Scenes/GamePlay.unity", "r", encoding="utf-8") as fp:
    gp_content = fp.read()
    assert "78188148" in gp_content, "Component 78188148 missing from GamePlay.unity"
    assert "18605ed8b05f05e33fa0d6be600dcf39" in gp_content, "BuddyCombatManager GUID missing from GamePlay.unity"
    print("[OK] Assets/Scenes/GamePlay.unity has BuddyCombatManager correctly linked!")

print("\nALL VERIFICATIONS PASSED!")
