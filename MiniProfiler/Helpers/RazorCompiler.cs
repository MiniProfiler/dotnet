using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web.Razor;
using System.Web.Razor.Parser;
using System.Web.Razor.Generator;
using System.CodeDom.Compiler;
using System.Runtime;
using Microsoft.CSharp;
using System.Diagnostics;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Concurrent;

namespace StackExchange.MvcMiniProfiler.Helpers
{
    public class RazorCompiler
    {

        public abstract class TemplateBase<T>
        {
            public class DummyHtmlHelper
            {
                StringBuilder sb;
                public DummyHtmlHelper(StringBuilder sb)
                {
                    this.sb = sb;
                }

                public IHtmlString Raw(object o)
                {
                    if (o == null) return null;
                    return MvcHtmlString.Create(o.ToString());
                }
            }

            DummyHtmlHelper helper;
            public DummyHtmlHelper Html
            {
                get 
                {
                    if (helper!= null) return helper;
                    helper = new DummyHtmlHelper(Builder);
                    return helper;
                } 
            }

            public T Model { get; set; }

            protected TemplateBase()
            {
                Builder = new StringBuilder();
            }
            public StringBuilder Builder { get; private set; }
            public string Result { get { return Builder.ToString(); } }
            public void Clear()
            {
                Builder.Clear();
            }
            public virtual void Execute() { }

            public void Write(object @object)
            {
                if (@object == null)
                    return;

                Builder.Append(@object);
            }

            public void WriteLiteral(string @string)
            {
                if (@string == null)
                    return;

                Builder.Append(@string);
            }

            public static void WriteLiteralTo(TextWriter writer, string literal)
            {
                if (literal == null)
                    return;

                writer.Write(literal);
            }


            public static void WriteTo(TextWriter writer, object obj)
            {
                if (obj == null)
                    return;

                writer.Write(obj);
            }
        }

        static ConcurrentDictionary<Tuple<string, Type>, Type> cache = new ConcurrentDictionary<Tuple<string, Type>, Type>(); 

        public static string Render<T>(string template, T model)
        {
            var key = Tuple.Create(template,typeof(T));
            Type type; 
            if (!cache.TryGetValue(key, out type))
            {
                type = GetCompiledType<T>(template);
                cache[key] = type;
            }
            
            var instance = (TemplateBase<T>)Activator.CreateInstance(type);
            instance.Model = model;
            instance.Execute();

            return(instance.Result);
        }

        private static Type GetCompiledType<T>(string template)
        {
            var parser = new HtmlMarkupParser();

            var baseType = typeof(TemplateBase<>).MakeGenericType(typeof(T));

            // @model is inferred from T ... so strip it
            var regex = new Regex("@model.*");
            template = regex.Replace(template, "");

            var host = new RazorEngineHost(new System.Web.Razor.CSharpRazorCodeLanguage(), () => parser)
            {
                DefaultBaseClass = baseType.FullName,
                DefaultClassName = "TestClass",
                DefaultNamespace = "CompiledRazorTemplates.Dynamic",
                GeneratedClassContext = new GeneratedClassContext("Execute", "Write", "WriteLiteral",
                                                                  "WriteTo", "WriteLiteralTo",
                                                                  "RazorEngine.Templating.TemplateWriter")
            };

            var engine = new RazorTemplateEngine(host);
            GeneratorResults result;
            using (var reader = new StringReader(template))
            {
                result = engine.GenerateCode(reader);
            }

            var code = result.GeneratedCode;

            var @params = new CompilerParameters
            {
#if DEBUG
                GenerateInMemory = true,
                GenerateExecutable = false,
                IncludeDebugInformation = true,
#else 
                GenerateInMemory = true,
                GenerateExecutable = false,
                IncludeDebugInformation = false,
#endif
                CompilerOptions = "/target:library /optimize"
            };

            var assemblies = AppDomain.CurrentDomain
               .GetAssemblies()
               .Where(a => !a.IsDynamic)
               .Select(a => a.Location)
               .ToArray();

            @params.ReferencedAssemblies.AddRange(assemblies);

            var provider = new CSharpCodeProvider();
            var compiled = provider.CompileAssemblyFromDom(@params, code);

            if (compiled.Errors.Count > 0)
            {
                foreach (var error in compiled.Errors)
                {
                    Trace.WriteLine(error);
                }
            }

            var type = compiled.CompiledAssembly.GetType("CompiledRazorTemplates.Dynamic.TestClass");
            return type;
        }
    }
}
