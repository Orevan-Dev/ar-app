# Phase 2 Implementation Guide
## Real-Time Winner Detection & Game End System

---

## Overview
Phase 2 implements a **real-time, event-driven game-ending system** that detects when any team wins and immediately ends the game for all players, showing appropriate WIN/LOST panels.

---

## Architecture

### 1. **TeamManager.cs** (Updated)
**Location**: `Assets/Scripts/Managers/TeamManager.cs`

**Changes Made**:
- Enhanced `ListenForWinner()` to extract both winner ID and name
- Triggers `GameEndManager.TriggerGameEnd()` when a winner is detected
- Maintains backward compatibility with existing Phase 1 logic

**Key Code**:
```csharp
// Inside ListenForWinner callback when winner found:
if (GameEndManager.Instance != null)
{
    GameEndManager.Instance.TriggerGameEnd(winnerTeamId, winnerName);
}
```

---

### 2. **GameEndManager.cs** (New)
**Location**: `Assets/Scripts/Managers/GameEndManager.cs`

**Purpose**: Central controller for game-ending logic

**Key Features**:
- **Feature Toggle**: `enableGlobalLose` (Inspector-editable)
  - `true`: Winner sees WIN, losers see LOST
  - `false`: Winner sees WIN, losers see nothing
- **Global Event**: `OnGameFinished(winnerTeamId)`
  - Other systems can subscribe to this event
- **Gameplay Shutdown**: Immediately stops:
  - Question UI
  - Item interactions
  - Any active popups

**Public Methods**:
```csharp
// Called by TeamManager when winner detected
public void TriggerGameEnd(string winnerTeamId, string winnerTeamName)

// Called by GameEndPanel when EXIT button pressed
public void OnExitButtonPressed()
```

**Auto-Initialization**: Created automatically by `GameModeManager.Awake()`

---

### 3. **GameEndPanel.cs** (New)
**Location**: `Assets/Scripts/UI/GameEndPanel.cs`

**Purpose**: UI controller for the end-game panel

**Features**:
- Smooth fade-in animation (configurable duration)
- Dynamic text based on win/loss
- Full-screen overlay design
- EXIT button with callback to GameEndManager

**Public Methods**:
```csharp
// Show panel with result
public void ShowPanel(bool didWin)

// Hide panel (called on reset)
public void HidePanel()
```

**Inspector Fields**:
- `panelRoot`: Main panel GameObject
- `resultText`: TextMeshProUGUI for "YOU WIN" / "YOU LOST"
- `robotImage`: Image component for robot graphic
- `exitButton`: Button component
- `fadeInDuration`: Animation speed (default: 1s)

---

### 4. **QuestionManager.cs** (Updated)
**Location**: `Assets/Scripts/Managers/QuestionManager.cs`

**Changes Made**:
- Added `CancelCurrentQuestion()` method
- Called by GameEndManager to force-close active questions

**Key Code**:
```csharp
public void CancelCurrentQuestion()
{
    if (IsSessionActive)
    {
        if (QuestionUI.Instance != null) QuestionUI.Instance.Hide();
        EndSession();
    }
}
```

---

### 5. **GameModeManager.cs** (Updated)
**Location**: `Assets/Scripts/GameModeManager.cs`

**Changes Made**:
- Auto-creates `GameEndManager` in `Awake()` if not present
- Ensures singleton pattern for Phase 2 managers

---

## Event Flow

### When a Team Wins (Reaches 6 Items):

```
1. GameSession.MarkItemCollected()
   └─> TeamManager.IncrementCollectedItemCount()
       └─> Firestore Transaction: collectedItemsCount++
           └─> If count == 6: isWinner = true

2. Firestore Listener (TeamManager.ListenForWinner)
   └─> Detects isWinner == true
       └─> Extracts winnerTeamId and winnerName
           └─> GameEndManager.TriggerGameEnd(id, name)

3. GameEndManager.TriggerGameEnd()
   ├─> StopAllGameplay()
   │   ├─> QuestionManager.CancelCurrentQuestion()
   │   └─> ItemCollector.enabled = false
   ├─> Fire OnGameFinished event
   └─> Determine Win/Loss
       ├─> If current team == winner: ShowWinPanel()
       └─> Else if enableGlobalLose: ShowLostPanel()

4. GameEndPanel.ShowPanel(didWin)
   └─> Set text ("YOU WIN" or "YOU LOST")
   └─> Fade in animation
   └─> Wait for EXIT button

5. User clicks EXIT
   └─> GameEndManager.OnExitButtonPressed()
       ├─> GameModeManager.EndGameMode()
       └─> GameEndPanel.HidePanel()
```

---

## Unity Setup Instructions

### 1. Create the UI Panel

1. **Create Panel GameObject**:
   - Right-click in Hierarchy → UI → Panel
   - Rename to `GameEndPanel`
   - Set to full-screen (Anchor: Stretch/Stretch)
   - Set background color (semi-transparent black recommended)

2. **Add Components**:
   - Add `CanvasGroup` component (for fade animation)
   - Add `GameEndPanel` script

3. **Create Child Elements**:
   - **Result Text**:
     - UI → Text - TextMeshPro
     - Name: `ResultText`
     - Font size: 80-100
     - Alignment: Center
     - Color: White
   
   - **Robot Image**:
     - UI → Image
     - Name: `RobotImage`
     - Assign your robot sprite
   
   - **Exit Button**:
     - UI → Button - TextMeshPro
     - Name: `ExitButton`
     - Button text: "EXIT"

4. **Assign References**:
   - Select `GameEndPanel` GameObject
   - In Inspector, assign:
     - Panel Root: `GameEndPanel` (itself)
     - Result Text: `ResultText` component
     - Robot Image: `RobotImage` component
     - Exit Button: `ExitButton` component
     - Canvas Group: Auto-assigned

5. **Disable by Default**:
   - Uncheck `GameEndPanel` GameObject in Inspector
   - This ensures it's hidden at game start

### 2. Configure GameEndManager

1. **Find or Create**:
   - GameEndManager is auto-created by GameModeManager
   - Or manually: Create Empty GameObject → Add `GameEndManager` script

2. **Set Feature Toggle**:
   - Select GameEndManager in Hierarchy
   - In Inspector, check/uncheck `Enable Global Lose`:
     - ✅ Checked: Losers see "YOU LOST"
     - ❌ Unchecked: Losers see nothing

### 3. Verify Auto-Initialization

- Play the game
- Check Console for:
  ```
  Created TeamManager automatically.
  Created GameEndManager automatically.
  [GameEndManager] Ready to listen for game end events.
  ```

---

## Testing Phase 2

### Test Case 1: Winner Sees Win Panel
1. Create a team
2. Collect all 6 items
3. **Expected**: "YOU WIN" panel fades in
4. Click EXIT
5. **Expected**: Return to scan state

### Test Case 2: Loser Sees Lost Panel (enableGlobalLose = true)
1. Create Team A on Device 1
2. Create Team B on Device 2
3. Team A collects all 6 items
4. **Expected on Device 2**: "YOU LOST" panel appears immediately
5. Click EXIT on Device 2
6. **Expected**: Return to scan state

### Test Case 3: Loser Sees Nothing (enableGlobalLose = false)
1. Set `enableGlobalLose = false` in Inspector
2. Repeat Test Case 2
3. **Expected on Device 2**: No panel, game continues (or manually end)

### Test Case 4: Question Cancellation
1. Start collecting an item (question UI visible)
2. Another team wins
3. **Expected**: Question UI immediately closes, end panel shows

### Test Case 5: Database Reset
1. After a game ends, right-click TeamManager → "Reset Game Data"
2. **Expected**: Console shows "Game Reset! No winners found"
3. Create a new team
4. **Expected**: Game works normally

---

## Debug Console Messages

### Normal Flow:
```
🏆 GAME OVER! Winner found: Team Alpha (ID: abc123)
🏁 [GameEndManager] Game Finished! Winner: Team Alpha (ID: abc123)
🛑 [GameEndManager] Stopping all gameplay...
🎉 [GameEndManager] Showing WIN panel.
```

### Loser Flow (enableGlobalLose = true):
```
🏆 GAME OVER! Winner found: Team Alpha (ID: abc123)
🏁 [GameEndManager] Game Finished! Winner: Team Alpha (ID: abc123)
🛑 [GameEndManager] Stopping all gameplay...
😢 [GameEndManager] Showing LOST panel.
```

### Errors to Watch For:
```
[TeamManager] GameEndManager not found! Game end UI will not show.
→ Fix: Ensure GameEndManager exists in scene

[GameEndPanel] Panel root is not assigned!
→ Fix: Assign references in Inspector
```

---

## Customization Options

### Change Win/Lost Text:
In `GameEndPanel` Inspector:
- `Win Text`: Default "YOU WIN"
- `Lost Text`: Default "YOU LOST"

### Adjust Fade Speed:
In `GameEndPanel` Inspector:
- `Fade In Duration`: Default 1.0 seconds

### Disable Loser Panels:
In `GameEndManager` Inspector:
- Uncheck `Enable Global Lose`

### Subscribe to Game End Event:
```csharp
// In any script's Start():
GameEndManager.OnGameFinished += (winnerTeamId) => {
    Debug.Log($"Game ended! Winner: {winnerTeamId}");
    // Your custom logic here
};
```

---

## Phase 2 Checklist

- [x] Real-time Firestore listener for winner detection
- [x] Global game-end event system
- [x] Immediate gameplay shutdown (questions, interactions)
- [x] Win/Lost panel with fade animation
- [x] Feature toggle for loser visibility
- [x] EXIT button returns to scan state
- [x] Clean separation of concerns (no UI in managers)
- [x] Auto-initialization of managers
- [x] Comprehensive debug logging
- [x] Modular and easy to disable

---

## Next Steps (Future Phases - NOT IMPLEMENTED)

Phase 3 might include:
- Leaderboard display
- Team statistics
- Replay functionality
- Social sharing

**These are NOT part of Phase 2 and should NOT be implemented yet.**

---

## Troubleshooting

### Panel Doesn't Show:
1. Check `GameEndPanel` is assigned in scene
2. Verify references in Inspector
3. Check Console for errors

### Game Doesn't End:
1. Verify TeamManager listener is active
2. Check Firestore rules allow reads
3. Ensure `isWinner` field is set correctly

### Both Teams See Win:
1. Check `TeamManager.CurrentTeamId` is set correctly
2. Verify team creation stores the ID properly

### Exit Button Doesn't Work:
1. Check button has `OnClick` event assigned
2. Verify `GameEndPanel.OnExitClicked()` is called
3. Check GameModeManager.EndGameMode() executes

---

## Code Comments Legend

- `// 🎯 PHASE 2:` - Code specific to Phase 2 implementation
- `// 🛑` - Gameplay shutdown logic
- `// 🏁` - Game-end trigger points
- `// 🎉` - Win condition
- `// 😢` - Loss condition

---

**Phase 2 Implementation Complete**
