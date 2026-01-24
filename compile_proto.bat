@echo off
chcp 65001 > nul
echo ========================================
echo Proto 파일 재컴파일
echo ========================================
echo.

REM protoc 컴파일러 확인
if not exist "protoc_compiler\bin\protoc.exe" (
    echo [오류] protoc 컴파일러를 찾을 수 없습니다.
    echo 다음 명령어로 설치하세요:
    echo.
    echo powershell -Command "Invoke-WebRequest -Uri 'https://github.com/protocolbuffers/protobuf/releases/download/v25.1/protoc-25.1-win64.zip' -OutFile 'protoc.zip'; Expand-Archive -Path 'protoc.zip' -DestinationPath 'protoc_compiler' -Force; Remove-Item 'protoc.zip'"
    echo.
    pause
    exit /b 1
)

echo [1/3] Proto 파일 컴파일 중...
protoc_compiler\bin\protoc.exe --csharp_out=Assets\Scripts\Multiplayer --proto_path=ProtoFiles ProtoFiles\GamePacket.proto

if errorlevel 1 (
    echo [실패] 컴파일 중 오류가 발생했습니다.
    pause
    exit /b 1
)

echo [2/3] 파일 생성 확인 중...
if not exist "Assets\Scripts\Multiplayer\GamePacket.cs" (
    echo [오류] GamePacket.cs 파일이 생성되지 않았습니다.
    pause
    exit /b 1
)

echo [3/3] 파일 이름 변경 중...
move /Y "Assets\Scripts\Multiplayer\GamePacket.cs" "Assets\Scripts\Multiplayer\GameProtocol.cs" > nul

echo.
echo ========================================
echo ✓ 재컴파일 완료!
echo ========================================
echo.
echo 생성된 파일: Assets\Scripts\Multiplayer\GameProtocol.cs
echo Unity 에디터에서 자동으로 감지됩니다.
echo.
pause
