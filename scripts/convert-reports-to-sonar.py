#!/usr/bin/env python3
"""
convert-reports-to-sonar.py
Converts security tool reports (Trivy, OWASP Dependency-Check, OWASP ZAP)
into SonarQube Generic Issue Import Format.

SonarQube External Issues format:
https://docs.sonarqube.org/latest/analyzing-source-code/importing-external-issues/

Usage:
    python3 convert-reports-to-sonar.py --trivy trivy-merged.json --output-dir .
    python3 convert-reports-to-sonar.py --depcheck dependency-check-report.json --output-dir .
    python3 convert-reports-to-sonar.py --zap zap-report.json --output-dir .
    python3 convert-reports-to-sonar.py --trivy t.json --depcheck d.json --zap z.json --output-dir .
"""

import argparse
import json
import os
import sys
import re
from pathlib import Path
from typing import Optional


# ─────────────────────────────────────────────────────────────────────────────
# SonarQube severity + type mappings
# ─────────────────────────────────────────────────────────────────────────────

TRIVY_SEVERITY_MAP = {
    "CRITICAL": "CRITICAL",
    "HIGH":     "MAJOR",
    "MEDIUM":   "MINOR",
    "LOW":      "INFO",
    "UNKNOWN":  "INFO",
}

CVSS_TO_SONAR_SEVERITY = {
    (9.0, 10.0): "CRITICAL",
    (7.0,  8.9): "MAJOR",
    (4.0,  6.9): "MINOR",
    (0.0,  3.9): "INFO",
}

ZAP_RISK_MAP = {
    "3": "CRITICAL",   # High
    "2": "MAJOR",      # Medium
    "1": "MINOR",      # Low
    "0": "INFO",       # Informational
}

ZAP_CONFIDENCE_MAP = {
    "3": "High",
    "2": "Medium",
    "1": "Low",
    "0": "False Positive",
}

ISSUE_TYPE = "VULNERABILITY"
DEFAULT_EFFORT = "30min"


# ─────────────────────────────────────────────────────────────────────────────
# Helpers
# ─────────────────────────────────────────────────────────────────────────────

def load_json(filepath: str) -> Optional[dict]:
    """Load and parse a JSON file, return None on failure."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            return json.load(f)
    except FileNotFoundError:
        print(f"  [WARN] File not found: {filepath}", file=sys.stderr)
        return None
    except json.JSONDecodeError as e:
        print(f"  [WARN] JSON parse error in {filepath}: {e}", file=sys.stderr)
        return None


def save_json(data: dict, filepath: str) -> None:
    """Write dict as pretty-printed JSON."""
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    print(f"  [OK] Written: {filepath} ({len(data.get('issues', []))} issues)")


def cvss_to_severity(score: float) -> str:
    """Convert a CVSS score (float) to SonarQube severity string."""
    for (low, high), severity in CVSS_TO_SONAR_SEVERITY.items():
        if low <= score <= high:
            return severity
    return "INFO"


def sanitize_text(text: str) -> str:
    """Strip ANSI codes and excessive whitespace from text."""
    ansi_escape = re.compile(r"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])")
    text = ansi_escape.sub("", str(text))
    return " ".join(text.split())


def make_issue(
    engine_id: str,
    rule_id: str,
    severity: str,
    issue_type: str,
    primary_location_message: str,
    file_path: str,
    line: int = 1,
    effort: str = DEFAULT_EFFORT,
    secondary_locations: Optional[list] = None,
) -> dict:
    """
    Build a SonarQube generic issue dict.
    Line numbers must be >= 1.
    """
    line = max(1, int(line))

    issue = {
        "engineId": engine_id,
        "ruleId":   rule_id,
        "severity": severity,
        "type":     issue_type,
        "primaryLocation": {
            "message":   sanitize_text(primary_location_message)[:3900],
            "filePath":  file_path,
            "textRange": {
                "startLine": line,
            },
        },
        "effortMinutes": _effort_to_minutes(effort),
    }

    if secondary_locations:
        issue["secondaryLocations"] = secondary_locations

    return issue


def _effort_to_minutes(effort: str) -> int:
    """Convert effort string like '30min', '2h', '1h30min' to minutes."""
    total = 0
    for match in re.finditer(r"(\d+)\s*(h|min)", effort):
        value, unit = int(match.group(1)), match.group(2)
        total += value * 60 if unit == "h" else value
    return total if total > 0 else 30


def resolve_file_path(raw_path: str, workspace: str = ".") -> str:
    """
    Attempt to resolve an absolute path to a relative path
    within the project workspace. Falls back to 'pom.xml'
    equivalent for .NET (the .csproj or solution root).
    """
    if not raw_path:
        return "."

    # Already relative
    if not os.path.isabs(raw_path):
        return raw_path

    # Try to make it relative to workspace
    try:
        rel = os.path.relpath(raw_path, workspace)
        if not rel.startswith(".."):
            return rel
    except ValueError:
        pass

    # Strip common container-mounted paths
    for prefix in ["/apps/src/", "/src/", "/repo/", "/home/runner/work/"]:
        if raw_path.startswith(prefix):
            return raw_path[len(prefix):]

    # Return basename as last resort - SonarQube needs a valid path
    return os.path.basename(raw_path) or "."


# ─────────────────────────────────────────────────────────────────────────────
# Converter: Trivy → SonarQube
# ─────────────────────────────────────────────────────────────────────────────

def convert_trivy(report_path: str, output_dir: str) -> dict:
    """
    Convert a merged Trivy JSON report to SonarQube generic issue format.

    Handles:
      - Vulnerability results (NuGet, npm, OS packages)
      - Secret results
      - IaC / Misconfiguration results
    """
    print(f"\n[Trivy] Converting: {report_path}")
    data = load_json(report_path)
    if data is None:
        return {"issues": []}

    issues = []
    results = data.get("Results", [])

    for result in results:
        target      = result.get("Target", ".")
        result_type = result.get("Type", "")
        file_path   = resolve_file_path(target)

        # ── Vulnerabilities ──────────────────────────────────────────────────
        for vuln in result.get("Vulnerabilities") or []:
            vuln_id     = vuln.get("VulnerabilityID", "UNKNOWN")
            pkg_name    = vuln.get("PkgName", "unknown-package")
            installed   = vuln.get("InstalledVersion", "?")
            fixed       = vuln.get("FixedVersion", "no-fix")
            severity    = vuln.get("Severity", "UNKNOWN")
            title       = vuln.get("Title", vuln_id)
            description = vuln.get("Description", "No description available.")
            cvss_score  = _extract_cvss_score(vuln)
            refs        = vuln.get("References", [])

            sonar_severity = TRIVY_SEVERITY_MAP.get(severity.upper(), "INFO")
            if cvss_score is not None:
                # CVSS score takes precedence over Trivy severity label
                sonar_severity = cvss_to_severity(cvss_score)

            message = (
                f"[{vuln_id}] {title} | "
                f"Package: {pkg_name}@{installed} | "
                f"Fixed in: {fixed} | "
                f"Severity: {severity}"
                + (f" (CVSS: {cvss_score:.1f})" if cvss_score else "")
                + (f" | Ref: {refs[0]}" if refs else "")
            )

            issues.append(make_issue(
                engine_id="trivy",
                rule_id=vuln_id,
                severity=sonar_severity,
                issue_type=ISSUE_TYPE,
                primary_location_message=message,
                file_path=file_path or ".",
                effort=_severity_to_effort(sonar_severity),
            ))

        # ── Secrets ──────────────────────────────────────────────────────────
        for secret in result.get("Secrets") or []:
            rule_id     = secret.get("RuleID", "SECRET")
            category    = secret.get("Category", "secret")
            title       = secret.get("Title", "Detected Secret")
            severity    = secret.get("Severity", "HIGH")
            start_line  = secret.get("StartLine", 1)
            match_text  = secret.get("Match", "")

            sonar_severity = TRIVY_SEVERITY_MAP.get(severity.upper(), "MAJOR")

            message = (
                f"[SECRET] {title} | "
                f"Category: {category} | "
                f"File: {file_path} | "
                f"Match: {match_text[:120]}"
            )

            issues.append(make_issue(
                engine_id="trivy",
                rule_id=f"SECRET-{rule_id}",
                severity=sonar_severity,
                issue_type="VULNERABILITY",
                primary_location_message=message,
                file_path=file_path or ".",
                line=start_line,
                effort="1h",
            ))

        # ── Misconfigurations / IaC ──────────────────────────────────────────
        for misconfig in result.get("Misconfigurations") or []:
            mc_id       = misconfig.get("ID", "MISCONFIG")
            mc_title    = misconfig.get("Title", "Misconfiguration")
            mc_desc     = misconfig.get("Description", "")
            mc_severity = misconfig.get("Severity", "MEDIUM")
            mc_type     = misconfig.get("Type", result_type)
            resolution  = misconfig.get("Resolution", "")
            refs        = misconfig.get("References", [])

            sonar_severity = TRIVY_SEVERITY_MAP.get(mc_severity.upper(), "MINOR")

            message = (
                f"[{mc_id}] {mc_title} | "
                f"Type: {mc_type} | "
                f"{mc_desc}"
                + (f" | Fix: {resolution}" if resolution else "")
                + (f" | Ref: {refs[0]}" if refs else "")
            )

            issues.append(make_issue(
                engine_id="trivy",
                rule_id=f"MISCONFIG-{mc_id}",
                severity=sonar_severity,
                issue_type="VULNERABILITY",
                primary_location_message=message,
                file_path=file_path or ".",
                effort=_severity_to_effort(sonar_severity),
            ))

    output = {"issues": issues}
    out_path = os.path.join(output_dir, "trivy-sonar-report.json")
    save_json(output, out_path)
    print(f"[Trivy] Total issues: {len(issues)}")
    return output


def _extract_cvss_score(vuln: dict) -> Optional[float]:
    """Extract the highest CVSS score from a Trivy vulnerability entry."""
    cvss_data = vuln.get("CVSS", {})
    scores = []
    for source, metrics in cvss_data.items():
        for key in ("V3Score", "V2Score"):
            score = metrics.get(key)
            if isinstance(score, (int, float)):
                scores.append(float(score))
    return max(scores) if scores else None


def _severity_to_effort(severity: str) -> str:
    """Map SonarQube severity to remediation effort estimate."""
    return {
        "CRITICAL": "4h",
        "MAJOR":    "2h",
        "MINOR":    "30min",
        "INFO":     "10min",
    }.get(severity, "30min")


# ─────────────────────────────────────────────────────────────────────────────
# Converter: OWASP Dependency-Check → SonarQube
# ─────────────────────────────────────────────────────────────────────────────

def convert_depcheck(report_path: str, output_dir: str) -> dict:
    """
    Convert OWASP Dependency-Check JSON report to SonarQube generic issue format.

    Handles:
      - CVE-based vulnerabilities
      - Multiple evidence sources
      - CPE identifiers
    """
    print(f"\n[Dependency-Check] Converting: {report_path}")
    data = load_json(report_path)
    if data is None:
        return {"issues": []}

    issues = []
    dependencies = data.get("dependencies", [])

    for dep in dependencies:
        file_path    = dep.get("filePath", ".")
        file_name    = dep.get("fileName", os.path.basename(file_path))
        relative_path = resolve_file_path(file_path)
        vulnerabilities = dep.get("vulnerabilities", [])

        if not vulnerabilities:
            continue

        for vuln in vulnerabilities:
            vuln_name   = vuln.get("name", "UNKNOWN-CVE")
            description = vuln.get("description", "No description available.")
            severity    = vuln.get("severity", "UNKNOWN").upper()
            cvss_score  = _extract_depcheck_cvss(vuln)
            cwe_list    = [c.get("cweId", "") for c in vuln.get("cwes", [])]
            references  = vuln.get("references", [])
            source      = vuln.get("source", "NVD")

            # Prefer CVSS-based severity
            sonar_severity = cvss_to_severity(cvss_score) if cvss_score else _depcheck_severity_map(severity)

            ref_url = ""
            if references:
                ref_url = references[0].get("url", "")

            cwe_str = ", ".join(filter(None, cwe_list))
            message = (
                f"[{vuln_name}] {description[:400]} | "
                f"Package: {file_name} | "
                f"Source: {source} | "
                f"CVSS: {cvss_score if cvss_score else 'N/A'}"
                + (f" | CWE: {cwe_str}" if cwe_str else "")
                + (f" | Ref: {ref_url}" if ref_url else "")
            )

            issues.append(make_issue(
                engine_id="dependency-check",
                rule_id=vuln_name,
                severity=sonar_severity,
                issue_type=ISSUE_TYPE,
                primary_location_message=message,
                file_path=relative_path or ".",
                effort=_severity_to_effort(sonar_severity),
            ))

    output = {"issues": issues}
    out_path = os.path.join(output_dir, "dependency-check-sonar-report.json")
    save_json(output, out_path)
    print(f"[Dependency-Check] Total issues: {len(issues)}")
    return output


def _extract_depcheck_cvss(vuln: dict) -> Optional[float]:
    """
    Extract CVSS score from Dependency-Check vulnerability.
    Tries CVSS v3 first, then v2.
    """
    # Try cvssv3 object
    cvssv3 = vuln.get("cvssv3", {})
    if cvssv3 and "baseScore" in cvssv3:
        return float(cvssv3["baseScore"])

    # Try cvssv2 object
    cvssv2 = vuln.get("cvssv2", {})
    if cvssv2 and "score" in cvssv2:
        return float(cvssv2["score"])

    # Try flat fields
    for field in ("cvssScore", "baseScore"):
        score = vuln.get(field)
        if isinstance(score, (int, float)):
            return float(score)

    return None


def _depcheck_severity_map(severity: str) -> str:
    """Map Dependency-Check severity string to SonarQube severity."""
    return {
        "CRITICAL":   "CRITICAL",
        "HIGH":       "MAJOR",
        "MEDIUM":     "MINOR",
        "MODERATE":   "MINOR",
        "LOW":        "INFO",
        "INFORMATIONAL": "INFO",
        "UNKNOWN":    "INFO",
    }.get(severity, "INFO")


# ─────────────────────────────────────────────────────────────────────────────
# Converter: OWASP ZAP → SonarQube
# ─────────────────────────────────────────────────────────────────────────────

def convert_zap(report_path: str, output_dir: str) -> dict:
    """
    Convert OWASP ZAP JSON report to SonarQube generic issue format.

    ZAP reports contain:
      - site[].alerts[] with instances[] showing exact URLs/evidence
    """
    print(f"\n[ZAP] Converting: {report_path}")
    data = load_json(report_path)
    if data is None:
        return {"issues": []}

    issues = []

    # ZAP report root can be:
    # - {"site": [...]}   → standard ZAP JSON
    # - [{"site": [...]}] → some ZAP versions wrap in array
    if isinstance(data, list):
        sites = []
        for entry in data:
            sites.extend(entry.get("site", []))
    else:
        sites = data.get("site", [])

    # Flatten: some reports put alerts at root level
    root_alerts = data.get("alerts", []) if isinstance(data, dict) else []

    all_alert_groups = root_alerts  # may be empty

    for site in sites:
        site_alerts = site.get("alerts", [])
        all_alert_groups.extend(site_alerts)

    for alert in all_alert_groups:
        alert_name   = alert.get("name", alert.get("alert", "Unknown Alert"))
        alert_ref    = alert.get("alertRef", alert.get("pluginid", "ZAP-0"))
        risk_code    = str(alert.get("riskcode", alert.get("riskCode", "1")))
        confidence   = str(alert.get("confidence", "2"))
        description  = alert.get("desc", alert.get("description", ""))
        solution     = alert.get("solution", "")
        reference    = alert.get("reference", alert.get("references", ""))
        cweid        = alert.get("cweid", alert.get("cweId", ""))
        wascid       = alert.get("wascid", "")

        sonar_severity = ZAP_RISK_MAP.get(risk_code, "INFO")
        confidence_str = ZAP_CONFIDENCE_MAP.get(confidence, "Medium")

        # ZAP instances hold individual URL/method/evidence hits
        instances = alert.get("instances", [])

        if instances:
            for instance in instances:
                uri      = instance.get("uri", instance.get("url", "."))
                method   = instance.get("method", "GET")
                evidence = instance.get("evidence", instance.get("param", ""))
                attack   = instance.get("attack", "")

                # Use relative path for SonarQube file path
                # ZAP findings are URL-based; map to closest source file
                file_path = _zap_uri_to_filepath(uri)

                message = (
                    f"[ZAP-{alert_ref}] {alert_name} | "
                    f"Risk: {_riskcode_label(risk_code)} | "
                    f"Confidence: {confidence_str} | "
                    f"URL: {uri} | "
                    f"Method: {method}"
                    + (f" | Evidence: {evidence[:200]}" if evidence else "")
                    + (f" | Attack: {attack[:100]}" if attack else "")
                    + (f" | CWE: {cweid}" if cweid else "")
                    + (f" | Fix: {sanitize_text(solution)[:300]}" if solution else "")
                )

                issues.append(make_issue(
                    engine_id="zap",
                    rule_id=f"ZAP-{alert_ref}",
                    severity=sonar_severity,
                    issue_type=ISSUE_TYPE,
                    primary_location_message=message,
                    file_path=file_path,
                    effort=_severity_to_effort(sonar_severity),
                ))
        else:
            # No instances, create one issue per alert
            message = (
                f"[ZAP-{alert_ref}] {alert_name} | "
                f"Risk: {_riskcode_label(risk_code)} | "
                f"Confidence: {confidence_str} | "
                f"{sanitize_text(description)[:400]}"
                + (f" | CWE: {cweid}" if cweid else "")
                + (f" | Fix: {sanitize_text(solution)[:300]}" if solution else "")
            )

            issues.append(make_issue(
                engine_id="zap",
                rule_id=f"ZAP-{alert_ref}",
                severity=sonar_severity,
                issue_type=ISSUE_TYPE,
                primary_location_message=message,
                file_path=".",
                effort=_severity_to_effort(sonar_severity),
            ))

    output = {"issues": issues}
    out_path = os.path.join(output_dir, "zap-sonar-report.json")
    save_json(output, out_path)
    print(f"[ZAP] Total issues: {len(issues)}")
    return output


def _zap_uri_to_filepath(uri: str) -> str:
    """
    Convert a ZAP URI to a relative file path SonarQube can reference.
    Since ZAP works against live URLs, we map to a placeholder path.
    The path format: src/WebApi/<path_segments>
    """
    try:
        from urllib.parse import urlparse
        parsed = urlparse(uri)
        path_part = parsed.path.lstrip("/")
        if path_part:
            # Map to a known source directory convention
            return f"src/WebApi/{path_part}"
    except Exception:
        pass
    return "."


def _riskcode_label(code: str) -> str:
    return {
        "3": "High",
        "2": "Medium",
        "1": "Low",
        "0": "Informational",
    }.get(str(code), "Unknown")


# ─────────────────────────────────────────────────────────────────────────────
# Main entry point
# ─────────────────────────────────────────────────────────────────────────────

def parse_args():
    parser = argparse.ArgumentParser(
        description="Convert AppSec tool reports to SonarQube Generic Issue Format",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Convert only Trivy report
  python3 convert-reports-to-sonar.py --trivy trivy-merged.json --output-dir .

  # Convert only Dependency-Check report
  python3 convert-reports-to-sonar.py --depcheck dependency-check-report.json --output-dir .

  # Convert only ZAP report
  python3 convert-reports-to-sonar.py --zap zap-report.json --output-dir .

  # Convert all at once
  python3 convert-reports-to-sonar.py \\
      --trivy trivy-merged.json \\
      --depcheck dependency-check-report.json \\
      --zap zap-report.json \\
      --output-dir ./sonar-imports
        """,
    )
    parser.add_argument(
        "--trivy",
        metavar="FILE",
        help="Path to merged Trivy JSON report",
    )
    parser.add_argument(
        "--depcheck",
        metavar="FILE",
        help="Path to OWASP Dependency-Check JSON report",
    )
    parser.add_argument(
        "--zap",
        metavar="FILE",
        help="Path to OWASP ZAP JSON report",
    )
    parser.add_argument(
        "--output-dir",
        metavar="DIR",
        default=".",
        help="Directory to write converted reports (default: current directory)",
    )
    parser.add_argument(
        "--workspace",
        metavar="DIR",
        default=".",
        help="Workspace root for resolving absolute paths (default: current directory)",
    )
    return parser.parse_args()


def main():
    args = parse_args()

    if not any([args.trivy, args.depcheck, args.zap]):
        print("[ERROR] At least one input report must be specified.", file=sys.stderr)
        print("        Use --trivy, --depcheck, or --zap", file=sys.stderr)
        sys.exit(1)

    # Create output directory
    os.makedirs(args.output_dir, exist_ok=True)

    results_summary = {}

    if args.trivy:
        result = convert_trivy(args.trivy, args.output_dir)
        results_summary["trivy"] = len(result.get("issues", []))

    if args.depcheck:
        result = convert_depcheck(args.depcheck, args.output_dir)
        results_summary["dependency-check"] = len(result.get("issues", []))

    if args.zap:
        result = convert_zap(args.zap, args.output_dir)
        results_summary["zap"] = len(result.get("issues", []))

    # Print summary
    print("\n" + "═" * 60)
    print("  Conversion Summary")
    print("═" * 60)
    total = 0
    for tool, count in results_summary.items():
        print(f"  {tool:<25} {count:>5} issue(s)")
        total += count
    print("─" * 60)
    print(f"  {'TOTAL':<25} {total:>5} issue(s)")
    print("═" * 60)

    if total > 0:
        print(f"\n[INFO] Reports written to: {os.path.abspath(args.output_dir)}")

    # Exit 0 always - failures are handled by Quality Gate
    sys.exit(0)


if __name__ == "__main__":
    main()