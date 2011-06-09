using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using Microsoft.CSharp;

#pragma warning disable 1591 // xml doc comments warnings

namespace MvcMiniProfiler.Helpers
{
    public class RazorCompiler
    {
        // begged borrowed and stole from http://razorengine.codeplex.com/

        public class TemplateWriter
        {
            private readonly Action<TextWriter> writerDelegate;

            public TemplateWriter(Action<TextWriter> writer)
            {
                if (writer == null)
                    throw new ArgumentNullException("writer");

                writerDelegate = writer;
            }

            public override string ToString()
            {
                using (var writer = new StringWriter())
                {
                    writerDelegate(writer);
                    return writer.ToString();
                }
            }

        }

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
                    return new HtmlString(o.ToString());
                }
            }

            DummyHtmlHelper helper;
            public DummyHtmlHelper Html
            {
                get
                {
                    if (helper != null) return helper;
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
            var key = Tuple.Create(template, typeof(T));
            Type type;
            if (!cache.TryGetValue(key, out type))
            {
                type = GetCompiledType<T>(template);
                cache[key] = type;
            }

            var instance = (TemplateBase<T>)Activator.CreateInstance(type);
            instance.Model = model;
            instance.Execute();

            return (instance.Result);
        }

        private static Type GetCompiledType<T>(string template)
        {
            var key = "C" + Guid.NewGuid().ToString("N");

            var parser = new HtmlMarkupParser();

            var baseType = typeof(TemplateBase<>).MakeGenericType(typeof(T));

            // @model is inferred from T ... so strip it
            var regex = new Regex("@model.*");
            template = regex.Replace(template, "");

            var host = new RazorEngineHost(new System.Web.Razor.CSharpRazorCodeLanguage(), () => parser)
            {
                DefaultBaseClass = baseType.FullName,
                DefaultClassName = key,
                DefaultNamespace = "CompiledRazorTemplates.Dynamic",
                GeneratedClassContext = new GeneratedClassContext("Execute", "Write", "WriteLiteral",
                                                                  "WriteTo", "WriteLiteralTo",
                                                                  "MvcMiniProfiler.Helpers.RazorCompiler.TemplateWriter")
            };

            CodeCompileUnit code;
            using (var reader = new StringReader(template))
            {
                code = new RazorTemplateEngine(host).GenerateCode(reader).GeneratedCode;
            }

            var outputAssembly = Path.Combine(AppDomain.CurrentDomain.DynamicDirectory, "MvcMiniProfiler.RazorViews.dll");

            var @params = new CompilerParameters
            {
                IncludeDebugInformation = false,
                TempFiles = new TempFileCollection(AppDomain.CurrentDomain.DynamicDirectory),
                CompilerOptions = "/target:library /optimize",
                OutputAssembly = outputAssembly
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
                var compileErrors = string.Join("\r\n", compiled.Errors.Cast<object>().Select(o => o.ToString()));
                throw new ApplicationException("Failed to compile Razor:" + compileErrors);
            }

            var assembly = Assembly.Load(File.ReadAllBytes(outputAssembly));
            var type = assembly.GetType("CompiledRazorTemplates.Dynamic." + key);
            File.Delete(outputAssembly);

            return type;
        }
    }
}

#pragma warning restore 1591 // xml doc comments warnings