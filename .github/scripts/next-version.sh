#!/usr/bin/env bash
# Computes the next release version for the given Major.Minor prefix from the git tags.
#
# Usage: next-version.sh <prefix>        e.g. next-version.sh 1.1
# Output (GitHub Actions "key=value" format):
#   version=<prefix>.<patch>    patch = highest existing v<prefix>.<n> + 1, or 0 if none
#   previous_tag=<tag>          most recent reachable v* tag (empty if none), for release notes
set -euo pipefail

prefix="${1:?usage: next-version.sh <major.minor>}"
escaped_prefix="${prefix//./\\.}"

latest_patch="$(git tag --list "v${prefix}.*" \
    | sed -nE "s/^v${escaped_prefix}\.([0-9]+)$/\1/p" \
    | sort -n \
    | tail -n 1)"

if [ -z "$latest_patch" ]; then
    next_patch=0
else
    next_patch=$((latest_patch + 1))
fi

previous_tag="$(git describe --tags --abbrev=0 --match 'v*' 2>/dev/null || true)"

echo "version=${prefix}.${next_patch}"
echo "previous_tag=${previous_tag}"
