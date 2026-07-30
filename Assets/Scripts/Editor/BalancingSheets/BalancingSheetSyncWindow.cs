using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace NeoSurvive.Editor.BalancingSheets
{
    internal sealed class BalancingSheetSyncWindow : EditorWindow
    {
        private const string SpreadsheetUrlPreference =
            "NeoSurvive.BalancingSheets.SpreadsheetUrl";

        private readonly List<string> logLines = new List<string>();
        private string spreadsheetUrl;
        private Vector2 logScroll;
        private bool isBusy;
        private CancellationTokenSource cancellation;

        [MenuItem("Tools/NeoSurvive/Balancing Sheets")]
        private static void Open()
        {
            var window = GetWindow<BalancingSheetSyncWindow>("Balancing Sheets");
            window.minSize = new Vector2(620f, 430f);
        }

        private void OnEnable()
        {
            spreadsheetUrl = EditorPrefs.GetString(
                SpreadsheetUrlPreference,
                BalancingSheetCatalog.DefaultSpreadsheetUrl
            );
        }

        private void OnDisable()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("NeoSurvive 밸런싱 시트 동기화", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Google Sheets의 29개 데이터 탭을 검증한 뒤 기존 CSV 내용만 갱신합니다. "
                    + "검증 실패 시 어떤 CSV도 수정하지 않습니다.",
                MessageType.Info
            );

            EditorGUI.BeginChangeCheck();
            spreadsheetUrl = EditorGUILayout.TextField("Google Sheets 링크", spreadsheetUrl);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString(SpreadsheetUrlPreference, spreadsheetUrl.Trim());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("시트 열기", GUILayout.Height(26f)))
                Application.OpenURL(spreadsheetUrl);

            using (new EditorGUI.DisabledScope(isBusy))
            {
                if (GUILayout.Button("시트 연결 확인", GUILayout.Height(26f)))
                    RunConnectionCheck();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(isBusy || EditorApplication.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("전체 데이터 검증", GUILayout.Height(32f)))
                    RunValidation(showOnlyChanges: false);

                if (GUILayout.Button("변경 내역 확인", GUILayout.Height(32f)))
                    RunValidation(showOnlyChanges: true);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0.72f, 0.9f, 0.72f);
                if (GUILayout.Button("전체 동기화", GUILayout.Height(36f)))
                    RunSynchronization();
                GUI.backgroundColor = Color.white;
            }

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play Mode에서는 동기화할 수 없습니다. Play를 종료한 뒤 실행해주세요.",
                    MessageType.Warning
                );
            }

            if (isBusy)
                EditorGUILayout.HelpBox("Google Sheets 데이터를 처리하고 있습니다.", MessageType.None);

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("실행 결과", EditorStyles.boldLabel);
            if (GUILayout.Button("로그 지우기", GUILayout.Width(90f)))
                logLines.Clear();
            EditorGUILayout.EndHorizontal();

            logScroll = EditorGUILayout.BeginScrollView(logScroll, GUI.skin.box);
            EditorGUILayout.SelectableLabel(
                logLines.Count == 0 ? "아직 실행 결과가 없습니다." : string.Join("\n", logLines),
                EditorStyles.wordWrappedLabel,
                GUILayout.ExpandHeight(true)
            );
            EditorGUILayout.EndScrollView();
        }

        private async void RunConnectionCheck()
        {
            if (!TryBeginOperation(out string spreadsheetId))
                return;

            try
            {
                BalancingSheetDefinition first = BalancingSheetCatalog.All[0];
                string csv = await DownloadSheetAsync(first, spreadsheetId, cancellation.Token);
                string localCsv = File.ReadAllText(GetFullPath(first.AssetPath), Encoding.UTF8);
                BalancingCsvValidationResult result =
                    BalancingCsvValidator.Validate(first, csv, localCsv);
                if (!result.IsValid)
                {
                    LogValidationErrors(result);
                    EditorUtility.DisplayDialog(
                        "연결은 됐지만 데이터 오류",
                        "시트에는 연결됐지만 첫 번째 데이터 탭 검증에 실패했습니다.",
                        "확인"
                    );
                    return;
                }

                Log($"연결 성공: {first.SheetName}");
                EditorUtility.DisplayDialog(
                    "연결 성공",
                    "Google Sheets에서 CSV 데이터를 정상적으로 내려받았습니다.",
                    "확인"
                );
            }
            catch (OperationCanceledException)
            {
                Log("연결 확인이 취소됐습니다.");
            }
            catch (Exception exception)
            {
                HandleOperationError("연결 확인 실패", exception);
            }
            finally
            {
                EndOperation();
            }
        }

        private async void RunValidation(bool showOnlyChanges)
        {
            if (!TryBeginOperation(out string spreadsheetId))
                return;

            try
            {
                SyncSnapshot snapshot =
                    await DownloadAndValidateAllAsync(spreadsheetId, cancellation.Token);
                LogSnapshot(snapshot, showOnlyChanges);
                if (!snapshot.IsValid)
                {
                    EditorUtility.DisplayDialog(
                        "검증 실패",
                        $"오류 {snapshot.ErrorCount}개가 있습니다. CSV는 수정되지 않았습니다.",
                        "확인"
                    );
                    return;
                }

                string message =
                    $"29개 탭 검증 완료\n변경 예정 CSV: {snapshot.ChangedDefinitions.Count}개"
                    + $"\n경고: {snapshot.WarningCount}개";
                EditorUtility.DisplayDialog("검증 성공", message, "확인");
            }
            catch (OperationCanceledException)
            {
                Log("전체 검증이 취소됐습니다.");
            }
            catch (Exception exception)
            {
                HandleOperationError("전체 검증 실패", exception);
            }
            finally
            {
                EndOperation();
            }
        }

        private async void RunSynchronization()
        {
            if (!TryBeginOperation(out string spreadsheetId))
                return;

            try
            {
                SyncSnapshot snapshot =
                    await DownloadAndValidateAllAsync(spreadsheetId, cancellation.Token);
                LogSnapshot(snapshot, showOnlyChanges: false);
                if (!snapshot.IsValid)
                {
                    EditorUtility.DisplayDialog(
                        "동기화 중단",
                        $"검증 오류 {snapshot.ErrorCount}개가 있어 어떤 CSV도 수정하지 않았습니다.",
                        "확인"
                    );
                    return;
                }

                if (snapshot.ChangedDefinitions.Count == 0)
                {
                    EditorUtility.DisplayDialog(
                        "변경 없음",
                        "Google Sheets와 Unity CSV가 이미 동일합니다.",
                        "확인"
                    );
                    return;
                }

                bool confirmed = EditorUtility.DisplayDialog(
                    "전체 동기화 확인",
                    $"{snapshot.ChangedDefinitions.Count}개 CSV의 내용을 Google Sheets 값으로 갱신합니다."
                        + "\n.meta와 Unity GUID는 변경하지 않습니다.\n계속할까요?",
                    "동기화",
                    "취소"
                );
                if (!confirmed)
                {
                    Log("사용자가 동기화를 취소했습니다.");
                    return;
                }

                ApplyAtomically(snapshot);
                Log($"동기화 완료: CSV {snapshot.ChangedDefinitions.Count}개 갱신");
                EditorUtility.DisplayDialog(
                    "동기화 완료",
                    $"{snapshot.ChangedDefinitions.Count}개 CSV를 갱신했습니다."
                        + "\nPlay를 다시 시작하면 변경된 값이 적용됩니다.",
                    "확인"
                );
            }
            catch (OperationCanceledException)
            {
                Log("전체 동기화가 취소됐습니다.");
            }
            catch (Exception exception)
            {
                HandleOperationError("전체 동기화 실패", exception);
            }
            finally
            {
                EndOperation();
            }
        }

        private bool TryBeginOperation(out string spreadsheetId)
        {
            spreadsheetId = string.Empty;
            if (isBusy)
                return false;

            if (
                !BalancingSheetCatalog.TryExtractSpreadsheetId(
                    spreadsheetUrl,
                    out spreadsheetId
                )
            )
            {
                EditorUtility.DisplayDialog(
                    "잘못된 링크",
                    "올바른 Google Sheets 링크 또는 스프레드시트 ID를 입력해주세요.",
                    "확인"
                );
                return false;
            }

            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            isBusy = true;
            Repaint();
            return true;
        }

        private void EndOperation()
        {
            EditorUtility.ClearProgressBar();
            cancellation?.Dispose();
            cancellation = null;
            isBusy = false;
            Repaint();
        }

        private async Task<SyncSnapshot> DownloadAndValidateAllAsync(
            string spreadsheetId,
            CancellationToken token
        )
        {
            var snapshot = new SyncSnapshot();
            for (int index = 0; index < BalancingSheetCatalog.All.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                BalancingSheetDefinition definition = BalancingSheetCatalog.All[index];
                EditorUtility.DisplayProgressBar(
                    "Balancing Sheets 검증",
                    $"{definition.SheetName} ({index + 1}/{BalancingSheetCatalog.All.Count})",
                    (float)index / BalancingSheetCatalog.All.Count
                );

                string fullPath = GetFullPath(definition.AssetPath);
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException("로컬 CSV를 찾을 수 없습니다.", fullPath);

                string downloadedCsv =
                    await DownloadSheetAsync(definition, spreadsheetId, token);
                string currentLocalCsv = File.ReadAllText(fullPath, Encoding.UTF8);
                BalancingCsvValidationResult result =
                    BalancingCsvValidator.Validate(definition, downloadedCsv, currentLocalCsv);
                snapshot.Results[definition] = result;

                if (
                    result.IsValid
                    && !string.Equals(
                        result.NormalizedCsv,
                        BalancingCsvValidator.Normalize(currentLocalCsv),
                        StringComparison.Ordinal
                    )
                )
                {
                    snapshot.ChangedDefinitions.Add(definition);
                }
            }

            return snapshot;
        }

        private static async Task<string> DownloadSheetAsync(
            BalancingSheetDefinition definition,
            string spreadsheetId,
            CancellationToken token
        )
        {
            string url = BalancingSheetCatalog.BuildExportUrl(
                spreadsheetId,
                definition.SheetName
            );
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(50, token);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException(
                        $"{definition.SheetName} 다운로드 실패: HTTP {request.responseCode} {request.error}"
                    );
                }

                return request.downloadHandler.text;
            }
        }

        private static void ApplyAtomically(SyncSnapshot snapshot)
        {
            var originals = new Dictionary<string, byte[]>();
            var temporaryFiles = new Dictionary<string, string>();
            bool assetEditingStarted = false;

            try
            {
                foreach (BalancingSheetDefinition definition in snapshot.ChangedDefinitions)
                {
                    string fullPath = GetFullPath(definition.AssetPath);
                    string temporaryPath = fullPath + ".sheetsync.tmp";
                    originals[fullPath] = File.ReadAllBytes(fullPath);
                    File.WriteAllText(
                        temporaryPath,
                        snapshot.Results[definition].NormalizedCsv,
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                    );
                    temporaryFiles[fullPath] = temporaryPath;
                }

                AssetDatabase.StartAssetEditing();
                assetEditingStarted = true;
                foreach (KeyValuePair<string, string> pair in temporaryFiles)
                    File.Replace(pair.Value, pair.Key, null);
            }
            catch
            {
                foreach (KeyValuePair<string, byte[]> original in originals)
                    File.WriteAllBytes(original.Key, original.Value);
                throw;
            }
            finally
            {
                foreach (string temporaryPath in temporaryFiles.Values)
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }

                if (assetEditingStarted)
                    AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        private void LogSnapshot(SyncSnapshot snapshot, bool showOnlyChanges)
        {
            logLines.Clear();
            Log(
                $"검증 결과: 오류 {snapshot.ErrorCount}개, 경고 {snapshot.WarningCount}개, "
                    + $"변경 예정 {snapshot.ChangedDefinitions.Count}개"
            );

            foreach (
                KeyValuePair<BalancingSheetDefinition, BalancingCsvValidationResult> pair
                in snapshot.Results
            )
            {
                BalancingCsvValidationResult result = pair.Value;
                bool isChanged = snapshot.ChangedDefinitions.Contains(pair.Key);
                if (showOnlyChanges && !isChanged && result.IsValid && result.Warnings.Count == 0)
                    continue;

                string marker = result.IsValid ? (isChanged ? "[변경]" : "[동일]") : "[오류]";
                Log($"{marker} {pair.Key.SheetName} → {pair.Key.AssetPath}");
                foreach (string warning in result.Warnings)
                    Log($"  경고: {warning}");
                foreach (string error in result.Errors)
                    Log($"  오류: {error}");
            }
        }

        private void LogValidationErrors(BalancingCsvValidationResult result)
        {
            Log($"[오류] {result.SheetName}");
            foreach (string error in result.Errors)
                Log($"  {error}");
        }

        private void HandleOperationError(string title, Exception exception)
        {
            Log($"{title}: {exception.Message}");
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(title, exception.Message, "확인");
        }

        private void Log(string message)
        {
            logLines.Add(message);
            logScroll.y = float.MaxValue;
            Repaint();
        }

        private static string GetFullPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                throw new DirectoryNotFoundException("Unity 프로젝트 루트 경로를 확인할 수 없습니다.");

            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private sealed class SyncSnapshot
        {
            public Dictionary<BalancingSheetDefinition, BalancingCsvValidationResult> Results { get; }
                = new Dictionary<BalancingSheetDefinition, BalancingCsvValidationResult>();

            public List<BalancingSheetDefinition> ChangedDefinitions { get; }
                = new List<BalancingSheetDefinition>();

            public int ErrorCount => Results.Values.Sum(result => result.Errors.Count);
            public int WarningCount => Results.Values.Sum(result => result.Warnings.Count);
            public bool IsValid => ErrorCount == 0;
        }
    }
}
