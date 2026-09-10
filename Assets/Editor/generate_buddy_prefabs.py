import hashlib
import os

prefabs_info = [
    {
        "name": "Buddy_Sloy",
        "id": 1,
        "drone_name": "Sloy",
        "sprite_id": 1258304572,
        "script_guid": "05e00c188f9721ca4e343bf7017939c0",
        "has_projectile": True,
        "has_vfx": True
    },
    {
        "name": "Buddy_TurretBuffer",
        "id": 2,
        "drone_name": "Turret Buffer",
        "sprite_id": -436103723,
        "script_guid": "96255bb5eb3a798bacbb94c39bd1db02",
        "has_projectile": True,
        "has_vfx": False
    },
    {
        "name": "Buddy_PurifyingDrone",
        "id": 10,
        "drone_name": "Purifying Drone",
        "sprite_id": 701475079,
        "script_guid": "e65366ccded2f6eb9eef87112356bb94",
        "has_projectile": True,
        "has_vfx": True
    },
    {
        "name": "Buddy_RadarEye",
        "id": 3,
        "drone_name": "Radar Eye",
        "sprite_id": -40828008,
        "script_guid": "98f2848134c4e29391c279f81a24bd02",
        "has_projectile": False,
        "has_vfx": False
    },
    {
        "name": "Buddy_AssaultBlaster",
        "id": 4,
        "drone_name": "Assault Blaster",
        "sprite_id": -326532469,
        "script_guid": "68349797ffa73d7b56ab542b77a63d31",
        "has_projectile": True,
        "has_vfx": False
    }
]

texture_guid = "46057652adfb23646b92e7503cbcbebb"
projectile_guid = "7a360c9f713fe5b4ebd236256f3bb53b"
projectile_root_id = "9176291970807468248"
vfx_boom_guid = "2019a65776aecea44abb57cfdb6e0df5"
vfx_boom_root_id = "3290514725870574986"

out_dir = "Assets/Prefabs/Buddy"
os.makedirs(out_dir, exist_ok=True)

for p in prefabs_info:
    prefab_name = p["name"]
    buddy_id = p["id"]
    drone_name = p["drone_name"]
    sprite_id = p["sprite_id"]
    script_guid = p["script_guid"]
    
    # Generate unique 64-bit integer IDs for YAML components based on prefab name
    root_id = int(hashlib.md5((prefab_name + "_root").encode("utf-8")).hexdigest()[:15], 16)
    transform_id = int(hashlib.md5((prefab_name + "_trans").encode("utf-8")).hexdigest()[:15], 16)
    sprite_renderer_id = int(hashlib.md5((prefab_name + "_sr").encode("utf-8")).hexdigest()[:15], 16)
    script_id = int(hashlib.md5((prefab_name + "_script").encode("utf-8")).hexdigest()[:15], 16)
    firepoint_go_id = int(hashlib.md5((prefab_name + "_fp_go").encode("utf-8")).hexdigest()[:15], 16)
    firepoint_trans_id = int(hashlib.md5((prefab_name + "_fp_trans").encode("utf-8")).hexdigest()[:15], 16)
    
    proj_line = f"  projectilePrefab: {{fileID: {projectile_root_id}, guid: {projectile_guid}, type: 3}}\n" if p["has_projectile"] else "  projectilePrefab: {fileID: 0}\n"
    vfx_line = f"  hitVfxPrefab: {{fileID: {vfx_boom_root_id}, guid: {vfx_boom_guid}, type: 3}}\n" if p["has_vfx"] else ""
    pulse_vfx_line = f"  pulseVfxPrefab: {{fileID: {vfx_boom_root_id}, guid: {vfx_boom_guid}, type: 3}}\n" if p.get("name") == "Buddy_PurifyingDrone" else ""

    yaml_content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &{root_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {transform_id}}}
  - component: {{fileID: {sprite_renderer_id}}}
  - component: {{fileID: {script_id}}}
  m_Layer: 0
  m_Name: {prefab_name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{transform_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 0.12, y: 0.12, z: 1}}
  m_ConstrainProportionsScale: 1
  m_Children:
  - {{fileID: {firepoint_trans_id}}}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &{sprite_renderer_id}
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_id}}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 15
  m_Sprite: {{fileID: {sprite_id}, guid: {texture_guid}, type: 3}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 1.92, y: 1.92}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
--- !u!114 &{script_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  buddyId: {buddy_id}
  droneName: {drone_name}
  followDistance: 0.55
  followSpeed: 12
  bobbingAmplitude: 0.05
  bobbingFrequency: 2.5
  tiltAmount: 15
  targetDetectionRadius: 9
  targetRefreshInterval: 0.2
  enemyLayer:
    serializedVersion: 2
    m_Bits: 128
  baseDamage: 25
  attackCooldown: 1.2
  combatScale: 0.12
  spriteRenderer: {{fileID: {sprite_renderer_id}}}
  firePoint: {{fileID: {firepoint_trans_id}}}
{proj_line}{vfx_line}{pulse_vfx_line}--- !u!1 &{firepoint_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {firepoint_trans_id}}}
  m_Layer: 0
  m_Name: FirePoint
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{firepoint_trans_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {firepoint_go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: -0.1, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {transform_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""

    prefab_path = os.path.join(out_dir, f"{prefab_name}.prefab")
    with open(prefab_path, "w", encoding="utf-8") as f:
        f.write(yaml_content)

    meta_path = prefab_path + ".meta"
    prefab_guid = hashlib.md5(("PGE_" + prefab_path).encode("utf-8")).hexdigest()
    meta_content = f"""fileFormatVersion: 2
guid: {prefab_guid}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(meta_content)
    
    print(f"Generated {prefab_path} (GUID: {prefab_guid}, RootID: {root_id})")

print("All 5 Buddy prefabs generated successfully!")
