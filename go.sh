#!/usr/bin/env bash
set -e
cd "$(dirname "$0")/src/S3Explorer"
dotnet build
dotnet run --no-build &
disown
