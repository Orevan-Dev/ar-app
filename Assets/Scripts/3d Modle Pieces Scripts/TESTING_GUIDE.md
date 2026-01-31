# Testing Guide: AR Item Discovery System

## ✅ Pre-Testing Checklist

Before testing, verify these setup steps:

### 1. **Prefab Setup**
- [ ] Create at least one Robot Piece prefab
- [ ] Place prefab(s) in: `Assets/Resources/Prefabs/RobotPieces/`
- [ ] Each prefab MUST have:
  - [ ] A **Renderer** component (MeshRenderer, SpriteRenderer, etc.)
  - [ ] A **Collider** component (BoxCollider, SphereCollider, etc.)
  - [ ] The **ARItem** script component attached

### 2. **ItemSpawner Setup**
- [ ] `PieceSpawnerManger` GameObject has `ItemSpawner` script attached
- [ ] `Prefab Folder Path` is set to: `"Prefabs/RobotPieces"`
- [ ] `Initial Spawn Count` is set (default: 5)
- [ ] `Min Spawn Distance` = 5
- [ ] `Max Spawn Distance` = 8
- [ ] `Height Offset` = 0.5

### 3. **Camera Setup**
- [ ] Main Camera is tagged as "MainCamera"
- [ ] Camera position is reasonable (not too far from spawn area)

---

## 🧪 Step 1: Test Item Spawning & Visibility

### What to Check:

1. **Console Logs** (Window → General → Console)
   - Look for: `"ItemSpawner: Loaded X prefab(s) from Resources/Prefabs/RobotPieces"`
   - Look for: `"ItemSpawner: Spawned X items around player at position (x, y, z)"`
   - ❌ If you see errors about prefabs not found → Check Resources folder path

2. **Scene View** (Window → General → Scene)
   - Enter Play Mode
   - Look for spawned items in the Hierarchy (they should appear as instantiated prefabs)
   - In Scene View, items should be visible as wireframes/spheres even if hidden
   - Items should be in a circle around the camera position

3. **Game View** (Main Camera view)
   - Items should be **HIDDEN** initially (you shouldn't see them)
   - This is correct! Items only appear when you get close

### Debugging Tips:

**If items don't spawn:**
- Check Console for error messages
- Verify prefabs exist in `Assets/Resources/Prefabs/RobotPieces/`
- Make sure prefabs have ARItem component
- Check that Main Camera is tagged correctly

**If items spawn but are always visible:**
- Check ARItem component on prefab
- Verify Renderer is being disabled in `SetVisibility(false)`
- Check Console for warnings

---

## 🧪 Step 2: Test Discovery (Visibility When Close)

### How to Test:

1. **In Scene View:**
   - Select the Main Camera
   - Move it closer to one of the spawned items
   - Watch the item in the Hierarchy - it should become visible when camera is within 2.0m

2. **In Game View:**
   - Move the camera/player closer to where items should be
   - Items should appear when you're within 2.0 meters
   - Items should disappear when you move away

### What to Verify:

- ✅ Items are **hidden** when distance > 2.0m
- ✅ Items **appear** when distance ≤ 2.0m
- ✅ Items **disappear** again when you move away
- ✅ Multiple items can be visible at once if you're close to them

### Debugging Tips:

**If items never appear:**
- Check ARItem `Show Distance` value (should be 2.0)
- Verify Main Camera position is updating (not stuck)
- Check Console for any errors
- In Scene View, verify items exist and camera is moving

**To see items in Scene View for debugging:**
- Select an item in Hierarchy
- In Inspector, temporarily enable the Renderer
- Or add a Gizmo/Debug draw in ARItem script

---

## 🧪 Step 3: Test Floating Animation

### What to Check:

1. **When item is visible:**
   - Item should have a subtle up/down floating motion
   - Animation should be smooth (sine wave)
   - Item should not drift away from its spawn position

2. **In Scene View:**
   - Select a visible item
   - Watch its Transform position in Inspector
   - Y position should oscillate smoothly

### Debugging Tips:

**If no floating animation:**
- Check ARItem `Float Amplitude` (should be 0.2)
- Check ARItem `Float Speed` (should be 2.0)
- Verify item is not being repositioned by other scripts

---

## 🧪 Step 4: Test Collection (After Visibility Works)

Once visibility is working, we'll test collection. But for now, focus on Steps 1-3.

---

## 🔍 Quick Debug Helper

Add this to ARItem.cs temporarily to see item state in Scene View:

```csharp
private void OnDrawGizmos()
{
    if (playerCamera == null) return;
    
    float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
    
    // Color based on state
    if (distance <= collectDistance)
        Gizmos.color = Color.green; // Interactable
    else if (distance <= showDistance)
        Gizmos.color = Color.yellow; // Visible
    else
        Gizmos.color = Color.red; // Hidden
    
    Gizmos.DrawWireSphere(transform.position, 0.5f);
    
    // Draw line to camera
    Gizmos.color = Color.white;
    Gizmos.DrawLine(transform.position, playerCamera.transform.position);
}
```

This will show:
- **Red sphere** = Hidden (too far)
- **Yellow sphere** = Visible (within show distance)
- **Green sphere** = Interactable (within collect distance)
- **White line** = Distance to camera

---

## 📝 Testing Checklist

- [ ] Items spawn on Start
- [ ] Items are hidden initially
- [ ] Items appear when camera moves within 2.0m
- [ ] Items disappear when camera moves away
- [ ] Floating animation works smoothly
- [ ] Multiple items can be visible simultaneously
- [ ] Items stay fixed in world space (don't follow camera)

---

## 🚨 Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| "No prefabs found" error | Check Resources folder path: `Assets/Resources/Prefabs/RobotPieces/` |
| Items always visible | Check ARItem component, verify Renderer is disabled initially |
| Items never appear | Check Show Distance (2.0m), verify camera is moving |
| Items follow camera | Verify items are NOT parented to camera (should be `SetParent(null)`) |
| Items spawn in wrong location | Check camera position, verify spawn distance settings |

---

## ✅ Ready to Test?

1. Make sure your prefabs are in `Assets/Resources/Prefabs/RobotPieces/`
2. Enter Play Mode
3. Check Console for spawn messages
4. Move camera around to discover items
5. Report back what you see!






