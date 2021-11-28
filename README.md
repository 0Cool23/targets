### Visual Studio solution targets {#PAGE_SolutionTargets}

Some build targets require additional tools to be installed. The default
path for this is the directory `C:\tools`. The tools required are noted
in the target section.

Note: In order to download/update a NuGet package and place it into the
`C:\tools` directory simply add the package to a project within the soultion.
Then copy the package from the `C:\Users\<user>\.nuget\packages` directory
and place it into the `C:\tools` directory.


#### BuildInfo {#SEC_SolutionTargets_BuildInfo}

#### Coverage {#SEC_SolutionTargets_Coverage}

For code coverage target to work in the solution a *MSTest* project named
*RegressionTests* must be added to the solution. The `RegressionTests.csproj`
file must then be edited by adding the coverage target file to the project.

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


#### LicenseInfo {#SEC_SolutionTargets_LicenseInfo}

#### SolutionInfo {#SEC_SolutionTargets_SolutionInfo}
