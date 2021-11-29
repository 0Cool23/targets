## Visual Studio solution targets {#PAGE_SolutionTargets}

Some build targets require additional tools to be installed. The default
path for this is the directory `C:\tools`. The tools required are noted
in the target section.

Note: In order to download/update a NuGet package and place it into the
`C:\tools` directory simply add the package to a project within the soultion.
Then copy the package from the `C:\Users\<user>\.nuget\packages` directory
and place it into the `C:\tools` directory.


### BuildInfo {#SEC_SolutionTargets_BuildInfo}

### Coverage {#SEC_SolutionTargets_Coverage}

For code coverage target to work in the solution a *MSTest* project named
*RegressionTests* must be added to the solution. The `RegressionTests.csproj`
file must then be edited by adding the coverage target file to the project
configuratrion file.

```
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

#### Preparing solution for licenses

Copy the file `dox\doxygen\licenses.dox` to the directory `dox` and include it
as existing solution item to the `dox`directory. Then edit the file and remove
the <i>\@cond</i> and <i>\@endcond</i> doxygen tags.

#### Adding a license for a project

Choose a sutible license file from the `dox/doxygen/licenses` directory and
copy the file into the root directory of your project (e.g. `license-lgpl2.1.md`).

Next adjust the license original file header

```
### Library (LGPL v2.1) {#LIBRARY_PROJECT_LGPL_2_1}

Library - short description.

    Copyright (C) 2021
....
```

with information to match your project by adjusting title, doxygen reference and
description.


```
### libId3Buffer Library (LGPL v2.1) {#LIBRARY_LIBID3BUFFER_LGPL_2_1}

liId3Buffer - Library to analyse and modify music files header information.

    Copyright (C) 2021
....
```

Now add the license reference `LIBRARY_LIBID3BUFFER_LGPL_2_1`  to a section
in the `dox\license.dox` file.

```
....
@section SEC_LIBRARIES Libraries
<!-- @subpage LIBRARY_PROJECT_LGPL_2_1 -->
@subpage LIBRARY_LIBID3BUFFER_LGPL_2_1
....
```

Finally the lincense build target must be activated by adding the LincenseInfo
target file to the project configuratrion file.

```
<Project Sdk="Microsoft.NET.Sdk">

    <Import Project="$(SolutionDir)/targets/LicenseInfo.targets" />
    ...

</Project>
```

### SolutionInfo {#SEC_SolutionTargets_SolutionInfo}
