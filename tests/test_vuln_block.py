import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))

import vuln_block  # noqa: E402


def test_matches_when_seeded_vulnerable_version_present():
    components = [{"name": "pyyaml", "version": "5.3.1"}, {"name": "requests", "version": "2.31.0"}]
    vulnerabilities = [{"name": "pyyaml", "version": "5.3.1", "cve": "CVE-2020-14343", "severity": "high", "note": "x"}]

    matches = vuln_block.find_matches(components, vulnerabilities)
    assert len(matches) == 1
    assert matches[0]["component"]["name"] == "pyyaml"


def test_no_match_when_version_is_patched():
    components = [{"name": "pyyaml", "version": "6.0.1"}, {"name": "requests", "version": "2.31.0"}]
    vulnerabilities = [{"name": "pyyaml", "version": "5.3.1", "cve": "CVE-2020-14343", "severity": "high", "note": "x"}]

    matches = vuln_block.find_matches(components, vulnerabilities)
    assert matches == []


def test_name_match_is_case_insensitive_version_match_is_exact():
    components = [{"name": "PyYAML", "version": "5.3.1"}]
    vulnerabilities = [{"name": "pyyaml", "version": "5.3.1", "cve": "CVE-2020-14343", "severity": "high", "note": "x"}]
    assert len(vuln_block.find_matches(components, vulnerabilities)) == 1

    components_wrong_version = [{"name": "PyYAML", "version": "5.3.2"}]
    assert vuln_block.find_matches(components_wrong_version, vulnerabilities) == []


def test_current_committed_manifest_is_clean(tmp_path):
    """The manifest committed to this repo ships the patched pyyaml==6.0.1,
    so generating a real SBOM from it and running vuln_block against the
    real known-vulnerable fixture must pass. The seeded-vulnerable state is
    captured separately in docs/vuln_block_run.txt (see README)."""
    import sbom_gen

    repo_root = Path(__file__).resolve().parents[1]
    python_deps = sbom_gen.load_python_manifest(repo_root / "data" / "python-requirements.txt")
    dotnet_deps = sbom_gen.load_dotnet_manifest(repo_root / "data" / "dotnet-packages.csv")
    sbom = sbom_gen.build_sbom(python_deps, dotnet_deps)

    vulnerabilities = vuln_block.load_known_vulnerable(repo_root / "data" / "known-vulnerable.json")
    matches = vuln_block.find_matches(sbom["components"], vulnerabilities)
    assert matches == []
