using System;
using System.Collections.Generic;
using StackExchange.Profiling.Internal;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    public class RenderTests : BaseTest
    {
        public RenderTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void DefaultRender()
        {
            var profiler = GetBasicProfiler();
            var renderOptions = new RenderOptions();
            var result = Render.Includes(profiler, "/", true, renderOptions, new List<Guid>() { profiler.Id });
            Output.WriteLine("Result: " + result);

            Assert.NotNull(result);
            Assert.Contains("id=\"mini-profiler\"", result);

            var expected = $@"<script async id=""mini-profiler"" src=""/includes.min.js?v={Options.VersionHash}"" data-version=""{Options.VersionHash}"" data-path=""/"" data-current-id=""{profiler.Id}"" data-ids=""{profiler.Id}"" data-position=""Left"""" data-scheme=""Light"" data-authorized=""true"" data-max-traces=""15"" data-toggle-shortcut=""Alt+P"" data-trivial-milliseconds=""2.0"" data-ignored-duplicate-execute-types=""Open,OpenAsync,Close,CloseAsync""></script>";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void OptionsSet()
        {
            var profiler = GetBasicProfiler();
            var renderOptions = new RenderOptions()
            {
                ColorScheme = ColorScheme.Auto,
                MaxTracesToShow = 12,
                Nonce = "myNonce",
                PopupToggleKeyboardShortcut = "Alt+Q",
                Position = RenderPosition.Right,
                ShowControls = true,
                ShowTimeWithChildren = true,
                ShowTrivial = true,
                StartHidden = true,
                TrivialDurationThresholdMilliseconds = 23
            };
            var result = Render.Includes(profiler, "/", true, renderOptions, new List<Guid>() { profiler.Id });
            Output.WriteLine("Result: " + result);

            Assert.NotNull(result);
            Assert.Contains("id=\"mini-profiler\"", result);

            var expected = $@"<script async id=""mini-profiler"" src=""/includes.min.js?v={Options.VersionHash}"" data-version=""{Options.VersionHash}"" data-path=""/"" data-current-id=""{profiler.Id}"" data-ids=""{profiler.Id}"" data-position=""Right"""" data-scheme=""Auto"" data-authorized=""true"" data-trivial=""true"" data-children=""true"" data-controls=""true"" data-start-hidden=""true"" nonce=""myNonce"" data-max-traces=""12"" data-toggle-shortcut=""Alt+Q"" data-trivial-milliseconds=""23"" data-ignored-duplicate-execute-types=""Open,OpenAsync,Close,CloseAsync""></script>";
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, @"data-scheme=""Light""")]
        [InlineData(ColorScheme.Auto, @"data-scheme=""Auto""")]
        [InlineData(ColorScheme.Dark, @"data-scheme=""Dark""")]
        [InlineData(ColorScheme.Light, @"data-scheme=""Light""")]
        public void ColorSchemes(ColorScheme? scheme, string expected)
        {
            var profiler = GetBasicProfiler();
            var renderOptions = new RenderOptions() { ColorScheme = scheme };

            var result = Render.Includes(profiler, " / ", true, renderOptions);
            Output.WriteLine("Result: " + result);

            Assert.NotNull(result);
            Assert.Contains(expected, result);
        }

        [Theory]
        [InlineData(null, @"data-position=""Left""")]
        [InlineData(RenderPosition.BottomLeft, @"data-position=""BottomLeft""")]
        [InlineData(RenderPosition.BottomRight, @"data-position=""BottomRight""")]
        [InlineData(RenderPosition.Left, @"data-position=""Left""")]
        [InlineData(RenderPosition.Right, @"data-position=""Right""")]
        public void Positions(RenderPosition? position, string expected)
        {
            var profiler = GetBasicProfiler();
            var renderOptions = new RenderOptions() { Position = position };

            var result = Render.Includes(profiler, " / ", true, renderOptions);
            Output.WriteLine("Result: " + result);

            Assert.NotNull(result);
            Assert.Contains(expected, result);
        }

        [Fact]
        public void Nonce()
        {
            var profiler = GetBasicProfiler();
            var renderOptions = new RenderOptions();

            // Default
            var result = Render.Includes(profiler, "/", true, renderOptions);
            Output.WriteLine("Result: " + result);
            Assert.DoesNotContain("nonce", result);

            // With nonce
            var nonce = Guid.NewGuid().ToString();
            renderOptions.Nonce = nonce;
            result = Render.Includes(profiler, "/", true, renderOptions);
            Assert.Contains($@"nonce=""{nonce}""", result);
        }

        [Theory]
        [InlineData("foo", @"nonce=""foo""")]
        [InlineData("foo!@#$%", @"nonce=""foo!@#$%""")]
        [InlineData("e31df82b-5102-4134-af97-f29bf724bedd", @"nonce=""e31df82b-5102-4134-af97-f29bf724bedd""")]
        [InlineData("f\"oo", @"nonce=""f&quot;oo""")]
        [InlineData("󆲢L軾󯮃򮬛ŝ󅫤򄷌򆰃񟕺􆷀;鮡ƾ󤕵ԁf\'\"&23", @"nonce=""󆲢L軾󯮃򮬛ŝ󅫤򄷌򆰃񟕺􆷀;鮡ƾ󤕵ԁf&#39;&quot;&amp;23""")]
        public void NonceEncoding(string nonce, string expected)
        {
            var profiler = GetBasicProfiler();
            var renderOptions = new RenderOptions() { Nonce = nonce };

            var result = Render.Includes(profiler, "/", true, renderOptions);
            Assert.Contains(expected, result);
        }
    }
}
