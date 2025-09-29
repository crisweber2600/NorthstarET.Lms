#!/bin/bash
# Run all tests in the solution

set -e

echo "Running all tests..."
dotnet test --no-build --verbosity normal --logger "console;verbosity=detailed"

echo "Testing completed successfully!"