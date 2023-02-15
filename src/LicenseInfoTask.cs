using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Build.Tasks
{
public sealed class LicenseInfoTask
    : Task
    {
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
        CSharp = 1,
        Undefined = 255,
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
            if (the_project_type_map.ContainsKey(ProjectExt))
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

    private DirectoryInfo create_directory( string directory_path )
        {
        if (Directory.Exists(directory_path))
            {
            return new DirectoryInfo(directory_path);
            }
        return Directory.CreateDirectory(directory_path);
        }

    private StreamWriter create_file( string file_path )
        {
        return new StreamWriter(file_path, false, Encoding.UTF8);
        }

    private void create_file( string file_path, List<string> line_list )
        {
        using (var file_stream = create_file(file_path))
            {
            foreach (var line in line_list)
                {
                file_stream.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}", line));
                }
            }
        }

    private string[] read_license_lines( string project_path )
        {
        var files = Directory.GetFiles(project_path, @"license-*.md", SearchOption.TopDirectoryOnly);
        if( files.Length == 1 )
            {
            string[] lines = File.ReadAllLines(files[0], Encoding.UTF8);
            return lines;
            }
        return null;
        }

    private void get_short_text_cs( List<string> line_list, string[] lines )
        {
        if( lines == null )
            {
            line_list.Add(string.Format(@"    public static readonly string ShortText = """";"));
            return;
            }
        line_list.Add(string.Format(@"    public static readonly string ShortText = """""));
        int count = 2;
        foreach( string line in lines )
            {
            if( line.StartsWith("### ") )
                {
                --count;
                }
            if( count == 0 )
                {
                break;
                }
            var text = line.Replace("\"", "\\\"");
            line_list.Add(string.Format(@"        + ""{0}"" + Environment.NewLine", text));
            }
        line_list.Add(string.Format(@"        ;"));
        }

    private void get_long_text_cs( List<string> line_list, string[] lines )
        {
        if( lines == null )
            {
            line_list.Add(string.Format(@"    public static readonly string LongText  = """";"));
            return;
            }
        line_list.Add(string.Format(@"    public static readonly string LongText = """""));
        int count = 2;
        foreach( string line in lines )
            {
            if( line.StartsWith("### ") )
                {
                --count;
                }
            if( count > 0 )
                {
                continue;
                }
            var text = line.Replace("\"", "\\\"");
            line_list.Add(string.Format(@"        + ""{0}"" + Environment.NewLine", text));
            }
        line_list.Add(string.Format(@"        ;"));
        }

    private void write_licenseinfo_cs( string destination_path )
        {
        Log.LogMessage(MessageImportance.High, "Writing CSharp output!");
        var line_list = new List<string>{};

        line_list.Add(string.Format(@"/** @file"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @author Auto generated source file"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"    @defgroup REF_License_{0} generated.License", ProjectNameClean));
        line_list.Add(string.Format(@"    @{{"));
        line_list.Add(string.Format(@"    @brief  Class with license information properties."));
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
        line_list.Add(string.Format(@"@ingroup REF_License_{0}", ProjectNameClean));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"@class License"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"*/"));
        line_list.Add(string.Format(@"public static class License"));
        line_list.Add(string.Format(@"    {{"));

        var lines = read_license_lines(ProjectDir);
        get_short_text_cs(line_list, lines);
        get_long_text_cs(line_list, lines);

        line_list.Add(string.Format(@"    }}"));
        line_list.Add(string.Format(@"}}"));

        create_file(destination_path + "/license.cs", line_list);
        }

    private static void write_licenseinfo_cs( LicenseInfoTask generator, string destination_path )
        {
        generator.write_licenseinfo_cs(destination_path);
        }

    private void get_short_text_vb( List<string> line_list, string[] lines )
        {
        if( lines == null )
            {
            line_list.Add(string.Format(@"    Public Const ShortText As String   = """""));
            return;
            }
        line_list.Add(string.Format(@"    Public Const ShortText As String   = """""));
        int count = 2;
        foreach( string line in lines )
            {
            if( line.StartsWith("### ") )
                {
                --count;
                }
            if( count == 0 )
                {
                break;
                }
            var text = line.Replace("\"", "\\\"");
            line_list.Add(string.Format(@"        + ""{0}"" + Environment.NewLine", text));
            }
        }

    private void get_long_text_vb( List<string> line_list, string[] lines )
        {
        if( lines == null )
            {
            line_list.Add(string.Format(@"    Public Const LongText  As String   = """""));
            return;
            }
        line_list.Add(string.Format(@"    Public Const LongText  As String   = """""));
        int count = 2;
        foreach( string line in lines )
            {
            if( line.StartsWith("### ") )
                {
                --count;
                }
            if( count > 0 )
                {
                continue;
                }
            var text = line.Replace("\"", "\\\"");
            line_list.Add(string.Format(@"        + ""{0}"" + Environment.NewLine", text));
            }
        }

    private void write_licenseinfo_vb(string destination_path)
        {
        Log.LogMessage(MessageImportance.High, "Writing VisualBasic output!");
        var line_list = new List<string>{};

        line_list.Add(string.Format(@"' @file"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"' @author Auto generated source file"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"' @defgroup REF_License_{0} generated.License", ProjectNameClean));
        line_list.Add(string.Format(@"' @{{"));
        line_list.Add(string.Format(@"' @brief  Class with license information properties."));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"' @ingroup @REF_{0}", ProjectNameClean));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"' @}}"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"Imports System"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"Namespace Generated"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"' @ingroup REF_License_{0}", ProjectNameClean));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"' @class License"));
        line_list.Add(string.Format(@"'"));
        line_list.Add(string.Format(@"Public Module License"));
        line_list.Add(string.Format(@""));

        var lines = read_license_lines(ProjectDir);
        get_short_text_vb(line_list, lines);
        get_long_text_vb(line_list, lines);

        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"End Module"));
        line_list.Add(string.Format(@""));
        line_list.Add(string.Format(@"End Namespace"));

        create_file(destination_path + "/license.vb", line_list);
        }

    private static void write_licenseinfo_vb(LicenseInfoTask generator, string destination_path)
        {
        generator.write_licenseinfo_vb(destination_path);
        }

    private delegate void LicenseInfoWriter(LicenseInfoTask generator, string destination_path);
    private static Dictionary<eProjectType, LicenseInfoWriter> the_licenseinfo_writer_map = new Dictionary<eProjectType, LicenseInfoWriter>
        {
            {eProjectType.VisualBasic, write_licenseinfo_vb},
            {eProjectType.CSharp,      write_licenseinfo_cs},
        };

    public override bool Execute()
        {
        Log.LogMessage(MessageImportance.High, "Executing LicenseInfoTask!");

        Log.LogMessage(MessageImportance.High, "Project name:    {0}", ProjectName);
        Log.LogMessage(MessageImportance.High, "Project type:    {0}", ProjectType.ToString());

        string destination_path = ProjectDir + "/generated";
        create_directory(destination_path);

        if (the_licenseinfo_writer_map.ContainsKey(ProjectType))
            {
            the_licenseinfo_writer_map[ProjectType](this, destination_path);
            }

        return true;
        }
    }
}
