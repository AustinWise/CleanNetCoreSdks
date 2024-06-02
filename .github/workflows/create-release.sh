#!/usr/bin/env bash

# This script takes following arguments:
#   1. The assembly name
#   2. A path to a directory containing the downloaded artifacts.
# This script can be tested on your local computer by downloading the artifacts with the
# `gh run download` command.

set -euo pipefail
set -x

if [[ ! -d $2 ]]; then
    echo "Missing directory argument"
    exit 1
fi

assembly_name=$1
temp_dir=$2

echo "$(cd $temp_dir && ls | grep symbols-)" | tar -C $temp_dir -czf $temp_dir/symbols.tar.gz --verbatim-files-from -T /dev/stdin

for d in $temp_dir/program-*/; do
    cp README.md $d/
    cp LICENSE $d/
    pushd $d
    if [[ $d =~ "/program-win" ]]; then
        zip -r "../$(basename $d | sed -e 's/^program-//').zip" *
    else
        echo skip
        chmod +x "$assembly_name"
        tar czf "../$(basename $d | sed -e 's/^program-//' -e 's/^osx-/macos-/').tar.gz" *
    fi
    popd
done
