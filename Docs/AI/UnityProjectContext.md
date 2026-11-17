# Unity project context

Reviewed 2026-11-16. Unity 6000.0.34f1, URP 17.0.3, Input System 1.11.2 (both input backends enabled), AI Navigation 2.0.5 and Unity Test Framework 1.4.5.

First-party code lives in Assets/Scripts, with MonoBehaviour components and no custom assembly definitions. MenuManager now loads MonsterSandbox from the existing Play button, retaining its fade. Enabled build scenes are MenuScene, MainScene and MonsterSandbox. The sandbox debug UI was removed at the user's request; the menu's existing UI is retained. Preserve other user changes in project settings.

Assets/Mounstros contains imported Lethal Company models; Assets/Nicrom contains vendor vegetation and shaders. The existing MounstrosScene has only camera and lighting. MonsterSandbox is a separate test scene, built by Assets/Editor/MonsterSandboxBuilder.cs, with its own player, three AI components and baked NavMesh. Eyeless Dog and Nutcracker are display models only.

No Unity MCP bridge is configured. The codebase-memory index does not cover this project (verified with list_projects and check_index_coverage); source inspection is the fallback. Installed Unity can compile and validate in batch mode. No existing gameplay AI/player or first-party tests were found in Assets/Scripts.

Evidence: Packages/manifest.json, ProjectSettings/ProjectVersion.txt, ProjectSettings/GraphicsSettings.asset, ProjectSettings/ProjectSettings.asset, ProjectSettings/EditorBuildSettings.asset, Assets/Scripts/MenuManager.cs, Assets/Scenes/MounstrosScene.unity and imported asset inventory.
