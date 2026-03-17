# CitySampleAirSimTest

UE5-based drone simulation project built on City Sample with AirSim integration.

## Highlights
- AirSim RPC telemetry bridge in C++ (`DroneApi`)
- Real-time telemetry updates at frame rate (~30-120 Hz)
- Large-scale simulation environment (~16 km^2)
- Runtime profile switching for Drone/Driving modes

## Repo Scope
This repository is a lightweight showcase version.

Included:
- `Source/`
- `Config/`
- `Platforms/`
- `Content-B/MyContent/` (custom assets only)
- `TECHNICAL_DOCUMENTATION.md`

Excluded:
- Derived caches/build outputs (`Binaries/`, `Intermediate*/`, `Saved*/`, `DerivedDataCache*/`)
- Non-essential large content

## Prerequisites
- Unreal Engine 5.0 (or compatible)
- Visual Studio 2022 (C++ workload)
- AirSim plugin available in your UE/plugins setup
- Git LFS (for `.uasset`/`.umap` tracked in this repo)

## Quick Start
1. Clone repo.
2. Install LFS objects: `git lfs pull`.
3. Open `CitySampleAirSimTest.uproject` in UE5.
4. Build project if prompted.

## Technical Details
See `TECHNICAL_DOCUMENTATION.md` for architecture, systems, and performance notes.
