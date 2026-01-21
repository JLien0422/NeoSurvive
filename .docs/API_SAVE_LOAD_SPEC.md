# Save/Load API 명세서

## 개요

플레이어별 key-value 저장소 API (deviceUID 기반 식별)

---

## 1. 저장 API

### `POST /api/save`

플레이어의 게임 데이터를 서버에 저장합니다.

#### Request Body

```json
{
  "deviceUID": "abc123-device-id", // 디바이스 고유 ID (필수)
  "key": "TotalGold", // 저장 키 (필수)
  "data": "{\"value\":1000}", // JSON 직렬화된 데이터 (필수)
  "dataType": "System.Int32", // 데이터 타입 (선택)
  "timestamp": "2026-01-20T12:34:56.789Z" // ISO 8601 타임스탬프 (선택)
}
```

#### Request Headers

```
Content-Type: application/json
```

#### Response (성공)

**Status Code**: `200 OK`

```json
{
  "success": true,
  "message": "Data saved successfully",
  "key": "TotalGold",
  "deviceUID": "abc123-device-id",
  "savedAt": "2026-01-20T12:34:56.789Z"
}
```

#### Response (실패)

**Status Code**: `400 Bad Request`

```json
{
  "success": false,
  "message": "Invalid request data",
  "errors": ["DeviceUID is required", "Key cannot be empty"]
}
```

**Status Code**: `404 Not Found`

```json
{
  "success": false,
  "message": "Device not found"
}
```

**Status Code**: `500 Internal Server Error`

```json
{
  "success": false,
  "message": "Failed to save data",
  "error": "Database connection error"
}
```

---

## 2. 로드 API

### `POST /api/load`

플레이어의 게임 데이터를 서버에서 로드합니다.

#### Request Body

deviceUID": "abc123-device-id", // 디바이스 고유 ID (필수)
"key": "TotalGold", // 로드할 키 (필수)
"dataType": "System.Int32"  
 "playerId": 123, // 플레이어 ID (필수)
"key": "TotalGold", // 로드할 키 (필수)
"dataType": "System.Int32" // 기대하는 데이터 타입 (선택)
}

```

#### Request Headers

```

Content-Type: application/json

````

#### Response (성공)

**Status Code**: `200 OK`

```json
{
  "key": "TotalGold",
  "data": "{\"value\":1000}",
  "dataType": "System.Int32",
  "timestamp": "2026-01-20T12:34:56.789Z"
}
````

#### Response (데이터 없음)

**Status Code**: `404 Not Found`

```json
{
  "success": false,
  "message": "Data not found for key 'TotalGold'"
}
```

#### Response (실패)

**Status Code**: `400 Bad Request`

```json
{
  "success": false,
  "message": "Invalid request data"
}
```

---

## 3. 삭제 API

### `POST /api/delete`

플레이어의 특정 키 데이터를 삭제합니다.

#### Request Body

```json
{
  "deviceUID": "abc123-device-id", // 디바이스 고유 ID (필수)
  "key": "TotalGold" // 삭제할 키 (필수)
}
```

#### Response (성공)

**Status Code**: `200 OK`

```json
{
  "success": true,
  "message": "Data deleted successfully",
  "key": "TotalGold"
}
```

#### Response (데이터 없음)

**Status Code**: `404 Not Found`

```json
{
  "success": false,
  "message": "Data not found"
}
```

---

## 4. 키 존재 확인 API (선택)

### `GET /api/save/exists?deviceUID=abc123-device-id&key=TotalGold`

특정 키의 존재 여부를 확인합니다.

#### Query Parameters

- `deviceUID` (required): 디바이스 고유 ID
- `key` (required): 확인할 키

#### Response (존재)

**Status Code**: `200 OK`

```json
{
  "exists": true,
  "key": "TotalGold",
  "lastModified": "2026-01-20T12:34:56.789Z"
}
```

#### Response (없음)

**Status Code**: `200 OK`

```json
{
  "exists": false,
  "key": "TotalGold"
}
```

---

## 데이터베이스 스키마 (권장)

### `player_save_data` 테이블

```sql
CREATE TABLE player_save_data (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    player_id INT NOT NULL,
    device_uid VARCHAR(255) NOT NULL,
    save_key VARCHAR(255) NOT NULL,
    save_data TEXT NOT NULL,
    data_type VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    UNIQUE KEY unique_device_key (device_uid, save_key),
    INDEX idx_device_uid (device_uid),
    INDEX idx_save_key (save_key)
```

---

## 클라이언트 사용 예시

```csharp
// 저장
await ServerSaveSystem.SaveAsync("TotalGold", 1000);

// 로드
int gold = await ServerSaveSystem.LoadAsync("TotalGold", 0);

// 삭제
await ServerSaveSystem.DeleteKeyAsync("TotalGold");

// 존재 확인
bool exists = await ServerSaveSystem.KeyExistsAsync("TotalGold");
```

---

## 보안 고려사항

1. **인증**: 모든 요청에 플레이어 인증 토큰 포함 권장
2. **속도 제한**: DoS 방지를 위한 Rate Limiting 구현
3. **데이터 암호화**: 민감한 데이터는 암호화하여 저장
4. **입력 검증**: SQL Injection, XSS 방지

---

## 에러 코드 정리

| Status Code | 설명                                        |
| ----------- | ------------------------------------------- |
| 200         | 성공                                        |
| 400         | 잘못된 요청 (필수 파라미터 누락, 형식 오류) |
| 401         | 인증 실패                                   |
| 404         | 데이터 없음 또는 플레이어 없음              |
| 429         | 요청 횟수 초과 (Rate Limit)                 |
| 500         | 서버 내부 오류                              |

---

## 성능 최적화 권장사항

1. **배치 저장**: 여러 키를 한 번에 저장하는 API 추가 고려
2. **캐싱**: Redis 등을 사용한 읽기 캐시
3. **압축**: 대용량 데이터는 gzip 압축
4. **인덱싱**: player_id + save_key 복합 인덱스
