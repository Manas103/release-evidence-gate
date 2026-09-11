"""
Cross-references a generated CycloneDX SBOM's components against a small
"newly disclosed vulnerability" fixture (data/known-vulnerable.json) and
fails (non-zero exit) when a component name+version match is found. This is
the merge-blocking step: a red exit here is meant to fail a CI job, not just
print a warning.
"""
import argparse
import json
import sys
from pathlib import Path


def load_sbom_components(sbom_path: Path) -> list[dict]:
    sbom = json.loads(sbom_path.read_text())
    return sbom.get("components", [])


def load_known_vulnerable(fixture_path: Path) -> list[dict]:
    data = json.loads(fixture_path.read_text())
    return data.get("vulnerabilities", [])


def find_matches(components: list[dict], vulnerabilities: list[dict]) -> list[dict]:
    matches = []
    for comp in components:
        comp_name = comp["name"].strip().lower()
        comp_version = comp["version"].strip()
        for vuln in vulnerabilities:
            if comp_name == vuln["name"].strip().lower() and comp_version == vuln["version"].strip():
                matches.append({"component": comp, "vulnerability": vuln})
    return matches


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sbom", default="docs/sbom_output.json")
    parser.add_argument("--known-vulnerable", default="data/known-vulnerable.json")
    args = parser.parse_args()

    components = load_sbom_components(Path(args.sbom))
    vulnerabilities = load_known_vulnerable(Path(args.known_vulnerable))

    print(f"checked {len(components)} SBOM components against {len(vulnerabilities)} known-vulnerable fixture entries")

    matches = find_matches(components, vulnerabilities)
    if matches:
        print(f"BLOCKED: {len(matches)} vulnerable dependency match(es) found:")
        for m in matches:
            c, v = m["component"], m["vulnerability"]
            print(f"  - {c['name']}@{c['version']} matches {v['cve']} ({v['severity']}): {v['note']}")
        print("MERGE BLOCK RESULT: FAIL")
        return 1

    print("no known-vulnerable components found")
    print("MERGE BLOCK RESULT: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
