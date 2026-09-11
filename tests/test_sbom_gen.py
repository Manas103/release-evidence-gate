import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import sbom_gen  # noqa: E402

REPO_ROOT = Path(__file__).resolve().parents[1]


def test_manifest_totals_exactly_63():
    python_deps = sbom_gen.load_python_manifest(REPO_ROOT / "data" / "python-requirements.txt")
    dotnet_deps = sbom_gen.load_dotnet_manifest(REPO_ROOT / "data" / "dotnet-packages.csv")
    assert len(python_deps) == 40
    assert len(dotnet_deps) == 23
    assert len(python_deps) + len(dotnet_deps) == 63


def test_sbom_has_one_component_per_dependency_no_duplicates():
    python_deps = sbom_gen.load_python_manifest(REPO_ROOT / "data" / "python-requirements.txt")
    dotnet_deps = sbom_gen.load_dotnet_manifest(REPO_ROOT / "data" / "dotnet-packages.csv")
    sbom = sbom_gen.build_sbom(python_deps, dotnet_deps)

    assert len(sbom["components"]) == 63
    purls = [c["purl"] for c in sbom["components"]]
    assert len(purls) == len(set(purls)), "duplicate purl in generated SBOM"


def test_sbom_passes_cyclonedx_1_5_schema_validation():
    from cyclonedx.schema import SchemaVersion
    from cyclonedx.validation.json import JsonStrictValidator

    python_deps = sbom_gen.load_python_manifest(REPO_ROOT / "data" / "python-requirements.txt")
    dotnet_deps = sbom_gen.load_dotnet_manifest(REPO_ROOT / "data" / "dotnet-packages.csv")
    sbom = sbom_gen.build_sbom(python_deps, dotnet_deps)

    validator = JsonStrictValidator(SchemaVersion.V1_5)
    errors = validator.validate_str(json.dumps(sbom), all_errors=True)
    assert not errors, f"schema validation errors: {list(errors) if errors else errors}"


def test_bom_format_and_spec_version_fields():
    sbom = sbom_gen.build_sbom([("a", "1.0.0")], [("B", "2.0.0")])
    assert sbom["bomFormat"] == "CycloneDX"
    assert sbom["specVersion"] == "1.5"
    assert sbom["serialNumber"].startswith("urn:uuid:")
