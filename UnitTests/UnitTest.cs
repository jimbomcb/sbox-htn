global using Microsoft.VisualStudio.TestTools.UnitTesting;

using HTN.Tests;

[TestClass]
public class TestInit
{
	[AssemblyInitialize]
	public static void ClassInitialize( TestContext context )
	{
		Sandbox.Application.InitUnitTest<TaskTests>();
	}
}
