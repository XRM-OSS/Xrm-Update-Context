// include Fake libs
#I @"packages\FAKE\tools\"
#r @"packages\FAKE\tools\FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile
open Fake.Testing.XUnit2
open System.IO

//Project config
let projectName = "Xrm.Oss.UpdateContext"
let projectDescription = "A Dynamics CRM / Dynamics365 library for easing updates on CRM records"
let authors = ["Florian Kroenert"]

// Directories
let buildDir  = @".\build\"
let libbuildDir = buildDir + @"lib\"

let testDir   = @".\test\"

let deployDir = @".\Publish\"
let libdeployDir = deployDir + @"lib\"
let nugetDir = @".\nuget\"
let packagesDir = @".\packages\"

// version info
let mutable majorversion    = "1"
let mutable minorversion    = "0"
let mutable build           = buildVersion
let mutable nugetVersion    = ""
let mutable asmVersion      = ""
let mutable asmInfoVersion  = ""

let sha                     = Git.Information.getCurrentHash() 

// Targets
Target "Clean" (fun _ ->

    CleanDirs [buildDir; testDir; deployDir; nugetDir]
)

Target "RestorePackages" (fun _ ->

   let RestorePackages2() =
     !! "./src/**/packages.config"
     |> Seq.iter ( RestorePackage (fun p -> {p with Sources = ["http://go.microsoft.com/fwlink/?LinkID=206669"] } ))
     ()

   RestorePackages2()
)

Target "BuildVersions" (fun _ ->
    asmVersion      <- majorversion + "." + minorversion + "." + build
    asmInfoVersion  <- asmVersion + " - " + sha

    let nugetBuildNumber = if not isLocalBuild then build else "0"
    
    nugetVersion    <- majorversion + "." + minorversion + "." + nugetBuildNumber

    SetBuildNumber nugetVersion   // Publish version to TeamCity
)

Target "AssemblyInfo" (fun _ ->
    BulkReplaceAssemblyInfoVersions "src" (fun f -> 
                                              {f with
                                                  AssemblyVersion = asmVersion
                                                  AssemblyInformationalVersion = asmInfoVersion
                                                  AssemblyFileVersion = asmVersion})
)

Target "BuildLib" (fun _ ->
    !! @"src\lib\**\*.csproj"
        |> MSBuildRelease libbuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildTest" (fun _ ->
    !! @"src\test\**\*.csproj"
      |> MSBuildDebug testDir "Build"
      |> Log "Build Log: "
)

Target "RunTest" (fun _ ->
    let testFiles = !!(testDir @@ @"\**\*.Tests.dll")
    
    if testFiles.Includes.Length <> 0 then
      testFiles
        |> xUnit2 (fun test ->
            {test with
               ToolPath = findToolInSubPath "xunit.console.exe" (currentDirectory @@ "packages" @@ "xunit.runner.console")
               ShadowCopy = false;
            })
)

Target "Publish" (fun _ ->
    CreateDir libdeployDir

    !! (libbuildDir @@ @"MsDyn*.dll")
            |> CopyTo libdeployDir
)

Target "CreateNuget" (fun _ ->
    "Xrm.Oss.UpdateContext.nuspec"
          |> NuGet (fun p ->
            {p with
                Authors = authors
                Project = projectName
                Version = nugetVersion
                NoPackageAnalysis = true
                Description = projectDescription
                ToolPath = @".\tools\Nuget\Nuget.exe"
                OutputPath = nugetDir })
)

Target "PublishNuget" (fun _ ->

  let nugetPublishDir = (deployDir + "nuget")
  CreateDir nugetPublishDir

  !! (nugetDir + "*.nupkg")
     |> Copy nugetPublishDir
)

// Dependencies
"Clean"
  ==> "RestorePackages"
  ==> "BuildVersions"
  =?> ("AssemblyInfo", not isLocalBuild )
  ==> "BuildLib"
  ==> "BuildTest"
  ==> "RunTest"
  ==> "Publish"
  ==> "CreateNuget"
  ==> "PublishNuget"

// start build
RunTargetOrDefault "Publish"
