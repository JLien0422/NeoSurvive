namespace NeoSurvive.Network
{
  /// <summary>
  /// 원격 플레이어 액션 실행 중인지 표시하는 컨텍스트
  /// </summary>
  public static class NetworkDamageContext
  {
    public static bool IsRemoteAction { get; private set; }

    public static void BeginRemoteAction()
    {
      IsRemoteAction = true;
    }

    public static void EndRemoteAction()
    {
      IsRemoteAction = false;
    }
  }
}
