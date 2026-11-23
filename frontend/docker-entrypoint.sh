#!/bin/sh
set -eu

envsubst '${API_UPSTREAM}' \
  < /etc/nginx/templates/nginx.conf.template \
  > /tmp/nginx.conf

exec "$@"
