"""Render frames for the GoConsoleOS promo trailer (1920x1080, 30fps)."""
import math, sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

W, H = 1920, 1080
FPS = 30
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.join(os.path.dirname(HERE), "GoConsoleOS")  # the GoConsoleOS repo
WP = os.path.join(REPO, "boot", "system", "wallpapers")
OUT = os.path.join(HERE, "frames")
os.makedirs(OUT, exist_ok=True)

NAVY = (13, 27, 42)
CYAN = (0, 201, 219)
WHITE = (255, 255, 255)
GREY = (136, 153, 170)
FONT_BOLD = r"C:\Windows\Fonts\arialbd.ttf"
FONT_REG = r"C:\Windows\Fonts\arial.ttf"

def load_bg(name, zoom, cx=0.5, cy=0.5):
    img = Image.open(os.path.join(WP, name)).convert("RGB")
    scale = max(W / img.width, H / img.height) * zoom
    nw, nh = int(img.width * scale) + 1, int(img.height * scale) + 1
    img = img.resize((nw, nh), Image.LANCZOS)
    x = int((nw - W) * cx)
    y = int((nh - H) * cy)
    return img.crop((x, y, x + W, y + H))

def dark_overlay(img, alpha=150):
    ov = Image.new("RGB", (W, H), (6, 10, 18))
    return Image.blend(img, ov, alpha / 255.0)

def vignette(img, strength=110):
    v = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(v)
    d.ellipse([-W*0.25, -H*0.3, W*1.25, H*1.3], fill=255)
    v = v.filter(ImageFilter.GaussianBlur(220))
    black = Image.new("RGB", (W, H), (2, 4, 8))
    m = np.array(v).astype(np.float32) / 255.0
    arr = np.array(img).astype(np.float32) * (0.45 + 0.55 * m[..., None]) + np.array(black) * (1 - (0.45 + 0.55 * m[..., None]))
    return Image.fromarray(arr.clip(0, 255).astype(np.uint8))

def letterspace_text(draw, xy, text, font, fill, spacing=8, anchor="lm"):
    total = sum(draw.textlength(ch, font=font) for ch in text) + spacing * (len(text) - 1)
    x, y = xy
    if anchor.startswith("m") or anchor.startswith("c"):
        x = x - total / 2
    for ch in text:
        draw.text((x, y), ch, font=font, fill=fill, anchor="lm")
        x += draw.textlength(ch, font=font) + spacing
    return total

def diamond(draw, cx, cy, r, color, inner=True):
    draw.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=color)
    if inner:
        r2 = int(r * 0.62)
        draw.polygon([(cx, cy - r2), (cx + r2, cy), (cx, cy + r2), (cx - r2, cy)], fill=NAVY)
        r3 = int(r * 0.30)
        draw.polygon([(cx, cy - r3), (cx + r3, cy), (cx, cy + r3), (cx - r3, cy)], fill=color)

def logo_glow(cx, cy, r, intensity):
    """Returns an RGBA glow layer around the diamond."""
    g = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(g)
    steps = 14
    for i in range(steps, 0, -1):
        rr = r + i * 10
        a = int(28 * (1 - i / steps) * intensity)
        if a > 0:
            d.polygon([(cx, cy - rr), (cx + rr, cy), (cx, cy + rr), (cx - rr, cy)], fill=(0, 201, 219, a))
    return g.filter(ImageFilter.GaussianBlur(24))

def ease(t):  # t in [0,1] -> ease in-out
    return t * t * (3 - 2 * t)

def clamp01(t):
    return max(0.0, min(1.0, t))

SCENES = []  # (start_s, end_s, draw_fn(img_base, t01_local))

def solid(img_rgba_top=None):
    return Image.new("RGB", (W, H), NAVY)

def bg_wallpaper(name, zoom, cx, cy, alpha):
    return dark_overlay(load_bg(name, zoom, cx, cy), alpha)

def scene_boot(base, t):
    img = solid()
    p = clamp01(t / 0.55)
    e = ease(p)
    if e > 0:
        glow = logo_glow(W // 2, H // 2 - 60, int(46 + 40 * e), e)
        img.paste(glow, (0, 0), glow)
        d = ImageDraw.Draw(img)
        diamond(d, W // 2, H // 2 - 60, int(20 + 98 * e), CYAN)
    fade = clamp01((t - 0.55) / 0.30)
    if fade > 0:
        d = ImageDraw.Draw(img)
        f = ImageFont.truetype(FONT_BOLD, 52)
        letterspace_text(d, (W // 2, H // 2 + 90), "GOCONSOLEOS", f, (int(255 * ease(fade)),) * 3, spacing=26, anchor="mm")
    return img

def scene_title(base, t):
    img = bg_wallpaper("aurora.png", 1.0 + 0.08 * ease(clamp01(t)), 0.5, 0.5, 125)
    img = vignette(img)
    d = ImageDraw.Draw(img)
    p = clamp01(t / 0.35)
    a = ease(p)
    f = ImageFont.truetype(FONT_BOLD, 118)
    glow = logo_glow(W // 2, 240, 46, a)
    img.paste(glow, (0, 0), glow)
    diamond(d, W // 2, 240, 46, CYAN)
    letterspace_text(d, (W // 2, 470), "GOCONSOLEOS", f, (255, 255, 255), spacing=30, anchor="mm")
    f2 = ImageFont.truetype(FONT_REG, 30)
    letterspace_text(d, (W // 2, 560), "T H E   P O R T A B L E   C O N S O L E", f2, CYAN, spacing=6, anchor="mm")
    return img

def scene_tagline(base, t):
    img = bg_wallpaper("default.png", 1.15, 0.3 + 0.25 * ease(t), 0.5, 165)
    d = ImageDraw.Draw(img)
    lines = [("YOUR LIBRARY.", 0.0), ("YOUR CONTROLLER.", 0.18), ("ANY PC.", 0.36)]
    f = ImageFont.truetype(FONT_BOLD, 92)
    y = 300
    for text, delay in lines:
        p = clamp01((t - delay) / 0.3)
        if p > 0:
            a = ease(p)
            d.text((W // 2, y), text, font=f, fill=(int(255 * a), int(255 * a), int(255 * a)), anchor="mm")
        y += 110
    if t > 0.62:
        f3 = ImageFont.truetype(FONT_REG, 28)
        a = ease(clamp01((t - 0.62) / 0.25))
        d.text((W // 2, 660), "No desktop. No compromise. Just plug in and play.", font=f3, fill=(int(180 * a + 60), int(200 * a + 55), int(220 * a + 35)), anchor="mm")
    return img

def feature(bg, title, sub, accent=CYAN, zoom0=1.05, zoom1=1.18, cx=0.5):
    def fn(base, t):
        img = bg_wallpaper(bg, zoom0 + (zoom1 - zoom0) * ease(t), cx, 0.5, 165)
        img = vignette(img)
        d = ImageDraw.Draw(img)
        p = ease(clamp01(t / 0.28))
        # accent bar
        bw = int(220 * p)
        d.rectangle([W // 2 - bw, 380, W // 2 + bw, 392], fill=accent)
        f = ImageFont.truetype(FONT_BOLD, 76)
        a = p
        letterspace_text(d, (W // 2, 320), title, f, (int(255 * a), int(255 * a), int(255 * a)), spacing=6, anchor="mm")
        f2 = ImageFont.truetype(FONT_REG, 30)
        d.text((W // 2, 450), sub, font=f2, fill=(int(190 + 65 * a), int(200 + 55 * a), int(210 + 45 * a)), anchor="mm")
        return img
    return fn

def scene_usb(base, t):
    img = bg_wallpaper("minimal.png", 1.1 + 0.12 * ease(t), 0.5, 0.45, 110)
    d = ImageDraw.Draw(img)
    p = ease(clamp01(t / 0.3))
    f = ImageFont.truetype(FONT_BOLD, 88)
    letterspace_text(d, (W // 2, 330), "ONE USB DRIVE", f, (255, 255, 255), spacing=10, anchor="mm")
    f2 = ImageFont.truetype(FONT_BOLD, 88)
    letterspace_text(d, (W // 2, 440), "YOUR CONSOLE BOOTS ANYWHERE", f2, CYAN, spacing=3, anchor="mm")
    f3 = ImageFont.truetype(FONT_REG, 30)
    d.text((W // 2, 565), "Windows 11 PC  •  Fullscreen shell  •  Auto-starts on plug-in", font=f3, fill=(210, 220, 230), anchor="mm")
    return img

def scene_end(base, t):
    img = solid()
    img = vignette(img)
    d = ImageDraw.Draw(img)
    p = ease(clamp01(t / 0.35))
    glow = logo_glow(W // 2, H // 2 - 110, int(40 + 26 * p), 0.4 + 0.6 * p)
    img.paste(glow, (0, 0), glow)
    diamond(d, W // 2, H // 2 - 110, int(34 + 36 * p), CYAN)
    f = ImageFont.truetype(FONT_BOLD, 96)
    letterspace_text(d, (W // 2, H // 2 + 55), "GOCONSOLEOS", f, (255, 255, 255), spacing=24, anchor="mm")
    f2 = ImageFont.truetype(FONT_REG, 28)
    d.text((W // 2, H // 2 + 135), "The Portable Console Experience", font=f2, fill=CYAN, anchor="mm")
    f3 = ImageFont.truetype(FONT_REG, 22)
    d.text((W // 2, H // 2 + 190), "v2.2.0    •    github.com/GoStudios-Real/GoConsoleOS", font=f3, fill=GREY, anchor="mm")
    fade = clamp01((t - 0.78) / 0.22)
    if fade > 0:
        overlay = Image.new("RGB", (W, H), (0, 0, 0))
        img = Image.blend(img, overlay, ease(fade))
    return img

SCENES = [
    (0.0, 4.0, scene_boot),
    (4.0, 8.5, scene_title),
    (8.5, 13.5, scene_tagline),
    (13.5, 18.0, feature("aurora.png", "CONTROLLER FIRST", "A full console shell, driven entirely from your Xbox controller")),
    (18.0, 22.5, feature("default.png", "EVERY LIBRARY. ONE PLACE.", "Steam  •  Epic Games  •  Xbox  •  GOG  •  Game Pass", zoom0=1.0, zoom1=1.15, cx=0.35)),
    (22.5, 27.0, feature("default.png", "GOCONSOLE CLOUD", "Stream to your phone and Google TV Streamer  —  scan a QR, enter a code, play", zoom0=1.05, zoom1=1.2, cx=0.65)),
    (27.0, 31.0, feature("default.png", "GOSTORE", "Themes  •  Plugins  •  Overlays  •  Controller layouts", cx=0.6)),
    (31.0, 35.0, feature("aurora.png", "ACC + GOAI", "Accounts, friends, achievements & an on-console gaming assistant", zoom0=1.0, zoom1=1.14, cx=0.5)),
    (35.0, 39.5, scene_usb),
    (39.5, 45.0, scene_end),
]

def render():
    frame_idx = 0
    for start, end, fn in SCENES:
        n = int((end - start) * FPS)
        for i in range(n):
            t = (i / n) if n else 0
            img = fn(None, t)
            img.save(os.path.join(OUT, f"{frame_idx:04d}.png"), optimize=False)
            frame_idx += 1
        print(f"scene {start}-{end}s done ({frame_idx} frames)", flush=True)
    print("TOTAL_FRAMES", frame_idx)

if __name__ == "__main__":
    render()
