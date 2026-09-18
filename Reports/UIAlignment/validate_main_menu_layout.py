"""Check serialized horizontal UI geometry across portrait phone resolutions.

Runs without Unity; uses CanvasScaler's logarithmic MatchWidthOrHeight formula
and RectTransform anchor/pivot/scale geometry. This is a scene-data regression
check, not a substitute for rendering an APK on a device.
"""
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCENE = ROOT / "Assets/Scenes/MainMenu.unity"
text = SCENE.read_text(encoding="utf-8-sig")
blocks = {
    identifier: body
    for _, identifier, body in re.findall(
        r"^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)", text, re.M | re.S
    )
}


def field(body, key):
    return re.search(r"^  " + key + r": (.*)$", body, re.M)[1]


def vector(body, key):
    return {k: float(v) for k, v in re.findall(r"(\w+): ([\d.eE+-]+)", field(body, key))}


def reference(body, key):
    return re.search(r"fileID: (\d+)", field(body, key))[1]


rects = {key: b for key, b in blocks.items() if b.startswith("RectTransform:")}
names = {key: field(blocks[reference(b, "m_GameObject")], "m_Name") for key, b in rects.items()}
parents = {key: reference(b, "m_Father") for key, b in rects.items()}
canvas = next(key for key in rects if names[key] == "Canvas")
canvas_go = reference(rects[canvas], "m_GameObject")
scaler = next(b for b in blocks.values() if "  m_UiScaleMode:" in b and reference(b, "m_GameObject") == canvas_go)
assert field(scaler, "m_UiScaleMode") == "1", "Expected Scale With Screen Size"
assert field(scaler, "m_ScreenMatchMode") == "0", "Expected Match Width Or Height"
resolution = vector(scaler, "m_ReferenceResolution")
match = float(field(scaler, "m_MatchWidthOrHeight"))


def bounds(width, height):
    factor = 2 ** ((1 - match) * math.log2(width / resolution["x"]) + match * math.log2(height / resolution["y"]))
    cache = {canvas: (0.0, width / factor, 1.0)}

    def calculate(key):
        if key in cache:
            return cache[key]
        body = rects[key]
        left, parent_width, parent_scale = calculate(parents[key])
        amin, amax, pivot, offset, delta, scale = [vector(body, k)["x"] for k in (
            "m_AnchorMin", "m_AnchorMax", "m_Pivot", "m_AnchoredPosition", "m_SizeDelta", "m_LocalScale")]
        own_width = parent_width * (amax - amin) + delta
        anchor = parent_width * (amin + (amax - amin) * pivot)
        own_scale = parent_scale * scale
        own_left = left + parent_scale * (anchor + offset) - own_width * pivot * own_scale
        cache[key] = own_left, own_width, own_scale
        return cache[key]

    return factor, calculate


buddy = next(k for k in rects if names[k] == "BuddyPanel")
inventory = next(k for k in rects if names[k] == "SlotIconBuddy" and parents[k] == buddy)
targets = [k for k in rects if parents[k] == inventory]
targets += [k for k in rects if parents[k] == buddy and names[k] in ("EquippedBoard", "SortFilterBar")]
tabs = next(k for k in rects if names[k] == "TopTabs" and parents[k] == buddy)
targets += [k for k in rects if parents[k] == tabs]
nav = next(k for k in rects if names[k] == "BottomNavigation" and parents[k] == canvas)
targets += [k for k in rects if parents[k] == nav]
assert len(targets) == 14, "UI structure changed; review the regression targets"
_, baseline = bounds(1080, 1920)
failures = []
for width, height in [(1080, 1920), (720, 1280), (720, 1600), (1080, 2340), (1080, 2400), (1440, 3200)]:
    factor, geometry = bounds(width, height)
    drift = overflow = 0.0
    for key in targets:
        left, size, scale = geometry(key)
        base_left, base_size, base_scale = baseline(key)
        center = (left + size * scale / 2) * factor
        expected_center = (base_left + base_size * base_scale / 2) / 1080 * width
        drift = max(drift, abs(center - expected_center))
        overflow = max(overflow, -left * factor, (left + size * scale) * factor - width)
    passed = drift < 0.1 and overflow < 0.1
    print(f"{'PASS' if passed else 'FAIL'} {width}x{height}: max horizontal drift={drift:.2f}px, overflow={overflow:.2f}px")
    if not passed:
        failures.append((width, height))
print(f"Canvas Match={match:g}; {len(targets)} elements checked at 6 resolutions")
raise SystemExit(bool(failures))
