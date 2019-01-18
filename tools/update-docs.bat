@echo off

git diff --staged --exit-code
IF errorlevel 1 (
    echo Error: commit staged changes before updating the documentation
    exit /B
)

docfx %~dp0/../src/docs/docfx.json
git add %~dp0/../src/docs
git add %~dp0/../docs
git commit -m "Update documentation"
echo Done