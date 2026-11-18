# Track presentation — v0.7

The existing campaign and expedition routes now use a brighter satin maple deck, consistently scaled planks across wide junctions, ceramic edge caps, gold fascia inlays, rounded striped rails, and triangular bridge supports. Floating islands have layered rock strata and sculpted trees; stepped skyline buildings, clouds, waterfalls, and themed distant terrain add depth around the elevated roads. Existing route landmarks remain in place. Screenshot review prompted a final pass adding bevelled tower corners, pale ceramic roof terraces, and restrained ocean colour/ripple variation instead of a uniform blue plane.

## Rendering and loading

- `PremiumTrackPresentation` writes decorative shapes directly into material/spatial mesh batches. It creates no decorative colliders or per-piece update behaviours. For example, the geometry test constructs 200 rail segments in at most seven renderers before the final course batching pass.
- `CourseRenderBatcher` then combines compatible architecture by material, shadow settings, and 32-metre spatial buckets. It disables/removes source renderers while retaining their MeshFilters and every physical collider. Disabled renderers, Rigidbody children, text, and transparent materials are excluded.
- Premium geometry is initially grouped in 48-metre buckets. The final pass groups these complete meshes by their bounds centre; it does not split triangles across cell boundaries. This preserves correct rendering but means an individual visible batch can extend beyond a 32-metre cell.
- Maple, window, and ocean textures are generated into `Resources/TrackArt` during project generation, imported with mipmaps, trilinear filtering, and anisotropic filtering. Courses reuse those assets rather than generating the same pixels on every load. Maple/window runtime fallback generation remains available if assets are missing; the optional ocean map simply falls back to the existing coloured material. Ocean variation uses the same existing opaque material and renderer, with no animated shader or additional layers.
- Generated source and combined meshes remain owned by the course and are destroyed on course teardown. Retaining source meshes supports collider/geometry validation but costs additional mesh memory until the course unloads.

## Collision and route audit

The physical roads and rail clearance are unchanged by this presentation pass. Each authored road still uses its existing full solid MeshCollider, while its visible surface uses the existing clipped road-render mesh. The UV change affects plank scale only.

Each authored rail edge still has one oriented BoxCollider sized **0.43 × 0.58 × (edge length + 0.08) metres**. The previous alternating primitive cylinders were visual-only; their colliders were disabled. Native tube geometry replaces those cylinders at the same **0.42-metre diameter**. There is no conversion from segmented physical cylinders to a new continuous collision wall.

Deck trim is omitted where sampled edge points lie inside a neighbouring coplanar road, keeping open junctions visually open. Support placement checks for roads below it. Scenic island placement checks neighbouring road clearance and either moves the island farther out or skips it. All new decorative geometry has no physical collider, so it cannot introduce an invisible obstruction or alter ball contact friction.

## Validation and limitations

`PremiumTrackV7Tests` checks rail batching/collider absence, outward-facing mesh geometry (including the bevelled towers) with finite bounds, and consistent plank scale across narrow/wide roads. The initial standalone source verification run recorded **123 passing checks** in `TestResults/premium-tracks-source-v7.txt`; the repeat after the final screenshot-driven changes passed **124 checks**. Unity test execution and screenshot/device acceptance belong to the integrated build verification.

This is a presentation upgrade over the existing authored courses, not a replacement of their road topology or collision solver. Existing small contact transitions at adjacent solid road meshes are not changed here. Exposed-edge and scenic-clearance checks are conservative placement heuristics rather than full constructive solid geometry; complicated junctions and descending routes still need screenshot review. Source/combined mesh counts are not draw-call counts because retained source MeshFilters have no active renderer after batching. On-device performance must be measured on the target phone; batching alone does not establish an FPS guarantee.
