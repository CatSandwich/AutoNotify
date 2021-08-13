using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoNotify
{
    [Generator]
    class PropertyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            if (!Debugger.IsAttached) Debugger.Launch();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                var semanticModel = context.Compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                foreach (var @class in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var serializedProperties = 
                        // Foreach field
                        from field in @class.DescendantNodes().OfType<FieldDeclarationSyntax>()
                        // Get all attributes
                        let attributes = field.DescendantNodes().OfType<AttributeSyntax>()
                        // Find SerializedProperty attribute and get the Property name from argument list
                        let serializedProperty = attributes
                            .FirstOrDefault(att =>
                                semanticModel.GetTypeInfo(att.Name).Type.Name == nameof(AutoNotifyPropertyAttribute))
                            ?.ArgumentList.Arguments[0].ToFullString()
                        where serializedProperty is not null
                        // Store as tuple for later
                        select (field, name: serializedProperty.Substring(1, serializedProperty.Length - 2));
                    
                    // If no AutoProperties in this class, don't try to write code for it
                    if (!serializedProperties.Any()) continue;
                    
                    // Write source file
                    var hint = $"{@class.Identifier.ToString()}_SerializedProperties";
                    var source = "";
                    foreach (var (field, propertyName) in serializedProperties)
                    {
                        var fieldName = field.Declaration.Variables.ElementAt(0).ToFullString();
                        var fieldType = field.Declaration.Type.ToString();
                        source += GenProperty(fieldType, propertyName, fieldName);
                    }

                    source += "public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;\n" +
                    "protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)\n" +
                    "{\n" + 
                    "    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));\n" + 
                    "}\n";
                    source = source.TagClass(@class.Modifiers.Select(modifier => modifier.Text), @class.Identifier.Text, "System.ComponentModel.INotifyPropertyChanged");
                    source = source.TagNamespace(semanticModel.GetDeclaredSymbol(@class)?.ContainingNamespace.Name);
                    context.AddSource(hint, source);
                }
            }
        }
        
        private static string GenProperty(string type, string name, string fieldName)
        {
            return $"public {type} {name}\n{{\n" +
                   $"    get => {fieldName};\n" +
                    "    set\n" +
                    "    {\n" +
                   $"        {fieldName} = value;\n" +
                    "        OnPropertyChanged();\n" +
                    "    }\n" + 
                    "}\n";
        }
    }

    static class StringExtensions
    {
        internal static string TagNamespace(this string str, string ns)
        {
            if (ns is null) return str;
            return $"namespace {ns}\n{{\n{str.Indent()}\n}}";
        }

        internal static string Indent(this string str) 
            => string.Join("\n", str.Split('\n').Select(line => $"\t{line}"));

        internal static string TagClass(this string str, IEnumerable<string> modifiers, string name, params string[] parents)
        {
            var ret = $"{string.Join(" ", modifiers)} class {name}";
            if (parents.Any()) ret += " : " + string.Join(", ", parents);
            ret += "\n{\n";
            ret += str.Indent();
            ret += "\n}\n";
            return ret;
        }
    }
}
