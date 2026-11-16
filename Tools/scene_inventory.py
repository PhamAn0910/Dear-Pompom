#!/usr/bin/env python3
"""Print the GameObject tree of every Unity scene and prefab as Markdown.

Why: this project is edited without the Unity Editor. Code that attaches behaviour at runtime
(see docs/guides/no-editor-unity.md) must find objects by name or by component, so the names
have to come from somewhere. This script reads the scene YAML and writes them down.

Usage (from the project root):
  python3 Tools/scene_inventory.py                 # rewrites docs/scenes/*.md
  python3 Tools/scene_inventory.py --check         # exit 1 if docs/scenes is out of date

It reads only; it never changes a scene.
"""
import glob
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(ROOT, "docs", "scenes")

# Unity class ids -> short names. Anything missing is printed as "Type<id>".
BUILTIN = {
    4: "Transform", 20: "Camera", 23: "MeshRenderer", 33: "MeshFilter", 50: "Rigidbody2D",
    58: "CircleCollider2D", 60: "PolygonCollider2D", 61: "BoxCollider2D", 81: "AudioListener",
    82: "AudioSource", 95: "Animator", 124: "Behaviour", 198: "ParticleSystem",
    199: "ParticleSystemRenderer", 212: "SpriteRenderer", 222: "CanvasRenderer", 223: "Canvas",
    224: "RectTransform", 225: "CanvasGroup", 328: "VideoPlayer", 1660057539: "SceneRoots",
}
# Built-in UI scripts live in packages; their GUIDs are stable across projects.
KNOWN_SCRIPTS = {
    "fe87c0e1cc204ed48ad3b37840f39efc": "Image",
    "4e29b1a8efbd4b44bb3f3716e73f07ff": "Button",
    "0cd44c1031e13a943bb63640046fad76": "CanvasScaler",
    "dc42784cf147c0c48a680349fa168899": "GraphicRaycaster",
    "76c392e42b5098c458856cdf6ecaaaa1": "EventSystem",
    "f4688fdb7df04437aeb418b961361dc5": "TextMeshProUGUI",
    "2da0c512f12947e489f739169773d7ca": "TMP_InputField",
    "5f7201a12d95ffc409449d95f23cf332": "Text",
    "1344c3c82d62a2a41a3576d8abb8e3ea": "RawImage",
    "67db9e8f0e2ae9c40bc1e2b64352a6b4": "Slider",
    "9085046f02f69544eb97fd06b6048fe2": "Toggle",
    "1aa08ab6e0800fa44ae55d278d1423e3": "ScrollRect",
    "31a19414c41e5ae4aae2af33fee712f6": "Mask",
    "3312d7739989d2b4e91e6319e9a96d76": "RectMask2D",
    "d199490a83bb2b844b9695cbf13b01ef": "ContentSizeFitter",
    "30649d3a9faa99c48a7b1166b86bf2a0": "HorizontalLayoutGroup",
    "59f8146938fff824cb5fd77236b75775": "VerticalLayoutGroup",
    "8a8695521f0d02e499659fee002a26c2": "GridLayoutGroup",
    "01614664b831546d2ae94a42149d80ac": "InputSystemUIInputModule",
    "4f231c4fb786f3946a6b90b886c48677": "StandaloneInputModule",
    "56666c5a40171f54783dd416a44f42bf": "Physics2DRaycaster",
    "a79441f348de89743a2939f4d699eac1": "UniversalAdditionalCameraData",
    "073797afb82c5a1438f328866b10b3f0": "Light2D",
    "e9620f8c34305754d8cc9a7e49e852d9": "TMP_Dropdown",
    "0d0b652f32a2cc243917e4028fa0f046": "Dropdown",
}


def script_names():
    """guid -> class file name, for every script under Assets/."""
    names = dict(KNOWN_SCRIPTS)
    for meta in glob.glob(os.path.join(ROOT, "Assets", "**", "*.cs.meta"), recursive=True):
        with open(meta, encoding="utf-8", errors="ignore") as handle:
            match = re.search(r"guid: ([0-9a-f]{32})", handle.read())
        if match:
            names[match.group(1)] = os.path.basename(meta)[:-len(".cs.meta")]
    return names


def asset_names():
    """guid -> file name for sprites, clips and controllers, so references are readable."""
    names = {}
    for meta in glob.glob(os.path.join(ROOT, "Assets", "**", "*.meta"), recursive=True):
        ext = os.path.splitext(meta[:-5])[1].lower()
        if ext in (".png", ".jpg", ".jpeg", ".anim", ".controller", ".mp3", ".wav", ".ogg", ".prefab"):
            with open(meta, encoding="utf-8", errors="ignore") as handle:
                match = re.search(r"guid: ([0-9a-f]{32})", handle.read())
            if match:
                names[match.group(1)] = os.path.relpath(meta[:-5], os.path.join(ROOT, "Assets"))
    return names


def parse(path):
    """Returns {file_id: (class_id, body_text)} for one YAML scene or prefab."""
    with open(path, encoding="utf-8", errors="ignore") as handle:
        text = handle.read()
    docs = {}
    for match in re.finditer(r"^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)", text, re.S | re.M):
        docs[match.group(2)] = (int(match.group(1)), match.group(3))
    return docs


def field(body, name):
    match = re.search(r"^\s*" + re.escape(name) + r": (.*)$", body, re.M)
    return match.group(1).strip() if match else ""


def vec(body, name):
    match = re.search(re.escape(name) + r": \{x: ([-\d.e]+), y: ([-\d.e]+)", body)
    return (float(match.group(1)), float(match.group(2))) if match else None


def describe(path, scripts, assets):
    docs = parse(path)
    lines = []
    objects = {fid: body for fid, (cid, body) in docs.items() if cid == 1}
    transforms = {fid: body for fid, (cid, body) in docs.items() if cid in (4, 224)}
    owner = {}       # transform id -> game object id
    for fid, body in transforms.items():
        match = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
        if match:
            owner[fid] = match.group(1)
    transform_of = {go: tid for tid, go in owner.items()}

    def components(go_id):
        result = []
        for match in re.finditer(r"- component: \{fileID: (-?\d+)\}", objects[go_id]):
            cid, body = docs.get(match.group(1), (0, ""))
            if cid in (4, 222):          # plain Transform and CanvasRenderer are noise
                continue
            if cid == 114:               # MonoBehaviour: resolve the script
                guid = re.search(r"m_Script: \{fileID: \d+, guid: ([0-9a-f]{32})", body)
                name = scripts.get(guid.group(1), "Script<" + guid.group(1)[:8] + ">") if guid else "MissingScript"
            else:
                name = BUILTIN.get(cid, "Type%d" % cid)
            extra = ""
            if name in ("Image", "SpriteRenderer"):
                sprite = re.search(r"m_Sprite: \{fileID: -?\d+, guid: ([0-9a-f]{32})", body)
                if sprite:
                    extra = " `" + assets.get(sprite.group(1), "?") + "`"
            elif name == "Animator":
                ctrl = re.search(r"m_Controller: \{fileID: -?\d+, guid: ([0-9a-f]{32})", body)
                if ctrl:
                    extra = " `" + assets.get(ctrl.group(1), "?") + "`"
            elif name == "Canvas":
                extra = " renderMode=%s sortingOrder=%s" % (field(body, "m_RenderMode"), field(body, "m_SortingOrder"))
            elif name == "CanvasScaler":
                ref = vec(body, "m_ReferenceResolution")
                extra = " scaleMode=%s reference=%s match=%s" % (
                    field(body, "m_UiScaleMode"), "%gx%g" % ref if ref else "?", field(body, "m_MatchWidthOrHeight"))
            elif name == "Camera":
                extra = " orthoSize=%s" % field(body, "orthographic size")
            elif name == "AudioSource":
                clip = re.search(r"m_audioClip: \{fileID: -?\d+, guid: ([0-9a-f]{32})", body)
                extra = (" `" + assets.get(clip.group(1), "?") + "`") if clip else ""
                extra += " playOnAwake=%s loop=%s" % (field(body, "m_PlayOnAwake"), field(body, "Loop"))
            elif name in ("TextMeshProUGUI", "Text"):
                text = field(body, "m_text") or field(body, "m_Text")
                if text:
                    extra = ' "' + text.strip("'\"")[:40] + '"'
            elif name == "Button":
                calls = re.findall(r"m_MethodName: (\w+)", body)
                if calls:
                    extra = " onClick -> " + ", ".join(calls)
            if cid == 114 and field(body, "sceneName"):
                extra += " sceneName=" + field(body, "sceneName")
            if field(body, "m_Enabled") == "0":
                extra += " (disabled)"
            result.append(name + extra)
        return result

    def walk(tid, depth):
        go_id = owner.get(tid)
        if go_id is None or go_id not in objects:
            return
        body = objects[go_id]
        name = field(body, "m_Name")
        active = "" if field(body, "m_IsActive") != "0" else " **(inactive)**"
        tbody = transforms[tid]
        where = ""
        if docs[tid][0] == 224:
            pos, size = vec(tbody, "m_AnchoredPosition"), vec(tbody, "m_SizeDelta")
            if pos and size:
                where = " — ui pos (%g, %g) size (%g, %g)" % (pos + size)
        else:
            pos, scale = vec(tbody, "m_LocalPosition"), vec(tbody, "m_LocalScale")
            if pos and scale:
                where = " — pos (%g, %g) scale (%g, %g)" % (pos + scale)
        comps = components(go_id)
        lines.append("%s- `%s`%s%s%s" % ("  " * depth, name, active, where, (": " + "; ".join(comps)) if comps else ""))
        children = re.findall(r"^\s*- \{fileID: (-?\d+)\}", re.search(r"m_Children:(.*?)m_Father", tbody, re.S).group(1), re.M) \
            if "m_Children:" in tbody and "m_Father" in tbody else []
        for child in children:
            walk(child, depth + 1)

    roots = [tid for tid, body in transforms.items() if re.search(r"m_Father: \{fileID: 0\}", body)]
    # Keep the editor's root order when the scene records it.
    order = []
    for cid, body in docs.values():
        if cid == 1660057539:
            order = re.findall(r"- \{fileID: (-?\d+)\}", body)
    roots.sort(key=lambda t: order.index(t) if t in order else len(order))
    for tid in roots:
        walk(tid, 0)

    stripped = sum(1 for cid, _ in docs.values() if cid == 1001)
    if stripped:
        lines.append("")
        lines.append("Prefab instances in this file: %d (their inner objects are listed in the prefab's own page)." % stripped)
        for cid, body in docs.values():
            if cid == 1001:
                src = re.search(r"m_SourcePrefab: \{fileID: -?\d+, guid: ([0-9a-f]{32})", body)
                lines.append("- instance of `%s`" % (assets.get(src.group(1), "?") if src else "?"))
    return lines


def main():
    check = "--check" in sys.argv
    scripts, assets = script_names(), asset_names()
    targets = sorted(glob.glob(os.path.join(ROOT, "Assets", "Scenes", "*.unity"))) + \
        sorted(glob.glob(os.path.join(ROOT, "Assets", "Prefab", "*.prefab")))
    pages = {}
    for path in targets:
        rel = os.path.relpath(path, ROOT)
        name = os.path.splitext(os.path.basename(path))[0]
        kind = "scene" if path.endswith(".unity") else "prefab"
        text = "# %s (%s)\n\n" % (name, kind)
        text += "Generated by `python3 Tools/scene_inventory.py` from `%s`. Do not edit by hand.\n\n" % rel
        text += "Each line: `object name` — position: components. Indentation is the parent/child tree.\n\n"
        text += "\n".join(describe(path, scripts, assets)) + "\n"
        pages[("prefab-" if kind == "prefab" else "") + name.replace(" ", "_") + ".md"] = text

    stale = []
    if not check:
        os.makedirs(OUT, exist_ok=True)
    for file_name, text in pages.items():
        target = os.path.join(OUT, file_name)
        old = open(target, encoding="utf-8").read() if os.path.exists(target) else None
        if old != text:
            stale.append(file_name)
            if not check:
                with open(target, "w", encoding="utf-8") as handle:
                    handle.write(text)
    print("%d page(s), %d %s" % (len(pages), len(stale), "out of date" if check else "written"))
    return 1 if (check and stale) else 0


if __name__ == "__main__":
    sys.exit(main())
