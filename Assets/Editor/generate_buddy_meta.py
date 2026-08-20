import hashlib

meta_template_header = """fileFormatVersion: 2
guid: 81a9f30b91e362fa6849ba5f21e764a8
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: iPhone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
"""

sprite_names = [
    # Row 0
    "drone-snowflake", "drone-spider", "drone-antenna-eye", "drone-cross-visor", "drone-capsule",
    # Row 1
    "drone-spiky-mine", "drone-octagon-shield", "drone-claw-magnet", "drone-dual-rotor", "drone-stealth-wing",
    # Row 2
    "drone-laser-sentry", "drone-plasma-orb", "buddy-frame-normal", "buddy-frame-rare", "buddy-frame-epic",
    # Row 3
    "buddy-frame-holographic", "icon-lock-buddy", "badge-upgrade-green", "wave-pulse-cyan", "icon-drone-tab"
]

cols = 5
rows = 5
cell_size = 256

def gen_sprite_id(name):
    return hashlib.md5(("buddy_" + name).encode('utf-8')).hexdigest()

def gen_internal_id(name):
    h = int(hashlib.md5(("buddy_" + name + "_int").encode('utf-8')).hexdigest()[:16], 16)
    if h >= 2**63:
        h -= 2**64
    return h

sprites_yaml = ""
name_file_id_table = "    nameFileIdTable:\n"
sorted_name_table = []

for idx, name in enumerate(sprite_names):
    col = idx % cols
    row = idx // cols
    x = col * cell_size
    y = (rows - 1 - row) * cell_size

    sp_id = gen_sprite_id(name)
    int_id = gen_internal_id(name)
    sorted_name_table.append((name, int_id))

    sprites_yaml += f"""    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {cell_size}
        height: {cell_size}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {sp_id}
      internalID: {int_id}
      vertices: []
      indices: 
      edges: []
      weights: []
"""

sorted_name_table.sort(key=lambda x: x[0])
for name, int_id in sorted_name_table:
    name_file_id_table += f"      {name}: {int_id}\n"

meta_footer = f"""    outline: []
    physicsShape: []
    bones: []
    spriteID: 81a9f30b91e362fa6849ba5f21e764a8
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
{name_file_id_table}  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

full_meta = meta_template_header + sprites_yaml + meta_footer

with open(r"Assets/UI/Buddy/Generated/buddy-atlas.png.meta", "w", encoding="utf-8") as f:
    f.write(full_meta)

print("Updated Assets/UI/Buddy/Generated/buddy-atlas.png.meta with 20 sliced Buddy sprites successfully!")
