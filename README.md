## Visual Studio targets submodule {#PAGE_SolutionTargets}

## Using Visual Studio solution targets

Some build targets require additional tools to be installed. The default
path for this is the directory `C:\tools`. The tools required are noted
in the target section.

> **Note:**
>
> In order to download/update a NuGet package and place it into the
> `C:\tools` directory simply add the package to a project within the soultion.
> Then copy the package from the `C:\Users\<user>\.nuget\packages` directory
> and place it into the `C:\tools` directory.

### BuildInfo {#SEC_SolutionTargets_BuildInfo}

The build info target is activated by editing the project *\*.vbproj* or
*\*.csproj* file. The *BuildInfo.targets* must be added to the project
and the *Version* tag removed from the *PropertyGroup* to avoid a
duplicate version info conflict.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Import Project="$(SolutionDir)/targets/BuildInfo.targets" />

  <PropertyGroup>
  ...
     <!--<Version>1.0.0.0</Version>-->
     <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  ...
  </PropertyGroup>

```

### Coverage {#SEC_SolutionTargets_Coverage}

For code coverage target to work in the solution a *MSTest* project named
*RegressionTests* must be added to the solution. The `RegressionTests.csproj`
file must then be edited by adding the coverage target file to the project
configuration file.

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <Import Project="$(SolutionDir)/targets/Coverage.targets" />
    ...

</Project>
```

Finally the build type *Coverage* must be added to the solution configuration.
The *Coverage* build type is a copy of the *Release* build type. The build
configurations must match the table.

| Project Configuration  | Debug     | Release   | Coverage  |
| :--------------------  | :-------- | :-------- | :-------- |
| Application Project(s) | Debug     | Release   | Release   |
| Library Project(s)     | Debug     | Release   | Release   |
| Regression Tests       | Debug     | Release   | Coverage  |

Required targets:

- `OpenCover.4.7.922\tools\OpenCover.Console.exe`
- `ReportGenerator.4.8.2\tools\net47\ReportGenerator.exe`

### LicenseInfo {#SEC_SolutionTargets_LicenseInfo}

First the license file has to be copied into the project root directory. The
license filename must start with `license-*.md` in order to be found by the
build target. There is only one license file allowed for each project.

A markdown license file has two parts. A short license and a long license
terms section. These are separated by the first two `### ...` markdown
header lines. In the following example the line `### libId3Buffer Library ...`
marks the start of the short license term section and uses the following text
as license text. Up to the next line starting with `### ...` is found. The
rest of the text is then used as long license term section.

```text
### libId3Buffer Library (LGPL v2.1) {#LIBRARY_LIBID3BUFFER_LGPL_2_1}

liId3Buffer - Library to analyze and modify music files header information.

    Copyright (C) 2021
    - Goetz Olbrischewski

    ...

### GNU LESSER GENERAL PUBLIC LICENSE

Version 2.1, February 1999

    Copyright (C) 1991, ...
```

Finally the license build target must be activated by adding the LicenseInfo
target file to the project configuration file.

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <Import Project="$(SolutionDir)/targets/LicenseInfo.targets" />
    ...

</Project>
```

The build target will generate a static class `License`. The class is put into the namespace
`Generated` within the default project namespace. The short license terms are transformed
to the readonly string property `ShortText` and the long license terms are transformed to the
readonly string property `LongText`.

> **Note:**
>
> Some example license markdown files can be copied from
> [Github](https://github.com/0Cool23/doxygen)'s `doxygen`
> repository `licenses` subfolder.

### SolutionInfo {#SEC_SolutionTargets_SolutionInfo}
