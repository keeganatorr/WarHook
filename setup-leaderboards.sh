#!/usr/bin/env bash
# Set up WarHook online leaderboards for a Supabase project.
#
# What it does:
#   1. Applies supabase/schema.sql to your project (Management API).
#   2. Verifies the deployment with the anon key (top_scores RPC reachable).
#   3. Writes PyoroGL/supabase.json, which both builds embed at compile time
#      (no plaintext key file ships in the itch.io or desktop releases).
#   4. With --build, rebuilds the web and win-x64 artifacts.
#
# Usage:
#   ./setup-leaderboards.sh                          # interactive prompts
#   ./setup-leaderboards.sh --url https://abcdefgh.supabase.co \
#       --anon-key sb_publishable_... --access-token sbp_... [--build]
#
# The access token is created at https://supabase.com/dashboard/account/tokens
# and is only used by this script; it never reaches any build output.
set -euo pipefail

cd "$(dirname "$0")"

URL="" ANON_KEY="" ACCESS_TOKEN="" DO_BUILD=0

usage() {
    grep '^#' "$0" | sed -n '2,20p' | sed 's/^# \{0,1\}//'
    exit 0
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --url) URL="$2"; shift 2 ;;
        --anon-key) ANON_KEY="$2"; shift 2 ;;
        --access-token) ACCESS_TOKEN="$2"; shift 2 ;;
        --build) DO_BUILD=1; shift ;;
        -h|--help) usage ;;
        *) echo "Unknown option: $1" >&2; usage ;;
    esac
done

for tool in curl python3; do
    command -v "$tool" >/dev/null 2>&1 || { echo "ERROR: '$tool' is required." >&2; exit 1; }
done
[[ -f supabase/schema.sql ]] || { echo "ERROR: supabase/schema.sql not found (run from the repo root)." >&2; exit 1; }

mask() { local s="$1"; echo "${s:0:8}..."; }

if [[ -z "$URL" ]]; then
    read -r -p "Supabase project URL or ref (e.g. https://abcdefgh.supabase.co): " URL
fi
if [[ -z "$ANON_KEY" ]]; then
    read -r -p "Supabase anon/publishable key: " ANON_KEY
fi
if [[ -z "$ACCESS_TOKEN" ]]; then
    echo "Create a personal access token at https://supabase.com/dashboard/account/tokens"
    read -r -p "Supabase access token: " ACCESS_TOKEN
fi

# Accept either a full project URL or just the ref. Tolerate paste mistakes
# by picking the last https://... token whose ref looks valid (20 chars).
URL="$(python3 -c "
import re
from urllib.parse import urlparse
raw = '''$URL'''
tokens = re.findall(r'https://[A-Za-z0-9.-]+', raw) or [raw.strip()]
best = None
for token in tokens:
    url = token.rstrip('/').rstrip('):,;')
    if not url.startswith('https://'):
        url = 'https://' + url
    host = urlparse(url).hostname or ''
    ref = host.split('.')[0]
    if re.fullmatch(r'[a-z0-9]{20}', ref):
        best = url.rstrip('/') if '.' in host else f'https://{ref}.supabase.co'
print(best if best else tokens[0].rstrip('/'))")"
if [[ "$URL" == https://* ]]; then
    URL="${URL%/}"
else
    URL="https://${URL%.supabase.co}.supabase.co"
fi
REF="$(python3 -c "from urllib.parse import urlparse; print(urlparse('$URL').hostname.split('.')[0])")"
if ! [[ "$REF" =~ ^[a-z0-9]{20}$ ]]; then
    echo "ERROR: '$REF' (from '$URL') does not look like a Supabase project ref" >&2
    echo "(refs are 20 lowercase letters/digits, e.g. ugocarmmfddicqgeyeyh)." >&2
    echo "Paste the project URL from Supabase Dashboard -> Project Settings -> General." >&2
    exit 1
fi

# The anon key is public by design, but a service_role key must NEVER ship.
ROLE="$(python3 -c "
import base64, json, sys
token = '$ANON_KEY'
try:
    payload = token.split('.')[1]
    payload += '=' * (-len(payload) % 4)
    print(json.loads(base64.urlsafe_b64decode(payload)).get('role', 'unknown'))
except Exception:
    print('unknown')  # new opaque publishable keys are not JWTs; fine
")"
if [[ "$ROLE" == "service_role" ]]; then
    echo "ERROR: that key is a service_role (secret) key. It must never be" >&2
    echo "embedded in a game build. Use the anon/publishable key instead." >&2
    exit 1
fi

echo "==> Applying schema to project $REF"
BODY="$(python3 -c "
import json
print(json.dumps({'query': open('supabase/schema.sql').read(), 'read_only': False}))")"
HTTP_CODE="$(curl -s -o /tmp/wh-schema-resp.json -w '%{http_code}' \
    -X POST "https://api.supabase.com/v1/projects/$REF/database/query" \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -H "api-key: $ACCESS_TOKEN" \
    -H "Content-Type: application/json" \
    -d "$BODY")"
if [[ "$HTTP_CODE" != 2* ]]; then
    echo "ERROR: schema application failed (HTTP $HTTP_CODE):" >&2
    head -c 800 /tmp/wh-schema-resp.json >&2; echo >&2
    exit 1
fi
echo "    schema applied."

echo "==> Verifying leaderboards with the anon key"
VERIFY_CODE="" VERIFY_BODY=""
for attempt in {1..10}; do
    VERIFY="$(curl -s -w '\n%{http_code}' \
        "$URL/rest/v1/rpc/top_scores?p_mode=game_a&p_player_id=00000000000000000000000000000000" \
        -H "api-key: $ANON_KEY" \
        -H "apikey: $ANON_KEY" \
        -H "Authorization: Bearer $ANON_KEY")"
    VERIFY_CODE="${VERIFY##*$'\n'}"
    VERIFY_BODY="${VERIFY%$'\n'*}"
    if [[ "$VERIFY_CODE" == 2* ]] && python3 -c "import json, sys; value = json.load(sys.stdin); sys.exit(0 if isinstance(value, list) else 1)" <<<"$VERIFY_BODY"; then
        break
    fi
    [[ "$attempt" -lt 10 ]] && sleep 1
done
if [[ "$VERIFY_CODE" != 2* ]] || ! python3 -c "import json, sys; value = json.load(sys.stdin); sys.exit(0 if isinstance(value, list) else 1)" <<<"$VERIFY_BODY"; then
    echo "ERROR: leaderboard verification failed (HTTP $VERIFY_CODE):" >&2
    echo "$VERIFY_BODY" | head -c 400 >&2; echo >&2
    exit 1
fi
echo "    top_scores RPC reachable, anon key valid."

python3 -c "
import json
json.dump({'Url': '$URL', 'AnonKey': '$ANON_KEY'}, open('PyoroGL/supabase.json', 'w'), indent=2)
open('PyoroGL/supabase.json', 'a').write('\n')"
echo "==> Wrote PyoroGL/supabase.json ($(mask "$ANON_KEY")) — embedded into builds, git-ignored."

if [[ "$DO_BUILD" == "1" ]]; then
    echo "==> Rebuilding web + win-x64 artifacts"
    ./build-web.sh
    ./build.sh win-x64
    echo "    upload dist/WarHook-web.zip to itch.io"
else
    echo "==> Next: rebuild with ./build-web.sh and/or ./build.sh win-x64"
fi

cat <<'EOF'

Security notes:
  - The anon/publishable key is public by design (like a Firebase web key).
    It is embedded in the game binaries; expect it to be recoverable by a
    determined user from the web build. It grants no admin access.
  - No service_role key, database password, or access token is ever stored
    in the repo or shipped in a release.
  - Abuse is mitigated server-side: submit_score validates mode, initials,
    and score range, and rate-limits one submission per IP every 5 seconds.
  - You can revoke the access token now (Dashboard -> Account -> Access
    tokens); it is only needed when re-running this script.
EOF
