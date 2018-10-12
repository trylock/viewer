@echo off

git diff --staged --exit-code
IF errorlevel 1 (
    echo Error: commit staged changes before updating the documentation
    exit /B
)

docfx src/docs/docfx.json
git add src/docs
git add docs
git commit -m "Update documentation"
echo Done