# CI 설정 (GitHub Actions / Unity)

이 리포지토리는 Unity 프로젝트가 `notitle/` 하위 폴더에 있습니다.  
CI 워크플로우는 리포지토리 루트의 `.github/workflows/`에 생성됩니다.

## 1) 워크플로우

- `/.github/workflows/unity-ci.yml`
  - PR/푸시에서 테스트 실행(라이선스 필요)
  - 수동 실행(`workflow_dispatch`)에서 Windows 빌드도 가능
- `/.github/workflows/unity-activate.yml`
  - Unity Personal 라이선스용 활성화 파일(`.alf`) 생성(수동 실행)

## 2) Unity 라이선스(UNITY_LICENSE) 준비 (Personal 권장 플로우)

1. GitHub 리포지토리의 **Actions** 탭에서 **Unity Activation** 워크플로우를 실행합니다.
2. 실행 결과 아티팩트에서 `*.alf` 파일을 다운로드합니다.
3. Unity의 수동 활성화 페이지에서 `*.alf`를 업로드해 `*.ulf` 라이선스 파일을 발급받습니다.
4. GitHub 리포지토리 **Settings → Secrets and variables → Actions** 에서
   - Secret 이름: `UNITY_LICENSE`
   - 값: `*.ulf` 파일 내용 전체

## 3) CI 실행

- `UNITY_LICENSE`가 설정되면 `Unity CI`가 PR/푸시에서 테스트를 수행합니다.
- 수동 빌드는 **Actions → Unity CI → Run workflow** 로 실행합니다.

## 4) (선택) Professional/Team 라이선스

프로 라이선스를 쓰면 아래 시크릿을 추가로 사용할 수 있습니다(워크플로우에 이미 연결됨).

- `UNITY_EMAIL`
- `UNITY_PASSWORD`
- `UNITY_SERIAL`

