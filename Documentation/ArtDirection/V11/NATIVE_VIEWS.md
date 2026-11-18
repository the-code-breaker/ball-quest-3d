# Native world review

Generated chapter illustrations are stored separately from actual Unity views. The latest native review renders shipping road meshes, ball models, materials, lights and HUD in supported inspection poses. Overviews use a debug camera to show the actual route layout; they are not normal play-camera views.

The automated inspection covers courses 1–20, 999 and 1000, with every environment theme represented. `TestResults/grand-native-views-v11.json` records each view's file, renderer count and Editor construction/memory measurement. `TestResults/grand-traversal-v11.csv` separately records ordinary-force physical traversal results.

Examples after the final native review:

- [Lagoon](../../../TestResults/Previews/V11World01Precision.png)
- [Alpine](../../../TestResults/Previews/V11World03Precision.png)
- [Bamboo](../../../TestResults/Previews/V11World08Precision.png)
- [Mushrooms](../../../TestResults/Previews/V11World10Precision.png)
- [Clockwork](../../../TestResults/Previews/V11World13Precision.png)
- [Cloud palace](../../../TestResults/Previews/V11World16Precision.png)
- [Toy workshop](../../../TestResults/Previews/V11World19Precision.png)
- [Level 1000](../../../TestResults/Previews/V11World1000Precision.png)

The native meshes are intentionally simpler than the AI concept illustrations. Android device frame pacing, temperature and human control/audio preference still require a phone test.
