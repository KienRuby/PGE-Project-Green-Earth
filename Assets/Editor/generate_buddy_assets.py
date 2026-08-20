import os
import math
from PIL import Image, ImageDraw

def create_buddy_atlas():
    out_dir = r"Assets/UI/Buddy/Generated"
    os.makedirs(out_dir, exist_ok=True)
    atlas_path = os.path.join(out_dir, "buddy-atlas.png")

    cell_w, cell_h = 256, 256
    cols, rows = 6, 5
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
    LIME_GREEN = (132, 204, 22, 255)
    DARK_LIME = (77, 124, 15, 255)
    YELLOW = (255, 203, 73, 255)
    ORANGE = (255, 130, 30, 255)
    RED = (225, 45, 45, 255)
    PURPLE = (175, 55, 210, 255)
    DARK_PURPLE = (75, 18, 95, 255)
    BLUE = (38, 115, 225, 255)
    DARK_BLUE = (16, 50, 125, 255)
    PINK = (244, 63, 94, 255)
    DARK_PINK = (159, 18, 57, 255)

    # 1. Drone Snowflake (Frost Sentinel)
    img1 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d1 = ImageDraw.Draw(img1)
    cx, cy = 128, 128
    for i in range(6):
        ang = i * (math.pi / 3)
        x1 = cx + 85 * math.cos(ang)
        y1 = cy + 85 * math.sin(ang)
        d1.line([(cx, cy), (x1, y1)], fill=BRIGHT_CYAN, width=8)
        p1x = cx + 55 * math.cos(ang) + 20 * math.cos(ang + math.pi/3)
        p1y = cy + 55 * math.sin(ang) + 20 * math.sin(ang + math.pi/3)
        p2x = cx + 55 * math.cos(ang) + 20 * math.cos(ang - math.pi/3)
        p2y = cy + 55 * math.sin(ang) + 20 * math.sin(ang - math.pi/3)
        d1.line([(cx + 55 * math.cos(ang), cy + 55 * math.sin(ang)), (p1x, p1y)], fill=BRIGHT_CYAN, width=5)
        d1.line([(cx + 55 * math.cos(ang), cy + 55 * math.sin(ang)), (p2x, p2y)], fill=BRIGHT_CYAN, width=5)
        d1.ellipse([x1-7, y1-7, x1+7, y1+7], fill=WHITE, outline=BLACK, width=2)
    hex_pts = [(cx + 42 * math.cos(i*math.pi/3), cy + 42 * math.sin(i*math.pi/3)) for i in range(6)]
    d1.polygon(hex_pts, fill=DARK_TEAL, outline=BRIGHT_CYAN, width=4)
    d1.ellipse([cx-22, cy-22, cx+22, cy+22], fill=WHITE, outline=NAVY, width=4)
    d1.ellipse([cx-12, cy-12, cx+12, cy+12], fill=BRIGHT_CYAN)

    # 2. Drone Spider (Turret Buffer)
    img2 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d2 = ImageDraw.Draw(img2)
    d2.rounded_rectangle([78, 64, 178, 150], radius=16, fill=BRIGHT_CYAN, outline=BLACK, width=6)
    d2.rectangle([94, 80, 162, 108], fill=NAVY, outline=BLACK, width=3)
    d2.ellipse([106, 88, 122, 102], fill=WHITE)
    d2.ellipse([134, 88, 150, 102], fill=WHITE)
    d2.line([(100, 64), (92, 34)], fill=BLACK, width=6)
    d2.ellipse([84, 26, 100, 42], fill=WHITE, outline=BLACK, width=3)
    for lx in [92, 116, 140, 164]:
        d2.line([(lx, 150), (lx - 12 + (lx-128)*0.2, 198)], fill=DARK_TEAL, width=7)
        d2.ellipse([lx - 18 + (lx-128)*0.2, 192, lx - 6 + (lx-128)*0.2, 204], fill=BLACK)

    # 3. Drone Antenna Eye (Radar Eye)
    img3 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d3 = ImageDraw.Draw(img3)
    d3.line([(96, 68), (86, 32)], fill=BLACK, width=6)
    d3.line([(160, 68), (170, 32)], fill=BLACK, width=6)
    d3.arc([108, 18, 148, 58], start=0, end=360, fill=BLACK, width=5)
    d3.ellipse([54, 54, 202, 202], fill=DARK_TEAL, outline=BLACK, width=6)
    d3.ellipse([70, 70, 186, 186], fill=BRIGHT_CYAN)
    d3.ellipse([88, 88, 168, 168], fill=NAVY, outline=BLACK, width=4)
    d3.ellipse([104, 104, 152, 152], fill=WHITE)
    d3.ellipse([118, 118, 138, 138], fill=BLACK)

    # 4. Drone Cross Visor (Assault Blaster)
    img4 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d4 = ImageDraw.Draw(img4)
    d4.line([(90, 60), (74, 30)], fill=BLACK, width=6)
    d4.ellipse([66, 22, 82, 38], fill=WHITE, outline=BLACK, width=3)
    d4.line([(166, 60), (182, 30)], fill=BLACK, width=6)
    d4.ellipse([174, 22, 190, 38], fill=WHITE, outline=BLACK, width=3)
    d4.ellipse([56, 52, 200, 196], fill=BRIGHT_CYAN, outline=BLACK, width=6)
    d4.line([(88, 110), (116, 110)], fill=WHITE, width=6)
    d4.line([(102, 96), (102, 124)], fill=WHITE, width=6)
    d4.line([(140, 110), (168, 110)], fill=WHITE, width=6)
    d4.line([(154, 96), (154, 124)], fill=WHITE, width=6)
    d4.line([(106, 156), (150, 156)], fill=NAVY, width=5)

    # 5. Drone Capsule (Nano Healer)
    img5 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d5 = ImageDraw.Draw(img5)
    d5.line([(100, 60), (84, 28)], fill=BLACK, width=6)
    d5.ellipse([76, 20, 92, 36], fill=WHITE, outline=BLACK, width=3)
    d5.ellipse([54, 54, 202, 202], fill=WHITE, outline=BLACK, width=6)
    d5.ellipse([70, 70, 186, 186], fill=BRIGHT_CYAN, outline=NAVY, width=4)
    d5.ellipse([112, 94, 168, 150], fill=NAVY, outline=BLACK, width=4)
    d5.ellipse([126, 108, 154, 136], fill=WHITE)
    d5.ellipse([136, 118, 146, 128], fill=BLACK)

    # 6. Drone Spiky Mine (Mine Layer)
    img6 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d6 = ImageDraw.Draw(img6)
    for i in range(8):
        ang = i * (math.pi / 4)
        sp_x = 128 + 84 * math.cos(ang)
        sp_y = 128 + 84 * math.sin(ang)
        d6.polygon([(128 + 48*math.cos(ang-0.3), 128 + 48*math.sin(ang-0.3)),
                    (sp_x, sp_y),
                    (128 + 48*math.cos(ang+0.3), 128 + 48*math.sin(ang+0.3))], fill=DARK_TEAL, outline=BLACK)
    d6.ellipse([72, 72, 184, 184], fill=BLACK, outline=BRIGHT_CYAN, width=6)
    d6.ellipse([98, 98, 158, 158], fill=BRIGHT_CYAN, outline=WHITE, width=4)
    d6.ellipse([114, 114, 142, 142], fill=NAVY)

    # 7. Drone Octagon Shield (Aegis Defender)
    img7 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d7 = ImageDraw.Draw(img7)
    for i in range(8):
        ang = i * (math.pi / 4)
        x0 = 128 + 92 * math.cos(ang)
        y0 = 128 + 92 * math.sin(ang)
        d7.line([(128, 128), (x0, y0)], fill=BRIGHT_CYAN, width=6)
        d7.rectangle([x0-10, y0-10, x0+10, y0+10], fill=WHITE, outline=BLACK, width=3)
    oct_pts = [(128 + 64*math.cos(i*math.pi/4), 128 + 64*math.sin(i*math.pi/4)) for i in range(8)]
    d7.polygon(oct_pts, fill=DARK_TEAL, outline=BRIGHT_CYAN, width=5)
    d7.ellipse([96, 96, 160, 160], fill=NAVY, outline=WHITE, width=4)
    d7.ellipse([112, 112, 144, 144], fill=BRIGHT_CYAN)

    # 8. Drone Claw Magnet (Scavenger Unit)
    img8 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d8 = ImageDraw.Draw(img8)
    d8.rectangle([106, 68, 150, 188], fill=WHITE, outline=BLACK, width=5)
    d8.ellipse([114, 114, 142, 142], fill=BRIGHT_CYAN, outline=BLACK, width=3)
    d8.arc([42, 48, 148, 154], start=100, end=270, fill=BRIGHT_CYAN, width=16)
    d8.polygon([(64, 52), (40, 76), (68, 90)], fill=WHITE, outline=BLACK)
    d8.arc([42, 102, 148, 208], start=90, end=260, fill=BRIGHT_CYAN, width=16)
    d8.polygon([(64, 204), (40, 180), (68, 166)], fill=WHITE, outline=BLACK)
    d8.rectangle([150, 84, 178, 106], fill=DARK_TEAL, outline=BLACK, width=4)
    d8.rectangle([150, 150, 178, 172], fill=DARK_TEAL, outline=BLACK, width=4)

    # 9. Drone Dual Rotor (Air Striker)
    img9 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d9 = ImageDraw.Draw(img9)
    d9.ellipse([46, 60, 114, 128], fill=DARK_TEAL, outline=BLACK, width=5)
    d9.ellipse([58, 72, 102, 116], fill=BRIGHT_CYAN, outline=WHITE, width=3)
    d9.ellipse([142, 60, 210, 128], fill=DARK_TEAL, outline=BLACK, width=5)
    d9.ellipse([154, 72, 198, 116], fill=BRIGHT_CYAN, outline=WHITE, width=3)
    d9.line([(80, 94), (176, 94)], fill=BLACK, width=8)
    d9.rounded_rectangle([98, 86, 158, 168], radius=14, fill=WHITE, outline=BLACK, width=5)
    d9.ellipse([114, 112, 142, 140], fill=BRIGHT_CYAN)
    d9.line([(106, 168), (92, 198), (120, 198)], fill=BLACK, width=5)
    d9.line([(150, 168), (164, 198), (136, 198)], fill=BLACK, width=5)

    # 10. Drone Stealth Wing (Armor Piercer)
    img10 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d10 = ImageDraw.Draw(img10)
    d10.polygon([(40, 148), (96, 78), (160, 78), (216, 148), (180, 178), (128, 154), (76, 178)], fill=DARK_TEAL, outline=BLACK)
    d10.line([(96, 78), (160, 78)], fill=BRIGHT_CYAN, width=6)
    d10.line([(40, 148), (96, 78)], fill=BRIGHT_CYAN, width=6)
    d10.line([(216, 148), (160, 78)], fill=BRIGHT_CYAN, width=6)
    d10.polygon([(88, 114), (168, 114), (156, 142), (100, 142)], fill=WHITE, outline=BLACK, width=3)
    d10.rectangle([106, 120, 150, 136], fill=BRIGHT_CYAN)

    # 11. Drone Laser Sentry (Beam Sentry)
    img11 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d11 = ImageDraw.Draw(img11)
    d11.polygon([(60, 180), (196, 180), (170, 146), (86, 146)], fill=DARK_TEAL, outline=BLACK)
    d11.ellipse([82, 92, 174, 156], fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d11.rectangle([112, 48, 144, 106], fill=NAVY, outline=BLACK, width=5)
    d11.ellipse([116, 40, 140, 64], fill=RED, outline=WHITE, width=3)
    d11.line([(128, 48), (128, 16)], fill=RED, width=4)

    # 12. Drone Plasma Orb (Plasma Vortex)
    img12 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d12 = ImageDraw.Draw(img12)
    d12.ellipse([44, 44, 212, 212], fill=NAVY, outline=BLACK, width=6)
    for ang in range(0, 360, 60):
        rad = math.radians(ang)
        ox = 128 + 72 * math.cos(rad)
        oy = 128 + 72 * math.sin(rad)
        d12.ellipse([ox-14, oy-14, ox+14, oy+14], fill=BRIGHT_CYAN, outline=WHITE, width=3)
    d12.ellipse([80, 80, 176, 176], fill=BRIGHT_CYAN, outline=BLACK, width=4)
    d12.ellipse([100, 100, 156, 156], fill=WHITE)

    # 13. Buddy Card Frame - Common / Normal
    img13 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d13 = ImageDraw.Draw(img13)
    for px in [64, 178]:
        d13.rectangle([px, 6, px + 14, 32], fill=BRIGHT_CYAN, outline=BLACK, width=4)
        d13.rectangle([px, 224, px + 14, 250], fill=BRIGHT_CYAN, outline=BLACK, width=4)
    d13.line([(50, 18), (206, 18)], fill=BRIGHT_CYAN, width=6)
    d13.line([(50, 238), (206, 238)], fill=BRIGHT_CYAN, width=6)
    d13.rounded_rectangle([32, 26, 224, 230], radius=14, fill=DARK_TEAL, outline=BLACK, width=7)
    d13.rounded_rectangle([40, 34, 216, 222], radius=10, outline=BRIGHT_CYAN, width=5)
    d13.rectangle([40, 34, 216, 178], fill=MEDIUM_TEAL)
    d13.rectangle([40, 178, 216, 222], fill=DARK_GREEN)

    # 14. Buddy Card Frame - Rare
    img14 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d14 = ImageDraw.Draw(img14)
    for px in [64, 178]:
        d14.rectangle([px, 6, px + 14, 32], fill=WHITE, outline=BLACK, width=4)
        d14.rectangle([px, 224, px + 14, 250], fill=WHITE, outline=BLACK, width=4)
    d14.line([(50, 18), (206, 18)], fill=WHITE, width=6)
    d14.line([(50, 238), (206, 238)], fill=WHITE, width=6)
    d14.rounded_rectangle([32, 26, 224, 230], radius=14, fill=DARK_BLUE, outline=BLACK, width=7)
    d14.rounded_rectangle([40, 34, 216, 222], radius=10, outline=BLUE, width=5)
    d14.rectangle([40, 34, 216, 178], fill=NAVY)
    d14.rectangle([40, 178, 216, 222], fill=DARK_GREEN)

    # 15. Buddy Card Frame - Epic
    img15 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d15 = ImageDraw.Draw(img15)
    for px in [64, 178]:
        d15.rectangle([px, 6, px + 14, 32], fill=PURPLE, outline=BLACK, width=4)
        d15.rectangle([px, 224, px + 14, 250], fill=PURPLE, outline=BLACK, width=4)
    d15.line([(50, 18), (206, 18)], fill=PURPLE, width=6)
    d15.line([(50, 238), (206, 238)], fill=PURPLE, width=6)
    d15.rounded_rectangle([32, 26, 224, 230], radius=14, fill=DARK_PURPLE, outline=BLACK, width=7)
    d15.rounded_rectangle([40, 34, 216, 222], radius=10, outline=PURPLE, width=5)
    d15.rectangle([40, 34, 216, 178], fill=NAVY)
    d15.rectangle([40, 178, 216, 222], fill=DARK_GREEN)

    # 16. Buddy Card Frame - Holographic
    img16 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d16 = ImageDraw.Draw(img16)
    for px in [64, 178]:
        d16.rectangle([px, 6, px + 14, 32], fill=YELLOW, outline=BLACK, width=4)
        d16.rectangle([px, 224, px + 14, 250], fill=YELLOW, outline=BLACK, width=4)
    d16.line([(50, 18), (206, 18)], fill=YELLOW, width=6)
    d16.line([(50, 238), (206, 238)], fill=YELLOW, width=6)
    d16.rounded_rectangle([32, 26, 224, 230], radius=14, fill=NAVY, outline=BLACK, width=7)
    d16.rounded_rectangle([40, 34, 216, 222], radius=10, outline=YELLOW, width=5)
    d16.rectangle([40, 34, 216, 178], fill=DARK_TEAL)
    d16.rectangle([40, 178, 216, 222], fill=DARK_GREEN)

    # 17. Lock Icon for Buddy Locked Slot
    img17 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d17 = ImageDraw.Draw(img17)
    d17.arc([88, 48, 168, 128], start=180, end=0, fill=BRIGHT_CYAN, width=18)
    d17.rectangle([88, 86, 106, 128], fill=BRIGHT_CYAN)
    d17.rectangle([150, 86, 168, 128], fill=BRIGHT_CYAN)
    d17.rounded_rectangle([68, 114, 188, 208], radius=14, fill=DARK_TEAL, outline=BRIGHT_CYAN, width=7)
    d17.ellipse([114, 138, 142, 166], fill=WHITE)
    d17.polygon([(118, 160), (138, 160), (142, 188), (114, 188)], fill=WHITE)

    # 18. Green Upgrade Arrow Badge
    img18 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d18 = ImageDraw.Draw(img18)
    d18.rounded_rectangle([60, 36, 196, 220], radius=18, fill=YELLOW, outline=BLACK, width=7)
    d18.rounded_rectangle([74, 50, 182, 206], radius=12, fill=GREEN)
    d18.polygon([(128, 68), (166, 126), (146, 126), (146, 184), (110, 184), (110, 126), (90, 126)], fill=WHITE, outline=BLACK)

    # 19. Wave Pulse Line
    img19 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d19 = ImageDraw.Draw(img19)
    wave_pts = [(16, 128), (64, 128), (88, 72), (112, 184), (136, 56), (160, 168), (184, 128), (240, 128)]
    d19.line(wave_pts, fill=WHITE, width=8)

    # 20. Drone Tab Icon
    img20 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d20 = ImageDraw.Draw(img20)
    d20.ellipse([54, 54, 202, 202], fill=DARK_TEAL, outline=BRIGHT_CYAN, width=6)
    d20.polygon([(128, 74), (182, 154), (74, 154)], fill=BRIGHT_CYAN)

    # Helper for colored lock
    def draw_colored_lock(color, inner_color):
        im = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.arc([88, 48, 168, 128], start=180, end=0, fill=WHITE, width=18)
        d.rectangle([88, 86, 106, 128], fill=WHITE)
        d.rectangle([150, 86, 168, 128], fill=WHITE)
        d.rounded_rectangle([68, 114, 188, 208], radius=14, fill=color, outline=BLACK, width=7)
        d.rounded_rectangle([78, 124, 178, 198], radius=8, outline=WHITE, width=4)
        d.ellipse([114, 142, 142, 170], fill=WHITE)
        d.polygon([(120, 162), (136, 162), (140, 186), (116, 186)], fill=WHITE)
        return im

    # 21. Blue Lock (Magic)
    img21 = draw_colored_lock(BLUE, DARK_BLUE)
    # 22. Purple Lock (Rare)
    img22 = draw_colored_lock(PURPLE, DARK_PURPLE)
    # 23. Yellow Lock (Unique)
    img23 = draw_colored_lock(YELLOW, ORANGE)
    # 24. Pink Lock (Epic)
    img24 = draw_colored_lock(PINK, DARK_PINK)

    # 25. Unlocked Checkmark Icon
    img25 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d25 = ImageDraw.Draw(img25)
    d25.ellipse([64, 64, 192, 192], fill=GREEN, outline=BLACK, width=7)
    d25.polygon([(96, 128), (118, 156), (164, 98), (174, 108), (118, 176), (86, 138)], fill=WHITE, outline=BLACK)

    # 26. Button Plate: Enhance (Green cyber plate with corner rivets)
    img26 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d26 = ImageDraw.Draw(img26)
    d26.rounded_rectangle([20, 40, 236, 216], radius=18, fill=DARK_GREEN, outline=BLACK, width=8)
    d26.rounded_rectangle([28, 48, 228, 208], radius=12, fill=GREEN, outline=BRIGHT_CYAN, width=4)
    for rx, ry in [(44, 64), (212, 64), (44, 192), (212, 192)]:
        d26.ellipse([rx-6, ry-6, rx+6, ry+6], fill=BLACK, outline=WHITE, width=2)

    # 27. Button Plate: Advance Tier (Lime cyber plate with corner rivets)
    img27 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d27 = ImageDraw.Draw(img27)
    d27.rounded_rectangle([20, 40, 236, 216], radius=18, fill=DARK_LIME, outline=BLACK, width=8)
    d27.rounded_rectangle([28, 48, 228, 208], radius=12, fill=LIME_GREEN, outline=YELLOW, width=4)
    for rx, ry in [(44, 64), (212, 64), (44, 192), (212, 192)]:
        d27.ellipse([rx-6, ry-6, rx+6, ry+6], fill=BLACK, outline=WHITE, width=2)

    # 28. Button Plate: Equip (Yellow/Teal cyber plate)
    img28 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d28 = ImageDraw.Draw(img28)
    d28.rounded_rectangle([20, 40, 236, 216], radius=18, fill=NAVY, outline=BLACK, width=8)
    d28.rounded_rectangle([28, 48, 228, 208], radius=12, fill=YELLOW, outline=ORANGE, width=4)
    for rx, ry in [(44, 64), (212, 64), (44, 192), (212, 192)]:
        d28.ellipse([rx-6, ry-6, rx+6, ry+6], fill=BLACK, outline=WHITE, width=2)

    # 29. Mini Data Chip Icon
    img29 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d29 = ImageDraw.Draw(img29)
    d29.rounded_rectangle([60, 60, 196, 196], radius=14, fill=DARK_TEAL, outline=BLACK, width=8)
    d29.rounded_rectangle([72, 72, 184, 184], radius=10, fill=BRIGHT_CYAN, outline=WHITE, width=4)
    for px in [96, 128, 160]:
        d29.line([(px, 60), (px, 36)], fill=WHITE, width=6)
        d29.line([(px, 196), (px, 220)], fill=WHITE, width=6)
        d29.line([(60, px), (36, px)], fill=WHITE, width=6)
        d29.line([(196, px), (220, px)], fill=WHITE, width=6)
    d29.rectangle([104, 104, 152, 152], fill=WHITE, outline=NAVY, width=4)

    # 30. Mini Red Gem Icon
    img30 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d30 = ImageDraw.Draw(img30)
    d30.polygon([(128, 54), (196, 128), (128, 202), (60, 128)], fill=RED, outline=BLACK)
    d30.polygon([(128, 74), (176, 128), (128, 182), (80, 128)], fill=ORANGE)

    images = [
        img1, img2, img3, img4, img5, img6,
        img7, img8, img9, img10, img11, img12,
        img13, img14, img15, img16, img17, img18,
        img19, img20, img21, img22, img23, img24,
        img25, img26, img27, img28, img29, img30
    ]

    for idx, img in enumerate(images):
        col = idx % cols
        row = idx // cols
        atlas.paste(img, (col * cell_w, row * cell_h), img)

    atlas.save(atlas_path)
    print(f"Generated {atlas_path} with {len(images)} high-res Buddy sprites successfully!")

if __name__ == "__main__":
    create_buddy_atlas()
