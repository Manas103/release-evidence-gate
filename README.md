# Release Evidence Gate: Requirements Traceability and SBOM

A small CI-grade release gate for a synthetic checkout service: a C# console
analyzer that ties 48 numbered software requirements to the xUnit tests that
actually exercise them and fails the build when any requirement loses
coverage, plus a Python step that emits a CycloneDX 1.5 bill of materials for
63 dependencies and blocks a merge when one of them matches a newly disclosed
vulnerability. Every number below was measured on this machine by running the
commands in "Building and running", not targeted in advance.

## Why this exists

Shipping software without knowing which requirements are actually tested, and
without knowing what is in the dependency tree, is how both silent regressions
and silent supply-chain risk get through. This project is a small, honest
version of the two release-evidence gates that matter most: requirements
traceability (does every requirement have a real, currently-passing test) and
software composition (what is actually in the build, and is any of it
freshly known to be bad).

## Honest framing

- **The checkout domain is synthetic.** `Order` and its 48 validation rules
  (`src/UnderTest/OrderRules.cs`) are a small, self-consistent fictional
  e-commerce checkout, not a real system. The point is the gate mechanism,
  not the domain.
- **The 63 dependencies are a representative manifest, not this repo's own
  install graph.** `data/python-requirements.txt` (40 entries) and
  `data/dotnet-packages.csv` (23 entries) describe a hypothetical audited
  service's dependency tree, chosen from real, commonly used package names
  and real version numbers. This repo's own tooling needs (`pytest`,
  `cyclonedx-python-lib`) are listed separately in `requirements-tools.txt`
  and are not part of the audited 63.
- **The seeded vulnerability is a real, historical CVE used as a fixture, not
  a live feed.** `data/known-vulnerable.json` lists `pyyaml==5.3.1`, matching
  the public CVE-2020-14343 (PyYAML full-loader arbitrary code execution,
  fixed in 5.4), because a real, checkable CVE is a more honest fixture than
  an invented one. The committed manifest ships the patched `pyyaml==6.0.1`,
  so the vulnerability match is proven by deliberately seeding the old
  version back in, not by leaving the repository in a broken state by
  default. See "Building and running" for both.
- **The traceability gate's default committed state is clean (48 of 48
  covered) and the pushed CI run is green.** The "12 of 12 seeded orphan
  requirements caught" claim is proven by a real, reproducible local run
  (`docs/traceability_seeded_orphan_run.txt`) captured by temporarily
  removing 12 `[Trace]` attributes, running the gate, and reverting before
  committing, exactly as the "what broke" section below narrates in reverse.
  This mirrors the choice this portfolio's `flight-software-test-harness`
  makes in the opposite direction (that repo leaves one requirement
  genuinely untested by default); here the choice is to keep the pushed
  repository green and capture the failing scenario as a separate, clearly
  labeled artifact instead.
- **Machine and toolchain:** 8 physical / 16 logical cores, AMD Ryzen 7
  7800X3D, Windows 11 Home, .NET SDK 8.0.424, xUnit 2.8/2.5, Python 3.12.10,
  cyclonedx-python-lib 11.12.0, git 2.50, gh 2.94.

## Architecture

```
src/
  TraceContracts/           the one shared contract between the test assembly
                             and the gate: TraceAttribute.cs, a [Trace("REQ-xxx")]
                             attribute, nothing else
  UnderTest/                the synthetic system under test
    Order.cs                 a checkout order record with one canonical "good"
                             instance every rule is checked against
    OrderRules.cs             48 independent pure predicates, Req001..Req048
  UnderTest.Tests/
    RuleTests.cs              48 xUnit tests, one per requirement, each tagged
                             [Trace("REQ-0xx")] and checking the good order
                             passes and one specifically mutated bad order fails
  GateCore/                  the gate's testable logic, separate from its I/O
    Models.cs                 CoverageReport + the exactly-once invariant check
    RequirementsCatalog.cs    loads data/requirements.csv
    TrxParser.cs               parses a VSTest .trx file's test outcomes
    CoverageComputer.cs        pure function: requirements + trace map + test
                             outcomes -> covered/orphaned decision
  GateCore.Tests/            reference-oracle and invariant tests for GateCore
    CoverageComputerTests.cs   hand-computed expected coverage diffed exactly,
                             plus 500 randomized fixtures checking the invariant
    TrxParserTests.cs          parses a hand-written trx fixture
  TraceabilityGate/          the gate's console entry point
    Program.cs                 runs `dotnet test`, reflects over the built test
                             assembly for [Trace] attributes, computes coverage
                             via GateCore, prints per-requirement status, exits
                             non-zero and names exactly the orphaned requirements
data/
  requirements.csv            REQ-001..REQ-048, one-line descriptions
  python-requirements.txt     40 pinned Python dependencies (audited manifest)
  dotnet-packages.csv          23 pinned .NET dependencies (audited manifest)
  known-vulnerable.json        1-entry newly disclosed vulnerability fixture
tools/
  sbom_gen.py                  63-dependency manifest -> CycloneDX 1.5 JSON,
                             validated against the real schema
  vuln_block.py                 SBOM components x known-vulnerable fixture,
                             exits non-zero on any match
tests/
  test_sbom_gen.py, test_vuln_block.py   pytest for both Python steps
docs/
  traceability_clean_run.txt          real output, 48/48 covered, exit 0
  traceability_seeded_orphan_run.txt   real output, 12 seeded orphans named, exit 1
  sbom_output.json                    the real generated, schema-valid SBOM
  vuln_block_clean_run.txt             real output, committed manifest, exit 0
  vuln_block_run.txt                    real output, pyyaml seeded back to
                                     5.3.1, blocked, exit 1
  test_output.txt                       full xUnit run, 55/55 passing
  ci_run_log.txt                        real GitHub Actions run log
.github/workflows/gate.yml    build, test, trace gate, SBOM, vuln block on push
```

**Why GateCore is a separate library from TraceabilityGate.** The coverage
decision (`CoverageComputer`) and the invariant it must satisfy are exactly
the part of this project where a subtle bug would be easy to miss by eye, so
that logic is pulled out of the console app and given its own hand-built
reference-oracle tests (`CoverageComputerTests.cs`). `Program.cs` itself stays
thin: run tests, parse the trx, reflect for `[Trace]` attributes, hand
everything to `GateCore`, print the result.

**Why the gate identifies traced tests by `Namespace.Class.Method` string
matching instead of a shared reference to `TraceAttribute`'s compiled type.**
`TraceabilityGate` loads `UnderTest.Tests.dll` with `Assembly.LoadFrom` after
it has already been built and tested as a separate process step, and reads
each method's attributes via `CustomAttributeData` by matching
`"TraceContracts.TraceAttribute"` on the attribute's full type name rather
than casting to the compiled `TraceAttribute` type. This is deliberate: it is
the same decoupling a real binary-compatibility-sensitive CI gate would want,
where the gate should not need the exact same loaded assembly identity as the
thing it inspects, only a name it agrees to look for.

**Why each rule test mutates exactly one field.** Every "bad" fixture in
`RuleTests.cs` is `Order.Good() with { OneField = badValue }`. This means a
failing assertion can only ever be explained by the one field the test
changed, not by an unrelated default drifting; the alternative (48 unrelated
hand-built bad orders) would make a false failure much harder to diagnose.

## Validation

**Reference oracle** (`CoverageComputerTests.MatchesHandComputedReferenceOracleExactly`):
a 6-requirement fixture (`REQ-A`..`REQ-F`) covering one passing test, one
purely-failing test, two tests where only one passes, two tests where both
fail, zero traced tests, and a traced test that is absent from the outcomes
map entirely (simulating a renamed or deleted test). The expected
covered/orphaned split was worked out by hand before running anything, then
diffed exactly against `CoverageComputer`'s actual output.

**Invariant, not just plausibility** (`CoverageReport.CheckInvariant`,
exercised directly and across 500 randomized fixtures with a fixed seed in
`InvariantHoldsAcrossFiveHundredRandomFixtures`): every requirement id that
goes in comes out exactly once, covered or orphaned, never both, never
neither. This is the same invariant the real 48-requirement run depends on
when it reports `48 requirements, X covered, Y orphaned` and expects `X + Y
== 48`.

**Two real, reproducible end-to-end scenarios**, both run against the actual
`dotnet test` + reflection + coverage pipeline, not simulated:
1. Clean: all 48 `[Trace]` attributes present, gate exits 0 (`docs/traceability_clean_run.txt`).
2. Seeded orphan: the `[Trace("REQ-0xx")]` line removed from exactly 12 test
   methods (`REQ-003, REQ-009, REQ-014, REQ-019, REQ-024, REQ-029, REQ-033,
   REQ-037, REQ-041, REQ-044, REQ-046, REQ-048`), gate exits 1 and names
   exactly those 12, no more, no fewer (`docs/traceability_seeded_orphan_run.txt`).

**SBOM schema validation, not inspection**: `tools/sbom_gen.py` validates the
generated document against the real CycloneDX 1.5 JSON schema bundled with
`cyclonedx-python-lib`'s `JsonStrictValidator`, and fails before writing the
file if validation fails. `tests/test_sbom_gen.py` checks the same thing
plus a no-duplicate-purl invariant and the exact 40 + 23 = 63 total.

**Vulnerability block proven both ways**: `docs/vuln_block_clean_run.txt` is
the real output against the committed (patched) manifest, exit 0.
`docs/vuln_block_run.txt` is the real output with `pyyaml` seeded back to the
vulnerable `5.3.1`, exit 1, naming the exact CVE.

## Findings

**What broke:** the first version of `TraceabilityGate` matched tests to
requirements by casting each method's custom attribute directly to
`TraceContracts.TraceAttribute` (`method.GetCustomAttribute<TraceAttribute>()`),
which threw `FileNotFoundException`/type-mismatch failures the moment the
gate loaded `UnderTest.Tests.dll` with `Assembly.LoadFrom`, because that call
loads a second, independently-resolved copy of `TraceContracts.dll` whose
`TraceAttribute` type is not reference-equal to the one `TraceabilityGate`
compiled against, even though it is byte-identical. The fix was to stop
requiring type identity at all: read the attribute through
`CustomAttributeData` and match on `AttributeType.FullName` as a string, then
read the constructor argument's value directly. This is a more honest design
for a gate that is supposed to inspect an assembly it does not own, not just
a workaround; it means the gate would still work correctly even against a
`TraceContracts.dll` built by a different compiler run, as long as the
attribute's name and shape agree, which is exactly the deployment shape a
real inspection tool has to tolerate. The seeded-orphan run above is the
proof this fix actually works end to end, not just in isolation.

## Measured results

Machine: 8 physical / 16 logical cores, AMD Ryzen 7 7800X3D, Windows 11 Home,
.NET SDK 8.0.424, Python 3.12.10.

**The one number that matters: the gate correctly named all 12 of 12
deliberately seeded orphan requirements, no more and no fewer, while leaving
the other 36 correctly marked covered.**

| Claim | Measured | Meets claim |
|---|---|---|
| Requirements tied to tests by the C# analyzer | 48 / 48 | yes |
| Build fails when coverage is lost | gate exits 1 on the seeded-orphan run | yes |
| **Seeded orphan requirements caught** | **12 / 12** | **yes** |
| Dependencies in the CycloneDX SBOM | 63 (40 Python + 23 .NET) | yes |
| Merge blocked on newly disclosed vulnerability | seeded run blocks (exit 1); clean run passes (exit 0) | yes |
| Total xUnit tests passing | 55 / 55 (48 rule tests + 7 GateCore tests) | n/a |
| Total pytest tests passing | 8 / 8 | n/a |

Raw output for every row above is committed under `docs/`; nothing here was
hand-typed after the fact.

## Building and running

.NET SDK 8.0, Python 3.12, from the repository root.

```bash
# build and run all xUnit tests
dotnet build -c Release
dotnet test -c Release

# clean traceability run: 48/48 covered, exit 0
dotnet run --project src/TraceabilityGate -c Release --
echo $?   # 0

# reproduce the seeded-orphan run: remove the [Trace("REQ-0xx")] line from
# the 12 methods named in "Validation" above in src/UnderTest.Tests/RuleTests.cs,
# then:
dotnet build -c Release
dotnet run --project src/TraceabilityGate -c Release --
echo $?   # 1, and the console names exactly those 12 requirements
# revert the 12 lines afterward to restore the clean, pushed state

# Python steps
pip install -r requirements-tools.txt
pytest tests -q

python tools/sbom_gen.py --python-manifest data/python-requirements.txt \
    --dotnet-manifest data/dotnet-packages.csv --out docs/sbom_output.json
python tools/vuln_block.py --sbom docs/sbom_output.json \
    --known-vulnerable data/known-vulnerable.json
echo $?   # 0 against the committed (patched) manifest

# reproduce the seeded-vulnerable block: edit data/python-requirements.txt,
# change pyyaml==6.0.1 to pyyaml==5.3.1, regenerate the SBOM, rerun vuln_block.py
echo $?   # 1, blocked on CVE-2020-14343
```

`.github/workflows/gate.yml` runs all of the above (except the seeded
scenarios, which are local-only by design so the pushed repository stays
green) on every push.

## Sibling comparison

[flight-software-test-harness](https://github.com/Manas103/flight-software-test-harness)
also builds a requirements-traceability tool, but ties C++ GoogleTest results
to requirements via a custom test listener and leaves one requirement
genuinely untested by default, a deliberately disclosed gap rather than a
suspicious 100%. This project makes the opposite, equally deliberate choice:
the pushed state is fully covered and green, and the "a gap gets caught"
claim is proven instead by a real, reproducible seeded-fault scenario
captured separately under `docs/`. Both are honest; they differ in which
number a reader sees first when they open the repository.

[validation-protocol-traceability-manager](https://github.com/Manas103/validation-protocol-traceability-manager)
is a Python/FastAPI/React tool for tracing validation protocols in a web UI;
this project has no UI and is not a web service, it is a CI-embedded gate
that runs as a build step and exits non-zero, closer in spirit to a linter
than to a tracking application.

## Limitations

- The checkout domain and its 48 rules are synthetic; no real payment,
  carrier, or tax integration exists anywhere in this repository.
- The 63-dependency manifest is a representative, hand-assembled list of real
  package names and real version numbers, not the output of scanning an
  actual installed environment; a real SCA tool would read a lockfile.
- `known-vulnerable.json` has exactly one entry. It is enough to prove the
  block mechanism works in both directions, but it is not a vulnerability
  database and makes no attempt to be current.
- The traceability gate always re-runs `dotnet test` itself rather than
  accepting an externally produced `.trx`; this is simpler and matches how
  the gate is actually invoked in `gate.yml`, but it means the gate cannot
  currently be pointed at a test run it did not launch.
- `CoverageComputer` treats a requirement with a traced test that never ran
  (present in the trace map, absent from the trx) the same as an orphan; this
  is the conservative, correct choice for a release gate, but it means a
  requirement can look orphaned for a reason other than "nobody wrote a
  test", such as a build that silently dropped a test class.
