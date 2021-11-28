### Visual Studio solution targets {#PAGE_SolutionTargets}


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


#### LicenseInfo {#SEC_SolutionTargets_LicenseInfo}

#### SolutionInfo {#SEC_SolutionTargets_SolutionInfo}
