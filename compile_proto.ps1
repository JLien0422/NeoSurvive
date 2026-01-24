# Proto 파일 재컴파일 스크립트
# 사용법: .\compile_proto.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Proto 파일 재컴파일" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# protoc 컴파일러 확인
if (-not (Test-Path ".\protoc_compiler\bin\protoc.exe")) {
    Write-Host "[오류] protoc 컴파일러를 찾을 수 없습니다." -ForegroundColor Red
    Write-Host ""
    Write-Host "다운로드 중..." -ForegroundColor Yellow
    
    try {
        Invoke-WebRequest -Uri "https://github.com/protocolbuffers/protobuf/releases/download/v25.1/protoc-25.1-win64.zip" -OutFile "protoc.zip"
        Expand-Archive -Path "protoc.zip" -DestinationPath "protoc_compiler" -Force
        Remove-Item "protoc.zip"
        Write-Host "✓ protoc 컴파일러 설치 완료" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ 다운로드 실패: $_" -ForegroundColor Red
        exit 1
    }
}

# Proto 파일 컴파일
Write-Host "[1/3] Proto 파일 컴파일 중..." -ForegroundColor Yellow
& ".\protoc_compiler\bin\protoc.exe" --csharp_out=Assets\Scripts\Multiplayer --proto_path=ProtoFiles ProtoFiles\GamePacket.proto

if ($LASTEXITCODE -ne 0) {
    Write-Host "[실패] 컴파일 중 오류가 발생했습니다." -ForegroundColor Red
    exit 1
}

# 파일 생성 확인
Write-Host "[2/3] 파일 생성 확인 중..." -ForegroundColor Yellow
if (-not (Test-Path "Assets\Scripts\Multiplayer\GamePacket.cs")) {
    Write-Host "[오류] GamePacket.cs 파일이 생성되지 않았습니다." -ForegroundColor Red
    exit 1
}

# 파일 이름 변경
Write-Host "[3/3] 파일 이름 변경 중..." -ForegroundColor Yellow
Move-Item -Path "Assets\Scripts\Multiplayer\GamePacket.cs" -Destination "Assets\Scripts\Multiplayer\GameProtocol.cs" -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ 재컴파일 완료!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "생성된 파일: Assets\Scripts\Multiplayer\GameProtocol.cs" -ForegroundColor White
Write-Host "Unity 에디터에서 자동으로 감지됩니다." -ForegroundColor Gray
Write-Host ""
