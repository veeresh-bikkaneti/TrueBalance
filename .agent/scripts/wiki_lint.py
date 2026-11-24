#!/usr/bin/env python3
"""Deterministic linter for Karpathy LLM Wiki (raw/ + wiki/).
Checks broken (source: raw/...) pointers, broken [[wikilinks]], and orphan pages.
Zero LLM tokens required.
"""
import re
import sys
from pathlib import Path

ROOT = Path.cwd()
WIKI_DIR = ROOT / "wiki"

SOURCE_RE = re.compile(r"\(source:\s*(raw/[^#,)\s]+)")
WIKILINK_RE = re.compile(r"\[\[([^\]|#]+)")


def main() -> int:
    if not WIKI_DIR.exists():
        print("FAIL: wiki/ directory not found.")
        return 1

    md_files = sorted(WIKI_DIR.rglob("*.md"))
    stems = {p.stem for p in md_files}
    rel_paths = {p.relative_to(WIKI_DIR).with_suffix("").as_posix() for p in md_files}
    inbound = {p.stem: 0 for p in md_files}

    errors = []
    for page in md_files:
        text = page.read_text(encoding="utf-8")
        rel = page.relative_to(ROOT).as_posix()

        for match in SOURCE_RE.finditer(text):
            raw_target = ROOT / match.group(1)
            if not raw_target.exists():
                errors.append(f"BROKEN SOURCE in {rel}: {match.group(1)}")

        for match in WIKILINK_RE.finditer(text):
            target = match.group(1).strip()
            target_stem = Path(target).stem
            if target not in rel_paths and target_stem not in stems:
                errors.append(f"BROKEN WIKILINK in {rel}: [[{target}]]")
            elif target_stem in inbound and target_stem != page.stem:
                inbound[target_stem] += 1

    for page in md_files:
        if page.name in ("index.md", "log.md"):
            continue
        if inbound[page.stem] == 0:
            errors.append(f"ORPHAN PAGE: {page.relative_to(ROOT).as_posix()} (no inbound [[wikilink]])")

    if errors:
        print(f"wiki_lint: {len(errors)} issue(s) found:")
        for e in errors:
            print(f"  - {e}")
        return 1

    print(f"wiki_lint: PASS ({len(md_files)} wiki pages checked, 0 broken sources/links/orphans).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
