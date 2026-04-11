#!/bin/bash
# Extracts the changelog section for a specific version from CHANGELOG.md
# Usage: extract-changelog.sh v2.4.0
# Output: The markdown content between the version header and the next version header

set -euo pipefail

VERSION="${1#v}"  # Remove 'v' prefix if present

if [[ -z "$VERSION" ]]; then
    echo "Usage: $0 <version>"
    exit 1
fi

CHANGELOG="CHANGELOG.md"

if [[ ! -f "$CHANGELOG" ]]; then
    echo "CHANGELOG.md not found"
    exit 1
fi

# Extract content between ## [VERSION] and the next ## [
# Using awk: start printing after matching header, stop at next header
awk -v ver="$VERSION" '
    /^## \[/ {
        if (found) exit
        if (index($0, "[" ver "]")) {
            found = 1
            next  # Skip the header line itself
        }
    }
    found { print }
' "$CHANGELOG" | sed '/^$/N;/^\n$/d' | sed -e :a -e '/^\n*$/{$d;N;ba;}'
# The sed commands: collapse multiple blank lines, trim trailing blanks
