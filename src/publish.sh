#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet publish CharSeal.Console/CharSeal.Console.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true
