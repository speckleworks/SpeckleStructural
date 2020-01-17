# SpeckleStructural

[![Build status](https://ci.appveyor.com/api/projects/status/fl47uk96qhrmo0u0?svg=true)](https://ci.appveyor.com/project/SpeckleWorks/specklestructural)

Structural based object model built on top of SpeckleCoreGeometry.

## Building SpeckleStructural

### Requirements

- Visual Studio 2019
- .NET Framework 4.7.1

### Release process

This process is just to prepare this prerequisite artifact for inclusion in both the SpeckleStructuralSuite-installer, and general Speckle, release processes.  

When the release process for SpeckleStructuralSuite-installer is invoked, it will include the latest SpeckleStructural artifact resulting from the process below.  

- Merge code to dev branch and and push to origin
- Create a pull request to merge to master
- Once this is approved, this will cause a new build at https://ci.appveyor.com/project/SpeckleWorks/specklestructural - check that the artefacts on the Artifacts tab have been built
- "Draft a new release" at https://github.com/speckleworks/SpeckleStructural/releases; add new version number as the tag

To include this in the general Speckle installer, the relevant version number needs to be updated in the appveyor.yml file in the SpeckleInstaller repo.
