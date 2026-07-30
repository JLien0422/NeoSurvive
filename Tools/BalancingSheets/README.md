# NeoSurvive 밸런싱 워크북 생성기

Unity가 사용하는 CSV 29개를 Google Sheets에 한 번에 업로드할 수 있는 Excel 워크북으로 변환합니다. 원본 CSV는 읽기만 하며 수정하지 않습니다.

## 생성

프로젝트 루트에서 실행합니다.

```powershell
python -m pip install -r Tools/BalancingSheets/requirements.txt
python Tools/BalancingSheets/build_workbook.py
```

생성 결과:

```text
Tools/BalancingSheets/output/NeoSurvive_Balancing.xlsx
```

워크북에는 `목차` 1개와 데이터 탭 29개가 포함됩니다. 목차에서 각 탭과 원본 CSV 경로, 데이터 행 수, 검토 항목을 확인할 수 있습니다.

## Google Sheets 변환

1. 생성된 `NeoSurvive_Balancing.xlsx`를 Google Drive에 업로드합니다.
2. 파일을 열고 `파일 > Google 스프레드시트로 저장`을 선택합니다.
3. 공유 설정에서 `일반 액세스 > 링크가 있는 모든 사용자 > 편집자`를 선택합니다.
4. 헤더와 탭 구조를 보호하고, 데이터 영역만 편집 가능하도록 설정합니다.

링크 편집 권한은 링크가 유출될 경우 누구나 데이터를 변경하거나 삭제할 수 있습니다. Google Sheets 버전 기록과 Unity 동기화 전 검증을 반드시 사용해야 합니다.

## 검증

기존 결과물을 다시 생성하지 않고 구조만 검증합니다.

```powershell
python Tools/BalancingSheets/build_workbook.py --verify-only
```

현재 생성기는 값의 의미를 추측해서 수정하지 않습니다. 숫자 열의 비숫자 값, 빈 헤더, 중복 키와 행/열 개수 불일치는 워크북의 `목차`와 해당 셀에 검토 항목으로 표시됩니다.
