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
public sealed class SolutionInfoTask
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
    public string SolutionName
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

    [Required]
    public string TargetPath
        {
        get;
        set;
        }

    private FileInfo TargetFile
        {
        get => new FileInfo(TargetPath);
        }

    private DirectoryInfo TargetDir
        {
        get => TargetFile.Directory;
        }


    private string escape_path( string input_string )
        {
        return Regex.Replace(input_string, @"\\", @"\\");
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

    private void write_solution_info_cs( string destination_path )
        {
        Log.LogMessage(MessageImportance.High, "Writing CSharp output!");
        var line_list = new List<string>{};

        line_list.Add(string.Format(@"/** @file"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @author Auto generated source file"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @{{"));
        line_list.Add(string.Format(@"    @page PAGE_SolutionInfo SolutionInfo"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @section SEC_SolutionInfo_{0} {0}", ProjectName));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    <table border='0'>"));
        line_list.Add(string.Format(@"    <tr><td colspan='3'><b>Solution</b></td></tr>"));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>SolutionName:</b></td><td>{0}</td></tr>", SolutionName));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>SolutionDir:</b></td> <td>{0}</td></tr>", escape_path(SolutionDir)));
        line_list.Add(string.Format(@"    <tr><td colspan='3'><b>Project</b></td></tr>"));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>ProjectName:</b></td><td>{0}</td></tr>", ProjectName));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>ProjectDir:</b></td> <td>{0}</td></tr>", escape_path(ProjectDir)));
        line_list.Add(string.Format(@"    <tr><td colspan='3'><b>Compilation</b></td></tr>"));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>Configruation:</b></td><td>{0}</td></tr>", Configuration));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>TargetDir:</b></td><td>{0}</td></tr>", escape_path(TargetDir.FullName)));
        line_list.Add(string.Format(@"    </table>"));
        line_list.Add(string.Format(@"    @}}"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @defgroup REF_SolutionInfo_{0} generated.SolutionInfo", ProjectName));
        line_list.Add(string.Format(@"    @{{"));
        line_list.Add(string.Format(@"    @brief    Class with compile time solution information properties."));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @ingroup  @REF_{0}", ProjectName));
        line_list.Add(string.Format(@"    @}}"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"*/"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"using System;"));
        line_list.Add(string.Format(@"using System.IO;"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"namespace {0}.Generated", ProjectName));
        line_list.Add(string.Format(@"{{"));
        line_list.Add(string.Format(@"/**"));
        line_list.Add(string.Format(@"@ingroup REF_SolutionInfo_{0}", ProjectName));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"@class SolutionInfo"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"*/"));
        line_list.Add(string.Format(@"public static class SolutionInfo"));
        line_list.Add(string.Format(@"    {{"));
        line_list.Add(string.Format(@"    // Solution information"));        
        line_list.Add(string.Format(@"    public const string         SolutionName          = ""{0}"";", SolutionName));
        line_list.Add(string.Format(@"    #pragma warning disable IDE0079 // unnecessary pragma warning"));
        line_list.Add(string.Format(@"    #pragma warning disable IDE0090 // simplify new(...)"));
        line_list.Add(string.Format(@"    public static readonly DirectoryInfo SolutiontDir = new DirectoryInfo(@""{0}"");", SolutionDir));
        line_list.Add(string.Format(@"    #pragma warning restore IDE0090"));
        line_list.Add(string.Format(@"    #pragma warning restore IDE0079"));
        line_list.Add(string.Format(@"    // Project information"));        
        line_list.Add(string.Format(@"    public const string         ProjectName           = ""{0}"";", ProjectName));
        line_list.Add(string.Format(@"    #pragma warning disable IDE0079 // unnecessary pragma warning"));
        line_list.Add(string.Format(@"    #pragma warning disable IDE0090 // simplify new(...)"));
        line_list.Add(string.Format(@"    public static readonly DirectoryInfo ProjectDir   = new DirectoryInfo(@""{0}"");", ProjectDir));
        line_list.Add(string.Format(@"    #pragma warning restore IDE0090"));
        line_list.Add(string.Format(@"    #pragma warning restore IDE0079"));
        line_list.Add(string.Format(@"    // Compile information"));        
        line_list.Add(string.Format(@"    public const string    Configuration       = ""{0}"";", Configuration));
        line_list.Add(string.Format(@"    #pragma warning disable IDE0079 // unnecessary pragma warning"));
        line_list.Add(string.Format(@"    #pragma warning disable IDE0090 // simplify new(...)"));
        line_list.Add(string.Format(@"    public static readonly DirectoryInfo TargetDir   = new DirectoryInfo(@""{0}"");", TargetDir.FullName));
        line_list.Add(string.Format(@"    #pragma warning restore IDE0090"));
        line_list.Add(string.Format(@"    #pragma warning restore IDE0079"));
        line_list.Add(string.Format(@"    }}"));
        line_list.Add(string.Format(@"}}"));

        create_file(destination_path + "/solution_info.cs", line_list);
        }

    private static void write_solution_info_cs( SolutionInfoTask generator, string destination_path )
        {
        generator.write_solution_info_cs(destination_path);
        }

    private void write_solution_info_vb( string destination_path )
        {
        Log.LogMessage(MessageImportance.High, "Writing VisualBasic output!");
        var line_list = new List<string>{};

        line_list.Add(string.Format(@"'   @file"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @author Auto generated source file"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @{{"));
        line_list.Add(string.Format(@"'   @page PAGE_SolutionInfo SolutionInfo"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @section SEC_SolutionInfo_{0} SolutionInfo {0}", ProjectName));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    <table border='0'>"));
        line_list.Add(string.Format(@"    <tr><td colspan='3'><b>Solution</b></td></tr>"));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>SolutionName:</b></td><td>{0}</td></tr>", SolutionName));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>SolutionDir:</b></td> <td>{0}</td></tr>", escape_path(SolutionDir)));
        line_list.Add(string.Format(@"    <tr><td colspan='3'><b>Project</b></td></tr>"));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>ProjectName:</b></td><td>{0}</td></tr>", ProjectName));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>ProjectDir:</b></td> <td>{0}</td></tr>", escape_path(ProjectDir)));
        line_list.Add(string.Format(@"    <tr><td colspan='3'><b>Compilation</b></td></tr>"));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>Configruation:</b></td><td>{0}</td></tr>", Configuration));
        line_list.Add(string.Format(@"      <tr><td>&nbsp;</td><td><b>TargetDir:</b></td><td>{0}</td></tr>", escape_path(TargetDir.FullName)));
        line_list.Add(string.Format(@"    </table>"));
        line_list.Add(string.Format(@"'   @}}"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @defgroup REF_SolutionInfo_{0} generated.SolutionInfo", ProjectName));
        line_list.Add(string.Format(@"'   @{{"));
        line_list.Add(string.Format(@"'   @brief  Class with compile time solution information properties."));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @ingroup @REF_{0}", ProjectName));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @}}"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"Imports System"));
        line_list.Add(string.Format(@"Imports System.IO"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"Namespace {0}.Generated", ProjectName));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @ingroup REF_SolutionInfo_{0}", ProjectName));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'   @class SolutionInfo"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"Public Module SolutionInfo"));
        line_list.Add(string.Format(@"    ' Solution information"));        
        line_list.Add(string.Format(@"    Public Const SolutionName  As String        = ""{0}""", SolutionName));
        line_list.Add(string.Format(@"    Public Shared SolutiontDir As DirectoryInfo = new DirectoryInfo(@""{0}"")", SolutionDir));
        line_list.Add(string.Format(@"    ' Project information"));        
        line_list.Add(string.Format(@"    Public Const ProjectName As String          = ""{0}""", ProjectName));
        line_list.Add(string.Format(@"    Public Shared ProjectDir As DirectoryInfo   = new DirectoryInfo(@""{0}"")", ProjectDir));
        line_list.Add(string.Format(@"    ' Compile information"));        
        line_list.Add(string.Format(@"    Public Const Configuration As String        = ""{0}""", Configuration));
        line_list.Add(string.Format(@"    Public Shared TargetDir As DirectoryInfo   = new DirectoryInfo(@""{0}"")", TargetDir.FullName));
        line_list.Add(string.Format(@"End Module"));
        line_list.Add(string.Format(@"End Namespace"));

        create_file(destination_path + "/solution_info.vb", line_list);
        }

    private static void write_solution_info_vb( SolutionInfoTask generator, string destination_path )
        {
        generator.write_solution_info_vb(destination_path);
        }

    private delegate void SolutionInfoWriter( SolutionInfoTask generator, string destination_path );
    private static Dictionary<eProjectType, SolutionInfoWriter> the_solutioninfo_writer_map = new Dictionary<eProjectType, SolutionInfoWriter>
        {
            {eProjectType.VisualBasic, write_solution_info_vb},
            {eProjectType.CSharp,      write_solution_info_cs},
        };

    public override bool Execute()
        {
        Log.LogMessage(MessageImportance.High, "Executing SolutionInfoTask!");

        Log.LogMessage(MessageImportance.High, "Project name:    {0}", ProjectName);
        Log.LogMessage(MessageImportance.High, "Project type:    {0}", ProjectType.ToString());

        string destination_path = ProjectDir + "/generated";
        create_directory(destination_path);

        if( the_solutioninfo_writer_map.ContainsKey(ProjectType) )
            {
            the_solutioninfo_writer_map[ProjectType](this, destination_path);
            }

        return true;
        }
    }
}
