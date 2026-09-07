# 3D_portpolio

Unity(URP/HDRP) 기반 커맨드형 RPG 배틀 시스템 포트폴리오 프로젝트입니다.
필드 탐험, ATB(Active Time Battle) 방식의 커맨드 배틀, 스킬/아이템 시스템, CSV 기반 데이터 관리 기능을 직접 구현했습니다.

## 개발 환경

- Engine: Unity 6000.3.21f1
- Render Pipeline: Universal RP / High Definition RP
- Language: C#

## 주요 기능

- **커맨드 배틀 시스템**: ATB 큐(`ATQueue`) 기반 턴 순서 관리, 명중/회피·방어력 반영 데미지, 속성 상성, 크리티컬 판정, 전멸 승패 판정
- **스킬 시스템**: `SkillDataBase` / `SkillResolver` / `SkillEffect`를 통한 스킬 데이터 관리 및 효과 처리
- **아이템 시스템**: `ItemDataBase` 및 CSV 기반 데이터 로더(`CSVFileLoader`, `CsvTable`)
- **필드 탐험 & 메뉴 UI**: 조우 트리거, 상태창, 아이템 메뉴 UI
- **세이브/로드**

## 폴더 구조

```
Assets/
  Scripts/        # 직접 작성한 게임 로직 (Battle, Field, Manager, Status 등)
  Scenes/         # Intro / Field / CommandBattle 등 씬
  Model/          # 캐릭터/오브젝트 모델
  ...
Docs/              # 기획/검증 문서
```

## 사용한 외부 에셋 및 라이선스 고지

이 프로젝트는 아래의 서드파티 에셋을 사용했습니다. 각 에셋의 저작권은 원저작자에게 있으며, 라이선스 조건에 따라 이 저장소에는 **원본 에셋 파일을 포함하지 않고** 필요 시 각 배포처에서 개별적으로 내려받아 사용해야 합니다.

| 에셋 | 제작/배포 | 용도 | 라이선스 |
|---|---|---|---|
| MicroSplat (Core) | Jason Booth (Unity Asset Store) | 터레인 텍스처링 셰이더 | 유료 / Asset Store EULA — 재배포 불가 |
| Hovl Studio VFX (Magic sword 등) | Hovl Studio | 이펙트 | Asset Store 라이선스 — 재배포 제한 |
| Fantasy Skybox FREE | Rugo Games | 스카이박스 | Asset Store 무료 라이선스 — 재배포 제한 |
| Free Island Collection | Unity Asset Store | 환경 에셋 | Asset Store 무료 라이선스 — 재배포 제한 |
| Nature Renderer (Free) | VacuumShaders | 식생 렌더링 | Asset Store 무료 라이선스 — 재배포 제한 |
| Unity-chan! Model | Unity Technologies Japan | 캐릭터 모델 (데모용) | [Unity-Chan License Terms](https://unity-chan.com/contents/license_en/) |
| Punk Female Model | 마켓플레이스 구매 모델 | 캐릭터 모델 | 개별 구매처 라이선스 — 재배포 불가 |
| TextMesh Pro | Unity Technologies | UI 텍스트 | Unity 기본 패키지 |
| glTFast | atteneder | glTF 임포트 | MIT |

> 위 에셋 중 재배포가 제한된 항목은 이 저장소를 클론해도 정상적으로 빌드되지 않을 수 있습니다. 직접 실행해보시려면 각 에셋을 Unity Asset Store 또는 원 배포처에서 개별적으로 받아 동일 경로에 배치해주세요.

## 문서

- `Docs/` — 스킬 시스템 적용 안내, 액션바/딜레이 UI 적용 안내, 검증 테스트케이스 등

---

📩 문의: lwy810@naver.com
