#!/bin/bash
# Wrapper script that strips xattrs before running codesign
# This is needed because iCloud sync adds xattrs faster than a single BeforeCodesign target can handle

# Find the actual path being signed (last argument)
SIGN_PATH="${@: -1}"

# Strip xattrs from the path
if [ -e "$SIGN_PATH" ]; then
    /usr/bin/xattr -cr "$SIGN_PATH" 2>/dev/null
fi

# Run actual codesign with all original arguments
exec /usr/bin/codesign "$@"
