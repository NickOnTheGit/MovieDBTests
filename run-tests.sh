#!/usr/bin/env bash
set -e

if [ -z "$1" ]; then
  echo "Usage: ./run-tests.sh YOUR_TMDB_API_KEY"
  exit 1
fi

export TMDB_API_KEY=$1

echo "Restoring packages..."
dotnet restore

echo "Running tests..."
dotnet test --logger "trx;LogFileName=test_results.trx" --results-directory ./TestResults

echo "Done. Test results in ./TestResults"
