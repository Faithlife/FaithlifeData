using System;
using FluentAssertions;
using NUnit.Framework;
using static Faithlife.Data.Tests.FluentAction;

namespace Faithlife.Data.Tests
{
	[TestFixture]
	public class DbConnectorTests
	{
		[Test]
		public void NullConnection()
		{
			Invoking(() => DbConnector.Create(null)).Should().Throw<ArgumentNullException>();
		}
	}
}
