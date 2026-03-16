#!/bin/bash
set -euo pipefail

# Only run in remote (Claude Code on the web) environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

echo "Installing .NET SDK 10.0..."
apt-get update -q
apt-get install -y dotnet-sdk-10.0

echo "Installing task (go-task)..."
TASK_VERSION="3.42.1"
curl -fsSL "https://github.com/go-task/task/releases/download/v${TASK_VERSION}/task_linux_amd64.tar.gz" \
  | tar -xz -C /usr/local/bin task

echo "Restoring .NET tools (CSharpier)..."
cd "$CLAUDE_PROJECT_DIR/app"
dotnet tool restore

echo "Restoring NuGet packages..."
dotnet restore

echo ".NET environment ready."
