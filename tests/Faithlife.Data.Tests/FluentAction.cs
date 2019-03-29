using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;

namespace Faithlife.Data.Tests
{
	public static class FluentAction
	{
		public static Action Invoking(Action action) => action;

		public static Func<T> Invoking<T>(Func<T> func) => func;

		public static Func<Task> Awaiting(Func<Task> action) => action;

		public static Func<Task<T>> Awaiting<T>(Func<Task<T>> func) => func;

		public static Action Enumerating(Func<IEnumerable> enumerable) => enumerable.Enumerating();

		public static Action Enumerating<T>(Func<IEnumerable<T>> enumerable) => enumerable.Enumerating();
	}
}
