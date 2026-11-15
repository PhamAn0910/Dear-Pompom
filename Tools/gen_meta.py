#!/usr/bin/env python3
"""Create missing Unity .meta files with stable GUIDs.

Why: this project is edited on a machine without the Unity Editor. Unity would
create .meta files on import, but with a random GUID on every CI run. Writing
them here keeps GUIDs stable, so scenes/prefabs can reference new scripts later.

The GUID is the MD5 of the asset path, so re-running is harmless and two people
adding the same path get the same GUID. Existing .meta files are never touched.

Usage:  python3 Tools/gen_meta.py          (from the project root)
        python3 Tools/gen_meta.py --check  (exit 1 if any are missing; for CI)
"""
import hashlib
import os
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets")
# Unity picks the importer for these itself; a hand-written meta could disable the plugin.
SKIP_EXTENSIONS = {".jslib", ".jspre"}


def hidden(name):
    return name.startswith(".") or name.endswith("~") or name.endswith(".tmp")


def guid_for(rel_path):
    return hashlib.md5(("DearPompom:" + rel_path.replace(os.sep, "/")).encode("utf-8")).hexdigest()


def main():
    check_only = "--check" in sys.argv
    missing = []
    for folder, dirs, files in os.walk(ROOT):
        dirs[:] = sorted(d for d in dirs if not hidden(d))
        entries = [(d, True) for d in dirs] + [(f, False) for f in sorted(files)]
        for name, is_dir in entries:
            if hidden(name) or name.endswith(".meta"):
                continue
            if not is_dir and os.path.splitext(name)[1] in SKIP_EXTENSIONS:
                continue
            path = os.path.join(folder, name)
            if os.path.exists(path + ".meta"):
                continue
            rel = os.path.relpath(path, os.path.dirname(ROOT))
            missing.append(rel)
            if check_only:
                continue
            lines = ["fileFormatVersion: 2", "guid: " + guid_for(rel)]
            if is_dir:
                lines.append("folderAsset: yes")
            with open(path + ".meta", "w", encoding="utf-8") as handle:
                handle.write("\n".join(lines) + "\n")
    for rel in missing:
        print(("missing " if check_only else "created ") + rel + ".meta")
    print("%d meta file(s) %s" % (len(missing), "missing" if check_only else "created"))
    return 1 if (check_only and missing) else 0


if __name__ == "__main__":
    sys.exit(main())
