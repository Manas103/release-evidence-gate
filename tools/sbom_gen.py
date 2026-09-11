"""
Generates a CycloneDX 1.5 JSON SBOM for a synthetic "audited service" made of
a Python dependency manifest (data/python-requirements.txt, 40 entries) and a
.NET dependency manifest (data/dotnet-packages.csv, 23 entries), 63 total.
These are a representative dependency manifest for a hypothetical audited
service, not this repository's own install graph (this repo's own tooling
needs are in requirements-tools.txt).

Validates the emitted document against the real CycloneDX 1.5 JSON schema
bundled with cyclonedx-python-lib, not just by inspection.
"""
import argparse
import csv
import json
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path

from cyclonedx.schema import SchemaVersion
from cyclonedx.validation.json import JsonStrictValidator


def load_python_manifest(path: Path) -> list[tuple[str, str]]:
    deps = []
    for line in path.read_text().splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        name, _, version = line.partition("==")
        deps.append((name.strip(), version.strip()))
    return deps


def load_dotnet_manifest(path: Path) -> list[tuple[str, str]]:
    deps = []
    with path.open(newline="") as f:
        for row in csv.DictReader(f):
            deps.append((row["name"].strip(), row["version"].strip()))
    return deps


def build_sbom(python_deps, dotnet_deps) -> dict:
    components = []
    for name, version in python_deps:
        purl = f"pkg:pypi/{name}@{version}"
        components.append({
            "type": "library",
            "bom-ref": purl,
            "name": name,
            "version": version,
            "purl": purl,
        })
    for name, version in dotnet_deps:
        purl = f"pkg:nuget/{name}@{version}"
        components.append({
            "type": "library",
            "bom-ref": purl,
            "name": name,
            "version": version,
            "purl": purl,
        })

    return {
        "bomFormat": "CycloneDX",
        "specVersion": "1.5",
        "serialNumber": f"urn:uuid:{uuid.uuid4()}",
        "version": 1,
        "metadata": {
            "timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "component": {
                "type": "application",
                "bom-ref": "release-evidence-gate-audited-service",
                "name": "release-evidence-gate-audited-service",
                "version": "1.0.0",
            },
        },
        "components": components,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--python-manifest", default="data/python-requirements.txt")
    parser.add_argument("--dotnet-manifest", default="data/dotnet-packages.csv")
    parser.add_argument("--out", default="docs/sbom_output.json")
    args = parser.parse_args()

    python_deps = load_python_manifest(Path(args.python_manifest))
    dotnet_deps = load_dotnet_manifest(Path(args.dotnet_manifest))
    total = len(python_deps) + len(dotnet_deps)

    print(f"python dependencies : {len(python_deps)}")
    print(f"dotnet dependencies : {len(dotnet_deps)}")
    print(f"total dependencies  : {total}")

    sbom = build_sbom(python_deps, dotnet_deps)
    sbom_json = json.dumps(sbom, indent=2)

    validator = JsonStrictValidator(SchemaVersion.V1_5)
    errors = validator.validate_str(sbom_json, all_errors=True)
    if errors:
        print("SBOM FAILED CycloneDX 1.5 schema validation:", file=sys.stderr)
        for e in errors:
            print(f"  - {e}", file=sys.stderr)
        return 1

    out_path = Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(sbom_json)

    print(f"wrote {out_path} -- {len(sbom['components'])} components, valid CycloneDX 1.5")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
