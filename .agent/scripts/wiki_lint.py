#!/usr/bin/env python3
"""Deterministic linter for the local LLM wiki (raw/ + wiki/).

Checks broken (source: raw/...) pointers, broken [[wikilinks]], orphan pages,
PII-like patterns, and (repo:) pointers: missing files, line anchors past the
end, missing checked hashes, stale blob hashes, and forbidden paths.

    python3 .agent/scripts/wiki_lint.py
    python3 .agent/scripts/wiki_lint.py <fixture-root>
    python3 .agent/scripts/wiki_lint.py --refresh <page>
"""
import re
import subprocess
import sys
from pathlib import Path

SOURCE_RE = re.compile(r"\(source:\s*(raw/[^#,)\s]+)")
WIKILINK_RE = re.compile(r"\[\[([^\]|#]+)")
REPO_RE = re.compile(r"\(repo:\s*([^)#\s]+)(?:#L(\d+)(?:-L(\d+))?)?\)")
PII_RES = (
    ("SSN", re.compile(r"\b\d{3}-\d{2}-\d{4}\b")),
    ("EIN", re.compile(r"\b\d{2}-\d{7}\b")),
    ("CARD", re.compile(r"\b\d{13,19}\b")),
)
SMALL_FILE_MAX_LINES = 60
BLOB_RE = re.compile(r"^[0-9a-f]{40}$")

ROOT = Path.cwd()
WIKI_DIR = ROOT / "wiki"


def pii_errors() -> list[str]:
    files: list[Path] = []
    raw = ROOT / "raw"
    if raw.exists():
        for path in sorted(raw.rglob("*")):
            if not path.is_file():
                continue
            rel = path.relative_to(raw)
            if rel.parts and rel.parts[0] == "private":
                continue
            files.append(path)
    if WIKI_DIR.exists():
        files.extend(path for path in sorted(WIKI_DIR.rglob("*")) if path.is_file())

    errors = []
    for path in files:
        try:
            text = path.read_text(encoding="utf-8")
        except (UnicodeDecodeError, OSError):
            continue
        rel = path.relative_to(ROOT).as_posix()
        for label, pattern in PII_RES:
            if pattern.search(text):
                errors.append(f"PII {label} in {rel}")
    return errors


def git_run(args: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )


def blob_sha(path: Path) -> str:
    result = git_run(["hash-object", "--", str(path)])
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or "git hash-object failed")
    return result.stdout.strip()


def forbidden_reason(rel: str) -> str | None:
    norm = rel.replace("\\", "/").lstrip("/")
    parts = Path(norm).parts
    if ".." in parts:
        return "path escapes the tree"
    if norm == "raw/private" or norm.startswith("raw/private/"):
        return "raw/private"
    if norm == ".env" or norm.endswith("/.env"):
        return ".env"
    ignored = git_run(["check-ignore", "-q", "--", norm])
    if ignored.returncode == 0:
        return "gitignored"
    return None


def parse_checked(text: str) -> dict[str, str]:
    if not text.startswith("---\n"):
        return {}
    end = text.find("\n---", 3)
    if end == -1:
        return {}
    front = text[4:end]
    checked: dict[str, str] = {}
    in_checked = False
    for line in front.splitlines():
        if line.strip() == "checked:" or line.startswith("checked:"):
            in_checked = True
            continue
        if not in_checked:
            continue
        if line.startswith(" ") or line.startswith("\t"):
            body = line.strip()
            if not body or ":" not in body:
                continue
            path, _, value = body.partition(":")
            checked[path.strip()] = value.strip()
            continue
        in_checked = False
    return checked


def repo_pointers(text: str) -> list[tuple[str, int | None, int | None]]:
    found: list[tuple[str, int | None, int | None]] = []
    seen: set[str] = set()
    for match in REPO_RE.finditer(text):
        path = match.group(1)
        start = int(match.group(2)) if match.group(2) else None
        end = int(match.group(3)) if match.group(3) else None
        found.append((path, start, end))
        seen.add(path)
    return found


def unique_paths(pointers: list[tuple[str, int | None, int | None]]) -> list[str]:
    paths: list[str] = []
    seen: set[str] = set()
    for path, _, _ in pointers:
        if path not in seen:
            seen.add(path)
            paths.append(path)
    return paths


def line_count(path: Path) -> int:
    return len(path.read_text(encoding="utf-8").splitlines())


def repo_errors(page: Path, text: str) -> list[str]:
    rel = page.relative_to(ROOT).as_posix()
    checked = parse_checked(text)
    errors: list[str] = []
    hashed: set[str] = set()
    for path, start, end in repo_pointers(text):
        reason = forbidden_reason(path)
        if reason is not None:
            errors.append(f"FORBIDDEN in {rel}: {path} ({reason})")
            continue

        target = ROOT / path
        if not target.is_file():
            errors.append(f"BROKEN REPO in {rel}: {path} (file not found)")
            continue

        try:
            count = line_count(target)
        except (UnicodeDecodeError, OSError):
            errors.append(f"BROKEN REPO in {rel}: {path} (not a text file)")
            continue

        if start is None:
            if count >= SMALL_FILE_MAX_LINES:
                errors.append(
                    f"BROKEN REPO in {rel}: {path} (line anchor required; file has {count} lines)"
                )
        else:
            last = end if end is not None else start
            if start < 1 or last < start or last > count:
                errors.append(
                    f"BROKEN REPO in {rel}: {path}#L{start}-L{last} (line past end; file has {count} lines)"
                )

        if path in hashed:
            continue
        hashed.add(path)
        recorded = checked.get(path)
        if recorded is None:
            errors.append(f"UNCHECKED in {rel}: {path}")
            continue
        if not BLOB_RE.fullmatch(recorded):
            errors.append(f"STALE in {rel}: {path} (page hash is not a git blob sha)")
            continue
        current = blob_sha(target)
        if recorded != current:
            errors.append(f"STALE in {rel}: {path} (page has {recorded}, file is {current})")
    return errors


def render_checked(pairs: list[tuple[str, str]]) -> str:
    lines = ["checked:"]
    for path, sha in pairs:
        lines.append(f"  {path}: {sha}")
    return "\n".join(lines)


def replace_checked(text: str, block: str) -> str:
    if text.startswith("---\n"):
        end = text.find("\n---\n", 3)
    else:
        end = -1
    if end == -1:
        body = text if text.startswith("\n") else "\n" + text
        return f"---\n{block}\n---{body}"

    front_lines = text[4:end].splitlines()
    kept: list[str] = []
    index = 0
    replaced = False
    while index < len(front_lines):
        line = front_lines[index]
        if line.strip() == "checked:" or line.startswith("checked:"):
            if not replaced:
                kept.extend(block.splitlines())
                replaced = True
            index += 1
            while index < len(front_lines) and (
                front_lines[index].startswith(" ") or front_lines[index].startswith("\t") or front_lines[index].strip() == ""
            ):
                index += 1
            continue
        kept.append(line)
        index += 1
    if not replaced:
        if kept and kept[-1].strip():
            kept.append("")
        kept.extend(block.splitlines())
    front = "\n".join(kept)
    return f"---\n{front}\n---\n{text[end + 5:]}"


def refresh_page(page_arg: str) -> int:
    global ROOT
    ROOT = ROOT.resolve()
    page = Path(page_arg)
    if not page.is_file():
        page = ROOT / page_arg
    if not page.is_file():
        print(f"FAIL: page not found: {page_arg}")
        return 1
    page = page.resolve()
    text = page.read_text(encoding="utf-8")
    pairs: list[tuple[str, str]] = []
    for path in unique_paths(repo_pointers(text)):
        target = ROOT / path
        if not target.is_file():
            print(f"FAIL: cannot hash missing file {path}")
            return 1
        pairs.append((path, blob_sha(target)))
    if not pairs:
        print(f"no repo pointers in {page.relative_to(ROOT).as_posix()}; left unchanged")
        return 0
    updated = replace_checked(text, render_checked(pairs))
    page.write_text(updated, encoding="utf-8")
    print(f"refreshed {page.relative_to(ROOT).as_posix()} ({len(pairs)} hashes)")
    return 0


def main() -> int:
    global ROOT, WIKI_DIR
    args = sys.argv[1:]
    if args and args[0] == "--refresh":
        if len(args) != 2:
            print("usage: wiki_lint.py --refresh <page>")
            return 2
        return refresh_page(args[1])
    if len(args) > 1:
        print("usage: wiki_lint.py [<fixture-root>]")
        return 2
    ROOT = Path(args[0]).resolve() if args else Path.cwd()
    WIKI_DIR = ROOT / "wiki"

    if not WIKI_DIR.exists():
        print("FAIL: wiki/ directory not found.")
        return 1

    md_files = sorted(WIKI_DIR.rglob("*.md"))
    stems = {p.stem for p in md_files}
    rel_paths = {p.relative_to(WIKI_DIR).with_suffix("").as_posix() for p in md_files}
    inbound = {p.stem: 0 for p in md_files}

    errors = pii_errors()
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

        errors.extend(repo_errors(page, text))

    for page in md_files:
        if page.name in ("index.md", "log.md"):
            continue
        if inbound[page.stem] == 0:
            errors.append(f"ORPHAN PAGE: {page.relative_to(ROOT).as_posix()} (no inbound [[wikilink]])")

    if errors:
        print(f"wiki_lint: {len(errors)} issue(s) found:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print(f"wiki_lint: PASS ({len(md_files)} wiki pages checked, 0 broken sources/links/orphans, 0 PII patterns, 0 repo issues).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
