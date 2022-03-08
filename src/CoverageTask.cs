using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Build.Tasks
{
public sealed class CoverageTask
    :   Task
    {
    private const string OPENCOVER_CONSOLE = @"C:\Tools\OpenCover\4.7.1221\tools\OpenCover.Console.exe";
    private const string REPORT_GENERATOR  = @"C:\Tools\ReportGenerator\5.0.4\tools\netcoreapp3.1\ReportGenerator.exe";

    [Required]
    public string SolutionDir
        {
        get;
        set;
        }

    [Required]
    public string ProjectDir
        {
        get;
        set;
        }

    [Required]
    public string Configuration
        {
        get;
        set;
        }

    [Required]
    public string DevEnvDir
        {
        get;
        set;
        }

    [Required]
    public string MSBuildToolsPath
        {
        get;
        set;
        }

    [Required]
    public string TargetPath
        {
        get;
        set;
        }

    private void clear_testcoverage_dir( string report_directory )
        {
        var directory_info = new DirectoryInfo(report_directory);
        if( directory_info.Exists )
            {
            Log.LogMessage(MessageImportance.High, "Removing directory {0}", report_directory);
            directory_info.Delete(true);
            }
        Log.LogMessage(MessageImportance.High, "Creating directory {0}", report_directory);
        directory_info.Create();
        }

    private int process_command( string command, string argument_string, out string console_output )
        {
        console_output = string.Empty;
        var process = new Process
            {
            StartInfo = 
                {
                FileName = command,
                WorkingDirectory = SolutionDir,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = argument_string,
                }
            };
        process.Start();
        StreamReader stream_reader = process.StandardOutput;
        console_output = stream_reader.ReadToEnd().Trim();
        process.WaitForExit();
        return process.ExitCode;
        }

    private string get_user_profile()
        {
        if( Environment.GetEnvironmentVariable("USERPROFILE") == Environment.GetEnvironmentVariable("USERPROFILE", EnvironmentVariableTarget.Process) )
            {
            return "user";
            }
        return string.Empty;
        }

    private List<string> m_result_list = new List<string>{};
    private void execute_component_library( string target_path, string opencover_result )
        {
        Log.LogMessage(MessageImportance.High, "Running OpenCover {0} -> {1}", target_path, opencover_result);
        var vs_console = DevEnvDir + @"\Extensions\TestPlatform\vstest.console.exe";
        var opencover_args = string.Empty;
        opencover_args += string.Format(CultureInfo.InvariantCulture, @" -target:""{0}""", vs_console);
        opencover_args += string.Format(CultureInfo.InvariantCulture, @" -targetargs:""{0}""", target_path);
        opencover_args += string.Format(CultureInfo.InvariantCulture, @" -register:""{0}""", get_user_profile());
        opencover_args += string.Format(CultureInfo.InvariantCulture, @" -output:""{0}""", opencover_result);
        opencover_args += string.Format(CultureInfo.InvariantCulture, @" -returntargetcode");
        opencover_args += string.Format(CultureInfo.InvariantCulture, @" -showunvisited");
        //opencover_args += string.Format(CultureInfo.InvariantCulture, @" -filter:""-[*.Tests]*""");
        opencover_args += string.Format(CultureInfo.InvariantCulture, @" -log:All");
        opencover_args += string.Format(CultureInfo.InvariantCulture, @" -safemode:on");
        var exit_code = process_command(OPENCOVER_CONSOLE, opencover_args.Trim(), out string console_output);
        if( exit_code != 0 )
            {
            throw new Exception(string.Format(CultureInfo.InvariantCulture, "OpenCover finished with non zero exit code ({0})."
                                                                            + Environment.NewLine
                                                                            + "Command: '" + OPENCOVER_CONSOLE + " {1}'"
                                                                            + Environment.NewLine
                                                                            + "Console output: {2}", exit_code, opencover_args.Trim(), console_output));
            }
        m_result_list.Add(opencover_result.Trim());
        }

    private void execute_component_tests( string report_directory )
        {
        execute_component_library(TargetPath, report_directory + @"TestCoverage.xml");
        }

    private void generate_report( string report_directory )
        {
        var coverage_results = string.Join(";", m_result_list);
        Log.LogMessage(MessageImportance.High, "Generating coverage report in '{0}' for {1}", report_directory, coverage_results);
        var report_args = string.Empty;
        report_args += string.Format(CultureInfo.InvariantCulture, @" -reports:""{0}""", coverage_results);
        report_args += string.Format(CultureInfo.InvariantCulture, @" -targetdir:""{0}""", report_directory.TrimEnd('\\'));
        report_args += string.Format(CultureInfo.InvariantCulture, @" -reporttypes:Html");
        report_args += string.Format(CultureInfo.InvariantCulture, @" -verbosity:Verbose");
        var exit_code = process_command(REPORT_GENERATOR, report_args.Trim(), out string console_output);
        if( exit_code != 0 )
            {
            throw new Exception(string.Format(CultureInfo.InvariantCulture, "ReportGenerator finished with non zero exit code ({0})."
                                                                            + Environment.NewLine
                                                                            + "Command: '" + REPORT_GENERATOR + " {1}'"
                                                                            + Environment.NewLine
                                                                            + "Console output: {2}", exit_code, report_args.Trim(), console_output));
            }
        }

    private void run_component_tests( string report_directory )
        {
        m_result_list.Clear();
        clear_testcoverage_dir(report_directory);
        execute_component_tests(report_directory);
        generate_report(report_directory);
        }

    public override bool Execute()
        {
        Log.LogMessage(MessageImportance.High, "Executing CoverageTask!");
        Log.LogMessage(MessageImportance.High, "  SolutionDir:      '{0}'", SolutionDir);
        Log.LogMessage(MessageImportance.High, "  ProjectDir:       '{0}'", ProjectDir);
        Log.LogMessage(MessageImportance.High, "  Configuration:    '{0}'", Configuration);
        Log.LogMessage(MessageImportance.High, "  DevEnvDir:        '{0}'", DevEnvDir);
        Log.LogMessage(MessageImportance.High, "  MSBuildToolsPath: '{0}'", MSBuildToolsPath);
        Log.LogMessage(MessageImportance.High, "  TargetPath:       '{0}'", TargetPath);
        Log.LogMessage(MessageImportance.High, "  USERPROFILE:      '{0}'", Environment.GetEnvironmentVariable("USERPROFILE"));

        try
            {
            var directory_info = new DirectoryInfo(SolutionDir + @"\TestCoverage").FullName + @"\";
            Log.LogMessage(MessageImportance.High, "  ReportDir:        '{0}'", directory_info);
            run_component_tests(directory_info);
            }
        catch( Exception except )
            {
            Log.LogErrorFromException(except);
            return false;
            }

        return true;
        }
    }
}