"""Draws the particle sprite of every material, one atlas per material.

Each sprite is luminance (RGB, grayscale) plus coverage (alpha) on a transparent
field: the look table's birth/death ramp supplies the hue at runtime. An atlas
is a grid of frames; the shader either animates through them by particle age
(fire) or picks one per particle (everything else). See PHILOSOPHY.md.

    python generate_particles.py

Writes View/Resources/MagicCircleParticles/<material-key>.png and a contact
sheet next to this script. Deterministic: every random draw is seeded.
"""

from __future__ import annotations

import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "View", "Resources", "MagicCircleParticles"))
BODY_OUT = os.path.normpath(os.path.join(HERE, "..", "View", "Resources", "MagicCircleBodies"))


# ---------------------------------------------------------------------------
# Field helpers. A frame is drawn on (u, v) in -1..1 with v pointing up.
# ---------------------------------------------------------------------------

def coords(n):
    y, x = np.mgrid[0:n, 0:n].astype(np.float64)
    u = (x + 0.5) / n * 2 - 1
    v = 1 - (y + 0.5) / n * 2
    return u, v


def smoothstep(a, b, x):
    span = np.asarray(b, dtype=np.float64) - a
    span = np.where(np.abs(span) < 1e-9, 1e-9, span)
    t = np.clip((x - a) / span, 0, 1)
    return t * t * (3 - 2 * t)


class Noise:
    """Value noise on a seeded lattice, smooth-interpolated, plus fbm."""

    def __init__(self, seed):
        self.lattice = np.random.default_rng(seed).random((256, 256))

    def sample(self, x, y):
        xf, yf = np.floor(x), np.floor(y)
        tx, ty = x - xf, y - yf
        tx = tx * tx * tx * (tx * (tx * 6 - 15) + 10)
        ty = ty * ty * ty * (ty * (ty * 6 - 15) + 10)
        xi, yi = xf.astype(int) & 255, yf.astype(int) & 255
        l = self.lattice
        a, b = l[yi, xi], l[yi, (xi + 1) & 255]
        c, d = l[(yi + 1) & 255, xi], l[(yi + 1) & 255, (xi + 1) & 255]
        return (a + (b - a) * tx) + ((c + (d - c) * tx) - (a + (b - a) * tx)) * ty

    def fbm(self, x, y, octaves=5, gain=0.5):
        total, amp, freq, norm = 0.0, 1.0, 1.0, 0.0
        for i in range(octaves):
            total = total + amp * self.sample(x * freq + i * 17.3, y * freq - i * 9.1)
            norm += amp
            amp *= gain
            freq *= 2.03
        return total / norm


def segment_distance(u, v, points):
    """Distance from every pixel to a polyline."""
    d = np.full(u.shape, 1e9)
    for (ax, ay), (bx, by) in zip(points[:-1], points[1:]):
        dx, dy = bx - ax, by - ay
        length_sq = dx * dx + dy * dy or 1e-12
        t = np.clip(((u - ax) * dx + (v - ay) * dy) / length_sq, 0, 1)
        d = np.minimum(d, np.hypot(u - (ax + t * dx), v - (ay + t * dy)))
    return d


def rim_fade(u, v, inner=0.82, outer=0.98):
    """Forces true transparency before the frame edge, so no square ever shows."""
    return 1 - smoothstep(inner, outer, np.maximum(np.abs(u), np.abs(v)))


# ---------------------------------------------------------------------------
# Specimens. Each returns (luminance, alpha), both 0..1, for one frame.
# ---------------------------------------------------------------------------

def flame(n, t, seed):
    """A flame tongue at life fraction t: full and hot, then lifting and fraying."""
    u, v = coords(n)
    noise = Noise(seed)
    lift = 0.3 * t
    h = (v + 0.9 - lift) / (1.75 - 0.5 * t)             # 0 at the foot, 1 at the tip
    sway = 0.26 * (noise.fbm(v * 1.5 + t * 3.1, 3.3, 3) - 0.5) * np.clip(h, 0, 1) * 2
    width = 0.64 * (1 - 0.3 * t) * np.clip(1 - h, 0, 1) ** 0.6 * np.clip(h * 4 + 0.35, 0, 1)
    body = smoothstep(width, width * 0.3, np.abs(u - sway)) * (h > 0) * (h < 1)
    licks = noise.fbm(u * 2.6, v * 2.0 - t * 5.0, 5)
    erosion = 0.12 + 0.7 * t ** 1.3 + 0.3 * np.clip(h, 0, 1) ** 2
    alpha = smoothstep(erosion, erosion + 0.35, body * (0.55 + 0.9 * licks))
    core = smoothstep(width * 0.75, 0, np.abs(u - sway)) * np.clip(1 - h * 1.15, 0, 1)
    luminance = np.clip(0.55 + 0.45 * core * (1 - 0.6 * t), 0, 1)
    return luminance, alpha * rim_fade(u, v)


def whirl(n, seed, arms, twist):
    """A whirlwind seen down its axis: log-spiral arms around a hollow eye."""
    u, v = coords(n)
    noise = Noise(seed)
    r = np.hypot(u, v) + 1e-6
    theta = np.arctan2(v, u)
    phase = theta * arms - np.log(r) * twist
    arm = (0.5 + 0.5 * np.cos(phase)) ** 3
    streaks = noise.fbm(phase * 0.6, r * 9, 4)
    radial = smoothstep(0.98, 0.55, r) * smoothstep(0.05, 0.28, r)
    alpha = np.clip(arm * (0.45 + 0.75 * streaks) * radial * 1.5, 0, 1)
    luminance = np.clip(0.7 + 0.3 * smoothstep(0.7, 0.2, r), 0, 1)
    return luminance, alpha * rim_fade(u, v)


def wisp(n, seed):
    """A gust: a tapered arc of air, streaked along its sweep."""
    u, v = coords(n)
    rng = np.random.default_rng(seed)
    noise = Noise(seed)
    radius = rng.uniform(0.6, 0.8)
    cx, cy = rng.uniform(-0.15, 0.15), -radius * rng.uniform(0.25, 0.45)
    r = np.hypot(u - cx, v - cy)
    angle = np.arctan2(v - cy, u - cx)
    span = rng.uniform(1.6, 2.4)
    start = math.pi / 2 - span / 2 + rng.uniform(-0.3, 0.3)
    along = np.clip((angle - start) / span, 0, 1)
    inside = (angle > start) & (angle < start + span)
    taper = np.sin(along * math.pi) ** 0.8
    thickness = 0.2 * taper + 0.005
    band = smoothstep(thickness, 0, np.abs(r - radius)) * inside
    streaks = noise.fbm(along * 3, (r - radius) * 30, 4)
    alpha = np.clip(band * (0.35 + 0.9 * streaks), 0, 1)
    return np.full(u.shape, 0.92), alpha * rim_fade(u, v)


def droplet(n, seed, splash):
    """A drop of water: a tense skin with a bright rim and one highlight."""
    u, v = coords(n)
    rng = np.random.default_rng(seed)
    if splash:
        alpha = np.zeros(u.shape)
        luminance = np.zeros(u.shape)
        for _ in range(7):
            x, y, s = rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), rng.uniform(0.08, 0.22)
            d = np.hypot(u - x, v - y)
            a = smoothstep(s, s * 0.8, d)
            hl = smoothstep(s * 0.45, 0, np.hypot(u - x + s * 0.35, v - y - s * 0.35))
            lum = 0.55 + 0.35 * smoothstep(s * 0.6, s, d) + 0.4 * hl
            luminance = np.where(a > alpha, lum, luminance)
            alpha = np.maximum(alpha, a)
        return np.clip(luminance, 0, 1), alpha * rim_fade(u, v)
    size = rng.uniform(0.42, 0.52)
    body = np.hypot(u, v + 0.2) - size
    point = np.abs(u) * 1.6 + (v + 0.2) * 0.75 - size * 1.15       # a cone on top
    d = np.minimum(body, np.where(v > -0.2, point, 1))
    alpha = smoothstep(0.02, -0.02, d)
    rim = smoothstep(-0.14, 0, d)
    highlight = smoothstep(0.16, 0, np.hypot(u + 0.17, v + 0.0))
    luminance = np.clip(0.45 + 0.4 * rim + 0.6 * highlight, 0, 1)
    return luminance, alpha * rim_fade(u, v)


def grains(n, seed):
    """A clump of sand: many grains, each its own tone, crowded at the center."""
    u, v = coords(n)
    rng = np.random.default_rng(seed)
    alpha = np.zeros(u.shape)
    luminance = np.zeros(u.shape)
    for _ in range(60):
        x, y = rng.normal(0, 0.28, 2)
        s = rng.uniform(0.025, 0.07)
        d = np.hypot(u - x, (v - y) * rng.uniform(0.8, 1.25))
        a = smoothstep(s, s * 0.6, d)
        lum = rng.uniform(0.45, 1.0) * (0.75 + 0.25 * smoothstep(s, 0, np.hypot(u - x + s * 0.3, v - y - s * 0.3)))
        luminance = np.where(a > alpha, lum, luminance)
        alpha = np.maximum(alpha, a)
    return luminance, alpha * rim_fade(u, v)


def rock(n, seed):
    """A broken stone: an irregular outline cut into lit facets."""
    u, v = coords(n)
    rng = np.random.default_rng(seed)
    noise = Noise(seed)
    corners = rng.integers(7, 11)
    radii = rng.uniform(0.55, 0.85, corners)
    theta = np.arctan2(v, u)
    r = np.hypot(u, v)
    k = (theta + math.pi) / (2 * math.pi) * corners
    i0 = np.floor(k).astype(int) % corners
    i1 = (i0 + 1) % corners
    edge = radii[i0] + (radii[i1] - radii[i0]) * (k - np.floor(k))
    edge = edge * (0.92 + 0.12 * noise.fbm(u * 4, v * 4, 3))
    alpha = smoothstep(edge, edge - 0.03, r)
    seeds = rng.uniform(-0.6, 0.6, (6, 2))
    tones = rng.uniform(0.35, 0.95, 6)
    dist = np.stack([np.hypot(u - sx, v - sy) for sx, sy in seeds])
    order = np.sort(dist, axis=0)
    facet = tones[np.argmin(dist, axis=0)]
    crease = smoothstep(0, 0.05, order[1] - order[0])
    grain = 0.85 + 0.3 * noise.fbm(u * 18, v * 18, 3)
    luminance = np.clip(facet * (0.55 + 0.45 * crease) * grain, 0, 1)
    return luminance, alpha * rim_fade(u, v)


def chip(n, seed):
    """A splinter of hard wood: an elongated chip with grain running its length."""
    u, v = coords(n)
    rng = np.random.default_rng(seed)
    noise = Noise(seed)
    a = rng.uniform(-0.5, 0.5)
    x = u * math.cos(a) + v * math.sin(a)
    y = -u * math.sin(a) + v * math.cos(a)
    length, width = rng.uniform(0.75, 0.9), rng.uniform(0.18, 0.28)
    taper = np.clip(1 - (y / length) ** 2, 0, 1) ** 0.5
    ragged = 1 + 0.25 * (noise.fbm(y * 6, 3.3, 3) - 0.5)
    alpha = smoothstep(width * taper * ragged, width * taper * ragged - 0.03, np.abs(x)) * (np.abs(y) < length)
    rings = 0.5 + 0.5 * np.sin(x * 70 + noise.fbm(x * 3, y * 2.5, 4) * 9)
    luminance = np.clip(0.5 + 0.35 * rings * (0.7 + 0.3 * noise.fbm(u * 10, v * 30, 3)), 0, 1)
    return luminance, alpha * rim_fade(u, v)


def leaf(n, seed, tendril):
    """A leaf on a curling tendril, or the leaf alone: midrib and veins in value."""
    u, v = coords(n)
    rng = np.random.default_rng(seed)
    a = rng.uniform(-0.6, 0.6)
    x = u * math.cos(a) + v * math.sin(a)
    y = -u * math.sin(a) + v * math.cos(a)
    length = 0.62
    half = 0.32 * np.sin(np.clip((y + length) / (2 * length), 0, 1) * math.pi) ** 0.9
    alpha = smoothstep(half, half - 0.025, np.abs(x)) * (np.abs(y) < length)
    rib = smoothstep(0.018, 0, np.abs(x))
    veins = smoothstep(0.012, 0, np.abs(np.mod(y - np.abs(x) * 0.9, 0.17) - 0.085)) * smoothstep(0, 0.05, np.abs(x))
    luminance = np.clip(0.6 + 0.3 * x / 0.32 * 0.3 + 0.35 * rib + 0.2 * veins, 0, 1)
    if tendril:
        t = np.linspace(0, 1, 60)
        spiral = [(-0.05 + (0.32 - 0.3 * s) * math.cos(s * 9), -0.62 - 0.2 * s + (0.32 - 0.3 * s) * math.sin(s * 9) * 0.6) for s in t]
        spiral = [(px * math.cos(-a) + py * math.sin(-a), -px * math.sin(-a) + py * math.cos(-a)) for px, py in spiral]
        stem = smoothstep(0.025, 0.012, segment_distance(u, v, spiral))
        luminance = np.where(stem > alpha, 0.7, luminance)
        alpha = np.maximum(alpha, stem)
    return luminance, alpha * rim_fade(u, v)


def crystal(n, seed, kind):
    """Ice: a six-fold plate, a six-armed star, or a single shard, cut into facets."""
    u, v = coords(n)
    rng = np.random.default_rng(seed)
    r = np.hypot(u, v)
    theta = np.mod(np.arctan2(v, u), math.pi / 3) - math.pi / 6
    x, y = r * np.cos(theta), r * np.sin(theta)                    # folded into one sixth
    if kind == "plate":
        alpha = smoothstep(0.72, 0.69, x)
        facet = 0.55 + 0.45 * (np.sign(y) * 0.5 + 0.5) * smoothstep(0, 0.7, x)
        luminance = facet * (0.8 + 0.2 * smoothstep(0.62, 0.7, x))
    elif kind == "star":
        arm = np.abs(y) - 0.09 * (1 - x / 0.85)
        branches = np.abs(np.abs(y) - np.mod(x, 0.22) * 0.8) - 0.02
        d = np.minimum(arm, np.where(x < 0.75, branches, 1))
        alpha = smoothstep(0.015, -0.01, d) * (x < 0.85) + smoothstep(0.16, 0.13, r)
        alpha = np.clip(alpha, 0, 1)
        luminance = 0.65 + 0.35 * (np.sign(y) * 0.5 + 0.5)
    else:
        a = rng.uniform(-0.4, 0.4)
        sx = u * math.cos(a) + v * math.sin(a)
        sy = -u * math.sin(a) + v * math.cos(a)
        width = 0.22 * (1 - np.clip((sy + 0.8) / 1.6, 0, 1))
        alpha = smoothstep(width, width - 0.02, np.abs(sx)) * (sy > -0.8) * (sy < 0.8)
        luminance = 0.5 + 0.5 * (sx > 0) * smoothstep(-0.8, 0.8, sy)
    return np.clip(luminance, 0, 1), alpha * rim_fade(u, v)


def beam(n):
    """A laser segment: a white-hot core inside a soft sheath, tapered at both ends."""
    u, v = coords(n)
    ends = smoothstep(0.98, 0.55, np.abs(v))
    core = np.exp(-(u / 0.045) ** 2)
    sheath = np.exp(-(u / 0.2) ** 2) * 0.45
    alpha = np.clip((core + sheath) * ends, 0, 1)
    luminance = np.clip(0.6 + 0.4 * core, 0, 1)
    return luminance, alpha * rim_fade(u, v, 0.9, 1.0)


def ripple(n):
    """A sound wave: one bright ring with fainter echoes inside it."""
    u, v = coords(n)
    r = np.hypot(u, v)
    alpha = np.zeros(u.shape)
    for radius, width, strength in ((0.8, 0.05, 1.0), (0.62, 0.035, 0.55), (0.46, 0.03, 0.3)):
        alpha = np.maximum(alpha, strength * np.exp(-((r - radius) / width) ** 2))
    return np.full(u.shape, 0.95), alpha * rim_fade(u, v)


def petal(n, seed, whole):
    """A blossom petal with a notched tip, or a whole five-petal flower."""
    u, v = coords(n)
    rng = np.random.default_rng(seed)

    def one(x, y, scale):
        y = y / scale + 0.55
        x = x / scale
        half = 0.36 * np.sin(np.clip(y / 1.15, 0, 1) * math.pi) ** 0.7
        notch = smoothstep(0.08, 0, np.abs(x)) * smoothstep(0.95, 1.12, y) * 0.2
        a = smoothstep(half, half - 0.03, np.abs(x)) * (y > 0) * (y < 1.15 - notch)
        lum = 0.55 + 0.35 * np.clip(y, 0, 1) + 0.1 * smoothstep(0.02, 0, np.abs(np.mod(x * 6 + y, 0.5) - 0.25))
        return lum, a

    if not whole:
        a = rng.uniform(-0.5, 0.5)
        x = u * math.cos(a) + v * math.sin(a)
        y = -u * math.sin(a) + v * math.cos(a)
        luminance, alpha = one(x, y, 0.85)
        return np.clip(luminance, 0, 1), alpha * rim_fade(u, v)
    luminance = np.zeros(u.shape)
    alpha = np.zeros(u.shape)
    for k in range(5):
        a = k * 2 * math.pi / 5 + rng.uniform(-0.1, 0.1)
        x = u * math.cos(a) + v * math.sin(a)
        y = -u * math.sin(a) + v * math.cos(a)
        lum, al = one(x, y - 0.45, 0.42)
        luminance = np.where(al > alpha, lum, luminance)
        alpha = np.maximum(alpha, al)
    heart = smoothstep(0.13, 0.1, np.hypot(u, v))
    luminance = np.where(heart > 0, 1.0, luminance)
    alpha = np.maximum(alpha, heart)
    return np.clip(luminance, 0, 1), alpha * rim_fade(u, v)


def puff(n, seed):
    """A billow of steam: soft cauliflower edges, denser at heart."""
    u, v = coords(n)
    noise = Noise(seed)
    r = np.hypot(u, v * 1.1)
    billow = noise.fbm(u * 2.2 + 3, v * 2.2 - 1, 5)
    alpha = smoothstep(0.92, 0.25, r + 0.55 * (0.5 - billow))
    luminance = 0.75 + 0.25 * noise.fbm(u * 4, v * 4 + 7, 3)
    return np.clip(luminance, 0, 1), alpha * rim_fade(u, v)


def bolt(n, seed):
    """A lightning stroke: a jagged main channel with forks, hot core and glow."""
    u, v = coords(n)
    rng = np.random.default_rng(seed)

    def channel(a, b, depth, rough):
        if depth == 0:
            return [a, b]
        mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        length = math.hypot(b[0] - a[0], b[1] - a[1])
        nx, ny = -(b[1] - a[1]) / (length or 1), (b[0] - a[0]) / (length or 1)
        off = rng.normal(0, rough * length)
        m = (mx + nx * off, my + ny * off)
        return channel(a, m, depth - 1, rough)[:-1] + channel(m, b, depth - 1, rough)

    main = channel((rng.uniform(-0.2, 0.2), 0.88), (rng.uniform(-0.25, 0.25), -0.88), 6, 0.28)
    d = segment_distance(u, v, main)
    for _ in range(rng.integers(1, 4)):
        root = main[rng.integers(len(main) // 4, len(main) * 3 // 4)]
        tip = (root[0] + rng.uniform(-0.6, 0.6), root[1] - rng.uniform(0.25, 0.6))
        d = np.minimum(d, segment_distance(u, v, channel(root, tip, 4, 0.3)) + 0.006)
    core = np.exp(-(d / 0.012) ** 2)
    glow = np.exp(-(d / 0.07) ** 2) * 0.5
    alpha = np.clip(core + glow, 0, 1)
    luminance = np.clip(0.55 + 0.45 * core, 0, 1)
    return luminance, alpha * rim_fade(u, v)


def glint(n):
    """A metal spark: a four-point glint with a faint diagonal cross."""
    u, v = coords(n)
    au, av = np.abs(u), np.abs(v)
    cross = np.exp(-au / 0.025) * np.exp(-av / 0.45) + np.exp(-av / 0.025) * np.exp(-au / 0.45)
    diag = 0.35 * (np.exp(-np.abs(u - v) / 0.02) + np.exp(-np.abs(u + v) / 0.02)) * np.exp(-np.hypot(u, v) / 0.25)
    core = np.exp(-(np.hypot(u, v) / 0.09) ** 2)
    alpha = np.clip(cross + diag + core, 0, 1)
    return np.clip(0.7 + 0.3 * core, 0, 1), alpha * rim_fade(u, v)


# ---------------------------------------------------------------------------
# Stroke textures for bodies (Bodies.cs): u runs along a stroke, v across it.
# The stroke's own geometry tapers its ends; the texture gives it its hand.
# ---------------------------------------------------------------------------

def brush(width=512, height=64, seed=7):
    """A dry-brush stroke: bristle lines along its length, ragged gaps, soft edges."""
    y, x = np.mgrid[0:height, 0:width].astype(np.float64)
    u, v = (x + 0.5) / width, (y + 0.5) / height * 2 - 1
    noise = Noise(seed)
    bristles = noise.fbm(u * 3, v * 22, 4)
    gaps = smoothstep(0.32, 0.55, noise.fbm(u * 9 + 4, v * 5, 4))
    edge = smoothstep(1.0, 0.55, np.abs(v + 0.12 * (noise.fbm(u * 5, 2.7, 3) - 0.5)))
    alpha = np.clip(edge * (0.35 + 0.85 * bristles) * (0.45 + 0.55 * gaps), 0, 1)
    alpha *= smoothstep(0, 0.04, u) * smoothstep(1, 0.96, u)
    luminance = np.clip(0.72 + 0.28 * bristles, 0, 1)
    return luminance, alpha


def line(width=256, height=32):
    """A clean front: a white-hot core inside a soft glow, for wavefronts."""
    y, x = np.mgrid[0:height, 0:width].astype(np.float64)
    v = (y + 0.5) / height * 2 - 1
    core = np.exp(-(v / 0.18) ** 2)
    glow = np.exp(-(v / 0.55) ** 2) * 0.5
    alpha = np.clip(core + glow, 0, 1) * smoothstep(1.0, 0.85, np.abs(v))
    return np.clip(0.75 + 0.25 * core, 0, 1), alpha


BODY_TEXTURES = [("brush", brush), ("line", line)]


# ---------------------------------------------------------------------------
# The catalog: material key -> (grid side, frame painter). Keys are Materials.cs's.
# Birth/death colors are copied from Looks.cs for the contact sheet only.
# ---------------------------------------------------------------------------

CATALOG = [
    ("gio", "Gió", 2, lambda n, i: wisp(n, 11 + i), "#c8fff0", "#4fa98a"),
    ("day-leo", "Dạng dây leo", 2, lambda n, i: leaf(n, 21 + i, i % 2 == 0), "#a6f07c", "#2f6b2a"),
    ("nuoc", "Nước", 2, lambda n, i: droplet(n, 31 + i, i == 3), "#bfe3ff", "#1f5fb3"),
    ("lua", "Lửa", 4, lambda n, i: flame(n, i / 15, 41 + i % 4), "#ffd77a", "#e0401a"),
    ("cat-dat", "Cát, Đất", 2, lambda n, i: grains(n, 51 + i), "#e6c48a", "#6b4a22"),
    ("loc", "Lốc", 2, lambda n, i: whirl(n, 61 + i, (2, 3, 3, 4)[i], (3.5, 4.5, 2.8, 5.0)[i]), "#e9fffd", "#5c8f8c"),
    ("than-cung", "Dạng thân cứng", 2, lambda n, i: chip(n, 71 + i), "#b9d58a", "#4a3a22"),
    ("bang", "Băng", 2, lambda n, i: crystal(n, 81 + i, ("plate", "star", "shard", "shard")[i]), "#ffffff", "#7fc4ff"),
    ("laser", "Laser", 1, lambda n, i: beam(n), "#ffd0dc", "#ff2050"),
    ("da", "Đá", 2, lambda n, i: rock(n, 91 + i), "#c9c4bb", "#4d4b47"),
    ("song-am", "Sóng âm", 1, lambda n, i: ripple(n), "#efe3ff", "#6a3fc4"),
    ("dang-hoa", "Dạng hoa", 2, lambda n, i: petal(n, 101 + i, i == 0), "#ffe0f0", "#e0508f"),
    ("hoi-nuoc", "Hơi nước", 2, lambda n, i: puff(n, 111 + i), "#ffffff", "#9aa8b8"),
    ("loi", "Lôi", 4, lambda n, i: bolt(n, 121 + i), "#ffffff", "#8a6bff"),
    ("kim-loai", "Kim loại", 1, lambda n, i: glint(n), "#ffffff", "#ffb24a"),
]

ATLAS_PX = 512


def atlas(grid, painter):
    frame = ATLAS_PX // grid
    rgba = np.zeros((ATLAS_PX, ATLAS_PX, 4))
    for i in range(grid * grid):
        luminance, alpha = painter(frame, i)
        row, col = divmod(i, grid)
        cell = rgba[row * frame:(row + 1) * frame, col * frame:(col + 1) * frame]
        cell[..., 0:3] = luminance[..., None]
        cell[..., 3] = np.clip(alpha, 0, 1)
    return rgba


def to_image(rgba):
    return Image.fromarray(np.round(np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA")


def hex_rgb(h):
    return np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) / 255.0


def font(names, size):
    for name in names:
        path = os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", name)
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def contact_sheet(atlases):
    """The cabinet drawer: every specimen, tinted by its own ramp, on one quiet grid."""
    cols, cell, margin, label_h = 5, 300, 64, 54
    rows = math.ceil(len(atlases) / cols)
    width = margin * 2 + cols * cell
    height = margin * 2 + 70 + rows * (cell + label_h)
    bg = np.array([11, 13, 20]) / 255.0
    sheet = Image.new("RGB", (width, height), tuple(int(c * 255) for c in bg))
    draw = ImageDraw.Draw(sheet)
    title = font(["segoeuil.ttf", "segoeui.ttf"], 30)
    mono = font(["consola.ttf", "cour.ttf"], 13)
    label = font(["segoeuisl.ttf", "segoeui.ttf"], 17)
    ink, faint = (214, 218, 230), (92, 99, 120)
    draw.text((margin, margin - 8), "Elemental Specimens", font=title, fill=ink)
    draw.text((width - margin, margin + 6), "15 materials · luminance + coverage · tinted by birth ramp",
              font=mono, fill=faint, anchor="ra")
    for k, (key, name, grid, rgba, birth, death) in enumerate(atlases):
        row, col = divmod(k, cols)
        x0 = margin + col * cell
        y0 = margin + 70 + row * (cell + label_h)
        inner = cell - 24
        img = np.asarray(to_image(rgba).resize((inner, inner), Image.LANCZOS)).astype(np.float64) / 255
        # Tint top-to-bottom from birth to death, so the ramp reads across the frames.
        ramp = np.linspace(0, 1, inner)[:, None, None]
        tint = hex_rgb(birth) * (1 - ramp) + hex_rgb(death) * ramp
        color = img[..., :3] * tint
        a = img[..., 3:4]
        patch = bg * (1 - a) + color * a
        sheet.paste(Image.fromarray(np.round(patch * 255).astype(np.uint8), "RGB"), (x0 + 12, y0 + 12))
        draw.rectangle([x0 + 12, y0 + 12, x0 + 12 + inner, y0 + 12 + inner], outline=(32, 37, 52))
        for g in range(1, grid):
            p = x0 + 12 + inner * g // grid
            q = y0 + 12 + inner * g // grid
            draw.line([(p, y0 + 12), (p, y0 + 12 + inner)], fill=(26, 30, 43))
            draw.line([(x0 + 12, q), (x0 + 12 + inner, q)], fill=(26, 30, 43))
        draw.text((x0 + 12, y0 + cell - 2), name, font=label, fill=ink)
        draw.text((x0 + 12 + inner, y0 + cell + 2), f"{k + 1:02d} · {key} · {grid}×{grid}", font=mono, fill=faint, anchor="ra")
    return sheet


def main():
    os.makedirs(OUT, exist_ok=True)
    made = []
    for key, name, grid, painter, birth, death in CATALOG:
        rgba = atlas(grid, painter)
        to_image(rgba).save(os.path.join(OUT, f"{key}.png"), optimize=True)
        made.append((key, name, grid, rgba, birth, death))
        print(f"{key:10s} {grid}x{grid}")
    os.makedirs(BODY_OUT, exist_ok=True)
    for key, painter in BODY_TEXTURES:
        luminance, alpha = painter()
        rgba = np.concatenate([np.repeat(luminance[..., None], 3, axis=2), alpha[..., None]], axis=2)
        to_image(rgba).save(os.path.join(BODY_OUT, f"{key}.png"), optimize=True)
        print(f"body {key}")
    contact_sheet(made).save(os.path.join(HERE, "contact-sheet.png"))
    print("contact sheet:", os.path.join(HERE, "contact-sheet.png"))


if __name__ == "__main__":
    main()
