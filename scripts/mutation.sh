#!/usr/bin/env bash
# Mutation testing for the Domain project. Run from repo root.
# Install once: dotnet tool install -g dotnet-stryker --version 4.13.0
set -euo pipefail
cd "$(dirname "$0")/../backend/tests/InvoiceApp.Tests"
dotnet stryker --project InvoiceApp.Domain.csproj --reporter cleartext --reporter html --reporter json
