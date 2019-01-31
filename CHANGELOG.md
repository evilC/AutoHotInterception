# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Concurrency switch for executing subscription callback functions. Was implicitly executing on a new thread from the pool, now there is an option to execute each callback on a single thread (one worker per subscription).
### Changed
- By default the new concurrency switch will be set to false meaning that for every subscription there will be only a single worker thread and callbacks will be run sequentially.
### Deprecated
### Removed
### Fixed
- SubscribeMouseMove endpoint fixed to not return bool. (Fix "Can not implicitly convert type Void to object" error)

## [0.3.7] - 2018-12-11