#!/usr/bin/env python3
# scripts/trivy-to-sonar.py
"""
Converts Trivy JSON output to SonarQube Generic Issue Import format.
Usage: python3 trivy-to-sonar.py trivy-results.json > sonar-trivy-issues.json
"""

import json
import sys
from pathlib import Path

SEVERITY_MAP = {
    "CRITICAL": "CRITICAL",
    "HIGH": "MAJOR",
    "MEDIUM": "MINOR",
    "LOW": "INFO",
    "UNKNOWN": "INFO",
}

TYPE_MAP = {
    "CRITICAL": "VULNERABILITY",
    "HIGH": "VULNERABILITY",
    "MEDIUM": "VULNERABILITY",
    "LOW": "VULNERABILITY",
    "UNKNOWN": "VULNERABILITY",
}


def convert_trivy_to_sonar(trivy_file: str, project_base: str = "") -> dict:
    """Convert Trivy JSON to SonarQube external issues format."""
    with open(trivy_file, "r") as f:
        trivy_data = json.load(f)

    issues = []

    # Handle both filesystem scan and image scan formats
    results = trivy_data if isinstance(trivy_data, list) else trivy_data.get("Results", [])

    for result in results:
        target = result.get("Target", "unknown")
        result_type = result.get("Type", "unknown")
        vulnerabilities = result.get("Vulnerabilities", [])
        misconfigurations = result.get("Misconfigurations", [])
        secrets = result.get("Secrets", [])

        # ── Vulnerabilities (CVEs in dependencies) ──────────────
        for vuln in vulnerabilities:
            severity = SEVERITY_MAP.get(vuln.get("Severity", "UNKNOWN"), "INFO")

            # Determine the file path for the issue
            # For package managers, map to the lock/project file
            file_path = _resolve_file_path(target, result_type, project_base)

            issue = {
                "engineId": "trivy",
                "ruleId": vuln.get("VulnerabilityID", "UNKNOWN"),
                "severity": severity,
                "type": "VULNERABILITY",
                "primaryLocation": {
                    "message": (
                        f"[{vuln.get('VulnerabilityID')}] "
                        f"{vuln.get('PkgName', 'unknown')} "
                        f"{vuln.get('InstalledVersion', '')} → "
                        f"Fix: {vuln.get('FixedVersion', 'no fix available')}. "
                        f"{vuln.get('Title', '')}"
                    ),
                    "filePath": file_path,
                },
                "effortMinutes": _estimate_effort(severity),
            }

            # Add secondary info if CVSS score exists
            if vuln.get("CVSS"):
                for source, scores in vuln["CVSS"].items():
                    if "V3Score" in scores:
                        issue["primaryLocation"]["message"] += (
                            f" [CVSS: {scores['V3Score']}]"
                        )
                        break

            issues.append(issue)

        # ── Misconfigurations (IaC issues) ──────────────────────
        for misconf in misconfigurations:
            severity = SEVERITY_MAP.get(misconf.get("Severity", "UNKNOWN"), "INFO")

            issue = {
                "engineId": "trivy",
                "ruleId": misconf.get("ID", misconf.get("AVDID", "UNKNOWN")),
                "severity": severity,
                "type": "VULNERABILITY",
                "primaryLocation": {
                    "message": (
                        f"[{misconf.get('ID', '')}] "
                        f"{misconf.get('Title', '')} - "
                        f"{misconf.get('Message', '')} "
                        f"Resolution: {misconf.get('Resolution', 'N/A')}"
                    ),
                    "filePath": target if target != "unknown" else "Dockerfile",
                },
                "effortMinutes": _estimate_effort(severity),
            }

            # Add line number if available
            cause_metadata = misconf.get("CauseMetadata", {})
            if cause_metadata.get("StartLine"):
                issue["primaryLocation"]["textRange"] = {
                    "startLine": cause_metadata["StartLine"],
                    "endLine": cause_metadata.get("EndLine", cause_metadata["StartLine"]),
                }

            issues.append(issue)

        # ── Secrets ─────────────────────────────────────────────
        for secret in secrets:
            issue = {
                "engineId": "trivy",
                "ruleId": f"SECRET-{secret.get('RuleID', 'UNKNOWN')}",
                "severity": "CRITICAL",
                "type": "VULNERABILITY",
                "primaryLocation": {
                    "message": (
                        f"Hardcoded secret detected: {secret.get('Title', '')} "
                        f"Category: {secret.get('Category', 'unknown')} "
                        f"Match: {secret.get('Match', '')[:50]}..."
                    ),
                    "filePath": target,
                },
                "effortMinutes": 15,
            }

            if secret.get("StartLine"):
                issue["primaryLocation"]["textRange"] = {
                    "startLine": secret["StartLine"],
                    "endLine": secret.get("EndLine", secret["StartLine"]),
                }

            issues.append(issue)

    return {"issues": issues}


def _resolve_file_path(target: str, result_type: str, project_base: str) -> str:
    """Map Trivy target names to actual project file paths."""
    # .NET project files
    dotnet_map = {
        "nuget": [
            "*.csproj",        # Check common patterns
            "packages.config",
            "Directory.Packages.props",
        ],
    }

    # Frontend (Amplify/Node) project files
    node_map = {
        "npm": "package-lock.json",
        "yarn": "yarn.lock",
        "pnpm": "pnpm-lock.yaml",
        "node-pkg": "package.json",
    }

    # If the target is already a real file path, use it
    if "/" in target or target.endswith((".csproj", ".json", ".lock", ".yaml", ".config")):
        path = target
        if project_base and path.startswith(project_base):
            path = path[len(project_base):].lstrip("/")
        return path

    # Map package manager type to file
    if result_type in node_map:
        return node_map[result_type]
    if result_type == "nuget":
        return "Directory.Packages.props"
    if result_type == "dotnet-core":
        return target if target.endswith(".csproj") else "project.assets.json"

    return target if target != "unknown" else "project-dependencies"


def _estimate_effort(severity: str) -> int:
    """Estimate remediation effort in minutes based on severity."""
    effort = {
        "CRITICAL": 60,
        "MAJOR": 30,
        "MINOR": 15,
        "INFO": 5,
    }
    return effort.get(severity, 15)


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(f"Usage: {sys.argv[0]} <trivy-results.json> [project-base-path]", file=sys.stderr)
        sys.exit(1)

    trivy_file = sys.argv[1]
    project_base = sys.argv[2] if len(sys.argv) > 2 else ""

    result = convert_trivy_to_sonar(trivy_file, project_base)
    print(json.dumps(result, indent=2))