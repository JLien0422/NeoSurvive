using UnityEngine;

namespace NeoSurvive.Map.Map1.Gimmicks
{
    /// <summary>
    /// [Map1 Gimmick]
    /// 화물 낙하 경고 표시 오브젝트
    ///
    /// [역할]
    /// - 화물 낙하 직전에 바닥에 잠깐 표시되는 경고 마커
    /// - 일정 시간 동안 유지되며 필요 시 깜빡임(blink) 연출 수행
    /// - 실제 데미지는 주지 않고, 순수 경고/연출 역할만 담당
    ///
    /// [연결 구조]
    /// - 이 스크립트는 단독으로 주요 로직을 돌리는 메인 기믹이 아님
    /// - 상위 제어기인 CargoDropCrane가 warningPrefab으로 Instantiate 후 사용
    /// - CargoDropCrane.SpawnWarning() -> CargoDropWarning.Initialize() 흐름으로 연결됨
    ///
    /// [다른 시스템과의 관계]
    /// - SpriteRenderer: 경고 마커의 시각 표현 및 알파값 깜빡임 처리
    /// - CargoDropCrane: 생성 시 duration / blink / blinkSpeed 값을 전달하는 상위 제어기
    ///
    /// [Git 병합 시 주의]
    /// - warningPrefab에 이 스크립트가 반드시 붙어 있어야 함
    /// - SpriteRenderer가 프리팹에서 빠지면 경고 마커는 보여도 blink 연출이 깨질 수 있음
    /// - sorting layer / order가 잘못되면 바닥에 생성돼도 안 보일 수 있음
    ///
    /// [QA 체크 포인트]
    /// 1. 경고 프리팹이 낙하 예정 위치에 생성되는가
    /// 2. blink=true일 때 알파값이 깜빡이며 변하는가
    /// 3. blink=false일 때 고정 표시되는가
    /// 4. duration 시간 이후 자동 삭제되는가
    /// 5. 낙하 위치와 경고 표시 위치가 일치하는가
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CargoDropWarning : MonoBehaviour
    {
        [Header("Lifetime")]
        // 경고 표시가 유지되는 시간
        [SerializeField] private float duration = 3f;

        [Header("Blink")]
        // 깜빡임 연출 사용 여부
        [SerializeField] private bool blink = true;

        // 깜빡임 속도
        [SerializeField] private float blinkSpeed = 8f;

        // 현재 오브젝트의 SpriteRenderer 캐싱
        private SpriteRenderer sr;

        // 생성 후 누적된 시간
        private float timer = 0f;

        private void Awake()
        {
            // 경고 마커의 시각 연출을 위해 SpriteRenderer를 가져옴
            sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// CargoDropCrane가 warningPrefab 생성 직후 호출하는 초기화 함수
        ///
        /// [의미]
        /// - 프리팹의 기본 인스펙터 값에만 의존하지 않고
        ///   상위 기믹(CargoDropCrane)의 CSV/설정값을 여기로 전달하기 위한 구조
        ///
        /// [호출 흐름]
        /// CargoDropCrane.SpawnWarning()
        ///   -> Instantiate(warningPrefab)
        ///   -> warning.Initialize(warningDuration, blink, blinkSpeed)
        /// </summary>
        public void Initialize(float duration, bool blink, float blinkSpeed)
        {
            this.duration = duration;
            this.blink = blink;
            this.blinkSpeed = blinkSpeed;
        }

        private void Update()
        {
            // 경고 유지 시간 누적
            timer += Time.deltaTime;

            // 깜빡임 연출:
            // alpha를 Sin 파형으로 변화시켜 반투명하게 점멸하는 느낌을 만듦
            if (blink && sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.25f, 0.8f, (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f);
                sr.color = c;
            }

            // 유지 시간이 끝나면 자동 삭제
            if (timer >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}