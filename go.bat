@echo off
pushd src\S3Explorer
dotnet build
if %errorlevel% neq 0 (
    popd
    exit /b %errorlevel%
)
dotnet run
popd
