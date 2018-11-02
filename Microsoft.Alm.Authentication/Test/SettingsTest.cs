using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Alm.Authentication.Test
{
    public class SettingsTest
    {
        private readonly ITestOutputHelper _output;

        public SettingsTest(ITestOutputHelper testOutputHelper)
        {
            _output = testOutputHelper;
        }

        [Fact]
        public void VerifyDeduplicateStringDictionaryStripsEntriesWithDuplicatedByCaseKeys()
        {
            var vars = new Dictionary<string,string>()
            {
                { "home", "value-1" },
                { "Home", "value-2" },
                { "HOme", "value-3" },
                { "HOMe", "value-4" },
                { "HOME", "value-5" },
                { "homE", "value-6" },
                { "away", "value-7" },
            };

            var settings = new Settings(RuntimeContext.Default);
            var result = settings.DeduplicateStringDictionary(vars);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("home", result.First().Key);
            Assert.Equal("value-6", result.First().Value);
            Assert.Equal("value-6", result["HOME"]);
            Assert.Equal("away", result.Last().Key);
            Assert.Equal("value-7", result.Last().Value);
            Assert.Equal("value-7", result["AwAy"]);

        }
    }
}