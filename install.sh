#!/bin/bash
# Mercury - Any GitHub file & Direct Repo PowerShell / Shell Installer for Apple & Windows
# Usage: curl -fsSL https://raw.githubusercontent.com/svreddy313-dev/mercury/main/install.sh | bash

set -e

APP_NAME="Mercury"
INSTALL_DIR="$HOME/.mercury"
BIN_DIR="$INSTALL_DIR/bin"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

header() {
    echo ""
    echo -e "${CYAN}${BOLD}  ☿ Mercury Installer${NC}"
    echo -e "  ${BLUE}Any GitHub file & Direct Repo Installer for Apple and Windows${NC}"
    echo ""
}

info()    { echo -e "  ${BLUE}→${NC} $1"; }
success() { echo -e "  ${GREEN}✓${NC} $1"; }
error()   { echo -e "  ${RED}✗${NC} $1"; exit 1; }

header

# ─── Check prerequisites ───
info "Checking prerequisites..."

if ! command -v git &>/dev/null; then
    error "Git is not installed. Install with: brew install git"
fi
success "Git found: $(git --version)"

if ! command -v dotnet &>/dev/null; then
    info ".NET runtime not found — Mercury CLI will be self-contained (no runtime needed)"
fi

# ─── Detect architecture ───
ARCH=$(uname -m)
case "$ARCH" in
    x86_64)  RID="osx-x64" ;;
    arm64)   RID="osx-arm64" ;;
    *)       error "Unsupported architecture: $ARCH" ;;
esac
info "Detected: macOS $ARCH ($RID)"

# ─── Download or build ───
echo ""
info "Installing Mercury..."

mkdir -p "$BIN_DIR"

# Check if we're running from the repo
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
if [ -f "$SCRIPT_DIR/src/UniversalGitHubInstaller.Cli/UniversalGitHubInstaller.Cli.csproj" ]; then
    info "Building from source..."
    if ! command -v dotnet &>/dev/null; then
        error ".NET 8 SDK required to build from source. Install: https://dotnet.microsoft.com/download"
    fi
    dotnet publish "$SCRIPT_DIR/src/UniversalGitHubInstaller.Cli/UniversalGitHubInstaller.Cli.csproj" \
        -c Release -r "$RID" --self-contained true -p:PublishSingleFile=true \
        -o "$BIN_DIR" 2>/dev/null
    # Rename to mercury
    if [ -f "$BIN_DIR/UniversalGitHubInstaller.Cli" ]; then
        mv "$BIN_DIR/UniversalGitHubInstaller.Cli" "$BIN_DIR/mercury"
    fi
    success "Built from source"
else
    # Download pre-built release
    REPO="svreddy313-dev/mercury"
    LATEST_URL="https://github.com/$REPO/releases/latest/download/mercury-$RID"
    
    if command -v curl &>/dev/null; then
        curl -fsSL "$LATEST_URL" -o "$BIN_DIR/mercury" 2>/dev/null || {
            error "Download failed. Build from source instead:\n  git clone https://github.com/$REPO && cd mercury && ./install.sh"
        }
    elif command -v wget &>/dev/null; then
        wget -q "$LATEST_URL" -O "$BIN_DIR/mercury" || {
            error "Download failed."
        }
    else
        error "curl or wget required"
    fi
    success "Downloaded Mercury"
fi

chmod +x "$BIN_DIR/mercury"

# ─── Add to PATH ───
SHELL_NAME=$(basename "$SHELL")
SHELL_RC=""
case "$SHELL_NAME" in
    zsh)  SHELL_RC="$HOME/.zshrc" ;;
    bash) SHELL_RC="$HOME/.bashrc" ;;
    fish) SHELL_RC="$HOME/.config/fish/config.fish" ;;
esac

if [ -n "$SHELL_RC" ]; then
    if ! grep -q "$BIN_DIR" "$SHELL_RC" 2>/dev/null; then
        echo "" >> "$SHELL_RC"
        echo "# Mercury - GitHub Installer" >> "$SHELL_RC"
        echo "export PATH=\"$BIN_DIR:\$PATH\"" >> "$SHELL_RC"
        success "Added to PATH in $SHELL_RC"
    else
        success "Already in PATH"
    fi
fi

# ─── Done ───
echo ""
echo -e "${GREEN}${BOLD}  ✓ Mercury installed successfully!${NC}"
echo ""
echo -e "  ${BOLD}Quick start:${NC}"
echo -e "    mercury install https://github.com/user/project"
echo -e "    mercury scan ./my-project"
echo -e "    mercury info ./my-project"
echo ""
echo -e "  Restart your terminal or run:"
echo -e "    source $SHELL_RC"
echo ""
