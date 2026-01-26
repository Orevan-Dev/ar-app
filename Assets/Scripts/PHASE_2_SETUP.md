# Phase 2 Setup Guide - Using Existing WinUIManager

## Overview
Phase 2 now integrates with your **existing WinUIManager** instead of creating duplicate UI. This means you can use your current Win Overlay for both local wins (Phase 1) and global wins/losses (Phase 2).

---

## What Changed

### ✅ **WinUIManager.cs** (Updated)
Your existing `WinUIManager` now supports:
- **Phase 1**: Local wins (when YOU collect all 6 items)
- **Phase 2**: Global wins/losses (when ANY team wins via Firestore)

### ✅ **GameEndManager.cs** (Updated)
Now uses `WinUIManager` instead of creating a new `GameEndPanel`.

### ❌ **GameEndPanel.cs** (Optional - Can Delete)
This was created as a reference but is **NOT needed** since you already have `WinUIManager`.

---

## Unity Setup (Quick & Easy)

### 1. Update Your Existing Win Overlay

You already have a Win Overlay in your scene. Just add one optional field:

1. **Find Your Win Overlay**:
   - Open your scene
   - Find the GameObject with `WinUIManager` script (probably called "WinOverlay" or similar)

2. **Add Result Text (Optional but Recommended)**:
   - If you want dynamic "YOU WIN" / "YOU LOST" text:
     - Add a TextMeshProUGUI component to your overlay
     - In `WinUIManager` Inspector, assign it to the **Result Text** field
   
   - If you skip this, the panel will still show/hide correctly, just without dynamic text

3. **Configure Text (Optional)**:
   - In `WinUIManager` Inspector, you'll see:
     - **Win Text**: Default "YOU WIN" (customize if you want)
     - **Lost Text**: Default "YOU LOST" (customize if you want)

### 2. That's It!

No need to create new UI panels. Your existing Win Overlay now works for both:
- ✅ Local wins (you collect all items)
- ✅ Global wins (you win via Firestore)
- ✅ Global losses (another team wins, if `enableGlobalLose = true`)

---

## How It Works Now

### Scenario 1: You Collect All 6 Items Locally (Phase 1)
```
GameSession.OnAllItemsCollected fires
  → WinUIManager.HandleLocalWin()
    → Shows panel with "YOU WIN" text
    → Fades in smoothly
```

### Scenario 2: You Win via Firestore (Phase 2)
```
TeamManager detects isWinner = true for YOUR team
  → GameEndManager.OnGameFinished fires
    → WinUIManager.HandleGlobalGameEnd()
      → Checks: Did we win? YES
        → Shows panel with "YOU WIN" text
        → Fades in smoothly
```

### Scenario 3: Another Team Wins (Phase 2)
```
TeamManager detects isWinner = true for ANOTHER team
  → GameEndManager.OnGameFinished fires
    → WinUIManager.HandleGlobalGameEnd()
      → Checks: Did we win? NO
      → Checks: enableGlobalLose? YES
        → Shows panel with "YOU LOST" text
        → Fades in smoothly
```

---

## Inspector Fields Reference

### WinUIManager Inspector

**UI Panels** (Existing):
- `Win Panel Group` - Your CanvasGroup for fade animation
- `Exit Button` - Your existing exit button

**UI Text (Phase 2)** (New - Optional):
- `Result Text` - TextMeshProUGUI for dynamic "YOU WIN" / "YOU LOST"
  - Leave empty if you have static text/images

**Settings** (Existing):
- `Fade Duration` - How long the fade-in takes (default: 1 second)

**Text Content (Phase 2)** (New):
- `Win Text` - Text shown when you win (default: "YOU WIN")
- `Lost Text` - Text shown when you lose (default: "YOU LOST")

---

## Testing Checklist

### Test 1: Local Win (Phase 1 - Still Works)
1. Play game solo
2. Collect all 6 items
3. ✅ Win overlay fades in
4. ✅ Shows "YOU WIN" (if Result Text assigned)
5. Click EXIT
6. ✅ Returns to scan state

### Test 2: Global Win (Phase 2)
1. Create team on Device 1
2. Collect all 6 items
3. ✅ Firestore marks team as winner
4. ✅ Win overlay fades in on Device 1
5. ✅ Shows "YOU WIN"

### Test 3: Global Loss (Phase 2)
1. Create Team A on Device 1
2. Create Team B on Device 2
3. Team A collects all 6 items
4. ✅ Device 2 shows "YOU LOST" (if `enableGlobalLose = true`)
5. ✅ Device 2 can click EXIT to return to scan

### Test 4: Feature Toggle
1. Set `GameEndManager.enableGlobalLose = false`
2. Repeat Test 3
3. ✅ Device 2 (loser) sees nothing
4. ✅ Only winner sees UI

---

## Migration Notes

### If You Had Custom Win Overlay Design:
✅ **Keep it!** All your existing design, animations, and layout are preserved.

### If You Want to Add "YOU LOST" Visuals:
You have two options:

**Option A: Dynamic Text (Recommended)**
- Add a TextMeshProUGUI to your overlay
- Assign it to `Result Text` in Inspector
- Text will change automatically: "YOU WIN" or "YOU LOST"

**Option B: Separate Panels**
- Keep your existing "Win" design
- Duplicate it and create a "Lost" design
- Modify `WinUIManager.ShowPanel()` to swap between them

---

## File Cleanup (Optional)

You can **delete** these files if you want (they're not used):
- `Assets/Scripts/UI/GameEndPanel.cs`
- `Assets/Scripts/UI/GameEndPanel.cs.meta`

They were created as a reference implementation but are **not needed** since you already have `WinUIManager`.

---

## Debug Console Messages

### Phase 1 (Local Win):
```
[WinUI] Local win event received! Starting fade in.
```

### Phase 2 (Global Win):
```
[WinUI] Global game end received. Winner: abc123
[WinUI] Local win event received! Starting fade in.
```

### Phase 2 (Global Loss - Shown):
```
[WinUI] Global game end received. Winner: abc123
[WinUI] Local win event received! Starting fade in.
```

### Phase 2 (Global Loss - Hidden):
```
[WinUI] Global game end received. Winner: abc123
[WinUI] Current team lost, but enableGlobalLose is false. No UI shown.
```

---

## Summary

✅ **No new UI needed** - Your existing Win Overlay works for everything  
✅ **Backward compatible** - Phase 1 local wins still work exactly the same  
✅ **Phase 2 ready** - Global wins/losses now supported  
✅ **One line of setup** - Just assign the optional Result Text field  
✅ **Feature toggle** - Control whether losers see UI via `enableGlobalLose`  

Your existing UI investment is preserved and enhanced! 🎉
