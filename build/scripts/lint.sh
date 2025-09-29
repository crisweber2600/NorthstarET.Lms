#!/bin/bash
# Lint all C# files in the solution

set -e

echo "Running dotnet format..."
dotnet format --verify-no-changes --verbosity diagnostic

echo "Running dotnet build for style analysis..."
dotnet build --no-restore --verbosity quiet

echo "Linting completed successfully!"