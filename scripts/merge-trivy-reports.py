#!/usr/bin/env python3
"""
merge-trivy-reports.py
Merges multiple Trivy JSON report files into a single unified report.
Used when individual scans (vuln, secrets, iac) are run separately.

Usage:
    python3 merge-trivy-reports.py \\
        --inputs security-reports/trivy-vuln-report.json \\
                 security-reports/trivy-secrets-report.json \\
                 security-reports/trivy-iac-report.json \\
        --output security-reports/trivy-merged.json

    # Or use glob pattern
    python3 merge-trivy-reports.py \\
        --dir security-reports \\
        --pattern "trivy-*-report.json" \\
        --output security-reports/trivy-merged.json
"""

import argparse
import json
import os
import glob
import sys
from typing import List, Optional


def load_trivy_report(filepath: str) -> Optional[dict]:
    """Load a single Trivy JSON report."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            data = json.load(f)
        print(f"  [OK] Loaded: {filepath}")
        return data
    except FileNotFoundError:
        print(f"  [SKIP] Not found: {filepath}", file=sys.stderr)
        return None
    except json.JSONDecodeError as e:
        print(f"  [SKIP] JSON error in {filepath}: {e}", file=sys.stderr)
        return None


def merge_reports(report_files: List[str]) -> dict:
    """
    Merge multiple Trivy JSON reports into one.

    Trivy report structure:
    {
      "SchemaVersion": 2,
      "ArtifactName": "...",
      "ArtifactType": "...",
      "Metadata": {...},
      "Results": [...]
    }
    """
    merged = {
        "SchemaVersion": 2,
        "ArtifactName":  "merged-scan",
        "ArtifactType":  "filesystem",
        "Metadata":      {},
        "Results":       [],
    }

    total_vulnerabilities = 0
    total_secrets         = 0
    total_misconfigs      = 0
    files_merged          = 0

    for filepath in report_files:
        data = load_trivy_report(filepath)
        if data is None:
            continue

        results = data.get("Results", [])

        for result in results:
            vuln_count    = len(result.get("Vulnerabilities") or [])
            secret_count  = len(result.get("Secrets") or [])
            misconfig_count = len(result.get("Misconfigurations") or [])

            total_vulnerabilities += vuln_count
            total_secrets         += secret_count
            total_misconfigs      += misconfig_count

            if vuln_count + secret_count + misconfig_count > 0:
                merged["Results"].append(result)
                print(
                    f"    → Target: {result.get('Target', '?')} | "
                    f"Vulns: {vuln_count}, Secrets: {secret_count}, Misconfigs: {misconfig_count}"
                )

        # Preserve metadata from the first valid report
        if files_merged == 0 and data.get("Metadata"):
            merged["Metadata"] = data["Metadata"]
            merged["ArtifactName"] = data.get("ArtifactName", "merged-scan")

        files_merged += 1

    print(f"\n  Merged {files_merged} report(s):")
    print(f"    Vulnerabilities:   {total_vulnerabilities}")
    print(f"    Secrets:           {total_secrets}")
    print(f"    Misconfigurations: {total_misconfigs}")
    print(f"    Total Results:     {len(merged['Results'])}")

    return merged


def deduplicate_results(merged: dict) -> dict:
    """
    Remove duplicate vulnerability entries across merged reports.
    Deduplication key: (target, vuln_id, pkg_name)
    """
    seen_vulns = set()
    cleaned_results = []

    for result in merged.get("Results", []):
        target = result.get("Target", "")
        cleaned_result = dict(result)

        # Deduplicate vulnerabilities
        if "Vulnerabilities" in result and result["Vulnerabilities"]:
            unique_vulns = []
            for vuln in result["Vulnerabilities"]:
                key = (
                    target,
                    vuln.get("VulnerabilityID", ""),
                    vuln.get("PkgName", ""),
                    vuln.get("InstalledVersion", ""),
                )
                if key not in seen_vulns:
                    seen_vulns.add(key)
                    unique_vulns.append(vuln)
            cleaned_result["Vulnerabilities"] = unique_vulns

        cleaned_results.append(cleaned_result)

    merged["Results"] = cleaned_results
    return merged


def parse_args():
    parser = argparse.ArgumentParser(
        description="Merge multiple Trivy JSON reports into a single unified report"
    )

    input_group = parser.add_mutually_exclusive_group(required=True)
    input_group.add_argument(
        "--inputs",
        nargs="+",
        metavar="FILE",
        help="Input Trivy JSON report files",
    )
    input_group.add_argument(
        "--dir",
        metavar="DIR",
        help="Directory containing Trivy report files",
    )

    parser.add_argument(
        "--pattern",
        metavar="GLOB",
        default="trivy-*-report.json",
        help="Glob pattern when using --dir (default: trivy-*-report.json)",
    )
    parser.add_argument(
        "--output",
        metavar="FILE",
        required=True,
        help="Output merged report path",
    )
    parser.add_argument(
        "--no-dedup",
        action="store_true",
        help="Skip deduplication of vulnerabilities",
    )
    return parser.parse_args()


def main():
    args = parse_args()

    if args.inputs:
        report_files = args.inputs
    else:
        pattern = os.path.join(args.dir, args.pattern)
        report_files = sorted(glob.glob(pattern))
        if not report_files:
            print(f"[WARN] No files matched: {pattern}", file=sys.stderr)

    print(f"Merging {len(report_files)} Trivy report(s)...")

    merged = merge_reports(report_files)

    if not args.no_dedup:
        before = sum(len(r.get("Vulnerabilities") or []) for r in merged["Results"])
        merged = deduplicate_results(merged)
        after  = sum(len(r.get("Vulnerabilities") or []) for r in merged["Results"])
        if before != after:
            print(f"  Deduplication removed {before - after} duplicate vulnerability entries")

    # Write output
    os.makedirs(os.path.dirname(os.path.abspath(args.output)), exist_ok=True)
    with open(args.output, "w", encoding="utf-8") as f:
        json.dump(merged, f, indent=2, ensure_ascii=False)

    print(f"\n[OK] Merged report written: {args.output}")
    sys.exit(0)


if __name__ == "__main__":
    main()