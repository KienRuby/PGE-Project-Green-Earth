import os
import math
from PIL import Image, ImageDraw

def create_chipset_atlas():
    out_dir = r"Assets/UI/Chipset/Generated"
    os.makedirs(out_dir, exist_ok=True)
    atlas_path = os.path.join(out_dir, "chipset-atlas.png")

    cell_w, cell_h = 256, 256
    cols, rows = 5, 5
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

    # 1. Standard Gun (Pistol)
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

    # 2. Rifle (SMG / Assault Rifle)
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
    star_x, star_y = 208, 44
    d9.polygon([(star_x, star_y-18), (star_x+7, star_y-5), (star_x+18, star_y), (star_x+7, star_y+7),
                (star_x, star_y+18), (star_x-7, star_y+7), (star_x-18, star_y), (star_x-7, star_y-5)], fill=YELLOW, outline=BLACK)

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
    d10.rectangle([114, 138, 120, 156], fill=BLACK)
    d10.rectangle([126, 138, 132, 156], fill=BLACK)
    d10.rectangle([138, 138, 144, 156], fill=BLACK)

    # 11. Aiming Lens (Vortex Scope)
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

    # 14. Biochemical Mine (Hazard Armor)
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

    # 16. IC Chip Card Frame - Common (Teal)
    img16 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d16 = ImageDraw.Draw(img16)
    for px in [50, 96, 148, 194]:
        d16.rectangle([px, 8, px + 14, 38], fill=BRIGHT_CYAN, outline=BLACK, width=4)
    for px in [36, 70, 104, 138, 172, 206]:
        d16.rectangle([px, 218, px + 14, 248], fill=BRIGHT_CYAN, outline=BLACK, width=4)
    d16.rounded_rectangle([20, 32, 236, 224], radius=16, fill=DARK_TEAL, outline=BLACK, width=7)
    d16.rounded_rectangle([28, 40, 228, 216], radius=10, outline=BRIGHT_CYAN, width=5)
    d16.rectangle([28, 40, 228, 86], fill=NAVY)
    d16.rectangle([28, 168, 228, 216], fill=DARK_GREEN)

    # 17. IC Chip Card Frame - Rare (Blue)
    img17 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d17 = ImageDraw.Draw(img17)
    for px in [50, 96, 148, 194]:
        d17.rectangle([px, 8, px + 14, 38], fill=WHITE, outline=BLACK, width=4)
    for px in [36, 70, 104, 138, 172, 206]:
        d17.rectangle([px, 218, px + 14, 248], fill=WHITE, outline=BLACK, width=4)
    d17.rounded_rectangle([20, 32, 236, 224], radius=16, fill=DARK_BLUE, outline=BLACK, width=7)
    d17.rounded_rectangle([28, 40, 228, 216], radius=10, outline=BLUE, width=5)
    d17.rectangle([28, 40, 228, 86], fill=BLACK)
    d17.rectangle([28, 168, 228, 216], fill=DARK_GREEN)

    # 18. IC Chip Card Frame - Epic (Purple)
    img18 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d18 = ImageDraw.Draw(img18)
    for px in [50, 96, 148, 194]:
        d18.rectangle([px, 8, px + 14, 38], fill=PURPLE, outline=BLACK, width=4)
    for px in [36, 70, 104, 138, 172, 206]:
        d18.rectangle([px, 218, px + 14, 248], fill=PURPLE, outline=BLACK, width=4)
    d18.rounded_rectangle([20, 32, 236, 224], radius=16, fill=DARK_PURPLE, outline=BLACK, width=7)
    d18.rounded_rectangle([28, 40, 228, 216], radius=10, outline=PURPLE, width=5)
    d18.rectangle([28, 40, 228, 86], fill=BLACK)
    d18.rectangle([28, 168, 228, 216], fill=DARK_GREEN)

    # 19. IC Chip Card Frame - Legendary / Holographic
    img19 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d19 = ImageDraw.Draw(img19)
    for px in [50, 96, 148, 194]:
        d19.rectangle([px, 8, px + 14, 38], fill=YELLOW, outline=BLACK, width=4)
    for px in [36, 70, 104, 138, 172, 206]:
        d19.rectangle([px, 218, px + 14, 248], fill=YELLOW, outline=BLACK, width=4)
    for gy in range(32, 224):
        t = (gy - 32) / 192.0
        r = int(120 + 135 * math.sin(t * math.pi))
        g = int(210 + 45 * math.cos(t * math.pi))
        b = int(240 - 110 * t)
        d19.line([(20, gy), (236, gy)], fill=(r, g, b, 255))
    d19.rounded_rectangle([20, 32, 236, 224], radius=16, outline=BLACK, width=7)
    d19.rounded_rectangle([28, 40, 228, 216], radius=10, outline=WHITE, width=5)
    d19.rectangle([28, 40, 228, 86], fill=NAVY)
    d19.rectangle([28, 168, 228, 216], fill=DARK_GREEN)

    # 20. Upgrade Button Arrow (Green arrow badge with yellow border)
    img20 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d20 = ImageDraw.Draw(img20)
    d20.rounded_rectangle([60, 36, 196, 220], radius=18, fill=YELLOW, outline=BLACK, width=7)
    d20.rounded_rectangle([74, 50, 182, 206], radius=12, fill=GREEN)
    d20.polygon([(128, 68), (166, 126), (146, 126), (146, 184), (110, 184), (110, 126), (90, 126)], fill=WHITE, outline=BLACK)

    # 21. High-Tech Chipset Lock Icon
    img21 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d21 = ImageDraw.Draw(img21)
    d21.arc([88, 52, 168, 128], start=180, end=0, fill=WHITE, width=18)
    d21.rectangle([88, 86, 106, 128], fill=WHITE)
    d21.rectangle([150, 86, 168, 128], fill=WHITE)
    d21.rounded_rectangle([68, 114, 188, 208], radius=14, fill=BLACK, outline=WHITE, width=7)
    d21.ellipse([114, 134, 142, 162], fill=WHITE)
    d21.polygon([(118, 156), (138, 156), (142, 188), (114, 188)], fill=WHITE)

    # 22. Circuit Wave Line for Chipset Header
    img22 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d22 = ImageDraw.Draw(img22)
    wave_pts = [(16, 128), (64, 128), (88, 72), (112, 184), (136, 56), (160, 168), (184, 128), (240, 128)]
    d22.line(wave_pts, fill=BRIGHT_CYAN, width=10)
    for p in wave_pts:
        d22.ellipse([p[0]-8, p[1]-8, p[0]+8, p[1]+8], fill=WHITE)

    # 23. Star Icon
    img23 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d23 = ImageDraw.Draw(img23)
    sx, sy, sr_out, sr_in = 128, 128, 92, 40
    star_pts = []
    for i in range(10):
        ang = i * (math.pi / 5) - math.pi / 2
        r = sr_out if i % 2 == 0 else sr_in
        star_pts.append((sx + r * math.cos(ang), sy + r * math.sin(ang)))
    d23.polygon(star_pts, fill=YELLOW, outline=BLACK)
    d23.polygon([(p[0]*0.7 + sx*0.3, p[1]*0.7 + sy*0.3) for p in star_pts], fill=WHITE)

    # 24. Blast Furnace Icon
    img24 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d24 = ImageDraw.Draw(img24)
    d24.rectangle([50, 68, 206, 196], fill=DARK_RED, outline=RED, width=7)
    d24.rectangle([62, 80, 194, 184], outline=ORANGE, width=5)
    d24.polygon([(88, 176), (128, 98), (168, 176), (146, 172), (128, 132), (110, 172)], fill=YELLOW, outline=RED)
    d24.polygon([(106, 176), (128, 122), (150, 176)], fill=WHITE)

    # 25. High-Tech Battery
    img25 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d25 = ImageDraw.Draw(img25)
    d25.rectangle([68, 58, 98, 84], fill=WHITE, outline=BLACK, width=5)
    d25.rectangle([158, 58, 188, 84], fill=WHITE, outline=BLACK, width=5)
    d25.rounded_rectangle([42, 80, 214, 192], radius=16, fill=DARK_TEAL, outline=BLACK, width=7)
    d25.rounded_rectangle([50, 88, 206, 184], radius=10, outline=BRIGHT_CYAN, width=5)
    d25.line([(76, 132), (88, 132)], fill=WHITE, width=5)
    d25.line([(82, 126), (82, 138)], fill=WHITE, width=5)
    d25.line([(168, 132), (180, 132)], fill=WHITE, width=5)
    d25.rectangle([104, 104, 152, 168], fill=GREEN, outline=BLACK, width=3)

    images = [
        img1, img2, img3, img4, img5,
        img6, img7, img8, img9, img10,
        img11, img12, img13, img14, img15,
        img16, img17, img18, img19, img20,
        img21, img22, img23, img24, img25
    ]

    for idx, img in enumerate(images):
        col = idx % cols
        row = idx // cols
        atlas.paste(img, (col * cell_w, row * cell_h), img)

    atlas.save(atlas_path)
    print(f"Generated {atlas_path} with {len(images)} high-res sprites successfully!")

if __name__ == "__main__":
    create_chipset_atlas()
