// Borrowed from: https://github.com/xunit/samples.xunit/blob/master/STAExamples/WpfTheoryAttribute.cs

using System;
using Xunit;
using Xunit.Sdk;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Xunit.WpfTheoryDiscoverer", "GitHub.Authentication.Test")]
public class WpfTheoryAttribute : TheoryAttribute { }