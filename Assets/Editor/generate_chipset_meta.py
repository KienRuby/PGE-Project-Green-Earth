import hashlib

meta_template_header = """fileFormatVersion: 2
guid: 790fa91e362fa6849ba5f21e764a8316
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
    "standard-gun", "rifle", "rocket-punch", "spinning-blade", "multigun", "gun-turret",
    # Row 1
    "spiky-discus", "shotgun", "energy-jumper-cables", "high-explosive-mine", "aiming-lens", "plasma-field",
    # Row 2
    "laser-eye", "biochemical-mine", "tesla-coil", "atk-module", "black-hole-mine", "sonic-boom",
    # Row 3
    "big-battery", "turret-module", "ice-turret", "invincible-shield", "healing-turret", "flamethrower",
    # Row 4
    "card-frame-common", "card-frame-rare", "card-frame-epic", "card-frame-holographic", "badge-upgrade", "icon-lock",
    # Row 5
    "wave-circuit", "icon-star", "furnace-border", "power-battery", "advance-stone", "badge-advance"
]

cols = 6
rows = 6
cell_size = 256

def gen_sprite_id(name):
    h = hashlib.md5(name.encode('utf-8')).hexdigest()
    return h

def gen_internal_id(name):
    h = int(hashlib.md5((name + "_internal").encode('utf-8')).hexdigest()[:16], 16)
    # Convert to signed 64-bit int
    if h >= 2**63:
        h -= 2**64
    return h

sprites_yaml = ""
name_file_id_table = "    nameFileIdTable:\n"

# Maintain existing IDs where available
known_sprite_ids = {
    "standard-gun": ("c5201119f2d90e047935fa1d1caab3a9", -6103525226337011396),
    "rifle": ("a46695f5538577346b215b87b02fc881", 8581563430471098634),
    "rocket-punch": ("a8fd02db2f8b8394887a0352acd0a237", 4231819220138351040),
    "spinning-blade": ("080a064931a8b884fa469c9a522f216f", -2778998792480739766),
    "multigun": ("7bcb1913ae308f64f9e0519571588d0f", -5109444298764135915),
    "gun-turret": ("b143ba79b00142040a07668e3357d3b7", 6894241887267050856),
    "spiky-discus": ("2367f303e8b8a71499943877ef7f4119", -2785223181230108149),
    "shotgun": ("dde20d69628c7434b89dda92abce5fb0", -6435363134993694004),
    "energy-jumper-cables": ("e2e3cb8b681f78b41afb2cc09ea0d905", 5170568551976924551),
    "high-explosive-mine": ("e9d85915ec70957429b842997aea385e", 7125556484904952107),
    "aiming-lens": ("8f940b8e8196d39498393fcf5af74d9b", -4796562146451971242),
    "plasma-field": ("8df9c63fc6a5713479d0b0238180fdcf", -5814445313220549694),
    "laser-eye": ("b6f2fbbc7f066e949afbefbc15728d46", -2538379449282638240),
    "biochemical-mine": ("015d5a8481c1eae41af07dccdc75fafc", -6575425849463812547),
    "tesla-coil": ("153f2d977a4b6c54f9f0e7f189fd8592", 5093887549840003368),
    "card-frame-common": ("e8e56d6af2f9a124e89ac80a1ca8433a", -341851465085348862),
    "card-frame-rare": ("aaeeb4b52e21d1f4ab1bd21d02400b22", 5820474228769164200),
    "card-frame-epic": ("eca48ae6934bc164bb1f60cc879552d7", -2983306540345666763),
    "card-frame-holographic": ("a6a374988541221408eacc418c8fba48", -1469001518291150936),
    "badge-upgrade": ("fcc549d93a06ec84f81867a06c5baec0", 8881422324888349463),
    "icon-lock": ("14412eb7e67fabc4ca186e5b6aaeffd6", -6707754332129007671),
    "wave-circuit": ("a5f2534fb76c9004b87813415b52bab0", -6568416663731092074),
    "icon-star": ("9636bfe31d5cfdf48924a4e5186e020f", -5683019630868176096),
    "furnace-border": ("b93b602262d88fc4eacbd87565c32073", 6881827167196030516),
    "power-battery": ("b71ac8cbd2ca80649b76d236f6d625e9", 1751919743643567911),
}

sorted_name_table = []

for idx, name in enumerate(sprite_names):
    col = idx % cols
    row = idx // cols
    x = col * cell_size
    y = (rows - 1 - row) * cell_size # Unity origin is bottom-left

    if name in known_sprite_ids:
        sp_id, int_id = known_sprite_ids[name]
    else:
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
    spriteID: 5e97eb03825dee720800000000000000
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

with open(r"Assets/UI/Chipset/Generated/chipset-atlas.png.meta", "w", encoding="utf-8") as f:
    f.write(full_meta)

print("Updated Assets/UI/Chipset/Generated/chipset-atlas.png.meta with 36 sliced sprites successfully!")
