import os
import hashlib
from PIL import Image

def generate_individual_chipset_assets():
    base_dir = "Assets/Sprites/UI/Chipset"
    icon_img_path = os.path.join(base_dir, "icon chipset.png")
    khung_img_path = os.path.join(base_dir, "khung chipset.png")
    atlas_img_path = "Assets/UI/Chipset/Generated/chipset-atlas.png"

    out_icons = os.path.join(base_dir, "Icons")
    out_frames = os.path.join(base_dir, "Frames")
    os.makedirs(out_icons, exist_ok=True)
    os.makedirs(out_frames, exist_ok=True)

    # 1. Folder metas
    write_folder_meta(out_icons)
    write_folder_meta(out_frames)

    # 2. Main 10 Real Art Icons from 'icon chipset.png'
    if os.path.exists(icon_img_path):
        icon_img = Image.open(icon_img_path).convert("RGBA")
        W, H = icon_img.size
        icons_def = [
            ("standard-gun", 1469, 1554, 230, 166),
            ("rifle", 515, 1192, 255, 134),
            ("rocket-punch", 996, 1551, 195, 146),
            ("spinning-blade", 523, 1572, 255, 118),
            ("multigun", 1460, 1964, 241, 193),
            ("gun-turret", 984, 1954, 212, 183),
            ("spiky-discus", 522, 1978, 192, 198),
            ("shotgun", 1452, 2447, 232, 95),
            ("energy-jumper-cables", 985, 2415, 171, 187),
            ("high-explosive-mine", 545, 2424, 162, 162)
        ]
        for name, ux, uy, w, h in icons_def:
            py = H - uy - h
            px = ux
            box = (max(0, px - 2), max(0, py - 2), min(W, px + w + 2), min(H, py + h + 2))
            cropped = icon_img.crop(box)
            png_path = os.path.join(out_icons, f"{name}.png")
            cropped.save(png_path)
            write_single_sprite_meta(png_path)
            print(f"Saved Real Art Icon: {png_path}")

    # 3. Additional 14 Icons from atlas if available
    all_24_keys = [
        "standard-gun", "rifle", "rocket-punch", "spinning-blade", "multigun", "gun-turret",
        "spiky-discus", "shotgun", "energy-jumper-cables", "high-explosive-mine", "aiming-lens", "plasma-field",
        "laser-eye", "biochemical-mine", "tesla-coil", "atk-module", "black-hole-mine", "sonic-boom",
        "big-battery", "turret-module", "ice-turret", "invincible-shield", "healing-turret", "flamethrower"
    ]
    if os.path.exists(atlas_img_path):
        atlas = Image.open(atlas_img_path).convert("RGBA")
        cell_size = 256
        for idx, name in enumerate(all_24_keys):
            png_path = os.path.join(out_icons, f"{name}.png")
            if not os.path.exists(png_path):
                col = idx % 6
                row = idx // 6
                x = col * cell_size
                y = row * cell_size
                cropped = atlas.crop((x, y, x + cell_size, y + cell_size))
                cropped.save(png_path)
                write_single_sprite_meta(png_path)
                print(f"Saved Distinct Icon: {png_path}")

    # 4. 5 Tier Frames from 'khung chipset.png'
    if os.path.exists(khung_img_path):
        khung_img = Image.open(khung_img_path).convert("RGBA")
        W, H = khung_img.size
        frames_def = [
            ("card-frame-tier1-green", 335, 2183, 255, 321),
            ("card-frame-tier2-blue", 335, 1747, 255, 321),
            ("card-frame-tier3-purple", 335, 1321, 255, 321),
            ("card-frame-tier4-yellow", 859, 2182, 255, 321),
            ("card-frame-tier5-red", 863, 1738, 255, 321),
            ("card-frame-wide-header", 308, 3191, 1555, 384),
            ("card-frame-wide-base", 308, 2755, 1555, 384)
        ]

        for name, ux, uy, w, h in frames_def:
            py = H - uy - h
            px = ux
            box = (max(0, px - 2), max(0, py - 2), min(W, px + w + 2), min(H, py + h + 2))
            cropped = khung_img.crop(box)
            png_path = os.path.join(out_frames, f"{name}.png")
            cropped.save(png_path)
            write_single_sprite_meta(png_path)
            print(f"Saved Frame: {png_path}")

def write_folder_meta(folder_path):
    meta_path = folder_path + ".meta"
    if os.path.exists(meta_path):
        return
    guid = hashlib.md5(folder_path.replace("\\", "/").encode("utf-8")).hexdigest()
    content = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)

def write_single_sprite_meta(png_path):
    meta_path = png_path + ".meta"
    guid = hashlib.md5(png_path.replace("\\", "/").encode("utf-8")).hexdigest()
    content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
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
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
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
  spriteSheet:
    serializedVersion: 2
    sprites: []
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)

if __name__ == "__main__":
    generate_individual_chipset_assets()
