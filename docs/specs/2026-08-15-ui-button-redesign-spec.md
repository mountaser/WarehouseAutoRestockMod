# Market App UI Button Redesign & Raycast Fix Spec

## Goal
Redesign the "FILL RACK STOCK" UI button in `MarketShoppingCart` so it fits naturally inside the Market App UI footer layout, maintains clean UI/UX scaling, and guarantees click registration.

## Key Changes

### 1. UI Hierarchy & Docking
- Target Parent: `MarketShoppingCart` panel footer container (or child panel containing existing shopping cart buttons).
- Layout: Attached as a sibling to existing cart action buttons with explicit `RectTransform` size `150px` x `36px` and `SetAsLastSibling()` to prevent overlapping canvas elements from blocking input.

### 2. UI/UX Styling
- Background Color: Emerald Green (`#166534`, `RGBA(0.08, 0.40, 0.20, 1.0)`).
- Hover State: Bright Emerald (`RGBA(0.12, 0.55, 0.28, 1.0)`).
- Text: Crisp white (`#FFFFFF`), `13pt` bold, centered.

### 3. Raycast & Click Handling
- Set `Image.raycastTarget = true` on the button background.
- Set `Text.raycastTarget = false` on child text to ensure text mesh does not block click raycasts.
- Bind `Button.onClick` via `Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<UnityAction>(OnFillButtonClick)`.

---
