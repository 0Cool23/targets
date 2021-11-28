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
public sealed class BuildInfoTask
    :   Task
    {
    private const string DATE_FORMAT = "yyyy-MM-ddTHH:mm:sszzzz";

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

    public string ProjectName
        {
        get;
        set;
        }

    public string ProjectNameClean
        {
        get
            {
            return ProjectName.Replace(" ", "_").Trim();
            }
        }

    public enum eProjectType : byte
        {
        VisualBasic = 0,
        CSharp      = 1,
        Undefined   = 255,
        }

    private static Dictionary<string, eProjectType> the_project_type_map = new Dictionary<string, eProjectType>
        {
            {".vbproj", eProjectType.VisualBasic},
            {".csproj", eProjectType.CSharp},
        };

    public eProjectType ProjectType
        {
        get
            {
            if( the_project_type_map.ContainsKey(ProjectExt) )
                {
                return the_project_type_map[ProjectExt];
                }
            return eProjectType.Undefined;
            }
        }

    [Required]
    public string ProjectExt
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

    private DirectoryInfo create_directory( string directory_path )
        {
        if( Directory.Exists(directory_path) )
            {
            return new DirectoryInfo(directory_path);
            }
        return Directory.CreateDirectory(directory_path);
        }

    private List<string> split_lines( string input_string )
        {
        return Regex.Split(input_string, "\r\n|\r|\n").ToList();
        }

    private int run_git( string argument_string, out string console_output )
        {
        console_output = string.Empty;
        try
            {
            var process = new Process
                {
                StartInfo = 
                    {
                    FileName = @"git.exe",
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
        catch( Exception except )
            {
            Log.LogError("Caught exception: {0}", except.GetType());
            Log.LogErrorFromException(except);
            }
        return -1;
        }

    private string get_last_commit_hash_string( string branch = "HEAD" )
        {
        var _ = run_git(string.Format(CultureInfo.InvariantCulture, "rev-parse {0}", branch), out string console_output);
        return console_output;
        }

    private string get_last_commit_date_string( string branch = "HEAD" )
        {
        var _ = run_git(string.Format(CultureInfo.InvariantCulture, "log -1 --format=%cI  {0}", branch), out string console_output);
        return console_output;
        }

    public string BranchName
        {
        get;
        private set;
        } = string.Empty;

    private void git_branch()
        {
        var branch_name = string.Empty;
        var _ = run_git("branch --show-current --no-color", out branch_name);
        if( branch_name == string.Empty )
            {
            _ = run_git("reflog show -1 --format=%D --no-color", out branch_name);
            branch_name = Regex.Replace(branch_name, @"^.*, origin/", string.Empty);
            }
        Log.LogMessage(MessageImportance.High, "Git branch:      {0}", branch_name);
        BranchName = branch_name;
        }

    public string CommitHash
        {
        get;
        private set;
        } = string.Empty;

    private void git_commit_hash()
        {
        CommitHash = get_last_commit_hash_string();
        Log.LogMessage(MessageImportance.High, "Git commit:      {0}", CommitHash);
        }

    public DateTime CommitDate
        {
        get;
        private set;
        } = DateTime.MinValue;

    private void git_commit_date()
        {
        CommitDate = DateTime.Parse(get_last_commit_date_string());
        Log.LogMessage(MessageImportance.High, "Git commit date: {0}", CommitDate.ToString(DATE_FORMAT));
        }    

    public bool IsClean
        {
        get;
        private set;
        } = false;

    private void git_is_clean()
        {
        var _ = run_git("status --untracked-files=no --porcelain", out string console_output);
        Log.LogMessage(MessageImportance.High, "Git is clean:    {0}", (string.IsNullOrEmpty(console_output) ? "clean" : "dirty"));
        IsClean = string.IsNullOrEmpty(console_output);
        }

    private List<string> RemoteList = new List<string>{};
    public bool HasRemotes
        {
        get
            {
            return (RemoteList.Count > 0);
            }
        }

    private void get_remote_list()
        {
        var _ = run_git("branch -a --no-color", out string console_output);
        foreach( var remote_entry in split_lines(console_output) )
            {
            var match_list = Regex.Matches(remote_entry.Trim(), @"^remotes/(.*)/" + BranchName + "$");
            if( match_list.Count == 1 )
                {
                var remote_branch = match_list[0].Value;
                remote_branch = Regex.Replace(remote_branch, @"^remotes/", string.Empty);
                remote_branch = Regex.Replace(remote_branch, @"->.*$", string.Empty);
                RemoteList.Add(remote_branch);
                }
            }
        Log.LogMessage(MessageImportance.High, "Git has remotes: {0} ({1})", HasRemotes, RemoteList.Count);
        }
    
    private List<string> get_remote_difflist( string branch_a, string branch_b )
        {
        var git_arguments = string.Format(CultureInfo.InvariantCulture, "log --format=%H {0}..{1}", branch_a, branch_b);
        var _ = run_git(git_arguments, out string console_output);
        return split_lines(console_output);
        }

    public bool InSync
        {
        get;
        private set;
        } = true;

    private void git_remotes_in_sync()
        {
        InSync = (RemoteList.Count != 0);
        foreach( var remote_branch in RemoteList )
            {
            var commit_hash = get_last_commit_hash_string(remote_branch);
            var commit_date = get_last_commit_date_string(remote_branch);

            var remote_date = DateTime.Parse(commit_date);

            int commit_diff_count = 0;
            if( remote_date < CommitDate )
                {
                var commit_hash_list = get_remote_difflist(commit_hash, "HEAD");
                commit_diff_count = commit_hash_list.Count; 
                }
            else if( remote_date > CommitDate )
                {
                var commit_hash_list = get_remote_difflist("HEAD", commit_hash);
                commit_diff_count = -commit_hash_list.Count; 
                }

            InSync &= (commit_diff_count == 0);
            Log.LogMessage(MessageImportance.High, "Git commit diff: {0}", commit_diff_count);
            }
        Log.LogMessage(MessageImportance.High, "Git in sync:     {0}", InSync);
        }

    public string BuildHost
        {
        get;
        private set;
        } = string.Empty;

    private void get_full_qualified_domainname()
        {
        var ip_properties = IPGlobalProperties.GetIPGlobalProperties();
        BuildHost = string.Format("{0}.{1}", ip_properties.HostName, ip_properties.DomainName);
        Log.LogMessage(MessageImportance.High, "Build host:      {0}", BuildHost);
        }

    public DateTime BuildDate
        {
        get;
        private set;
        } = DateTime.MinValue;

    private void get_build_datetime()
        {
        BuildDate = DateTime.Now;
        Log.LogMessage(MessageImportance.High, "Build date:      {0}", BuildDate.ToString(DATE_FORMAT));
        }

    private StreamWriter create_file( string file_path )
        {
        return new StreamWriter(file_path, false, Encoding.UTF8);
        }

    private void create_file( string file_path, List<string> line_list )
        {
        using( var file_stream = create_file(file_path) )
            {
            foreach( var line in line_list )
                {
                file_stream.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}", line));
                }
            }
        }

    private void write_version_file( FileInfo version_file_info, Version version )
        {
        create_directory(version_file_info.DirectoryName);
        using( var file_stream = create_file(version_file_info.FullName) )
            {
            file_stream.WriteLine(string.Format(CultureInfo.InvariantCulture, @"set(VERSION_MAJOR ""{0}"")", version.Major));
            file_stream.WriteLine(string.Format(CultureInfo.InvariantCulture, @"set(VERSION_MINOR ""{0}"")", version.Minor));
            file_stream.WriteLine(string.Format(CultureInfo.InvariantCulture, @"set(VERSION_PATCH ""{0}"")", version.Build));
            file_stream.WriteLine(string.Format(CultureInfo.InvariantCulture, @"set(VERSION_BUILD ""{0}"")", version.Revision));
            }
        }

    private void write_version_data( string version_file_path, Version version )
        {
        var version_file_info = new FileInfo(version_file_path);
        write_version_file(version_file_info, version);
        }

    private int get_version_value( string[] lines, string version_pattern )
        {
        foreach( var line in lines )
            {
            var pattern = string.Format(CultureInfo.InvariantCulture, @"^set\({0} ""(\d+)""\)$", version_pattern);
            var matches = Regex.Match(line, pattern, RegexOptions.ECMAScript);
            if( matches.Groups.Count > 1 )
                {
                return int.Parse(matches.Groups[1].Value);
                }
            }
        return -1;
        }

    private Version read_version_file( FileInfo version_file_info )
        {
        var lines = File.ReadAllLines(version_file_info.FullName, Encoding.UTF8);
        
        int major = get_version_value(lines, "VERSION_MAJOR");
        int minor = get_version_value(lines, "VERSION_MINOR");
        int patch = get_version_value(lines, "VERSION_PATCH");
        int build = get_version_value(lines, "VERSION_BUILD");

        return new Version(major, minor, patch, build);
        }


    private Version read_version_data( string version_file_path )
        {
        var version_file_info = new FileInfo(version_file_path);
        if( !version_file_info.Exists )
            {
            write_version_file(version_file_info, new Version(0, 0, 0, 0));
            }
        var version = read_version_file(version_file_info );
        return version;
        }

    private Version update_version_data( string version_file_path )
        {
        var version = read_version_data(version_file_path);
        if( !(InSync && IsClean) )
            {
            version     = new Version(version.Major, version.Minor, version.Build, version.Revision + 1);
            Log.LogMessage(MessageImportance.High, "Updating version file {0} to {1}", version_file_path, version);
            write_version_data(version_file_path, version);
            }
        return version;
        }

    private void write_buildinfo_cs( string destination_path )
        {
        Log.LogMessage(MessageImportance.High, "Writing CSharp output!");
        var line_list = new List<string>{};

        var asm_version = update_version_data(ProjectDir + "/asm_version.in");
        var api_version = read_version_data(ProjectDir + "/api_version.in");

        line_list.Add(string.Format(@"/** @file"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @author Auto generated source file"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @{{"));
        line_list.Add(string.Format(@"    @page PAGE_BuildInfo BuildInfo"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @section SEC_BuildInfo_{0} BuildInfo {0}", ProjectNameClean));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    <table border='0'>"));
        line_list.Add(string.Format(@"    <tr><td colspan='3'><b>Compilation</b></td></tr>"));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>BuildHost:</b></td><td>{0}</td></tr>", BuildHost));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>BuildDate:</b></td><td>{0}</td></tr>", BuildDate.ToString(DATE_FORMAT)));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>BuildType:</b></td><td>{0}</td></tr>", Configuration));
        line_list.Add(string.Format(@"    <tr><td colspan='3'><b>Version</b></td></tr>"));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>Assembly:</b></td><td>{0}</td></tr>", asm_version.ToString()));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>API:</b></td>     <td>{0}</td></tr>", api_version.ToString()));
        line_list.Add(string.Format(@"    <tr><td colspan='3'><b>Git Repository</b></td></tr>"));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>BranchName:</b></td><td>{0}</td></tr>", BranchName));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>CommitDate:</b></td><td>{0}</td></tr>", CommitDate.ToString(DATE_FORMAT)));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>CommitHash:</b></td><td>{0}</td></tr>", CommitHash));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>Clean:</b></td>    <td>{0}</td></tr>", IsClean.ToString().ToLower()));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>Synced:</b></td>   <td>{0}</td></tr>", InSync.ToString().ToLower()));
        line_list.Add(string.Format(@"    </table>"));
        line_list.Add(string.Format(@"    @}}"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @defgroup REF_BuildInfo_{0} generated.BuildInfo", ProjectNameClean));
        line_list.Add(string.Format(@"    @{{"));
        line_list.Add(string.Format(@"    @brief  Class with compile time build information properties."));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @ingroup @REF_{0}", ProjectNameClean));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @}}"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"*/"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"using System;"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"namespace {0}.Generated", ProjectNameClean));
        line_list.Add(string.Format(@"{{"));
        line_list.Add(string.Format(@"/**"));
        line_list.Add(string.Format(@"@ingroup REF_BuildInfo_{0}", ProjectNameClean));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"@class BuildInfo"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"*/"));
        line_list.Add(string.Format(@"public static class BuildInfo"));
        line_list.Add(string.Format(@"    {{"));
        line_list.Add(string.Format(@"    // Project information"));        
        line_list.Add(string.Format(@"    public const string    ProjectName         = ""{0}"";", ProjectName));
        line_list.Add(string.Format(@"    // Compile information"));        
        line_list.Add(string.Format(@"    public const string    BuildHost           = ""{0}"";", BuildHost));
        line_list.Add(string.Format(@"    public const string    BuildType           = ""{0}"";", Configuration));
        line_list.Add(string.Format(@"    public static readonly DateTime BuildDate  = DateTime.Parse(""{0}"");", BuildDate.ToString(DATE_FORMAT)));
        line_list.Add(string.Format(@"    // Git repository information"));        
        line_list.Add(string.Format(@"    public const string    BranchName          = ""{0}"";", BranchName));
        line_list.Add(string.Format(@"    public const string    CommitHash          = ""{0}"";", CommitHash));
        line_list.Add(string.Format(@"    public static readonly DateTime CommitDate = DateTime.Parse(""{0}"");", CommitDate.ToString(DATE_FORMAT)));
        line_list.Add(string.Format(@"    public const bool      IsClean             = {0};", IsClean.ToString().ToLower()));
        line_list.Add(string.Format(@"    public const bool      InSync              = {0};", InSync.ToString().ToLower()));
        line_list.Add(string.Format(@"    // Version information"));
        line_list.Add(string.Format(@"    #pragma warning disable IDE0079 // unnecessary pragma warning"));
        line_list.Add(string.Format(@"    #pragma warning disable IDE0090 // simplify new(...)"));
        line_list.Add(string.Format(@"    public static readonly Version  AsmVersion = new Version({0}, {1}, {2}, {3});", asm_version.Major, asm_version.Minor, asm_version.Build, asm_version.Revision));
        line_list.Add(string.Format(@"    public static readonly Version  ApiVersion = new Version({0}, {1}, {2}, {3});", api_version.Major, api_version.Minor, api_version.Build, api_version.Revision));
        line_list.Add(string.Format(@"    #pragma warning restore IDE0090"));
        line_list.Add(string.Format(@"    #pragma warning restore IDE0079"));
        line_list.Add(string.Format(@"    }}"));
        line_list.Add(string.Format(@"}}"));

        create_file(destination_path + "/build_info.cs", line_list);
        }

    private static void write_buildinfo_cs( BuildInfoTask generator, string destination_path )
        {
        generator.write_buildinfo_cs(destination_path);
        }

    private void write_buildinfo_vb( string destination_path )
        {
        Log.LogMessage(MessageImportance.High, "Writing VisualBasic output!");
        var line_list = new List<string>{};

        var asm_version = update_version_data(ProjectDir + "/asm_version.in");
        var api_version = read_version_data(ProjectDir + "/api_version.in");

        line_list.Add(string.Format(@"' @file"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"' @author Auto generated source file"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @{{"));
        line_list.Add(string.Format(@"'   @page PAGE_BuildInfo BuildInfo"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @section SEC_BuildInfo_{0} BuildInfo {0}", ProjectNameClean));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   <table border='0'>"));
        line_list.Add(string.Format(@"'   <tr><td colspan='3'><b>Compilation</b></td></tr>"));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>BuildHost:</b></td><td>{0}</td></tr>", BuildHost));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>BuildDate:</b></td><td>{0}</td></tr>", BuildDate.ToString(DATE_FORMAT)));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>BuildType:</b></td><td>{0}</td></tr>", Configuration));
        line_list.Add(string.Format(@"'   <tr><td colspan='3'><b>Version</b></td></tr>"));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>Assembly:</b></td><td>{0}</td></tr>", asm_version.ToString()));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>API:</b></td>     <td>{0}</td></tr>", api_version.ToString()));
        line_list.Add(string.Format(@"'   <tr><td colspan='3'><b>Git Repository</b></td></tr>"));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>BranchName:</b></td><td>{0}</td></tr>", BranchName));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>CommitDate:</b></td><td>{0}</td></tr>", CommitDate.ToString(DATE_FORMAT)));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>CommitHash:</b></td><td>{0}</td></tr>", CommitHash));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>Clean:</b></td>    <td>{0}</td></tr>", IsClean.ToString().ToLower()));
        line_list.Add(string.Format(@"'     <tr><td>&nbsp;</td><td><b>Synced:</b></td>   <td>{0}</td></tr>", InSync.ToString().ToLower()));
        line_list.Add(string.Format(@"'   </table>"));
        line_list.Add(string.Format(@"'   @}}"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @defgroup REF_BuildInfo_{0} generated.BuildInfo", ProjectNameClean));
        line_list.Add(string.Format(@"'   @{{"));
        line_list.Add(string.Format(@"'   @brief  Class with compile time build information properties."));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @ingroup @REF_{0}", ProjectNameClean));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @}}"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"Imports System"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"Namespace {0}.Generated", ProjectNameClean));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"' @ingroup REF_BuildInfo_{0}", ProjectNameClean));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"' @class BuildInfo"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"Public Module BuildInfo"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    ' Project information"));        
        line_list.Add(string.Format(@"    Public Const ProjectName As String = ""{0}""", ProjectName));
        line_list.Add(string.Format(@"    ' Compile information"));        
        line_list.Add(string.Format(@"    Public Const BuildHost As String   = ""{0}""", BuildHost));
        line_list.Add(string.Format(@"    Public Const BuildType As String   = ""{0}""", Configuration));
        line_list.Add(string.Format(@"    public       BuildDate As Date     = DateTime.Parse(""{0}"")", BuildDate.ToString(DATE_FORMAT)));
        line_list.Add(string.Format(@"    ' Git repository information"));        
        line_list.Add(string.Format(@"    Public Const BranchName As String  = ""{0}""", BranchName));
        line_list.Add(string.Format(@"    Public Const CommitHash As String  = ""{0}""", CommitHash));
        line_list.Add(string.Format(@"    Public       CommitDate As Date    = DateTime.Parse(""{0}"")", CommitDate.ToString(DATE_FORMAT)));
        line_list.Add(string.Format(@"    Public Const IsClean As Boolean    = {0}", IsClean.ToString()));
        line_list.Add(string.Format(@"    Public Const InSync As Boolean     = {0}", InSync.ToString()));
        line_list.Add(string.Format(@"    ' Version information"));
        line_list.Add(string.Format(@"    Public       Version  AsmVersion   = new Version({0}, {1}, {2}, {3});", asm_version.Major, asm_version.Minor, asm_version.Build, asm_version.Revision));
        line_list.Add(string.Format(@"    Public       Version  ApiVersion   = new Version({0}, {1}, {2}, {3});", api_version.Major, api_version.Minor, api_version.Build, api_version.Revision));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"End Module"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"End Namespace"));

        create_file(destination_path + "/build_info.vb", line_list);
        }

    private static void write_buildinfo_vb( BuildInfoTask generator, string destination_path )
        {
        generator.write_buildinfo_vb(destination_path);
        }

    private delegate void BuildInfoWriter( BuildInfoTask generator, string destination_path );
    private static Dictionary<eProjectType, BuildInfoWriter> the_buildinfo_writer_map = new Dictionary<eProjectType, BuildInfoWriter>
        {
            {eProjectType.VisualBasic, write_buildinfo_vb},
            {eProjectType.CSharp,      write_buildinfo_cs},
        };

    public override bool Execute()
        {
        Log.LogMessage(MessageImportance.High, "Executing BuildInfoTask!");

        Log.LogMessage(MessageImportance.High, "Project name:    {0}", ProjectName);
        Log.LogMessage(MessageImportance.High, "Project type:    {0}", ProjectType.ToString());
        git_branch();
        git_commit_hash();
        git_commit_date();
        git_is_clean();
        get_remote_list();
        git_remotes_in_sync();

        get_full_qualified_domainname();
        get_build_datetime();

        string destination_path = ProjectDir + "/generated";
        create_directory(destination_path);

        if( the_buildinfo_writer_map.ContainsKey(ProjectType) )
            {
            the_buildinfo_writer_map[ProjectType](this, destination_path);
            }

        return true;
        }
    }
}
