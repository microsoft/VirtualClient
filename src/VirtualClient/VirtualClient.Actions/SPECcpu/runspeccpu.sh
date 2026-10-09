ulimit -s unlimited

export LOGNAME="${LOGNAME:-$(/usr/bin/id -un)}"
export USER="${USER:-$LOGNAME}"
export HOME="${HOME:-$(/usr/bin/getent passwd "$LOGNAME" | /usr/bin/cut -d: -f6)}"
export PATH="${PATH:-/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin}"

if [ "$1" = "--sysinfo" ]; then
    spec_root="$(cd "$(dirname "$0")" && pwd)"
    exec "$spec_root/bin/specperl" "$spec_root/bin/sysinfo"
fi

bin/runcpu $1