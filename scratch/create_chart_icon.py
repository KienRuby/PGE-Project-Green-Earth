from PIL import Image, ImageDraw

def create_chart_icon(path):
    size = (256, 256)
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Coordinate Axis (L-shaped) in soft light cyan/white
    axis_color = (200, 230, 240, 255)
    axis_width = 8
    # Vertical axis: from (36, 40) down to (36, 216)
    # Horizontal axis: from (36, 216) right to (236, 216)
    draw.line([(36, 40), (36, 216)], fill=axis_color, width=axis_width)
    draw.line([(36, 216), (236, 216)], fill=axis_color, width=axis_width)

    # 4 Bar Columns (ascending heights from left to right) in bright warm orange/amber
    bar_color = (255, 160, 32, 255)
    bars = [
        (56, 175, 84, 212),   # Bar 1: x0, y0, x1, y1
        (100, 135, 128, 212), # Bar 2
        (144, 155, 172, 212), # Bar 3
        (188, 85, 216, 212),  # Bar 4
    ]
    for x0, y0, x1, y1 in bars:
        draw.rectangle([x0, y0, x1, y1], fill=bar_color)

    # Trend line with arrow in bright orange/yellow-orange
    line_color = (255, 190, 50, 255)
    line_width = 8
    points = [
        (48, 145),
        (105, 95),
        (150, 125),
        (220, 48)
    ]
    draw.line(points, fill=line_color, width=line_width, joint="curve")

    # Arrow head at (220, 48) pointing up-right
    arrow_pts = [
        (225, 42),
        (190, 44),
        (222, 76)
    ]
    draw.polygon(arrow_pts, fill=line_color)

    img.save(path, "PNG")
    print(f"Successfully generated {path}")

create_chart_icon("Assets/Sprites/UI/icon-damage-details.png")
