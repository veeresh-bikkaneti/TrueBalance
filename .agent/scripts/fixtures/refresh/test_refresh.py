"""Run: python3 .agent/scripts/fixtures/refresh/test_refresh.py"""
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

FIXTURE = Path(__file__).resolve().parent
LINT = FIXTURE.parents[1] / "wiki_lint.py"
STALE = "0123456789abcdef0123456789abcdef01234567"


class RefreshTest(unittest.TestCase):
    def test_refresh_fixes_the_stale_hash_only_and_a_second_run_changes_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "fixture"
            shutil.copytree(FIXTURE, root, ignore=shutil.ignore_patterns("__pycache__"))
            page = root / "wiki" / "page.md"
            before = page.read_bytes()
            run(root, "git", "init", "-q")  # its own repo, so an ignored temp folder does not count
            current = run(root, "git", "hash-object", "--", "stale.txt").stdout.strip()

            first = run(root, sys.executable, str(LINT), "--refresh")

            self.assertEqual(0, first.returncode, first.stdout + first.stderr)
            # Only the checked: line changes; the prose copy and line endings stay.
            expected = before.replace(f"stale.txt: {STALE}".encode(), f"stale.txt: {current}".encode())
            self.assertEqual(expected, page.read_bytes())
            self.assertIn(f"stale.txt {STALE} -> {current}", first.stdout)
            self.assertNotIn("fresh.txt", first.stdout)

            second = run(root, sys.executable, str(LINT), "--refresh")

            self.assertEqual(0, second.returncode, second.stdout + second.stderr)
            self.assertIn("nothing to refresh", second.stdout)
            self.assertEqual(expected, page.read_bytes())


def run(cwd: Path, *command: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, cwd=cwd, capture_output=True, text=True, check=False)


if __name__ == "__main__":
    unittest.main()
