# h_sms_sender

PC에서 편지를 작성한 다음 휴대폰을 통해 SMS로 발송하는 프로그램.
대상 사용자는 컴퓨터에 약한 사람 (마스터의 가족 등) — 그쪽 PC에 폴더째 배포해서 쓰게 만드는 것이 목표.

## 스택
- C# WinForms, .NET 8 (`net8.0-windows`)
- framework-dependent 빌드 (사용자 PC에 .NET 8 Desktop Runtime 설치 필요)
- 외부 의존성: **KDE Connect** (PC 클라이언트 + 안드로이드 앱)

## 동작 원리
PC ↔ 폰을 KDE Connect로 페어링해두고, 우리 앱은 `kdeconnect-cli.exe`를 호출해서 폰에 SMS 발송 명령을 던짐.
폰의 KDE Connect 앱이 그 명령을 받아 안드로이드 SMS API로 실제 SMS를 발송.
PC는 GUI/편지 작성 + 발송 트리거만 담당, 실제 통신은 폰의 SIM이 함.

## 파일 구조
```
h_sms_sender/
├── Program.cs                  // 진입점, 예외 로깅 (%TEMP%\h_sms_sender_crash.log)
├── MainForm.cs                 // 좌측 수신자 체크리스트 + 우측 큰 편집창 + 발송
├── SettingsForm.cs             // CLI 경로/기기/폰트/수신자 그리드
├── HistoryForm.cs              // 발송 기록 (날짜+첫줄, 본문 미리보기, 삭제)
├── Models/
│   ├── AppConfig.cs            // KDE CLI 경로, DeviceId, FontConfig, Recipients
│   ├── Recipient.cs
│   └── HistoryEntry.cs         // Timestamp, RecipientNames/Phones, Body, Failures
├── Services/
│   ├── Storage.cs              // %APPDATA%\h_sms_sender\config.json + history.json
│   └── KdeConnectService.cs    // CLI 호출 (ListDevicesAsync, SendSmsAsync, PingAsync, AutoDetectCliPath)
└── 설치_설명서.md               // 그쪽 PC + 폰 환경설정 가이드 (배포 시 동봉)
```

## 빌드/배포
```bash
# 디버그 빌드
dotnet build

# 배포용 (framework-dependent, 폴더째 복사하면 됨)
dotnet publish -c Release -o publish --self-contained false
```
publish/ 폴더 + 설치_설명서.md를 zip으로 묶어 그쪽 PC에 풀어놓는 식.
첫 실행 시 .NET 8 Desktop Runtime이 없으면 안내 메시지가 뜸 (Microsoft URL).

## 알려진 함정 (디버깅하면서 발견)

### 1. SplitContainer SplitterDistance 초기화 순서
- 컨트롤이 부모에 추가되어 사이즈를 받기 전에 `SplitterDistance`를 직접 설정하면 default(150px)보다 큰 값일 때 폼 생성이 silent하게 실패 (창 안 뜸)
- **해결:** `HandleCreated` 이벤트에서 try/catch로 설정
- MainForm + HistoryForm 둘 다 적용

### 2. KDE Connect 설치 경로
- 기본 경로: `C:\Program Files\KDE Connect\bin\kdeconnect-cli.exe` (공백 포함)
- `KDE\bin`이 아님 — 처음 잘못 박아놨다가 수정
- `KdeConnectService.AutoDetectCliPath()`로 일반적 위치들 자동 탐색
- `Storage.LoadConfig()`에서 저장된 경로가 무효하면 자동 보정해서 다시 저장

### 3. 폰 쪽 SMS 권한 (가장 큰 함정)
- KDE Connect 앱 안의 "SMS messages" 플러그인 체크만으로는 부족
- **안드로이드 시스템 설정 → 앱 → KDE Connect → 권한**에서 SMS/전화/연락처 모두 명시적 허용 필요
- 권한 안 주면 CLI가 exit 0으로 성공 반환하지만 실제 SMS는 silent하게 무시됨
- 설치_설명서.md 5단계에 강조

### 4. 폰의 KDE Connect 잠들음 (배터리 최적화)
- 갤럭시 등은 배터리 절약 때문에 KDE Connect를 죽이거나 deep sleep으로 보냄
- 결과: PC쪽 CLI는 "연결되고 접근 가능함"으로 보고하지만 실제로는 통신 불가
- **해결:** 폰 설정 → 배터리 → 백그라운드 사용 한도 → "절대 잠자기 앱"에 KDE Connect 추가
- 추가 보완: **앱 시작 시 자동으로 ping을 던져 폰 깨우기** (`MainForm.WakeDeviceAsync`) — ping 알림이 폰에 뜨는데 사용자는 "연결 살아있다는 신호"로 받아들여 OK함

### 5. CLI는 fire-and-forget
- `kdeconnect-cli --send-sms`는 폰에 요청을 던지고 exit 0 반환할 뿐, 실제 SMS 도착 여부 모름
- 우리 앱의 "발송 완료" 메시지도 엄밀히 "발송 요청 완료"임 (TODO: 메시지 정직하게 바꾸기 항목으로 남아있음)

## 데이터 저장 위치
- 설정: `%APPDATA%\h_sms_sender\config.json`
- 발송 기록: `%APPDATA%\h_sms_sender\history.json`
- 충돌 로그: `%TEMP%\h_sms_sender_crash.log`

## 미완 / 향후 항목
- "발송 완료" → "발송 요청 보냄"으로 메시지 정직하게
- 창 종료 시 미저장 본문 보호 다이얼로그
- (선택) Inno Setup 인스톨러로 KDE Connect까지 묶기

## Repo
https://github.com/Gayasaka-Rin/h_sms_sender (main 브랜치)
