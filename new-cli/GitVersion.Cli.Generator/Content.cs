namespace GitVersion;

public static class Content
{
    /*language=cs*/
    private const string GeneratedHeader = """
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#nullable enable
""";

    /*language=cs*/
    public const string CommandImplContent = $$$"""
{{{GeneratedHeader}}}
using System.CommandLine;
using System.CommandLine.Binding;

using {{Model.CommandTypeNamespace}};
{{- if Model.SettingsTypeNamespace != Model.CommandTypeNamespace }}
using {{Model.SettingsTypeNamespace}};{{ end }}

namespace {{Namespace}};

public class {{Model.CommandTypeName}}Impl : Command, ICommandImpl
{
    public string CommandName => nameof({{Model.CommandTypeName}}Impl);
    {{- if (Model.ParentCommand | string.empty) }}
    public string ParentCommandName => string.Empty;
    {{- else }}
    public string ParentCommandName => nameof({{Model.ParentCommand}}Impl);
    {{ end }}
    {{- $settingsProperties = Model.SettingsProperties | array.sort "Name" }}
    // Options list
    {{~ for $prop in $settingsProperties ~}}
    protected readonly Option<{{$prop.TypeName}}> {{$prop.Name}}Option;
    {{~ end ~}}

    public {{Model.CommandTypeName}}Impl({{Model.CommandTypeName}} command)
        : base("{{Model.CommandName}}", "{{Model.CommandDescription}}")
    {
        {{~ for $prop in $settingsProperties ~}}
        {{$prop.Name}}Option = new Option<{{$prop.TypeName}}>("{{$prop.OptionName}}", [{{$prop.Aliases}}])
        {
            Required = {{$prop.Required}},
            Description = "{{$prop.Description}}",
        };
        {{~ end ~}}

        {{- for $prop in $settingsProperties ~}}
        Add({{$prop.Name}}Option);
        {{~ end ~}}
        
        this.SetAction(Run);
        return;

        Task<int> Run(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var settings = new {{Model.SettingsTypeName}}
            {
                {{~ for $prop in $settingsProperties ~}}
                {{$prop.Name}} = parseResult.GetValue({{$prop.Name}}Option){{ if $prop.Required }}!{{ end}},
                {{~ end ~}}
            };
            return command.InvokeAsync(settings, cancellationToken);
        }
    }
}
""";

    /*language=cs*/
    public const string RootCommandImplContent = $$$"""
{{{GeneratedHeader}}}
using System.CommandLine;
using {{InfraNamespaceName}};
namespace {{Namespace}};

public class RootCommandImpl : RootCommand
{
    public RootCommandImpl(IEnumerable<ICommandImpl> commands)
    {
        var map = commands.ToDictionary(c => c.CommandName);
        foreach (var command in map.Values)
        {
            AddCommand(command, map);
        }
    }
    private void AddCommand(ICommandImpl command, IDictionary<string, ICommandImpl> map)
    {
        if (!string.IsNullOrWhiteSpace(command.ParentCommandName))
        {
            var parent = map[command.ParentCommandName] as Command;
            parent?.Add((Command)command);
        }
        else
        {
            Add((Command)command);
        }
    }
}
""";

    /*language=cs*/
    public const string CommandsModuleContent = $$$"""
{{{GeneratedHeader}}}
using System.CommandLine;
using {{DependencyInjectionNamespaceName}};
using {{CommandNamespaceName}};
using {{InfraNamespaceName}};

namespace {{Namespace}};

public class CommandsImplModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        {{- $commands = Model | array.sort "CommandTypeName" }}
        services.AddSingleton<RootCommandImpl>();
        {{~ for $command in $commands ~}}
        services.AddSingleton<{{$command.CommandTypeName}}>();
        services.AddSingleton<ICommandImpl, {{$command.CommandTypeName}}Impl>();

        {{~ end ~}}
    }
}
""";
}
