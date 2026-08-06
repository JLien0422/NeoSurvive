---
name: neosurvive-git
description: >
  NeoSurvive(JLien0422/NeoSurvive) 전용 깃·PR 고정 규칙.
  프로젝트 주인/기본 리뷰어 JLien0422, Xabuna451, csy40110, tttghost 전원 요청,
  Unity 잡음 파일 커밋 제외 등. 공통 git-pr-auto와 함께 쓰며 NeoSurvive 작업 시 이 값을 우선한다.
  Use when working in NeoSurvive repo on PR, commit, push, issue, merge, reviewers, labels, milestone.
---

# NeoSurvive 깃 / PR 전용 규칙

이 Skill은 NeoSurvive 작업에만 적용한다.
공통 Skill `git-pr-auto`의 절차를 따르되, 아래 고정값이 있으면 그걸 최우선으로 쓴다.
대답은 한국어. 항상 주인님이라고 부른다. 모르는 건 모른다고 말한다.

## 적용 대상
- 원격이 `JLien0422/NeoSurvive` 이거나
- 워크스페이스가 NeoSurvive 프로젝트일 때

## 저장소
- owner: `JLien0422`
- repo: `NeoSurvive`
- 기본 리모트: `origin` (없으면 실제 remote 확인)
- **베이스 / 디폴트 브랜치: `Master`** (구 `lien`)
- PR base는 항상 `Master`를 기본으로 쓴다. 소문자 `master` / `lien`을 base로 쓰지 않는다.

## 프로젝트 주인 / 기본 리뷰어 (고정 — 항상 전원)
아래는 NeoSurvive 프로젝트 주인이다. PR 생성/갱신 시 전원 `reviewers`에 넣는다. (한 명이라도 빼지 말 것)

- * `JLien0422`
- * `Xabuna451`
- * `csy40110`
- * `tttghost`

실패 시 누구까지 성공/실패했는지 보고할 것.

레포 고정 파일: `.github/CODEOWNERS`
(`* @JLien0422 @Xabuna451 @csy40110 @tttghost`)
Skill 요청과 CODEOWNERS를 함께 사용한다.

## 라벨 / 마일스톤
- 공통 Skill대로 최대한 설정한다.
- NeoSurvive에 이미 있는 라벨만 사용한다.
- 열린 마일스톤이 있으면 현재 작업에 맞게 연결한다.
- PR 생성 직후 `issue_write`(update, issue_number=PR번호)로 labels/milestone 설정을 시도한다.

## 프로젝트
- MCP로 프로젝트 보드 연결이 안 되면 “프로젝트: MCP 미지원”으로 보고한다.

## 잡음 파일 (커밋 제외 기본)
- `Library/`
- `Temp/`
- `Obj/`
- `Logs/`
- `.vs/`
- `*.csproj.user`
- 기타 Unity/IDE 생성물로 보이는 대량 변경

주인님이 명시적으로 포함하라면 포함한다.

## 보고
공통 Skill 체크리스트 + NeoSurvive임을 한 줄로 명시.

예:
`NeoSurvive PR: <url> / Reviewers: JLien0422, Xabuna451, csy40110, tttghost / Labels: ... / Milestone: ... / Project: 미지원`
