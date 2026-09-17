#!/usr/bin/env bash
set -euo pipefail

# Run from the extracted portable client directory, independently of the caller's cwd.
package_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"

if [[ "$(uname -s)" != Linux || "$(uname -m)" != x86_64 ]]; then
    printf '%s\n' 'This package requires 64-bit x86 Linux.' >&2
    exit 1
fi

if [[ ! -x "$package_dir/osu!" ]]; then
    printf '%s\n' 'Extract the entire Linux package before running this launcher.' >&2
    exit 1
fi

# A framework.ini beside the executable selects osu!framework portable storage.
# Never truncate an existing configuration or redirect to the normal client database.
if [[ ! -e "$package_dir/framework.ini" ]]; then
    (set -o noclobber; printf 'WindowMode = Windowed\n' > "$package_dir/framework.ini")
fi

# Keep the official updater from replacing this personal practice build.
export OSU_EXTERNAL_UPDATE_PROVIDER='Phantom Misses personal fork'
unset OSU_WEBSOCKET_SERVER

printf '%s\n' 'Phantom Misses portable client. Close other osu! clients before starting.'
cd -- "$package_dir"
exec "$package_dir/osu!" "$@"
