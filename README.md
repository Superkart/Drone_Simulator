# CitySampleAirSimTest — UE5 Drone Simulator

**A production-scale Unreal Engine 5 drone simulation platform integrating AirSim for real-time Hardware-in-the-Loop (HITL) telemetry and control in a photorealistic open-world urban environment.**

**Demo Videos**: [HITL Flight Demo](https://www.youtube.com/watch?v=NT1wvHfB-Yc&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW) | [First Full Flight Success](https://www.youtube.com/watch?v=53wkBYKwNHM&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW&index=2) | [Custom Drone Model](https://www.youtube.com/watch?v=Mqouelkm-Y0&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW&index=4)

**Technical Documentation**: [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)

![CitySampleAirSimTest](CitySampleAirSimTest.png)

---

## Overview

**CitySampleAirSimTest** is a UE5 C++ project that transforms Epic Games' photorealistic City Sample environment into a fully functional drone simulation platform. By bridging Unreal Engine 5's real-time rendering capabilities with Microsoft's [AirSim](https://github.com/microsoft/AirSim) physics and sensor simulation framework, the project delivers a complete pipeline for drone flight testing, HITL experimentation, and real-time telemetry visualization—all within a ~16 km² high-fidelity urban world.

The project is built for engineers, researchers, and simulation developers who need a production-quality environment to develop, test, and demonstrate autonomous drone systems. The custom C++ AirSim integration layer exposes drone control and telemetry directly to Unreal Engine's Blueprint scripting system, enabling rapid prototyping of flight logic without sacrificing low-level control.

---

## Key Features

### Real-Time HITL Drone Simulation
- **AirSim RPC Integration**: Custom C++ wrapper (`ADroneApi`) connects Unreal Engine to a running AirSim server via the multirotor RPC client library
- **Live Telemetry**: Frame-rate-driven telemetry loop (~30–120 Hz) delivering real-time GPS altitude, velocity, and flight state data
- **Blueprint-Callable API**: All drone control functions (`ConnectToDroneNetwork`, `ArmDrone`, `GetAltitudeDrone`, `GetVelocityDrone`) are exposed to UE5 Blueprints for flexible simulation orchestration
- **Hardware-in-the-Loop Support**: Designed for HITL workflows where a physical or simulated flight controller drives the drone inside the virtual environment

### Photorealistic Open-World Environment
- **~16 km² Urban Map**: Full Epic City Sample world with skyscrapers, road networks, and dynamic urban infrastructure powered by World Partition streaming
- **UE5 Rendering Stack**: Lumen global illumination, Nanite virtualized geometry, and ray tracing enabled for cinematic visual fidelity
- **Dynamic City Life**: Mass AI-driven traffic and pedestrian crowd simulation for a living, breathing city environment
- **Day/Night and Post-Processing**: Configurable sunlight angles and post-processing blend control via `ACitySampleWorldInfo`

### Multi-Mode Traversal System
- **Three Traversal Modes**: Seamlessly switch between on-foot, vehicle, and drone modes within the same simulation session
- **Drone Flight Mode**: Full hover drone controller (`ACitySampleHoverDrone`) with UE5 physics-based flight dynamics
- **Vehicle Driving Mode**: Physics-driven ground vehicles via Chaos Vehicles with full possession and camera transition support
- **On-Foot Exploration**: Character traversal with interaction system for navigating the city on foot
- **Safe Mode Transitions**: Camera fade, collision checks, lane fallback logic, and streaming-safe transitions between all modes

### Custom Simulation UI and UX
- **Complete Front-End Layer**: Custom-built main menu, level selection screen, simulation HUD, pause menu, and game-over/mission widgets
- **Simulation Scenario Selection**: Level selection map (`LevelSelection.umap`) for choosing simulation environments before launch
- **AirSim Test Maps**: Dedicated maps (`AirSimApiTest.umap`, `SimApiTest.umap`) for isolated API testing and validation
- **Loading Screen and HUD**: Polished loading transitions and in-flight heads-up display for telemetry readout

### Performance-Optimized Architecture
- **Runtime Device Profiles**: Dynamic switching between `DroneMode` and `DrivingMode` device profile presets at runtime for mode-specific GPU/CPU budget allocation
- **World Partition Streaming**: Large-scale level streaming with dynamic bounds calculation for memory-efficient open-world traversal
- **GPU Instancing and Nanite**: Hardware-accelerated geometry and instancing for high-density urban scenes at target 60 FPS
- **Profile-Based Tuning**: Separate Windows device profiles for editor and runtime, optimized per traversal mode

---

## Architecture

### System Architecture

```
CitySampleAirSimTest/
├── AirSim Integration Layer
│   ├── ADroneApi (C++ Actor)           ← RPC client wrapper, Blueprint-callable telemetry/control
│   └── DroneApi_Blueprint (UE Asset)   ← Blueprint subclass for game-flow orchestration
│
├── Gameplay Framework
│   ├── ACitySampleGameMode             ← Startup policy, intro sequencing, performance data layers
│   ├── UCitySampleGameInstanceBase     ← Save/load, input preferences, nanite visualization
│   ├── ACitySamplePlayerController     ← Input contexts, pawn transitions, traversal orchestration
│   └── ACitySampleGameState            ← Intro/test sequence state and broadcast events
│
├── Traversal System
│   ├── ACitySampleHoverDrone           ← Drone flight controller (Drone mode)
│   ├── ACitySampleVehicleBase          ← Ground vehicle controller (Vehicle mode)
│   └── UDrivableVehicleComponent       ← Possession transfer and driving lifecycle
│
├── World and Environment
│   └── ACitySampleWorldInfo            ← World map capture, sunlight control, post-processing
│
├── Custom Content (MyContent)
│   ├── Maps/                           ← MainMenu, LevelSelection, AirSimApiTest, SimApiTest
│   ├── Blueprints/                     ← MenuGameMode, DroneApi_Blueprint
│   ├── Ui/                             ← HUD, menus, loading screen, pause/mission widgets
│   └── DroneModels/ Models/ Meshes/    ← Custom drone 3D assets and materials
│
└── Editor and Automation
    ├── CitySampleUnrealEdEngine        ← Perforce source-control branch registration
    └── CitySample.Automation.cs        ← Content validation commandlets for CI pipelines
```

### AirSim Integration Architecture

The core integration is built around a thin, Blueprint-friendly C++ actor:

```
AirSim Server (external process)
        │
        │  RPC over TCP (localhost or network)
        ▼
ADroneApi (UE5 C++ Actor)
├── airlib::MultirotorRpcLibClient      ← AirSim RPC client instance
├── ConnectToDroneNetwork()             ← enableApiControl + confirmConnection
├── ArmDrone()                          ← armDisarm(true), sets isArmed flag
├── GetAltitudeDrone()                  ← GPS-derived altitude computation
├── GetVelocityDrone()                  ← 3D velocity vector magnitude (m/s)
└── Tick() @ ~30-120 Hz                 ← Per-frame telemetry available
        │
        ▼
Blueprint Graph (DroneApi_Blueprint)
        │
        ▼
Simulation HUD / Flight Logic / Mission System
```

### Module Structure

| Module | Type | Purpose |
|--------|------|---------|
| `CitySample` | Runtime | Core gameplay: traversal, vehicles, drone, UI, world utilities, AirSim API |
| `CitySampleEditor` | Editor | Validation commandlets, Perforce editor engine customization |
| `CitySampleAnimGraphRuntime` | UncookedOnly | Animation graph runtime nodes for City Sample characters |

---

## Technical Specifications

| Specification | Value |
|---------------|-------|
| **Engine** | Unreal Engine 5.0 |
| **AirSim Integration** | `airlib::MultirotorRpcLibClient` via RPC |
| **Telemetry Rate** | ~30–120 Hz (frame-rate driven) |
| **Environment Scale** | ~16 km² open-world urban map |
| **Performance Target** | 60 FPS (high-end hardware) |
| **Rendering Features** | Lumen, Nanite, Ray Tracing, UE5 post-processing |
| **Custom C++ Footprint** | 2 new files, ~123 LOC + 3-line build modification |
| **Custom Content** | 42 assets in `Content-B/MyContent` |
| **Traversal Modes** | OnFoot, InVehicle, Drone |
| **Target Platforms** | Windows, Linux, PS5, XSX |
| **Primary Language** | C++ (UE5 UCLASS architecture) |

---

## Project Structure

```
CitySampleAirSimTest/
├── Source/
│   ├── CitySample/
│   │   ├── AirSimApi/
│   │   │   ├── DroneApi.h              # AirSim C++ wrapper actor (header)
│   │   │   └── DroneApi.cpp            # AirSim C++ wrapper actor (implementation)
│   │   ├── Game/
│   │   │   ├── CitySampleGameMode.h/.cpp
│   │   │   ├── CitySampleGameInstanceBase.h/.cpp
│   │   │   ├── CitySamplePlayerController.h/.cpp
│   │   │   ├── CitySampleGameState.h/.cpp
│   │   │   └── CitySampleWorldInfo.h/.cpp
│   │   ├── Character/
│   │   │   └── CitySampleHoverDrone.h/.cpp
│   │   ├── Vehicles/
│   │   │   ├── CitySampleVehicleBase.h
│   │   │   └── DrivableVehicleComponent.h/.cpp
│   │   ├── Util/
│   │   │   ├── CitySampleTypes.h       # EPlayerTraversalState enum
│   │   │   └── CitySampleBlueprintLibrary.cpp
│   │   └── CitySample.Build.cs         # Module build rules (AirSim dependency)
│   ├── CitySampleEditor/
│   │   ├── CitySampleUnrealEdEngine.cpp
│   │   └── CitySampleEditor.Build.cs
│   ├── CitySample.Target.cs
│   └── CitySampleEditor.Target.cs
├── Content-B/
│   └── MyContent/
│       ├── Maps/
│       │   ├── MainMenu.umap           # Project startup map
│       │   ├── LevelSelection.umap     # Scenario selection
│       │   ├── AirSimApiTest.umap      # AirSim API integration test map
│       │   └── SimApiTest.umap         # General simulation test map
│       ├── Blueprints/
│       │   ├── MenuGameMode.uasset
│       │   └── DroneApi_Blueprint.uasset
│       ├── Ui/                         # HUD, menus, loading screen widgets
│       ├── DroneModels/                # Custom drone 3D models
│       ├── Models/ Meshes/ Materials/ Textures/
│       └── ...
├── Config/
│   ├── DefaultEngine.ini               # Startup map, rendering, engine settings
│   ├── DefaultGame.ini                 # Packaging/cook config (includes AirSim assets)
│   └── DefaultInput.ini                # Enhanced Input defaults
├── Platforms/
│   └── Windows/Config/
│       └── WindowsDeviceProfiles.ini   # DroneMode / DrivingMode profiles
├── Build/
│   └── Scripts/
│       └── CitySample.Automation.cs    # CI content validation commandlets
├── CitySampleAirSimTest.uproject       # Unreal project descriptor
├── CitySampleAirSimTest.sln            # Visual Studio solution
├── TECHNICAL_DOCUMENTATION.md         # Full technical reference
└── README.md
```

---

## Getting Started

### Prerequisites

| Requirement | Version / Notes |
|-------------|-----------------|
| **Unreal Engine** | 5.0 (exact version tested; newer UE5.x may work with module/plugin updates) |
| **Visual Studio** | 2022 with "Game development with C++" workload |
| **AirSim Plugin** | Must be built and available in your UE plugins directory |
| **Git LFS** | Required to pull binary `.uasset` / `.umap` files |
| **OS** | Windows 10/11 (64-bit) or Linux |
| **RAM** | 32 GB recommended (16 GB minimum) |
| **GPU** | NVIDIA RTX 3080 or equivalent (Lumen + Nanite workloads) |

### Installation

**1. Clone the Repository**
```bash
git clone https://github.com/Superkart/Drone_Simulator.git
cd Drone_Simulator
```

**2. Pull LFS Assets**
```bash
git lfs pull
```
Verify with `git lfs ls-files` — all entries should resolve without errors.

**3. Install the AirSim Plugin**

Build or obtain the AirSim UE5-compatible plugin and place it in one of:
- `<UnrealEngine>/Plugins/AirSim/` (engine-wide), or
- `<ProjectRoot>/Plugins/AirSim/` (project-local)

Refer to the [AirSim UE5 documentation](https://github.com/microsoft/AirSim/blob/main/docs/unreal_proj.md) for build instructions.

**4. Open the Project**
1. Launch Unreal Engine Hub
2. Click **Add** → navigate to the cloned folder
3. Select **CitySampleAirSimTest.uproject**
4. Choose Unreal Engine 5.0
5. If prompted to rebuild modules, click **Yes**

**5. Verify the Setup**

Use the following checklist after first open:

- [ ] `git lfs ls-files` shows no missing-object errors
- [ ] `CitySampleAirSimTest.uproject` opens without fatal plugin/module errors
- [ ] C++ modules compile successfully (Unreal build step completes)
- [ ] Startup map loads: `MainMenu` opens as configured in `Config/DefaultEngine.ini`
- [ ] Play-in-Editor (PIE) starts without immediate crash
- [ ] AirSim plugin is present, enabled, and Blueprint/C++ references resolve

---

## Running the Simulation

### Starting AirSim

Before launching the drone simulation, an AirSim server must be running:

1. Start your AirSim-compatible flight controller or the AirSim standalone server
2. Ensure the RPC endpoint is reachable (default: `localhost:41451`)
3. Configure `settings.json` in your AirSim documents folder for multirotor mode:

```json
{
  "SettingsVersion": 1.2,
  "SimMode": "Multirotor"
}
```

### Launching the Simulation

1. Open `CitySampleAirSimTest.uproject` in Unreal Editor
2. Press **Play** (or **Launch** for standalone)
3. The project starts at the **Main Menu** (`Content-B/MyContent/Maps/MainMenu.umap`)
4. Select a simulation scenario from the **Level Selection** screen
5. Once in-simulation, use Blueprint logic or the HUD to connect to the drone network and begin telemetry

### Drone API Usage (Blueprint)

The `DroneApi_Blueprint` exposes the following Blueprint-callable functions:

| Function | Description |
|----------|-------------|
| `ConnectToDroneNetwork()` | Enables AirSim API control and confirms the RPC connection |
| `ArmDrone()` | Arms the simulated drone (required before flight commands) |
| `GetAltitudeDrone()` | Returns GPS-derived altitude in meters (float) |
| `GetVelocityDrone()` | Returns 3D velocity magnitude in m/s (float) |

**Typical Blueprint sequence:**
```
BeginPlay → ConnectToDroneNetwork → ArmDrone → [Tick] → GetAltitudeDrone / GetVelocityDrone
```

### Traversal Mode Switching

| Mode | Input / Trigger | Description |
|------|----------------|-------------|
| **On Foot** | Default spawn | Navigate the city on foot with the character controller |
| **Vehicle** | Enter vehicle actor | Physics-based driving via Chaos Vehicles |
| **Drone** | Enter drone actor | AirSim-connected hover drone with HITL telemetry |

Runtime performance profiles automatically switch between `DroneMode` and `DrivingMode` device presets when the traversal state changes, optimizing GPU budgets for each scenario.

---

## Demo Videos

### Hardware-in-the-Loop (HITL) Demonstrations

| Video | Description |
|-------|-------------|
| [Live HITL Demo](https://www.youtube.com/watch?v=NT1wvHfB-Yc&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW) | Full Hardware-in-the-Loop flight demonstration in the City Sample environment |
| [First Full Flight Success](https://www.youtube.com/watch?v=53wkBYKwNHM&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW&index=2) | First successful complete autonomous drone flight in the simulator |
| [Custom Drone Model Demo](https://www.youtube.com/watch?v=Mqouelkm-Y0&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW&index=4) | Showcase of the custom 3D drone model integrated into the simulation |

---

## Code Highlights

### AirSim C++ Wrapper (`ADroneApi`)

```cpp
// Source/CitySample/AirSimApi/DroneApi.h
UCLASS()
class CITYSAMPLE_API ADroneApi : public AActor
{
    GENERATED_BODY()
public:
    airlib::MultirotorRpcLibClient client;
    bool isArmed = false;

    UFUNCTION(BlueprintCallable, Category = "AirSimApi")
    void ConnectToDroneNetwork();   // enableApiControl + confirmConnection

    UFUNCTION(BlueprintCallable, Category = "AirSimApi")
    void ArmDrone();                // armDisarm(true)

    UFUNCTION(BlueprintCallable, Category = "AirSimApi")
    float GetAltitudeDrone();       // GPS-derived altitude (meters)

    UFUNCTION(BlueprintCallable, Category = "AirSimApi")
    float GetVelocityDrone();       // 3D velocity magnitude (m/s)
};
```

### Runtime Device Profile Switching

```cpp
// Source/CitySample/Util/CitySampleBlueprintLibrary.cpp
// Switches GPU/CPU budgets at runtime based on traversal mode
OverrideDeviceProfileForMode(EDeviceProfileOverrideMode::DroneMode);
OverrideDeviceProfileForMode(EDeviceProfileOverrideMode::DrivingMode);
```

### Traversal State Enum

```cpp
// Source/CitySample/Util/CitySampleTypes.h
UENUM(BlueprintType)
enum class EPlayerTraversalState : uint8
{
    OnFoot,
    InVehicle,
    Drone
};
```

---

## System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| **CPU** | Intel Core i7-8700K | Intel Core i9-12900K |
| **GPU** | NVIDIA GTX 1080 Ti | NVIDIA RTX 3080 / RTX 4080 |
| **RAM** | 16 GB | 32 GB |
| **Storage** | 200 GB SSD | 500 GB NVMe SSD |
| **OS** | Windows 10 64-bit | Windows 11 64-bit |
| **Unreal Engine** | 5.0 | 5.0 |
| **Network** | Localhost RPC | Localhost or LAN RPC |

---

## Repository Scope

This repository is intentionally scoped to exclude heavy generated artifacts and engine content, keeping it cloneable for technical review.

**Included:**
- `Source/` — All custom C++ source code and build rules
- `Config/` — All project configuration files
- `Platforms/` — Platform-specific device profiles
- `Content-B/MyContent/` — Custom content slice (maps, blueprints, UI, drone models)
- `Build/Scripts/` — Automation and CI commandlets
- `TECHNICAL_DOCUMENTATION.md` — Full architecture and system reference

**Excluded (via `.gitignore`):**
- `Binaries/`, `Intermediate/`, `Saved/`, `DerivedDataCache/` — Generated build and editor outputs
- Non-essential City Sample base content not required for technical evaluation

**Asset Tracking:** All Unreal binary assets (`.uasset`, `.umap`) are tracked with **Git LFS** for reliable clone behavior without bloating the repository history.

---

## Known Limitations

| Limitation | Notes / Workaround |
|------------|-------------------|
| **Hardcoded altitude offset** | `GetAltitudeDrone()` in `Source/CitySample/AirSimApi/DroneApi.cpp` uses a hardcoded constant (`1200 - 1077.311`) that may be environment-dependent; adjust the value in that file for different map configurations |
| **Broad exception handling** | `ConnectToDroneNetwork()` in `Source/CitySample/AirSimApi/DroneApi.cpp` uses `catch (...)` with a generic on-screen message; see [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md#11-risks-and-technical-debt-observed) for details on improving diagnosability |
| **Manual connection required** | `ConnectToDroneNetwork()` must be explicitly called from Blueprint; it is not invoked automatically in `Tick()` |
| **Single-drone architecture** | `ADroneApi` is designed for one drone instance; multi-drone support requires spawning multiple `ADroneApi` actors or Blueprint array orchestration |
| **Full City Sample content** | The complete ~16 km² City Sample experience requires the Epic City Sample content pack and AirSim plugin, which are external dependencies not included in this repository |
| **UE5.0 version lock** | The project targets Unreal Engine 5.0 specifically; compatibility with later UE5.x versions may require module and plugin updates — see Future Enhancements below |

---

## Future Enhancements

- **Improved Error Handling**: Replace broad exception catches in `ADroneApi` with specific AirSim error types and structured UE logging
- **Multi-Drone Fleet Support**: Extend `ADroneApi` architecture to support arrays of `MultirotorRpcLibClient` instances for swarm simulation
- **Mission System Integration**: Connect the AirSim telemetry pipeline to the existing mission/game-over UI widgets for scenario-based training
- **Autonomous Navigation**: Integrate waypoint-based pathfinding and obstacle avoidance using AirSim's navigation APIs
- **Sensor Simulation**: Expose AirSim camera, lidar, and IMU data to Blueprint for perception algorithm testing
- **Remote AirSim Support**: Add configurable RPC host/port settings to support networked HITL setups beyond localhost
- **UE5.1+ Upgrade**: Migrate to a newer UE5 LTS release for long-term plugin and platform compatibility

---

## Additional Documentation

Detailed architecture, system flow, file index, and technical notes are available in [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md).

---

## License

This project is based on Epic Games' City Sample and is subject to the [Unreal Engine End User License Agreement](https://www.unrealengine.com/en-US/eula/unreal). Custom code and content additions are authored by Karthik Ragi ([@Superkart](https://github.com/Superkart)).

---

## Acknowledgments

- **Epic Games** for the City Sample open-world environment and Unreal Engine 5
- **Microsoft AirSim Team** for the open-source drone simulation and RPC framework
- The broader **Unreal Engine** and **robotics simulation** communities for tooling standards and best practices

---

**Real-Time Drone Simulation | UE5 + AirSim HITL | Large-Scale Urban Environments | C++ Systems Engineering**
