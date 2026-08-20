import os
import math
from PIL import Image, ImageDraw

def create_buddy_atlas():
    out_dir = r"Assets/UI/Buddy/Generated"
    os.makedirs(out_dir, exist_ok=True)
    atlas_path = os.path.join(out_dir, "buddy-atlas.png")

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
    PURPLE = (175, 55, 210, 255)
    DARK_PURPLE = (75, 18, 95, 255)
    BLUE = (38, 115, 225, 255)
    DARK_BLUE = (16, 50, 125, 255)
    GREY = (150, 175, 190, 255)

    # 1. Drone Snowflake (Equipped Slot 1 Drone)
    img1 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d1 = ImageDraw.Draw(img1)
    cx, cy = 128, 128
    # 6-point snowflake star
    for i in range(6):
        ang = i * (math.pi / 3)
        x1 = cx + 85 * math.cos(ang)
        y1 = cy + 85 * math.sin(ang)
        d1.line([(cx, cy), (x1, y1)], fill=BRIGHT_CYAN, width=8)
        # Side prongs
        p1x = cx + 55 * math.cos(ang) + 20 * math.cos(ang + math.pi/3)
        p1y = cy + 55 * math.sin(ang) + 20 * math.sin(ang + math.pi/3)
        p2x = cx + 55 * math.cos(ang) + 20 * math.cos(ang - math.pi/3)
        p2y = cy + 55 * math.sin(ang) + 20 * math.sin(ang - math.pi/3)
        d1.line([(cx + 55 * math.cos(ang), cy + 55 * math.sin(ang)), (p1x, p1y)], fill=BRIGHT_CYAN, width=5)
        d1.line([(cx + 55 * math.cos(ang), cy + 55 * math.sin(ang)), (p2x, p2y)], fill=BRIGHT_CYAN, width=5)
        d1.ellipse([x1-7, y1-7, x1+7, y1+7], fill=WHITE, outline=BLACK, width=2)
    # Center hexagon & core
    hex_pts = [(cx + 42 * math.cos(i*math.pi/3), cy + 42 * math.sin(i*math.pi/3)) for i in range(6)]
    d1.polygon(hex_pts, fill=DARK_TEAL, outline=BRIGHT_CYAN, width=4)
    d1.ellipse([cx-22, cy-22, cx+22, cy+22], fill=WHITE, outline=NAVY, width=4)
    d1.ellipse([cx-12, cy-12, cx+12, cy+12], fill=BRIGHT_CYAN)

    # 2. Drone Spider (4-legged / 5-legged Quad Drone)
    img2 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d2 = ImageDraw.Draw(img2)
    # Head/body
    d2.rounded_rectangle([78, 64, 178, 150], radius=16, fill=BRIGHT_CYAN, outline=BLACK, width=6)
    d2.rectangle([94, 80, 162, 108], fill=NAVY, outline=BLACK, width=3)
    d2.ellipse([106, 88, 122, 102], fill=WHITE)
    d2.ellipse([134, 88, 150, 102], fill=WHITE)
    # Top antenna
    d2.line([(100, 64), (92, 34)], fill=BLACK, width=6)
    d2.ellipse([84, 26, 100, 42], fill=WHITE, outline=BLACK, width=3)
    # Legs (bottom tentacles/struts)
    for lx in [92, 116, 140, 164]:
        d2.line([(lx, 150), (lx - 12 + (lx-128)*0.2, 198)], fill=DARK_TEAL, width=7)
        d2.ellipse([lx - 18 + (lx-128)*0.2, 192, lx - 6 + (lx-128)*0.2, 204], fill=BLACK)

    # 3. Drone Antenna Eye (Spherical with top loop antenna)
    img3 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d3 = ImageDraw.Draw(img3)
    # Top loop antenna
    d3.line([(96, 68), (86, 32)], fill=BLACK, width=6)
    d3.line([(160, 68), (170, 32)], fill=BLACK, width=6)
    d3.arc([108, 18, 148, 58], start=0, end=360, fill=BLACK, width=5)
    # Ball body
    d3.ellipse([54, 54, 202, 202], fill=DARK_TEAL, outline=BLACK, width=6)
    d3.ellipse([70, 70, 186, 186], fill=BRIGHT_CYAN)
    # Big glowing eye visor
    d3.ellipse([88, 88, 168, 168], fill=NAVY, outline=BLACK, width=4)
    d3.ellipse([104, 104, 152, 152], fill=WHITE)
    d3.ellipse([118, 118, 138, 138], fill=BLACK)

    # 4. Drone Cross Visor (Round Robot with glowing cross/plus eye)
    img4 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d4 = ImageDraw.Draw(img4)
    # Ears/antenna
    d4.line([(90, 60), (74, 30)], fill=BLACK, width=6)
    d4.ellipse([66, 22, 82, 38], fill=WHITE, outline=BLACK, width=3)
    d4.line([(166, 60), (182, 30)], fill=BLACK, width=6)
    d4.ellipse([174, 22, 190, 38], fill=WHITE, outline=BLACK, width=3)
    # Body
    d4.ellipse([56, 52, 200, 196], fill=BRIGHT_CYAN, outline=BLACK, width=6)
    # Eyes (two glowing crosses)
    d4.line([(88, 110), (116, 110)], fill=WHITE, width=6)
    d4.line([(102, 96), (102, 124)], fill=WHITE, width=6)
    d4.line([(140, 110), (168, 110)], fill=WHITE, width=6)
    d4.line([(154, 96), (154, 124)], fill=WHITE, width=6)
    # Chin vent
    d4.line([(106, 156), (150, 156)], fill=NAVY, width=5)

    # 5. Drone Capsule (Spherical with single visor eye & mini antenna)
    img5 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d5 = ImageDraw.Draw(img5)
    # Side antenna
    d5.line([(100, 60), (84, 28)], fill=BLACK, width=6)
    d5.ellipse([76, 20, 92, 36], fill=WHITE, outline=BLACK, width=3)
    # Capsule Body
    d5.ellipse([54, 54, 202, 202], fill=WHITE, outline=BLACK, width=6)
    d5.ellipse([70, 70, 186, 186], fill=BRIGHT_CYAN, outline=NAVY, width=4)
    # Eye circle
    d5.ellipse([112, 94, 168, 150], fill=NAVY, outline=BLACK, width=4)
    d5.ellipse([126, 108, 154, 136], fill=WHITE)
    d5.ellipse([136, 118, 146, 128], fill=BLACK)

    # 6. Drone Spiky Mine
    img6 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d6 = ImageDraw.Draw(img6)
    # Spikes around
    for i in range(8):
        ang = i * (math.pi / 4)
        sp_x = 128 + 84 * math.cos(ang)
        sp_y = 128 + 84 * math.sin(ang)
        d6.polygon([(128 + 48*math.cos(ang-0.3), 128 + 48*math.sin(ang-0.3)),
                    (sp_x, sp_y),
                    (128 + 48*math.cos(ang+0.3), 128 + 48*math.sin(ang+0.3))], fill=DARK_TEAL, outline=BLACK)
    # Core
    d6.ellipse([72, 72, 184, 184], fill=BLACK, outline=BRIGHT_CYAN, width=6)
    d6.ellipse([98, 98, 158, 158], fill=BRIGHT_CYAN, outline=WHITE, width=4)
    d6.ellipse([114, 114, 142, 142], fill=NAVY)

    # 7. Drone Octagon Shield
    img7 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d7 = ImageDraw.Draw(img7)
    # 8-point star shield
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

    # 8. Drone Claw Magnet (C-Shape dual claw drone)
    img8 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d8 = ImageDraw.Draw(img8)
    # Center unit
    d8.rectangle([106, 68, 150, 188], fill=WHITE, outline=BLACK, width=5)
    d8.ellipse([114, 114, 142, 142], fill=BRIGHT_CYAN, outline=BLACK, width=3)
    # Upper C-Claw
    d8.arc([42, 48, 148, 154], start=100, end=270, fill=BRIGHT_CYAN, width=16)
    d8.polygon([(64, 52), (40, 76), (68, 90)], fill=WHITE, outline=BLACK)
    # Lower C-Claw
    d8.arc([42, 102, 148, 208], start=90, end=260, fill=BRIGHT_CYAN, width=16)
    d8.polygon([(64, 204), (40, 180), (68, 166)], fill=WHITE, outline=BLACK)
    # Thrusters
    d8.rectangle([150, 84, 178, 106], fill=DARK_TEAL, outline=BLACK, width=4)
    d8.rectangle([150, 150, 178, 172], fill=DARK_TEAL, outline=BLACK, width=4)

    # 9. Drone Dual Rotor (Propeller Drone)
    img9 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d9 = ImageDraw.Draw(img9)
    # Left rotor ring
    d9.ellipse([46, 60, 114, 128], fill=DARK_TEAL, outline=BLACK, width=5)
    d9.ellipse([58, 72, 102, 116], fill=BRIGHT_CYAN, outline=WHITE, width=3)
    # Right rotor ring
    d9.ellipse([142, 60, 210, 128], fill=DARK_TEAL, outline=BLACK, width=5)
    d9.ellipse([154, 72, 198, 116], fill=BRIGHT_CYAN, outline=WHITE, width=3)
    # Center frame & pod
    d9.line([(80, 94), (176, 94)], fill=BLACK, width=8)
    d9.rounded_rectangle([98, 86, 158, 168], radius=14, fill=WHITE, outline=BLACK, width=5)
    d9.ellipse([114, 112, 142, 140], fill=BRIGHT_CYAN)
    # Landing skids
    d9.line([(106, 168), (92, 198), (120, 198)], fill=BLACK, width=5)
    d9.line([(150, 168), (164, 198), (136, 198)], fill=BLACK, width=5)

    # 10. Drone Stealth Wing (Visor / Helmet Drone)
    img10 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d10 = ImageDraw.Draw(img10)
    # Angular stealth wings
    d10.polygon([(40, 148), (96, 78), (160, 78), (216, 148), (180, 178), (128, 154), (76, 178)], fill=DARK_TEAL, outline=BLACK)
    d10.line([(96, 78), (160, 78)], fill=BRIGHT_CYAN, width=6)
    d10.line([(40, 148), (96, 78)], fill=BRIGHT_CYAN, width=6)
    d10.line([(216, 148), (160, 78)], fill=BRIGHT_CYAN, width=6)
    # Glowing visor
    d10.polygon([(88, 114), (168, 114), (156, 142), (100, 142)], fill=WHITE, outline=BLACK, width=3)
    d10.rectangle([106, 120, 150, 136], fill=BRIGHT_CYAN)

    # 11. Drone Laser Sentry
    img11 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d11 = ImageDraw.Draw(img11)
    d11.polygon([(60, 180), (196, 180), (170, 146), (86, 146)], fill=DARK_TEAL, outline=BLACK)
    d11.ellipse([82, 92, 174, 156], fill=BRIGHT_CYAN, outline=BLACK, width=5)
    d11.rectangle([112, 48, 144, 106], fill=NAVY, outline=BLACK, width=5)
    d11.ellipse([116, 40, 140, 64], fill=RED, outline=WHITE, width=3)
    d11.line([(128, 48), (128, 16)], fill=RED, width=4)

    # 12. Drone Plasma Orb
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

    # 13. Buddy Card Frame - Common / Normal (Teal Bracket Frame with Top/Bottom Pins)
    img13 = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    d13 = ImageDraw.Draw(img13)
    # Top & Bottom Connector Pins / Lug brackets
    for px in [64, 178]:
        d13.rectangle([px, 6, px + 14, 32], fill=BRIGHT_CYAN, outline=BLACK, width=4)
        d13.rectangle([px, 224, px + 14, 250], fill=BRIGHT_CYAN, outline=BLACK, width=4)
    d13.line([(50, 18), (206, 18)], fill=BRIGHT_CYAN, width=6)
    d13.line([(50, 238), (206, 238)], fill=BRIGHT_CYAN, width=6)
    # Main Box
    d13.rounded_rectangle([32, 26, 224, 230], radius=14, fill=DARK_TEAL, outline=BLACK, width=7)
    d13.rounded_rectangle([40, 34, 216, 222], radius=10, outline=BRIGHT_CYAN, width=5)
    # Inner light body
    d13.rectangle([40, 34, 216, 178], fill=MEDIUM_TEAL)
    # Bottom progress area
    d13.rectangle([40, 178, 216, 222], fill=DARK_GREEN)

    # 14. Buddy Card Frame - Rare (Blue)
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

    # 15. Buddy Card Frame - Epic (Purple)
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

    # 16. Buddy Card Frame - Holographic (Gold)
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

    # 18. Green Upgrade Arrow Badge (Right Badge)
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

    images = [
        img1, img2, img3, img4, img5,
        img6, img7, img8, img9, img10,
        img11, img12, img13, img14, img15,
        img16, img17, img18, img19, img20
    ]

    for idx, img in enumerate(images):
        col = idx % cols
        row = idx // cols
        atlas.paste(img, (col * cell_w, row * cell_h), img)

    atlas.save(atlas_path)
    print(f"Generated {atlas_path} with {len(images)} high-res Buddy sprites successfully!")

if __name__ == "__main__":
    create_buddy_atlas()
