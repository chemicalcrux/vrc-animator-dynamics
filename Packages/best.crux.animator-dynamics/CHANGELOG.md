# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

- This changelog
- A license
- An exponential damping effect

### Fixed

- Initialize lists in the various Data classes to prevent errors upon creation
  - The custom editors assume that the lists already exist. Unity's serializer doesn't initialize the fields instantly, so it would error out.

## [0.3.1] - 2025-10-14

### Changed

- Bump the VRCSDK dependency up to 3.9.X

## [0.3.0] - 2025-10-10

### Added

- A Remap source, which can sum up and remap multiple values
- Icons for all of the sources

### Changed

- **BREAKING**: Rename the package and its namespaces
- **BREAKING**: Switch everything to use upgradable data
- Move the CreateAssetMenu paths into a category
  - They were originally dumped into the top-level menu
- Set reasonable default values for SecordOrderDynamics

### Fixed

- Negative frequency and damping values could be entered for the Second Order Dynamics source
- A dependency wasn't declared on the com.vrchat.base package

## [0.2.0] - 2025-03-20

### Changed

- **BREAKING**: Switch to using the Procedural Controller package
  - Originally, controllers had to be generated manually

## [0.1.0] - 2025-03-18

Initial release