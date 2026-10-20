#!/usr/bin/env bash
set -euo pipefail
BASE="${BASE:-http://localhost:5180}"
TOKEN="${API_TOKEN:?Set API_TOKEN to the same value as ApiToken before testing}"
request() {
  local method="$1" path="$2" expected="$3" body="${4:-}" code
  if [[ -n "$body" ]]; then
    code=$(curl -s -o /tmp/user-management-response.json -w '%{http_code}' -X "$method" "$BASE$path" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "$body")
  else
    code=$(curl -s -o /tmp/user-management-response.json -w '%{http_code}' -X "$method" "$BASE$path" -H "Authorization: Bearer $TOKEN")
  fi
  if [[ "$code" != "$expected" ]]; then
    echo "FAIL $method $path: expected $expected, got $code"; cat /tmp/user-management-response.json; exit 1
  fi
  echo "PASS $method $path ($code)"
}
request GET /api/users 200
request POST /api/users 400 '{"name":" A ","email":"jane@example.com","department":"HR"}'
request POST /api/users 201 '{"name":"Jane Doe","email":"jane@example.com","department":"HR"}'
request GET /api/users/1 200
request PUT /api/users/1 200 '{"name":"Jane Smith","email":"jane.smith@example.com","department":"IT"}'
request POST /api/users 400 '{"name":"","email":"bad-email","department":""}'
request GET /api/users/999 404
request DELETE /api/users/1 204
request GET /api/users/1 404
unauth=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/users")
[[ "$unauth" == 401 ]] && echo 'PASS unauthorized (401)' || { echo "FAIL unauthorized ($unauth)"; exit 1; }
echo 'All smoke tests passed.'
