# CitySampleAirSimTest - Technical Documentation

## 1. Project Overview
CitySampleAirSimTest is a UE5.0 C++ project derived from Epic's City Sample, customized for drone simulation workflows and AirSim integration.

The project combines:
- City Sample open-world systems (World Partition, Mass traffic/crowd, vehicle and character traversal)
- Custom AirSim plugin integration for drone telemetry/control hooks
- Custom content pipeline under `Content-B/MyContent` for menu/UI/maps and simulation assets
- Editor/build automation inherited from City Sample and extended with Perforce-aware validation tooling

Primary project descriptor:
- `CitySampleAirSimTest.uproject`

### 1.1 Technical Specifications

**Telemetry Update Rate**
- Frame-rate dependent via `ADroneApi::Tick()` (`bCanEverTick = true`)
- Typical range: 30-120 Hz depending on simulation performance
- No fixed update rate; telemetry queries execute per-frame when AirSim RPC connection is active

**Drone Support**
- Single-drone API wrapper architecture (`airlib::MultirotorRpcLibClient`)
- Designed for one drone instance per ADroneApi actor
- Extendable to multi-drone via Blueprint arrays or spawning multiple ADroneApi actors
- No built-in fleet management; scalability depends on AirSim server capabilities

**Map Size / Environment Scale**
- Full UE5 City Sample world (~16 km² urban environment)
- World Partition streaming with dynamic bounds calculation via `WorldMapBoundingBox`
- High-density urban scene: skyscrapers, road networks, Mass-based traffic/pedestrian simulation
- Optimized for large-scale open-world traversal (vehicle, character, drone modes)

**Frame Rate Performance**
- Target: 60 FPS on high-end hardware (RTX 3080+, 32GB RAM recommended)
- UE5 Lumen global illumination and Nanite virtualized geometry enabled
- Performance scales with active Mass entities (traffic density, crowd simulation)
- Device profiles: `DroneMode` and `DrivingMode` presets for runtime optimization

**Modified Codebase Size**
- **New files**: 2 C++ files (`DroneApi.h`, `DroneApi.cpp`) — 123 lines of code
- **Modified files**: `CitySample.Build.cs` — 3 lines (exception handling flags)
- **Custom content**: 42 assets in `Content-B/MyContent` (maps, UI, blueprints, drone models)
- **Base codebase**: 147 C++ files in `Source/CitySample` module (inherited from City Sample)
- **Total custom code footprint**: ~125 LOC across 3 modified files + Blueprint/asset pipeline

## 2. High-Level Architecture
### 2.1 Runtime modules
- `CitySample` (Runtime): Core gameplay systems (character, vehicle, drone traversal, UI, world utilities, interactions).
- `CitySampleEditor` (Editor): Validation commandlets, editor utility libraries, source-control-aware editor engine behavior.
- `CitySampleAnimGraphRuntime` (UncookedOnly): Animation graph runtime nodes used by City Sample animation systems.

Target definitions:
- `Source/CitySample.Target.cs`
- `Source/CitySampleEditor.Target.cs`

### 2.2 Core dependency profile
`Source/CitySample/CitySample.Build.cs` shows extensive integration with:
- AirSim (`"AirSim"`)
- Chaos vehicles, Mass systems, ZoneGraph, Enhanced Input
- Media/audio/rendering systems for City Sample feature parity

Notable custom build edit in runtime module:
- `//Changes by me`
- `_HAS_EXCEPTIONS=0` plus `bEnableExceptions = true`

This indicates custom handling around C++ exception behavior, likely to satisfy third-party integration requirements.

## 3. AirSim Integration
## 3.1 Plugin-level enablement
AirSim is explicitly enabled in:
- `CitySampleAirSimTest.uproject`

AirSim is also added as a module dependency in:
- `Source/CitySample/CitySample.Build.cs`
- `Source/CitySampleEditor/CitySampleEditor.Build.cs`

## 3.2 C++ AirSim wrapper actor
Custom API wrapper:
- `Source/CitySample/AirSimApi/DroneApi.h`
- `Source/CitySample/AirSimApi/DroneApi.cpp`

`ADroneApi` encapsulates an `airlib::MultirotorRpcLibClient` and exposes Blueprint-callable functions:
- `ConnectToDroneNetwork()`
- `ArmDrone()`
- `GetAltitudeDrone()`
- `GetVelocityDrone()`

This class is the key C++ bridge between UE gameplay logic and AirSim RPC telemetry/control.

## 3.3 Content-level AirSim assets and maps
Custom content indicates active simulation pipeline:
- `Content-B/MyContent/Blueprints/DroneApi_Blueprint.uasset`
- `Content-B/MyContent/Maps/AirSimApiTest.umap`
- `Content-B/MyContent/Maps/SimApiTest.umap`

Packaging config in `Config/DefaultGame.ini` also includes AirSim assets in cook maps:
- `/AirSim/AirSimAssets`
- `/AirSim/Blueprints/BP_UavioPawn1`

## 4. Gameplay Flow and Traversal
## 4.1 Startup flow
From `Config/DefaultEngine.ini`:
- `GameDefaultMap=/Game/MyContent/Maps/MainMenu.MainMenu`
- `EditorStartupMap=/Game/MyContent/Maps/MainMenu.MainMenu`
- `GlobalDefaultGameMode=/Game/MyContent/Blueprints/MenuGameMode.MenuGameMode_C`

This confirms your project starts in custom menu content rather than base City Sample maps.

## 4.2 Core gameplay classes
Game framework classes in `Source/CitySample/Game` include:
- `ACitySampleGameMode`
- `UCitySampleGameInstanceBase`
- `ACitySampleGameState`
- `ACitySamplePlayerController`
- interaction and save-game support classes

Responsibilities:
- `ACitySampleGameMode`: startup policy (sandbox intro control, performance mode data layer unloading)
- `UCitySampleGameInstanceBase`: save/load, user input preferences, nanite visualization controls
- `ACitySamplePlayerController`: input context orchestration, interaction system, pawn transitions, camera transition safety during world streaming
- `ACitySampleGameState`: intro/test sequence state and broadcast events

## 4.3 Traversal modes
Traversal state enum:
- `Source/CitySample/Util/CitySampleTypes.h`
- `EPlayerTraversalState = OnFoot, InVehicle, Drone`

Traversal interface:
- `Source/CitySample/Game/ICitySampleTraversalInterface.h`

Implementations include:
- Character traversal (`OnFoot`)
- Vehicle traversal via `ACitySampleVehicleBase` (`InVehicle`)
- Drone traversal via `ACitySampleHoverDrone` (`Drone`)

## 4.4 Vehicle/drone possession model
Vehicle and drone flow is built around:
- `Source/CitySample/Vehicles/CitySampleVehicleBase.h`
- `Source/CitySample/Vehicles/DrivableVehicleComponent.h/.cpp`
- `Source/CitySample/Character/CitySampleHoverDrone.h/.cpp`

`UDrivableVehicleComponent` handles possession transfer in/out of drivable vehicles.
`ACitySamplePlayerController` contains drone enter/exit spawn resolution, collision checks, lane fallback logic, and camera fade/streaming-safe transitions.

## 5. Performance and Device Profile Strategy
Your project retains City Sample mode-specific profile structure:
- `Platforms/Windows/Config/WindowsDeviceProfiles.ini`

Defined profiles:
- `Windows_DroneMode`
- `Windows_DrivingMode`
- `WindowsEditor_DroneMode`
- `WindowsEditor_DrivingMode`

Blueprint utility support exists in:
- `Source/CitySample/Util/CitySampleBlueprintLibrary.cpp`

`OverrideDeviceProfileForMode(EDeviceProfileOverrideMode)` dynamically switches profile suffixes (`DroneMode`/`DrivingMode`) at runtime.

## 6. World and Environment Utilities
`ACitySampleWorldInfo` (`Source/CitySample/Game/CitySampleWorldInfo.h/.cpp`) provides:
- world map capture setup (`SceneCapture2D` with computed orthographic projection)
- sunlight angle initialization/override/reset
- post-processing blend control hooks

This acts as a central world utility actor for simulation and UI map visualization.

## 7. Custom Content Footprint (MyContent)
Your custom content folder structure:
- `Content-B/MyContent/Blueprints`
- `Content-B/MyContent/Maps`
- `Content-B/MyContent/Ui`
- `Content-B/MyContent/DroneModels`, `Models`, `Meshes`, `Materials`, `Textures`

Observed user-authored map set:
- `MainMenu.umap`
- `LevelSelection.umap`
- `AirSimApiTest.umap`
- `SimApiTest.umap`

Observed user-authored UI/widget assets include:
- main menu, loading screen, pause menu, mission/game over, simulation HUD widgets

This indicates a complete custom front-end UX layer for simulation scenario selection and runtime HUD.

## 8. Editor and Automation Tooling
## 8.1 Custom editor engine behavior
`Source/CitySampleEditor/CitySampleUnrealEdEngine.cpp` customizes source-control branch registration for Perforce providers via:
- provider-change callback hook
- `RegisterStateBranches(...)`

Configured in `Config/DefaultEngine.ini`:
- `UnrealEdEngine=/Script/CitySampleEditor.CitySampleUnrealEdEngine`
- `EditorEngine=/Script/CitySampleEditor.CitySampleUnrealEdEngine`

## 8.2 Automation scripts
`Build/Scripts/CitySample.Automation.cs` includes commandlet automation for content validation with CL-range/opened/shelved file filtering and Perforce workflows.

This is suited for CI and asset reference/content validation gates in large-scale projects.

## 9. Project-Level Configuration Notes
Notable project-level config decisions:
- Rendering stack tuned for high-end UE5 features (Lumen/Nanite/RayTracing) in `Config/DefaultEngine.ini`
- Enhanced Input defaults configured in `Config/DefaultInput.ini`
- Packaging/cook settings in `Config/DefaultGame.ini` include both CitySample maps and AirSim resources
- Project title renamed to `CitySampleAirSim` (display title)

## 10. What Is Clearly Customized vs. Inherited
### Clearly customized by this project
- AirSim plugin enablement and C++ integration (`AirSimApi/DroneApi`)
- Startup map/game mode pointed to custom menu system in `MyContent`
- MyContent map/UI/asset pipeline for simulation UX
- Build.cs modification comment and exception-related compile flags
- AirSim assets included in packaging map list

### Inherited but actively used from City Sample
- Mass traffic/crowd systems and associated plugins
- City Sample traversal, interaction, and UI framework classes
- City Sample editor validation and automation patterns
- World partition and streaming transition logic

## 11. Risks and Technical Debt Observed
- `ADroneApi` currently catches all exceptions with `catch (...)` and emits a generic on-screen message. More specific error handling and logging would improve diagnosability.
- `ADroneApi::ConnectToDroneNetwork()` is not called in `Tick` (commented out) and requires explicit orchestration from Blueprints or game flow.
- `GetAltitudeDrone()` uses hardcoded constants (`1200 - 1077.311`) which may be environment-dependent.
- Core gameplay behavior is heavily Blueprint-driven (`.uasset`), so C++ analysis alone cannot fully validate menu flow/state transitions without in-editor inspection.

## 12. Suggested Next Documentation Enhancements
1. Add Blueprint graph documentation for:
- `MenuGameMode`
- `DroneApi_Blueprint`
- map-specific game flow blueprints in `MyContent/Maps`

2. Add sequence diagrams for:
- Menu -> level select -> AirSim map load
- OnFoot <-> Vehicle <-> Drone traversal possession pipeline

3. Add AirSim runtime ops notes:
- expected AirSim server settings
- connection/arming preconditions
- telemetry update rates and failure handling

4. Add test matrix:
- map boot tests
- possession and traversal transitions
- AirSim RPC connect/arm/telemetry validation
- package/cook smoke test

## 13. Key File Index
Project descriptor and build:
- `CitySampleAirSimTest.uproject`
- `Source/CitySample.Target.cs`
- `Source/CitySampleEditor.Target.cs`
- `Source/CitySample/CitySample.Build.cs`
- `Source/CitySampleEditor/CitySampleEditor.Build.cs`

AirSim integration:
- `Source/CitySample/AirSimApi/DroneApi.h`
- `Source/CitySample/AirSimApi/DroneApi.cpp`

Gameplay framework:
- `Source/CitySample/Game/CitySampleGameMode.h`
- `Source/CitySample/Game/CitySampleGameMode.cpp`
- `Source/CitySample/Game/CitySampleGameInstanceBase.h`
- `Source/CitySample/Game/CitySampleGameInstanceBase.cpp`
- `Source/CitySample/Game/CitySamplePlayerController.h`
- `Source/CitySample/Game/CitySamplePlayerController.cpp`
- `Source/CitySample/Game/CitySampleGameState.h`
- `Source/CitySample/Game/CitySampleGameState.cpp`
- `Source/CitySample/Game/CitySampleWorldInfo.h`
- `Source/CitySample/Game/CitySampleWorldInfo.cpp`

Traversal and vehicle/drone systems:
- `Source/CitySample/Util/CitySampleTypes.h`
- `Source/CitySample/Game/ICitySampleTraversalInterface.h`
- `Source/CitySample/Character/CitySampleHoverDrone.h`
- `Source/CitySample/Character/CitySampleHoverDrone.cpp`
- `Source/CitySample/Vehicles/CitySampleVehicleBase.h`
- `Source/CitySample/Vehicles/DrivableVehicleComponent.h`
- `Source/CitySample/Vehicles/DrivableVehicleComponent.cpp`

Config and packaging:
- `Config/DefaultEngine.ini`
- `Config/DefaultGame.ini`
- `Config/DefaultInput.ini`
- `Platforms/Windows/Config/WindowsDeviceProfiles.ini`

Editor and automation:
- `Source/CitySampleEditor/CitySampleUnrealEdEngine.cpp`
- `Build/Scripts/CitySample.Automation.cs`

Custom content roots:
- `Content-B/MyContent/Blueprints`
- `Content-B/MyContent/Maps`
- `Content-B/MyContent/Ui`
