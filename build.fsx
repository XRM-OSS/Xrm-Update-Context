// include Fake libs
#I @"packages\FAKE\tools\"
#r @"packages\FAKE\tools\FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile
open Fake.Testing.XUnit2
open System.IO
open Fake.Paket
open Fake.OpenCoverHelper
open Fake.ReportGeneratorHelper
open Fake.FileHelper

//Project config
let projectName = "Xrm.Oss.UpdateContext"
let projectDescription = "A Dynamics CRM / Dynamics365 library for easing updates on CRM records"
let authors = ["Florian Kroenert"]

// Directories
let buildDir  = @".\build\"
let libbuildDir = buildDir + @"lib\"

let testDir   = @".\test\"

let xUnitPath = "packages" @@ "xunit.runner.console" @@ "tools" @@ "net462" @@ "xunit.console.exe"
let reportGeneratorPath = "packages" @@ "ReportGenerator" @@ "tools" @@ "netcoreapp3.1" @@ "ReportGenerator.exe"
let deployDir = @".\Publish\"
let libdeployDir = deployDir + @"lib\"
let nugetDir = @".\nuget\"
let packagesDir = @".\packages\"

// version info
let majorversion    = "1"
let minorversion    = "3"
let build           = "1"
let mutable nugetVersion    = ""
let mutable asmVersion      = ""
let mutable asmInfoVersion  = ""

let sha                     = Git.Information.getCurrentHash() 

// Targets
Target "Clean" (fun _ ->

    CleanDirs [buildDir; testDir; deployDir; nugetDir]
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

Target "CodeCoverage" (fun _ ->
    OpenCover (fun p -> { p with 
                                TestRunnerExePath = xUnitPath
                                ExePath ="packages" @@ "OpenCover" @@ "tools" @@ "OpenCover.Console.exe"
                                Register = RegisterType.RegisterUser
                                WorkingDir = (testDir)
                                Filter = "+[Xrm.Oss*]* -[*.Tests*]*"
                                Output = "../coverage.xml"
                        }) "Xrm.Oss.UnitOfWork.Tests.dll"
)

Target "ReportCodeCoverage" (fun _ ->
    ReportGenerator (fun p -> { p with 
                                    ExePath = reportGeneratorPath
                                    WorkingDir = (testDir)
                                    TargetDir = "../reports"
                                    ReportTypes = [ReportGeneratorReportType.Html; ReportGeneratorReportType.Badges ]
                               }) [ "..\coverage.xml" ]
    
)

Target "Publish" (fun _ ->
    CreateDir libdeployDir

    !! (libbuildDir @@ @"MsDyn*.dll")
            |> CopyTo libdeployDir
)

Target "CreateNuget" (fun _ ->
    Pack (fun p ->
            {p with
                Version = nugetVersion
                
            })
)

// Dependencies
"Clean"
  ==> "BuildVersions"
  =?> ("AssemblyInfo", not isLocalBuild )
  ==> "BuildLib"
  ==> "BuildTest"
  ==> "RunTest"
  ==> "CodeCoverage"
  ==> "ReportCodeCoverage"
  ==> "Publish"
  ==> "CreateNuget"

// start build
RunTargetOrDefault "Publish"
