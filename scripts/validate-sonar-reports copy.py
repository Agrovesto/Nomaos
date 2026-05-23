#!/usr/bin/env python3
"""
validate-sonar-reports.py
Validates that generated SonarQube generic issue JSON files conform
to the expected schema before upload. Prevents pipeline failures due
to malformed reports.

Usage:
    python3 validate-sonar-reports.py trivy-sonar-report.json
    python3 validate-sonar-reports.py trivy-sonar-report.json dependency-check-sonar-report.json
    python3 validate-sonar-reports.py --dir . --pattern "*-sonar-report.json"
"""

import argparse
import json
import sys
import os
import glob
from typing import List, Tuple


# ─────────────────────────────────────────────────────────────────────────────
# Schema constants
# ─────────────────────────────────────────────────────────────────────────────

VALID_SEVERITIES = {"BLOCKER", "CRITICAL", "MAJOR", "MINOR", "INFO"}
VALID_TYPES      = {"BUG", "VULNERABILITY", "CODE_SMELL"}

# SonarQube limits
MAX_MESSAGE_LENGTH  = 4000
MAX_FILE_PATH_LEN   = 400
MAX_ISSUES_PER_FILE = 100_000

REQUIRED_ISSUE_FIELDS = {"engineId", "ruleId", "severity", "type", "primaryLocation"}
REQUIRED_LOCATION_FIELDS = {"message", "filePath"}


# ─────────────────────────────────────────────────────────────────────────────
# Validators
# ─────────────────────────────────────────────────────────────────────────────

class ValidationError:
    def __init__(self, issue_index: int, field: str, message: str):
        self.issue_index = issue_index
        self.field = field
        self.message = message

    def __str__(self):
        return f"  Issue #{self.issue_index}: [{self.field}] {self.message}"


def validate_report(filepath: str) -> Tuple[bool, List[ValidationError], dict]:
    """
    Validate a single SonarQube generic issue JSON file.

    Returns:
        (is_valid, errors, stats)
    """
    errors: List[ValidationError] = []
    stats = {
        "total_issues": 0,
        "by_severity":  {},
        "by_engine":    {},
        "by_type":      {},
        "warnings":     [],
    }

    # ── Load file ─────────────────────────────────────────────────────────────
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            data = json.load(f)
    except FileNotFoundError:
        return False, [ValidationError(-1, "file", f"File not found: {filepath}")], stats
    except json.JSONDecodeError as e:
        return False, [ValidationError(-1, "json", f"Invalid JSON: {e}")], stats

    # ── Root structure ────────────────────────────────────────────────────────
    if not isinstance(data, dict):
        errors.append(ValidationError(-1, "root", "Root must be a JSON object"))
        return False, errors, stats

    if "issues" not in data:
        errors.append(ValidationError(-1, "root", "Missing required field 'issues'"))
        return False, errors, stats

    issues = data["issues"]

    if not isinstance(issues, list):
        errors.append(ValidationError(-1, "issues", "'issues' must be an array"))
        return False, errors, stats

    stats["total_issues"] = len(issues)

    if len(issues) > MAX_ISSUES_PER_FILE:
        stats["warnings"].append(
            f"Issue count ({len(issues)}) exceeds recommended maximum ({MAX_ISSUES_PER_FILE})"
        )

    if len(issues) == 0:
        stats["warnings"].append("Report contains zero issues (may be intentional)")
        return True, errors, stats

    # ── Validate each issue ───────────────────────────────────────────────────
    for idx, issue in enumerate(issues):

        if not isinstance(issue, dict):
            errors.append(ValidationError(idx, "issue", "Issue must be a JSON object"))
            continue

        # Required top-level fields
        for field in REQUIRED_ISSUE_FIELDS:
            if field not in issue:
                errors.append(ValidationError(idx, field, f"Missing required field '{field}'"))

        # engineId
        engine_id = issue.get("engineId", "")
        if not isinstance(engine_id, str) or not engine_id.strip():
            errors.append(ValidationError(idx, "engineId", "Must be a non-empty string"))
        else:
            stats["by_engine"][engine_id] = stats["by_engine"].get(engine_id, 0) + 1

        # ruleId
        rule_id = issue.get("ruleId", "")
        if not isinstance(rule_id, str) or not rule_id.strip():
            errors.append(ValidationError(idx, "ruleId", "Must be a non-empty string"))
        if len(str(rule_id)) > 200:
            errors.append(ValidationError(idx, "ruleId", "Exceeds 200 character limit"))

        # severity
        severity = issue.get("severity", "")
        if severity not in VALID_SEVERITIES:
            errors.append(ValidationError(
                idx, "severity",
                f"Invalid value '{severity}'. Must be one of: {', '.join(sorted(VALID_SEVERITIES))}"
            ))
        else:
            stats["by_severity"][severity] = stats["by_severity"].get(severity, 0) + 1

        # type
        issue_type = issue.get("type", "")
        if issue_type not in VALID_TYPES:
            errors.append(ValidationError(
                idx, "type",
                f"Invalid value '{issue_type}'. Must be one of: {', '.join(sorted(VALID_TYPES))}"
            ))
        else:
            stats["by_type"][issue_type] = stats["by_type"].get(issue_type, 0) + 1

        # effortMinutes (optional)
        if "effortMinutes" in issue:
            effort = issue["effortMinutes"]
            if not isinstance(effort, int) or effort < 0:
                errors.append(ValidationError(
                    idx, "effortMinutes", "Must be a non-negative integer"
                ))

        # primaryLocation
        primary_loc = issue.get("primaryLocation")
        if primary_loc is not None:
            _validate_location(idx, "primaryLocation", primary_loc, errors)

        # secondaryLocations (optional)
        secondary_locs = issue.get("secondaryLocations", [])
        if not isinstance(secondary_locs, list):
            errors.append(ValidationError(idx, "secondaryLocations", "Must be an array"))
        else:
            for sec_idx, sec_loc in enumerate(secondary_locs):
                _validate_location(
                    idx, f"secondaryLocations[{sec_idx}]", sec_loc, errors
                )

    is_valid = len(errors) == 0
    return is_valid, errors, stats


def _validate_location(
    issue_idx: int,
    location_name: str,
    location: dict,
    errors: List[ValidationError],
) -> None:
    """Validate a primaryLocation or secondaryLocation object."""

    if not isinstance(location, dict):
        errors.append(ValidationError(issue_idx, location_name, "Must be a JSON object"))
        return

    # message
    message = location.get("message", "")
    if not isinstance(message, str) or not message.strip():
        errors.append(ValidationError(
            issue_idx, f"{location_name}.message", "Must be a non-empty string"
        ))
    elif len(message) > MAX_MESSAGE_LENGTH:
        errors.append(ValidationError(
            issue_idx, f"{location_name}.message",
            f"Exceeds {MAX_MESSAGE_LENGTH} character limit (got {len(message)})"
        ))

    # filePath
    file_path = location.get("filePath", "")
    if not isinstance(file_path, str):
        errors.append(ValidationError(
            issue_idx, f"{location_name}.filePath", "Must be a string"
        ))
    elif len(file_path) > MAX_FILE_PATH_LEN:
        errors.append(ValidationError(
            issue_idx, f"{location_name}.filePath",
            f"Exceeds {MAX_FILE_PATH_LEN} character limit"
        ))
    elif os.path.isabs(file_path) and file_path != "/":
        errors.append(ValidationError(
            issue_idx, f"{location_name}.filePath",
            f"Must be relative path, got absolute: '{file_path}'"
        ))

    # textRange (optional in secondaryLocations, but validate if present)
    text_range = location.get("textRange")
    if text_range is not None:
        if not isinstance(text_range, dict):
            errors.append(ValidationError(
                issue_idx, f"{location_name}.textRange", "Must be a JSON object"
            ))
        else:
            _validate_text_range(issue_idx, f"{location_name}.textRange", text_range, errors)


def _validate_text_range(
    issue_idx: int,
    range_name: str,
    text_range: dict,
    errors: List[ValidationError],
) -> None:
    """Validate a textRange object."""
    start_line = text_range.get("startLine")

    if start_line is None:
        errors.append(ValidationError(
            issue_idx, f"{range_name}.startLine", "Missing required field"
        ))
    elif not isinstance(start_line, int) or start_line < 1:
        errors.append(ValidationError(
            issue_idx, f"{range_name}.startLine",
            f"Must be a positive integer, got: {start_line}"
        ))

    # Optional fields: endLine, startColumn, endColumn
    end_line = text_range.get("endLine")
    if end_line is not None:
        if not isinstance(end_line, int) or end_line < 1:
            errors.append(ValidationError(
                issue_idx, f"{range_name}.endLine",
                f"Must be a positive integer, got: {end_line}"
            ))
        elif start_line and end_line < start_line:
            errors.append(ValidationError(
                issue_idx, f"{range_name}.endLine",
                f"endLine ({end_line}) must be >= startLine ({start_line})"
            ))

    for col_field in ("startColumn", "endColumn"):
        col = text_range.get(col_field)
        if col is not None and (not isinstance(col, int) or col < 0):
            errors.append(ValidationError(
                issue_idx, f"{range_name}.{col_field}",
                f"Must be a non-negative integer, got: {col}"
            ))


# ─────────────────────────────────────────────────────────────────────────────
# Report printing
# ─────────────────────────────────────────────────────────────────────────────

def print_report(filepath: str, is_valid: bool, errors: List[ValidationError], stats: dict) -> None:
    status = "✅ VALID" if is_valid else "❌ INVALID"
    print(f"\n{'─' * 60}")
    print(f"  File:   {filepath}")
    print(f"  Status: {status}")
    print(f"  Issues: {stats['total_issues']}")

    if stats["by_severity"]:
        sev_parts = ", ".join(f"{k}={v}" for k, v in sorted(stats["by_severity"].items()))
        print(f"  By Severity: {sev_parts}")

    if stats["by_engine"]:
        eng_parts = ", ".join(f"{k}={v}" for k, v in sorted(stats["by_engine"].items()))
        print(f"  By Engine:   {eng_parts}")

    if stats["warnings"]:
        print("\n  Warnings:")
        for w in stats["warnings"]:
            print(f"    ⚠️  {w}")

    if errors:
        print(f"\n  Errors ({len(errors)}):")
        # Show first 20 errors to avoid flooding output
        for err in errors[:20]:
            print(str(err))
        if len(errors) > 20:
            print(f"  ... and {len(errors) - 20} more errors")


# ─────────────────────────────────────────────────────────────────────────────
# Main
# ─────────────────────────────────────────────────────────────────────────────

def parse_args():
    parser = argparse.ArgumentParser(
        description="Validate SonarQube Generic Issue Format JSON reports"
    )
    parser.add_argument(
        "files",
        nargs="*",
        metavar="FILE",
        help="Report file(s) to validate",
    )
    parser.add_argument(
        "--dir",
        metavar="DIR",
        help="Directory to search for reports",
    )
    parser.add_argument(
        "--pattern",
        metavar="GLOB",
        default="*-sonar-report.json",
        help="Glob pattern for --dir mode (default: *-sonar-report.json)",
    )
    parser.add_argument(
        "--strict",
        action="store_true",
        help="Exit with code 1 if any warnings are found",
    )
    return parser.parse_args()


def main():
    args = parse_args()

    files_to_validate = list(args.files)

    if args.dir:
        pattern = os.path.join(args.dir, args.pattern)
        found = glob.glob(pattern)
        files_to_validate.extend(found)
        if not found:
            print(f"[WARN] No files matched pattern: {pattern}")

    if not files_to_validate:
        print("[ERROR] No files specified for validation.", file=sys.stderr)
        print("        Provide file paths as arguments or use --dir", file=sys.stderr)
        sys.exit(1)

    print(f"Validating {len(files_to_validate)} report file(s)...")

    all_valid = True
    total_issues = 0
    total_errors = 0
    total_warnings = 0

    for filepath in files_to_validate:
        is_valid, errors, stats = validate_report(filepath)
        print_report(filepath, is_valid, errors, stats)

        if not is_valid:
            all_valid = False

        total_issues   += stats["total_issues"]
        total_errors   += len(errors)
        total_warnings += len(stats.get("warnings", []))

    # Summary
    print(f"\n{'═' * 60}")
    print(f"  Validation Summary")
    print(f"{'═' * 60}")
    print(f"  Files validated:  {len(files_to_validate)}")
    print(f"  Total issues:     {total_issues}")
    print(f"  Validation errors: {total_errors}")
    print(f"  Warnings:          {total_warnings}")
    print(f"  Overall status:   {'✅ PASS' if all_valid else '❌ FAIL'}")
    print(f"{'═' * 60}")

    if not all_valid:
        print("\n[ERROR] One or more reports failed validation.", file=sys.stderr)
        sys.exit(1)

    if args.strict and total_warnings > 0:
        print("\n[ERROR] Warnings found in strict mode.", file=sys.stderr)
        sys.exit(1)

    sys.exit(0)


if __name__ == "__main__":
    main()