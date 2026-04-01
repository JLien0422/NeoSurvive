# Lobby UI Sound Map

## 1) LobbySoundManager setup

- Scene object: `LobbySoundManager` (one instance)
- Required component: `AudioSource` (auto-required)
- Fill `Lobby UI SFX Catalog` slots in inspector
- Add one slot per event type used below

## 2) Recommended core events (must-have)

- `UIHover`: all major button hover
- `UIClick`: all major button click
- `UIBack`: ESC/back navigation (`LobbyManager.BackTab`)
- `TabSwitch`: lobby tab switch (`LobbyManager.OpenTab`)
- `PanelOpen`: popup/panel open
- `PanelClose`: popup/panel close
- `UIConfirm`: confirm/apply actions
- `UICancel`: cancel/close actions
- `Error`: invalid action/fail feedback

## 3) Project-specific events from current lobby code

### Character selection

- `CharacterHover`: character button hover (`UICharacterSelection`)
- `CharacterSelect`: character selected (`UICharacterSelection`, `CharacterSelector`)
- `CharacterDeselect`: selection canceled (ESC/re-click in `UICharacterSelection`)

### Lobby flow

- `LobbyCreate`: create room action
- `LobbyJoin`: join room action
- `LobbyLeave`: leave room action
- `LobbyReadyOn`: ready toggle on
- `LobbyReadyOff`: ready toggle off
- `LobbyStart`: start game action

### Settings (`SettingsUI`)

- `SettingsOpen`: open settings (`OpenSettings`/`ToggleSettings`)
- `SettingsClose`: close settings (`CloseSettings`/`ToggleSettings`)
- `SettingsTabSwitch`: video/audio/gameplay/account tab buttons
- `SettingsApply`: slider/toggle/dropdown value applied

### Leaderboard (`LeaderboardUI`)

- `LeaderboardOpen`: open leaderboard
- `LeaderboardRefresh`: refresh button

### Nickname change (`NicknameChangeUI`)

- `NicknameOpen`: open nickname panel
- `NicknameConfirm`: confirm submit
- `NicknameCancel`: cancel/close
- `NicknameSuccess`: server success
- `NicknameFail`: server fail/validation fail

### Upgrade (`MainMenu_UI`)

- `UpgradeBuy`: buy health/damage/move upgrade

## 4) Designer workflow (easy wiring)

1. Add `LobbyUISoundEmitter` to each interactive button
2. Default hover/click works without extra setup
3. For specialized SFX, enable custom event and choose event type
4. For non-button actions (tab switch, open/close, success/fail), call `LobbySoundManager` wrapper methods from existing OnClick or script methods

## 5) Suggested audio style guide

- Hover: short, soft, high-frequency tick
- Click: short, solid mid click
- TabSwitch: light swipe or click variant
- Open/Close: paired whoosh in/out
- Confirm: positive short chime
- Cancel/Back: soft down tick
- Error/Fail: dry muted buzz
- Success: brighter short chime

## 6) Notes

- Keep clips short (0.05s-0.35s) for UI responsiveness
- Use 2-4 clip variations for `UIHover` and `UIClick` to reduce repetition
- Use `randomPitch` in slot for small variation when needed
