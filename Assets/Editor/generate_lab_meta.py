import hashlib

meta_template_header = """fileFormatVersion: 2
guid: 5424adb2f9ebec9459ec5817bc67a31c
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
  spriteSheet:
    serializedVersion: 2
    sprites:
"""

icon_grid = [
    ["energy", "chip-currency", "red-currency", "mail", "settings"],
    ["shop", "lab", "chapter", "chipset", "buddy"],
    ["lock", "armor", "plus", "leaf", "shield"]
]

known_ids = {
    "energy": ("9e10b9416d838e54dbc18ae3146650a8", -1218426287),
    "chip-currency": ("1c30c51c366c5d14da6a03c335354710", -1317006964),
    "red-currency": ("ee651ab68c3873f4193501620e187bee", -1806302092),
    "mail": ("d5ed07025c80496458c31898095e4834", -359928730),
    "settings": ("bb5ae9532ffe0a24c9030fb3e19756fe", 2033099199),
    "shop": ("6560ca5c039cb8c478d0d3b09bebba2b", -535247977),
    "lab": ("0f5e5ce262fe63e45bad28ee94b9ad03", 1936296624),
    "chapter": ("46783abdda308f64187d8a449def8fd7", -579946858),
    "chipset": ("19eea224ce29b674e819c954cc2ec45e", 1040604794),
    "buddy": ("a4bc822c8ce56d34d8097fe61d682ce0", -188171556),
    "lock": ("6ad77021a8f7a4f40bd4a90983990512", 1568807622),
    "armor": ("b7898b8adfb14774db40dfe2750bcf63", -1475386712),
    "plus": ("53dcb821b2c47a74992ca572c3356bb8", 1551726181),
    "leaf": ("fe90aceb7543ab44d8e6ee2989afd368", -57964638),
    "shield": ("22bb44538328c9846b441160b1d84e65", -1094185162),
}

width = 1619
height = 971
cols = 5
rows = 3
cell_w = width / cols
cell_h = height / rows

sprites_yaml = ""
name_file_id_table = "    nameFileIdTable:\n"
sorted_name_table = []

for row in range(rows):
    for col in range(cols):
        name = icon_grid[row][col]
        x = col * cell_w
        y = (rows - 1 - row) * cell_h
        
        sp_id, int_id = known_ids[name]
        sorted_name_table.append((name, int_id))
        
        sprites_yaml += f"""    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {cell_w}
        height: {cell_h}
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
    spriteID: 5424adb2f9ebec9459ec5817bc67a31c
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

with open(r"Assets/UI/Lab/Generated/lab-icon-atlas.png.meta", "w", encoding="utf-8") as f:
    f.write(full_meta)

print("Successfully regenerated Assets/UI/Lab/Generated/lab-icon-atlas.png.meta with matched internalIDs!")
