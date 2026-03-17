# CitySampleAirSimTest

CitySampleAirSimTest is a UE5-based drone simulation project built on Epic City Sample and integrated with AirSim for real-time telemetry and control workflows.

This repository is prepared as a professional showcase for simulation, AR/VR, and real-time systems roles.

## Project Snapshot

- Engine: Unreal Engine 5.0
- Core Integration: AirSim RPC + custom C++ wrapper actor
- Environment Scale: ~16 km2 open-world urban simulation
- Telemetry Rate: Frame-rate driven, typically ~30-120 Hz
- Performance Target: 60 FPS on high-end hardware
- Custom C++ Footprint: 2 new files, ~123 LOC (plus build customizations)
- Custom Content Footprint: 42 assets in MyContent

## Why This Project Matters

- Built a real-time telemetry bridge between Unreal Engine and AirSim using C++.
- Demonstrates large-scale simulation design with world streaming and runtime profile switching.
- Shows system-level optimization decisions for drone mode versus driving mode traversal.
- Combines engine programming, simulation architecture, and production-oriented tooling.

## Technical Highlights

### AirSim and Telemetry

- Integrated AirSim as a runtime/editor dependency in project modules.
- Implemented a DroneApi C++ wrapper exposing network connect, arm, altitude, and velocity APIs to Blueprint workflows.
- Designed telemetry behavior around real-time frame updates for responsive simulation feedback.

### Large-Scale Simulation Architecture

- Uses City Sample open-world stack with World Partition, Mass traffic, and crowd systems.
- Supports traversal modes for on-foot, vehicle, and drone workflows.
- Includes mode-specific runtime optimization using DroneMode and DrivingMode device profile patterns.

### Performance and Systems

- Performance target set to 60 FPS under simulation workloads.
- Environment configured for high-fidelity rendering (Lumen, Nanite) and large streaming scenes.
- Custom build configuration adjustments included for integration compatibility.

## Demo Videos

1. Live HITL demo: https://www.youtube.com/watch?v=NT1wvHfB-Yc&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW
2. First full flight success in drone sim (HITL): https://www.youtube.com/watch?v=53wkBYKwNHM&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW&index=2
3. First fly success: https://www.youtube.com/watch?v=53wkBYKwNHM&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW&index=2
4. Custom drone model: https://www.youtube.com/watch?v=Mqouelkm-Y0&list=PL_pgA-B53fz83bnfc2PzuO1TEwuv5lagW&index=4

## Recruiter-Friendly Contribution Summary

- Engineered C++ simulation integration for AirSim RPC in a UE5 production-scale project.
- Delivered real-time telemetry at interactive rates while supporting large-world traversal.
- Applied runtime systems thinking to profile-based performance tuning and streaming behavior.
- Shipped a structured technical artifact set suitable for review: source, config, docs, and demos.

## Repository Scope

This repository is intentionally lightweight and excludes heavy generated artifacts.

Included:

- Source
- Config
- Platforms
- MyContent custom showcase assets
- Technical documentation

Excluded:

- Derived/build caches and editor outputs (Binaries, Intermediate, Saved, DerivedDataCache)
- Non-essential heavy content not required for technical review

## Setup

### Prerequisites

- Unreal Engine 5.0 (or compatible UE5 build)
- Visual Studio 2022 with C++ workload
- AirSim plugin available in your UE/plugin setup
- Git LFS for binary UE asset retrieval

### Quick Start

1. Clone the repository.
2. Pull LFS assets: git lfs pull
3. Open CitySampleAirSimTest.uproject in Unreal Engine 5.
4. Rebuild modules if Unreal prompts for compilation.

## Additional Documentation

Detailed architecture, system flow, and technical notes are also available in TECHNICAL_DOCUMENTATION.md.
