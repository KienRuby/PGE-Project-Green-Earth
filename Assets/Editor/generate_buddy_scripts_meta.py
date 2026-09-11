import hashlib
import os

items = [
    ("Assets/Scripts/Buddy", True),
    ("Assets/Scripts/Buddy/BuddyCombatDrone.cs", False),
    ("Assets/Scripts/Buddy/SloyFrostBuddy.cs", False),
    ("Assets/Scripts/Buddy/TurretBufferBuddy.cs", False),
    ("Assets/Scripts/Buddy/PurifyingBuddy.cs", False),
    ("Assets/Scripts/Buddy/RadarEyeBuddy.cs", False),
    ("Assets/Scripts/Buddy/AssaultBlasterBuddy.cs", False),
    ("Assets/Scripts/Buddy/BuddyCombatManager.cs", False),
    ("Assets/Editor/BuddyPrefabBuilder.cs", False),
    ("Assets/Editor/BuddyCombatSystemTests.cs", False),
    ("Assets/Prefabs/Buddy", True),
]

for path, is_folder in items:
    meta_path = path + ".meta"
    if os.path.exists(meta_path):
        print(f"Exists: {meta_path}")
        continue
    guid = hashlib.md5(("PGE_" + path).encode("utf-8")).hexdigest()
    if is_folder:
        content = (
            "fileFormatVersion: 2\n"
            f"guid: {guid}\n"
            "folderAsset: yes\n"
            "DefaultImporter:\n"
            "  externalObjects: {}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n"
        )
    else:
        content = (
            "fileFormatVersion: 2\n"
            f"guid: {guid}\n"
            "MonoImporter:\n"
            "  externalObjects: {}\n"
            "  serializedVersion: 2\n"
            "  defaultReferences: []\n"
            "  executionOrder: 0\n"
            "  icon: {fileID: 0}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n"
        )
    os.makedirs(os.path.dirname(meta_path), exist_ok=True)
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Generated {meta_path} with guid {guid}")
