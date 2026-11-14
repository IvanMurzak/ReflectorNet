"""Regenerate the README images with the shared svgkit style.

Usage (from the repo root): py -3 docs/assets/build-svgs.py
Writes docs/assets/hero.svg and docs/assets/flow.svg.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import svgkit as K  # noqa: E402


def code_card():
    """Right side of the hero: a static editor card with real ReflectorNet output (x 820..1180)."""
    x, y, w, h = 820, 56, 360, 272
    s = [f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="14" fill="#070D14" stroke="{K.HAIR_STRONG}" stroke-opacity=".6"/>',
         f'<rect x="{x}" y="{y}" width="{w}" height="40" rx="14" fill="{K.SURF}"/>',
         f'<rect x="{x}" y="{y + 26}" width="{w}" height="14" fill="{K.SURF}"/>',
         f'<rect x="{x}" y="{y + 40}" width="{w}" height="1" fill="{K.HAIR}"/>']
    for i, c in enumerate(("#FF5F57", "#FEBC2E", "#28C840")):
        s.append(f'<circle cx="{x + 22 + i * 20}" cy="{y + 20}" r="6" fill="{c}"/>')
    s.append(f'<text x="{x + w - 18}" y="{y + 26}" text-anchor="end" class="m" font-size="16" fill="{K.MUTED}">Program.cs</text>')
    T, M, KEY, STR, NUM, OK, RED = K.TEXT, K.MUTED, "#A5C8E4", "#F5D08A", "#F59AB0", K.GREEN, K.RED_TEXT
    lines = [
        [(RED, "› "), (T, "reflector.Serialize(player)")],
        [(T, "{ "), (KEY, '"name"'), (T, ": "), (STR, '"player"'), (T, ",")],
        [(T, "  "), (KEY, '"typeName"'), (T, ": "), (STR, '"Player"'), (T, ",")],
        [(T, "  "), (KEY, '"props"'), (T, ": [ { "), (KEY, '"name"'), (T, ": "), (STR, '"Level"'), (T, ",")],
        [(T, "      "), (KEY, '"value"'), (T, ": "), (NUM, "7"), (T, " }, … ] }")],
        [(RED, "› "), (T, "reflector.MethodCall(…"), (STR, '"Buy"'), (T, "…)")],
        [(OK, "[Success] "), (STR, '"Bought 2 x potion"')],
    ]
    for i, parts in enumerate(lines):
        n = sum(len(t) for _, t in parts)
        assert n * 9.7 <= w - 36, (n, parts)
        spans = "".join(f'<tspan fill="{c}">{K.esc(t)}</tspan>' for c, t in parts)
        s.append(f'<text x="{x + 18}" y="{y + 74 + i * 29}" class="m" font-size="16">{spans}</text>')
    s.append(f'<rect class="blink" x="{x + 26 + 28 * 9.7:.0f}" y="{y + 74 + 6 * 29 - 15}" width="9" height="19" fill="{K.TEXT}"/>')
    return "".join(s)


def hero():
    return K.hero(
        title="Reflector", accent="Net", eyebrow_text=".NET reflection toolkit", accent_fx="flow",
        subtitle=["Live objects to JSON and back,", "JSON Schema and method calls for AI agents."],
        chip_items=["netstandard2.1", "net8.0", "net9.0"],
        right=K.mark(688, 190, 76) + code_card(),
        label="ReflectorNet: .NET reflection toolkit. Live objects to JSON and back, JSON Schema and method calls "
              "for AI agents")


def flow():
    W, H = 1200, 470
    cw, gap, x0, cy, ch = 256, 32, 40, 96, 128
    cards = [
        ("Live object", "your .NET instance", "TryModifyAt · TryPatch", "blue"),
        ("SerializedMember", "JSON with type names", "Serialize · Deserialize", "red"),
        ("JSON Schema", "of types and arguments", "GetArgumentsSchema", "yellow"),
        ("Method call", "MethodRef + JSON args", "FindMethod · MethodCall", "green"),
    ]
    b = [K.eyebrow(40, 56, "How it works")]
    for i, (title, sub, sub2, acc) in enumerate(cards):
        x = x0 + i * (cw + gap)
        for t in (sub, sub2):
            assert len(t) * 8.6 <= cw - 40, t
        b.append(K.card(x, cy, cw, ch, title, sub=sub, sub2=sub2, accent=acc))
    mid = cy + ch / 2 - 12
    for i in range(3):
        xa = x0 + (i + 1) * cw + i * gap + 2
        b.append(K.wire(f"M{xa} {mid} H{xa + gap - 4}", color=K.BLUE, dur=1.6, begin=i * 0.5))
    ay, ah = 340, 96
    b.append(K.card(x0, ay, W - 2 * x0, ah, "AI agent or tool host",
                    sub="reads state as JSON · learns argument shapes from the schema · sends patches and calls",
                    accent="none"))
    verbs = [("patch", K.RED_TEXT, True), ("read", K.BLUE, False), ("schema", K.BLUE, False), ("call", K.RED_TEXT, True)]
    for i, (verb, col, up) in enumerate(verbs):
        cx = x0 + i * (cw + gap) + cw / 2
        y1, y2 = cy + ch + 4, ay - 4
        b.append(K.wire(f"M{cx} {y1} V{y2}", color=col, dur=1.4, begin=i * 0.3, reverse=up))
        b.append(f'<text x="{cx + 12}" y="{(y1 + y2) / 2 + 6:.0f}" class="m" font-size="16" fill="{K.MUTED}">{verb}</text>')
    label = ("ReflectorNet flow: a live .NET object is serialized to SerializedMember JSON and changed back in place; "
             "types and methods produce JSON Schema; an AI agent reads the JSON and the schema and sends patches and "
             "JSON method calls that ReflectorNet finds and invokes")
    return K.panel(W, H, "".join(b), label)


if __name__ == "__main__":
    K.write(os.path.join(HERE, "hero.svg"), hero())
    K.write(os.path.join(HERE, "flow.svg"), flow())
