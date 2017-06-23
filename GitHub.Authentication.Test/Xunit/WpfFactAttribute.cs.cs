// Borrowed from: https://github.com/xunit/samples.xunit/blob/master/STAExamples/WpfFactAttribute.cs

using System;
using Xunit;
using Xunit.Sdk;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Xunit.WpfFactDiscoverer", "GitHub.Authentication.Test")]
public class WpfFactAttribute : FactAttribute { }
