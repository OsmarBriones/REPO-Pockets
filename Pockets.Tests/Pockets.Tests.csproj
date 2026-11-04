<Project Sdk="Microsoft.NET.Sdk">

  <!-- Tier 1 (pure logic) and Tier 2 (reflection contract) tests. Pure domain files are linked in from the mod,
       so they must never depend on game or Unity types. -->
  <PropertyGroup>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\Domain\*.cs" Link="Domain\%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.3.1" />
    <PackageReference Include="MSTest.TestFramework" Version="3.3.1" />
    <PackageReference Include="System.Reflection.Metadata" Version="8.0.0" />
  </ItemGroup>

</Project>
