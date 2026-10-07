# Provenance and external dependencies

- QH host/state sources and tests: recovered corrected BETA 0.2.5 package.
- QH artwork: banner and icon previously approved by Frank ( Dgt Kipad QC ) in this project.
- EmpyrionModHost: https://github.com/GitHub-TC/EmpyrionModHost ; installed
  separately. Its original license applies to that project, not automatically
  to QH. No loader binaries or third-party library copies are in this repository.
- Eleon.Modding Mif.dll, ModApi.dll and UnityEngine.CoreModule.dll are build
  references supplied by the installed game/loader; they are not redistributed.
- Microsoft .NET Framework 4.7.2 targeting pack is a build prerequisite.
- API metadata research also inspected dependencies/ModApi.dll from
  https://github.com/GitHub-TC/EmpyrionScripting (downloaded 2026-10-07).

The archive containing this repository also includes an unchanged QH-only
0.2.5 runtime reference ZIP. It contains the two corrected QH DLLs and the state
Info.yaml only, with no game DLLs, loader lists, save data or active configuration.
