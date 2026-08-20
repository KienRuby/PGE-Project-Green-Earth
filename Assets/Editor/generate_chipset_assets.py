import os
import math
from PIL import Image, ImageDraw

def create_chipset_atlas():
    out_dir = r"Assets/UI/Chipset/Generated"
    os.makedirs(out_dir, exist_ok=True)
    atlas_path = os.path.join(out_dir, "chipset-atlas.png")

    cell_w, cell_h = 256, 256
    cols, rows = 6, 6
    atlas = Image.new("RGBA", (cols * cell_w, rows * cell_h), (0, 0, 0, 0))

    # Palettes
    BLACK = (6, 16, 24, 255)
    NAVY = (8, 32, 48, 255)
    DARK_TEAL = (14, 52, 64, 255)
    MEDIUM_TEAL = (32, 98, 110, 255)
    BRIGHT_CYAN = (64, 218, 210, 255)
    NEON_CYAN = (130, 255, 245, 255)
    WHITE = (248, 255, 255, 255)
    GREEN = (34, 197, 94, 255)
    DARK_GREEN = (12, 85, 40, 255)
    YELLOW = (255, 203, 73, 255)
    ORANGE = (255, 130, 30, 255)
    RED = (225, 45, 45, 255)
    DARK_RED = (95, 18, 18, 255)
    PURPLE = (175, 55, 210, 255)
    DARK_PURPLE = (75, 18, 95, 255)
    BLUE = (38, 115, 225, 255)
    DARK_BLUE = (16, 50, 125, 255)
    GOLD = (235, 165, 30, 255)
    GREY = (150, 175, 190, 255)
    ICE_BLUE = (180, 235, 255, 255)

    # 1. Standard Gun
    img1 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d1 = ImageDraw.Draw(img1)
    d1.rectangle([54, 76, 192, 122], fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d1.rectangle([54, 76, 110, 102], fill=WHITE)
    d1.rectangle([38, 86, 54, 112], fill=GREY, outline=BLACK, width=5)
    d1.polygon([(140, 122), (180, 122), (160, 196), (120, 196)], fill=DARK_TEAL, outline=BLACK)
    d1.line([(138, 142), (170, 142)], fill=BRIGHT_CYAN, width=5)
    d1.line([(132, 162), (164, 162)], fill=BRIGHT_CYAN, width=5)
    d1.line([(126, 182), (158, 182)], fill=BRIGHT_CYAN, width=5)
    d1.rectangle([(102, 122), (140, 156)], outline=BLACK, width=5)
    d1.line([(120, 126), (114, 144)], fill=WHITE, width=4)
    d1.line([(60, 82), (186, 82)], fill=WHITE, width=4)

    # 2. Rifle
    img2 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d2 = ImageDraw.Draw(img2)
    d2.rectangle([48, 92, 204, 134], fill=BLACK, outline=BLACK, width=5)
    d2.rectangle([54, 98, 198, 128], fill=BRIGHT_CYAN)
    for vx in range(70, 170, 18):
        d2.rectangle([vx, 104, vx + 10, 124], fill=BLACK)
    d2.rectangle([24, 102, 48, 124], fill=WHITE, outline=BLACK, width=5)
    d2.rectangle([12, 98, 24, 128], fill=GREY, outline=BLACK, width=5)
    d2.rectangle([98, 72, 164, 94], fill=NAVY, outline=BLACK, width=5)
    d2.rectangle([106, 78, 124, 88], fill=WHITE)
    d2.polygon([(204, 98), (242, 112), (242, 154), (204, 134)], fill=BLACK, outline=BLACK)
    d2.polygon([(112, 134), (134, 134), (122, 182), (100, 182)], fill=BLACK, outline=BLACK)
    d2.polygon([(160, 134), (184, 134), (172, 178), (148, 178)], fill=DARK_TEAL, outline=BLACK)

    # 3. Rocket Punch
    img3 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d3 = ImageDraw.Draw(img3)
    d3.rectangle([40, 94, 116, 168], fill=DARK_TEAL, outline=BLACK, width=5)
    d3.polygon([(18, 102), (40, 94), (40, 168), (18, 160)], fill=GREY, outline=BLACK)
    d3.polygon([(6, 114), (18, 106), (18, 156), (6, 148)], fill=YELLOW)
    d3.rounded_rectangle([112, 80, 226, 180], radius=22, fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d3.rectangle([178, 86, 214, 110], fill=WHITE, outline=BLACK, width=5)
    d3.rectangle([182, 116, 218, 140], fill=WHITE, outline=BLACK, width=5)
    d3.rectangle([178, 146, 214, 170], fill=WHITE, outline=BLACK, width=5)
    d3.rectangle([132, 146, 170, 176], fill=DARK_TEAL, outline=BLACK, width=5)

    # 4. Spinning Blade
    img4 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d4 = ImageDraw.Draw(img4)
    d4.polygon([(30, 154), (72, 84), (188, 72), (242, 136), (146, 178), (52, 170)], fill=BRIGHT_CYAN, outline=BLACK)
    d4.line([(72, 84), (188, 72), (242, 136)], fill=WHITE, width=8)
    d4.ellipse([88, 104, 154, 156], fill=WHITE, outline=BLACK, width=5)
    d4.ellipse([102, 118, 140, 142], fill=DARK_PURPLE)
    d4.polygon([(122, 140), (204, 106), (228, 136), (150, 164)], fill=PURPLE, outline=BLACK)

    # 5. Multigun
    img5 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d5 = ImageDraw.Draw(img5)
    d5.rectangle([44, 84, 116, 168], fill=DARK_TEAL, outline=BLACK, width=5)
    d5.rectangle([30, 58, 64, 88], fill=GREY, outline=BLACK, width=5)
    d5.ellipse([58, 156, 108, 204], fill=NAVY, outline=BLACK, width=5)
    for by in [88, 106, 124, 142]:
        d5.rectangle([116, by, 214, by + 12], fill=BRIGHT_CYAN, outline=BLACK, width=3)
    d5.rectangle([152, 84, 166, 162], fill=WHITE, outline=BLACK, width=5)
    d5.rectangle([200, 84, 214, 162], fill=GREY, outline=BLACK, width=5)
    d5.rectangle([214, 86, 228, 160], fill=WHITE)

    # 6. Gun Turret
    img6 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d6 = ImageDraw.Draw(img6)
    d6.polygon([(40, 186), (216, 186), (192, 152), (64, 152)], fill=DARK_TEAL, outline=BLACK)
    d6.line([(56, 176), (200, 176)], fill=BRIGHT_CYAN, width=5)
    d6.ellipse([72, 104, 184, 160], fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d6.ellipse([94, 116, 162, 150], fill=WHITE)
    d6.rectangle([102, 58, 126, 114], fill=NAVY, outline=BLACK, width=5)
    d6.rectangle([138, 58, 162, 114], fill=NAVY, outline=BLACK, width=5)
    d6.rectangle([98, 48, 130, 62], fill=WHITE, outline=BLACK, width=5)
    d6.rectangle([134, 48, 166, 62], fill=WHITE, outline=BLACK, width=5)

    # 7. Spiky Discus
    img7 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d7 = ImageDraw.Draw(img7)
    cx, cy, r_out, r_in = 128, 128, 106, 46
    points = []
    for i in range(16):
        ang = i * (math.pi / 8) - math.pi / 2
        r = r_out if i % 2 == 0 else r_in
        points.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    d7.polygon(points, fill=BRIGHT_CYAN, outline=BLACK)
    d7.ellipse([cx - 36, cy - 36, cx + 36, cy + 36], fill=WHITE, outline=BLACK, width=5)
    d7.ellipse([cx - 18, cy - 18, cx + 18, cy + 18], fill=DARK_TEAL)
    for p in points[::2]:
        d7.line([(cx, cy), p], fill=WHITE, width=3)

    # 8. Shotgun
    img8 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d8 = ImageDraw.Draw(img8)
    d8.rectangle([28, 106, 172, 132], fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d8.rectangle([18, 104, 32, 134], fill=GREY, outline=BLACK, width=5)
    d8.rectangle([64, 128, 114, 150], fill=NAVY, outline=BLACK, width=5)
    for px in range(72, 110, 9):
        d8.line([(px, 130), (px, 148)], fill=WHITE, width=3)
    d8.rectangle([142, 102, 194, 140], fill=DARK_TEAL, outline=BLACK, width=5)
    d8.polygon([(194, 106), (242, 124), (236, 164), (194, 140)], fill=DARK_BLUE, outline=BLACK)
    d8.line([(38, 110), (164, 110)], fill=WHITE, width=5)

    # 9. Energy Jumper Cables
    img9 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d9 = ImageDraw.Draw(img9)
    d9.rectangle([56, 108, 92, 174], fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d9.polygon([(56, 108), (92, 108), (82, 76), (66, 76)], fill=WHITE, outline=BLACK)
    d9.rectangle([164, 108, 200, 174], fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d9.polygon([(164, 108), (200, 108), (190, 76), (174, 76)], fill=WHITE, outline=BLACK)
    d9.line([(74, 174), (74, 204), (110, 204), (110, 174)], fill=NAVY, width=8)
    d9.line([(182, 174), (182, 204), (146, 204), (146, 174)], fill=NAVY, width=8)
    spark = [(74, 76), (106, 46), (120, 76), (148, 44), (182, 76)]
    d9.line(spark, fill=YELLOW, width=8)

    # 10. High-Explosive Mine
    img10 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d10 = ImageDraw.Draw(img10)
    d10.ellipse([46, 46, 210, 210], fill=DARK_TEAL, outline=BLACK, width=7)
    for ang in range(0, 360, 45):
        rad = math.radians(ang)
        sx = 128 + 84 * math.cos(rad)
        sy = 128 + 84 * math.sin(rad)
        d10.ellipse([sx - 12, sy - 12, sx + 12, sy + 12], fill=BRIGHT_CYAN, outline=BLACK, width=3)
    d10.rectangle([92, 94, 164, 134], fill=WHITE, outline=BLACK, width=5)
    d10.rectangle([106, 134, 150, 156], fill=WHITE, outline=BLACK, width=5)
    d10.ellipse([104, 104, 120, 120], fill=BLACK)
    d10.ellipse([136, 104, 152, 120], fill=BLACK)

    # 11. Aiming Lens
    img11 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d11 = ImageDraw.Draw(img11)
    d11.ellipse([36, 36, 220, 220], fill=DARK_TEAL, outline=BLACK, width=7)
    d11.ellipse([54, 54, 202, 202], fill=NAVY, outline=BRIGHT_CYAN, width=5)
    for i in range(8):
        ang = i * (math.pi / 4)
        x0 = 128 + 34 * math.cos(ang)
        y0 = 128 + 34 * math.sin(ang)
        x1 = 128 + 74 * math.cos(ang + 0.6)
        y1 = 128 + 74 * math.sin(ang + 0.6)
        d11.line([(x0, y0), (x1, y1)], fill=BRIGHT_CYAN, width=8)
    d11.ellipse([110, 110, 146, 146], fill=WHITE, outline=RED, width=5)
    d11.ellipse([120, 120, 136, 136], fill=RED)

    # 12. Plasma Field
    img12 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d12 = ImageDraw.Draw(img12)
    d12.rectangle([104, 68, 152, 184], fill=WHITE, outline=BLACK, width=5)
    d12.ellipse([108, 44, 148, 80], fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d12.rectangle([32, 92, 94, 158], fill=DARK_TEAL, outline=BLACK, width=5)
    d12.rectangle([162, 92, 224, 158], fill=DARK_TEAL, outline=BLACK, width=5)
    for wy in range(104, 156, 15):
        d12.line([(32, wy), (94, wy)], fill=BRIGHT_CYAN, width=3)
        d12.line([(162, wy), (224, wy)], fill=BRIGHT_CYAN, width=3)
    d12.arc([24, 24, 232, 232], start=0, end=360, fill=BRIGHT_CYAN, width=5)

    # 13. Laser Eye
    img13 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d13 = ImageDraw.Draw(img13)
    d13.rectangle([40, 122, 192, 150], fill=DARK_TEAL, outline=BLACK, width=5)
    d13.rectangle([164, 128, 240, 142], fill=GREY, outline=BLACK, width=5)
    d13.rectangle([230, 124, 246, 146], fill=WHITE, outline=BLACK, width=5)
    d13.ellipse([64, 64, 148, 128], fill=BRIGHT_CYAN, outline=BLACK, width=7)
    d13.ellipse([84, 76, 128, 116], fill=WHITE, outline=NAVY, width=5)
    d13.ellipse([98, 86, 114, 106], fill=RED)
    d13.polygon([(40, 130), (14, 156), (14, 184), (50, 168)], fill=NAVY, outline=BLACK)

    # 14. Biochemical Mine
    img14 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d14 = ImageDraw.Draw(img14)
    d14.polygon([(62, 52), (96, 52), (104, 84), (152, 84), (160, 52), (194, 52),
                 (212, 104), (200, 192), (128, 206), (56, 192), (44, 104)], fill=DARK_TEAL, outline=BLACK)
    d14.polygon([(62, 52), (96, 52), (84, 90), (50, 90)], fill=WHITE, outline=BLACK)
    d14.polygon([(160, 52), (194, 52), (206, 90), (172, 90)], fill=WHITE, outline=BLACK)
    d14.rectangle([92, 104, 164, 176], fill=NAVY, outline=BLACK, width=5)
    d14.rectangle([104, 114, 152, 134], fill=GREEN, outline=BLACK, width=3)
    d14.rectangle([104, 144, 152, 164], fill=GREEN, outline=BLACK, width=3)
    d14.line([(56, 192), (128, 206), (200, 192)], fill=BRIGHT_CYAN, width=5)

    # 15. Tesla Coil
    img15 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d15 = ImageDraw.Draw(img15)
    d15.polygon([(52, 200), (204, 200), (174, 168), (82, 168)], fill=DARK_TEAL, outline=BLACK)
    d15.line([(68, 186), (188, 186)], fill=BRIGHT_CYAN, width=5)
    d15.rectangle([110, 80, 146, 168], fill=NAVY, outline=BLACK, width=5)
    for cy_pos in range(90, 162, 13):
        d15.line([(100, cy_pos), (156, cy_pos)], fill=GOLD, width=7)
    d15.ellipse([74, 42, 182, 88], fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d15.ellipse([94, 50, 162, 80], fill=WHITE)
    d15.line([(74, 54), (38, 38), (56, 20)], fill=WHITE, width=5)
    d15.line([(182, 54), (218, 38), (200, 20)], fill=WHITE, width=5)
    d15.line([(128, 42), (128, 14), (150, 6)], fill=YELLOW, width=5)

    # 16. ATK Module (Red Power Matrix)
    img16 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d16 = ImageDraw.Draw(img16)
    d16.rounded_rectangle([44, 44, 212, 212], radius=16, fill=DARK_RED, outline=RED, width=6)
    d16.rounded_rectangle([58, 58, 198, 198], radius=10, fill=NAVY, outline=ORANGE, width=4)
    # Crossed laser swords / ATK symbol
    d16.polygon([(70, 70), (94, 70), (186, 186), (162, 186)], fill=YELLOW)
    d16.polygon([(186, 70), (162, 70), (70, 186), (94, 186)], fill=YELLOW)
    d16.ellipse([104, 104, 152, 152], fill=RED, outline=WHITE, width=4)
    d16.ellipse([116, 116, 140, 140], fill=WHITE)

    # 17. Black Hole Mine (Gravitational Vortex)
    img17 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d17 = ImageDraw.Draw(img17)
    d17.ellipse([32, 32, 224, 224], fill=BLACK, outline=DARK_PURPLE, width=8)
    for i in range(12):
        ang = i * (math.pi / 6)
        x0 = 128 + 40 * math.cos(ang)
        y0 = 128 + 40 * math.sin(ang)
        x1 = 128 + 92 * math.cos(ang + 0.9)
        y1 = 128 + 92 * math.sin(ang + 0.9)
        d17.line([(x0, y0), (x1, y1)], fill=PURPLE, width=6)
    d17.ellipse([92, 92, 164, 164], fill=BLACK, outline=NEON_CYAN, width=5)
    d17.ellipse([110, 110, 146, 146], fill=NAVY)

    # 18. Sonic Boom (Shockwave Cannon)
    img18 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d18 = ImageDraw.Draw(img18)
    d18.rectangle([36, 92, 110, 164], fill=DARK_TEAL, outline=BLACK, width=5)
    d18.polygon([(110, 80), (220, 40), (220, 216), (110, 176)], fill=NAVY, outline=BRIGHT_CYAN, width=6)
    for sx in [130, 160, 190]:
        d18.line([(sx, 64 + (sx-110)*0.2), (sx, 192 - (sx-110)*0.2)], fill=WHITE, width=4)
    d18.ellipse([200, 56, 238, 200], fill=BRIGHT_CYAN, outline=BLACK, width=4)
    d18.arc([16, 24, 240, 232], start=-45, end=45, fill=YELLOW, width=6)

    # 19. Big Battery (Energy Cell)
    img19 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d19 = ImageDraw.Draw(img19)
    d19.rectangle([96, 36, 160, 64], fill=GREY, outline=BLACK, width=5)
    d19.rounded_rectangle([52, 64, 204, 224], radius=16, fill=DARK_GREEN, outline=GREEN, width=7)
    d19.rectangle([68, 80, 188, 116], fill=GREEN)
    d19.rectangle([68, 126, 188, 162], fill=GREEN)
    d19.rectangle([68, 172, 188, 208], fill=GREEN)
    # Plus sign
    d19.line([(128, 128), (128, 160)], fill=WHITE, width=6)
    d19.line([(112, 144), (144, 144)], fill=WHITE, width=6)

    # 20. Turret Module (Support Defense Circuit)
    img20 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d20 = ImageDraw.Draw(img20)
    d20.polygon([(128, 24), (228, 80), (228, 180), (128, 236), (28, 180), (28, 80)], fill=DARK_TEAL, outline=BRIGHT_CYAN, width=6)
    d20.rectangle([88, 88, 168, 168], fill=NAVY, outline=YELLOW, width=5)
    d20.line([(128, 36), (128, 88)], fill=BRIGHT_CYAN, width=5)
    d20.line([(128, 168), (128, 224)], fill=BRIGHT_CYAN, width=5)
    d20.line([(38, 128), (88, 128)], fill=BRIGHT_CYAN, width=5)
    d20.line([(168, 128), (218, 128)], fill=BRIGHT_CYAN, width=5)
    d20.ellipse([110, 110, 146, 146], fill=YELLOW, outline=WHITE, width=3)

    # 21. Ice Turret (Cryo Cannon)
    img21 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d21 = ImageDraw.Draw(img21)
    d21.polygon([(40, 192), (216, 192), (184, 156), (72, 156)], fill=DARK_BLUE, outline=BLACK, width=5)
    d21.ellipse([76, 104, 180, 160], fill=BLUE, outline=ICE_BLUE, width=5)
    # Ice crystal barrel
    d21.polygon([(112, 104), (128, 36), (144, 104)], fill=ICE_BLUE, outline=WHITE, width=4)
    d21.polygon([(88, 104), (104, 52), (120, 104)], fill=ICE_BLUE, outline=WHITE, width=3)
    d21.polygon([(136, 104), (152, 52), (168, 104)], fill=ICE_BLUE, outline=WHITE, width=3)
    d21.ellipse([114, 120, 142, 148], fill=WHITE)

    # 22. Invincible Shield (Aegis Forcefield)
    img22 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d22 = ImageDraw.Draw(img22)
    d22.polygon([(128, 28), (220, 56), (200, 174), (128, 232), (56, 174), (36, 56)], fill=GOLD, outline=WHITE, width=6)
    d22.polygon([(128, 48), (202, 72), (184, 162), (128, 214), (72, 162), (54, 72)], fill=NAVY, outline=YELLOW, width=5)
    d22.polygon([(128, 68), (176, 88), (164, 148), (128, 188), (92, 148), (80, 88)], fill=YELLOW)
    d22.ellipse([112, 112, 144, 144], fill=WHITE)

    # 23. Healing Turret (Medical Nanite Beacon)
    img23 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d23 = ImageDraw.Draw(img23)
    d23.polygon([(40, 192), (216, 192), (184, 156), (72, 156)], fill=DARK_GREEN, outline=BLACK, width=5)
    d23.ellipse([76, 104, 180, 160], fill=GREEN, outline=WHITE, width=5)
    d23.rectangle([112, 48, 144, 112], fill=WHITE, outline=BLACK, width=4)
    # Green cross on turret
    d23.rectangle([118, 114, 138, 150], fill=WHITE)
    d23.rectangle([110, 124, 146, 140], fill=WHITE)
    d23.rectangle([122, 118, 134, 146], fill=GREEN)
    d23.rectangle([114, 128, 142, 136], fill=GREEN)
    d23.ellipse([118, 34, 138, 54], fill=GREEN, outline=WHITE, width=3)

    # 24. Flamethrower (Inferno Nozzle)
    img24 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d24 = ImageDraw.Draw(img24)
    d24.rectangle([34, 112, 138, 156], fill=DARK_RED, outline=BLACK, width=5)
    d24.rectangle([138, 104, 188, 164], fill=ORANGE, outline=BLACK, width=5)
    d24.polygon([(188, 92), (236, 72), (236, 196), (188, 176)], fill=DARK_TEAL, outline=BLACK, width=5)
    # Flame plume
    d24.polygon([(216, 134), (248, 98), (242, 134), (254, 120), (248, 144), (240, 140), (246, 168), (216, 134)], fill=YELLOW, outline=RED)
    # Fuel tank below
    d24.rounded_rectangle([48, 156, 124, 216], radius=12, fill=YELLOW, outline=BLACK, width=4)

    # 25. Card Frame - Common (Teal / Magic)
    img25 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d25 = ImageDraw.Draw(img25)
    for px in [50, 96, 148, 194]:
        d25.rectangle([px, 8, px + 14, 38], fill=BRIGHT_CYAN, outline=BLACK, width=4)
    for px in [36, 70, 104, 138, 172, 206]:
        d25.rectangle([px, 218, px + 14, 248], fill=BRIGHT_CYAN, outline=BLACK, width=4)
    d25.rounded_rectangle([20, 32, 236, 224], radius=16, fill=DARK_TEAL, outline=BLACK, width=7)
    d25.rounded_rectangle([28, 40, 228, 216], radius=10, outline=BRIGHT_CYAN, width=5)
    d25.rectangle([28, 40, 228, 86], fill=NAVY)
    d25.rectangle([28, 168, 228, 216], fill=DARK_GREEN)

    # 26. Card Frame - Rare (Blue)
    img26 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d26 = ImageDraw.Draw(img26)
    for px in [50, 96, 148, 194]:
        d26.rectangle([px, 8, px + 14, 38], fill=WHITE, outline=BLACK, width=4)
    for px in [36, 70, 104, 138, 172, 206]:
        d26.rectangle([px, 218, px + 14, 248], fill=WHITE, outline=BLACK, width=4)
    d26.rounded_rectangle([20, 32, 236, 224], radius=16, fill=DARK_BLUE, outline=BLACK, width=7)
    d26.rounded_rectangle([28, 40, 228, 216], radius=10, outline=BLUE, width=5)
    d26.rectangle([28, 40, 228, 86], fill=BLACK)
    d26.rectangle([28, 168, 228, 216], fill=DARK_GREEN)

    # 27. Card Frame - Epic (Purple)
    img27 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d27 = ImageDraw.Draw(img27)
    for px in [50, 96, 148, 194]:
        d27.rectangle([px, 8, px + 14, 38], fill=PURPLE, outline=BLACK, width=4)
    for px in [36, 70, 104, 138, 172, 206]:
        d27.rectangle([px, 218, px + 14, 248], fill=PURPLE, outline=BLACK, width=4)
    d27.rounded_rectangle([20, 32, 236, 224], radius=16, fill=DARK_PURPLE, outline=BLACK, width=7)
    d27.rounded_rectangle([28, 40, 228, 216], radius=10, outline=PURPLE, width=5)
    d27.rectangle([28, 40, 228, 86], fill=BLACK)
    d27.rectangle([28, 168, 228, 216], fill=DARK_GREEN)

    # 28. Card Frame - Holographic / Advance (Prismatic / Gold)
    img28 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d28 = ImageDraw.Draw(img28)
    for px in [50, 96, 148, 194]:
        d28.rectangle([px, 8, px + 14, 38], fill=YELLOW, outline=BLACK, width=4)
    for px in [36, 70, 104, 138, 172, 206]:
        d28.rectangle([px, 218, px + 14, 248], fill=YELLOW, outline=BLACK, width=4)
    for gy in range(32, 224):
        t = (gy - 32) / 192.0
        r = int(120 + 135 * math.sin(t * math.pi))
        g = int(210 + 45 * math.cos(t * math.pi))
        b = int(240 - 110 * t)
        d28.line([(20, gy), (236, gy)], fill=(r, g, b, 255))
    d28.rounded_rectangle([20, 32, 236, 224], radius=16, outline=BLACK, width=7)
    d28.rounded_rectangle([28, 40, 228, 216], radius=10, outline=WHITE, width=5)
    d28.rectangle([28, 40, 228, 86], fill=NAVY)
    d28.rectangle([28, 168, 228, 216], fill=DARK_GREEN)

    # 29. Upgrade Button Arrow
    img29 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d29 = ImageDraw.Draw(img29)
    d29.rounded_rectangle([60, 36, 196, 220], radius=18, fill=YELLOW, outline=BLACK, width=7)
    d29.rounded_rectangle([74, 50, 182, 206], radius=12, fill=GREEN)
    d29.polygon([(128, 68), (166, 126), (146, 126), (146, 184), (110, 184), (110, 126), (90, 126)], fill=WHITE, outline=BLACK)

    # 30. Lock Icon
    img30 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d30 = ImageDraw.Draw(img30)
    d30.arc([88, 52, 168, 128], start=180, end=0, fill=WHITE, width=18)
    d30.rectangle([88, 86, 106, 128], fill=WHITE)
    d30.rectangle([150, 86, 168, 128], fill=WHITE)
    d30.rounded_rectangle([68, 114, 188, 208], radius=14, fill=BLACK, outline=WHITE, width=7)
    d30.ellipse([114, 134, 142, 162], fill=WHITE)
    d30.polygon([(118, 156), (138, 156), (142, 188), (114, 188)], fill=WHITE)

    # 31. Circuit Wave Line
    img31 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d31 = ImageDraw.Draw(img31)
    wave_pts = [(16, 128), (64, 128), (88, 72), (112, 184), (136, 56), (160, 168), (184, 128), (240, 128)]
    d31.line(wave_pts, fill=BRIGHT_CYAN, width=10)
    for p in wave_pts:
        d31.ellipse([p[0]-8, p[1]-8, p[0]+8, p[1]+8], fill=WHITE)

    # 32. Star Icon
    img32 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d32 = ImageDraw.Draw(img32)
    sx, sy, sr_out, sr_in = 128, 128, 92, 40
    star_pts = []
    for i in range(10):
        ang = i * (math.pi / 5) - math.pi / 2
        r = sr_out if i % 2 == 0 else sr_in
        star_pts.append((sx + r * math.cos(ang), sy + r * math.sin(ang)))
    d32.polygon(star_pts, fill=YELLOW, outline=BLACK)
    d32.polygon([(p[0]*0.7 + sx*0.3, p[1]*0.7 + sy*0.3) for p in star_pts], fill=WHITE)

    # 33. Furnace Icon
    img33 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d33 = ImageDraw.Draw(img33)
    d33.rectangle([50, 68, 206, 196], fill=DARK_RED, outline=RED, width=7)
    d33.rectangle([62, 80, 194, 184], outline=ORANGE, width=5)
    d33.polygon([(88, 176), (128, 98), (168, 176), (146, 172), (128, 132), (110, 172)], fill=YELLOW, outline=RED)
    d33.polygon([(106, 176), (128, 122), (150, 176)], fill=WHITE)

    # 34. Power Battery
    img34 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d34 = ImageDraw.Draw(img34)
    d34.rectangle([68, 58, 98, 84], fill=WHITE, outline=BLACK, width=5)
    d34.rectangle([158, 58, 188, 84], fill=WHITE, outline=BLACK, width=5)
    d34.rounded_rectangle([42, 80, 214, 192], radius=16, fill=DARK_TEAL, outline=BLACK, width=7)
    d34.rounded_rectangle([50, 88, 206, 184], radius=10, outline=BRIGHT_CYAN, width=5)
    d34.line([(76, 132), (88, 132)], fill=WHITE, width=5)
    d34.line([(82, 126), (82, 138)], fill=WHITE, width=5)
    d34.line([(168, 132), (180, 132)], fill=WHITE, width=5)
    d34.rectangle([104, 104, 152, 168], fill=GREEN, outline=BLACK, width=3)

    # 35. Advance Stone (Glowing Evolution Core Crystal)
    img35 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d35 = ImageDraw.Draw(img35)
    # Diamond Octagon Shape
    d35.polygon([(128, 22), (206, 68), (224, 152), (128, 234), (32, 152), (50, 68)], fill=DARK_PURPLE, outline=GOLD, width=7)
    d35.polygon([(128, 38), (192, 78), (206, 146), (128, 216), (50, 146), (64, 78)], fill=PURPLE, outline=YELLOW, width=4)
    d35.polygon([(128, 56), (174, 90), (184, 140), (128, 196), (72, 140), (82, 90)], fill=NEON_CYAN)
    d35.polygon([(128, 76), (156, 102), (162, 134), (128, 172), (94, 134), (100, 102)], fill=WHITE)

    # 36. Advance Badge (Breakthrough Wings / Crown)
    img36 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d36 = ImageDraw.Draw(img36)
    d36.rounded_rectangle([44, 44, 212, 212], radius=24, fill=DARK_TEAL, outline=GOLD, width=7)
    # Wings
    d36.polygon([(128, 60), (196, 104), (170, 156), (128, 130), (86, 156), (60, 104)], fill=GOLD, outline=WHITE, width=4)
    # Crown upward spikes
    d36.polygon([(128, 50), (146, 92), (128, 82), (110, 92)], fill=WHITE)
    d36.polygon([(80, 90), (62, 60), (100, 84)], fill=YELLOW)
    d36.polygon([(176, 90), (194, 60), (156, 84)], fill=YELLOW)
    d36.ellipse([110, 134, 146, 170], fill=RED, outline=WHITE, width=4)

    images = [
        img1, img2, img3, img4, img5, img6,
        img7, img8, img9, img10, img11, img12,
        img13, img14, img15, img16, img17, img18,
        img19, img20, img21, img22, img23, img24,
        img25, img26, img27, img28, img29, img30,
        img31, img32, img33, img34, img35, img36
    ]

    for idx, img in enumerate(images):
        col = idx % cols
        row = idx // cols
        atlas.paste(img, (col * cell_w, row * cell_h), img)

    atlas.save(atlas_path)
    print(f"Generated {atlas_path} with {len(images)} high-res sprites in {cols}x{rows} grid successfully!")

if __name__ == "__main__":
    create_chipset_atlas()
