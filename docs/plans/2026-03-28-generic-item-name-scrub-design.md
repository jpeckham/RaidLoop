# Generic Item Name Scrub Design

**Goal:** Replace current real-world or branded weapon, armor, and backpack names with lightly flavored generic names, while preserving and correcting the intended power progression.

## Replacement Set

### Weapons

- `Makarov` -> `Light Pistol`
- `PPSH` -> `Drum SMG`
- `AK74` -> `Field Carbine`
- `AK47` -> `Battle Rifle`
- `SVDS` -> `Marksman Rifle`
- `PKP` -> `Support Machine Gun`

### Armor

- `6B2 body armor` -> `Soft Armor Vest`
- `BNTI Kirasa-N` -> `Reinforced Vest`
- `6B13 assault armor` -> `Light Plate Carrier`
- `FORT Defender-2` -> `Medium Plate Carrier`
- `6B43 Zabralo-Sh body armor` -> `Heavy Plate Carrier`
- `NFM THOR` -> `Assault Plate Carrier`

### Backpacks

- `Small Backpack` stays `Small Backpack`
- `Large Backpack` stays `Large Backpack`
- `Tactical Backpack` stays `Tactical Backpack`
- `Tasmanian Tiger Trooper 35` -> `Hiking Backpack`
- `6Sh118` -> `Raid Backpack`

## Progression Rules

- Weapon ordering must remain:
  - `Light Pistol`
  - `Drum SMG`
  - `Field Carbine`
  - `Battle Rifle`
  - `Marksman Rifle`
  - `Support Machine Gun`
- Armor ordering must remain:
  - `Soft Armor Vest`
  - `Reinforced Vest`
  - `Light Plate Carrier`
  - `Medium Plate Carrier`
  - `Heavy Plate Carrier`
  - `Assault Plate Carrier`
- Backpack ordering must remain:
  - `Small Backpack`
  - `Large Backpack`
  - `Tactical Backpack`
  - `Hiking Backpack`
  - `Raid Backpack`

## Stat Alignment

- Rename by tier slot, not by old real-world identity.
- Correct any current mismatches so the ladder is coherent after the rename.
- Known adjustments required from current data:
  - weapon value ordering must place `Battle Rifle` below `Marksman Rifle`
  - armor value ordering must place `Assault Plate Carrier` above `Heavy Plate Carrier`
  - armor weight ordering should no longer make a higher-tier carrier lighter than the tier below it

## Verification

- Update shared item catalog names and all authoritative SQL references.
- Update tests to assert the new names and intended ordering.
- Run focused item, combat, raid action, and handler tests.
- Finish with a repo-wide search to surface any remaining old risky names.
