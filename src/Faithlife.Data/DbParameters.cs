using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Faithlife.Reflection;

namespace Faithlife.Data
{
	/// <summary>
	/// An immutable list of parameters.
	/// </summary>
	public readonly struct DbParameters : IReadOnlyList<(string Name, object? Value)>
	{
		/// <summary>
		/// An empty list of parameters.
		/// </summary>
		public static readonly DbParameters Empty = default;

		/// <summary>
		/// Creates a list of parameters with one parameter.
		/// </summary>
		public static DbParameters Create(string name, object? value) =>
			new DbParameters(new[] { (name, value) });

		/// <summary>
		/// Creates a list of parameters from tuples.
		/// </summary>
		public static DbParameters Create(params (string Name, object? Value)[] parameters) =>
			new DbParameters(parameters ?? throw new ArgumentNullException(nameof(parameters)));

		/// <summary>
		/// Creates a list of parameters from a sequence of tuples.
		/// </summary>
		public static DbParameters Create(IEnumerable<(string Name, object? Value)> parameters) =>
			new DbParameters(parameters ?? throw new ArgumentNullException(nameof(parameters)));

		/// <summary>
		/// Creates a list of parameters from a sequence of tuples.
		/// </summary>
		public static DbParameters Create<T>(IEnumerable<(string Name, T Value)> parameters) =>
			new DbParameters((parameters ?? throw new ArgumentNullException(nameof(parameters))).Select(x => (x.Name, (object?) x.Value)));

		/// <summary>
		/// Creates a list of parameters from a dictionary.
		/// </summary>
		public static DbParameters Create<T>(IEnumerable<KeyValuePair<string, T>> parameters) =>
			new DbParameters((parameters ?? throw new ArgumentNullException(nameof(parameters))).Select(x => (x.Key, (object?) x.Value)));

		/// <summary>
		/// Creates a list of parameters from the properties of a DTO.
		/// </summary>
		public static DbParameters FromDto(object dto) =>
			new DbParameters(DtoInfo.GetInfo((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Properties.Select(x => (x.Name, x.GetValue(dto))));

		/// <summary>
		/// The number of parameters.
		/// </summary>
		public int Count => Parameters.Count;

		/// <summary>
		/// The parameter at the specified index.
		/// </summary>
		public (string Name, object? Value) this[int index] => Parameters[index];

		/// <summary>
		/// Adds a parameter.
		/// </summary>
		public DbParameters Add(string name, object? value) => new DbParameters(Parameters.Append((name, value)));

		/// <summary>
		/// Adds parameters from another instance.
		/// </summary>
		public DbParameters Add(DbParameters parameters) => new DbParameters(Parameters.Concat(parameters));

		/// <summary>
		/// Adds parameters from tuples.
		/// </summary>
		public DbParameters Add(params (string Name, object? Value)[] parameters) => Add(Create(parameters));

		/// <summary>
		/// Adds parameters from a sequence of tuples.
		/// </summary>
		public DbParameters Add(IEnumerable<(string Name, object? Value)> parameters) => Add(Create(parameters));

		/// <summary>
		/// Adds parameters from a sequence of tuples.
		/// </summary>
		public DbParameters Add<T>(IEnumerable<(string Name, T Value)> parameters) => Add(Create(parameters));

		/// <summary>
		/// Adds parameters from a dictionary.
		/// </summary>
		public DbParameters Add<T>(IEnumerable<KeyValuePair<string, T>> parameters) => Add(Create(parameters));

		/// <summary>
		/// Adds parameters from the properties of a DTO.
		/// </summary>
		public DbParameters AddDto(object dto) => Add(FromDto(dto));

		/// <summary>
		/// Creates a dictionary of parameters.
		/// </summary>
		public Dictionary<string, object?> ToDictionary()
		{
			var dictionary = new Dictionary<string, object?>();
			foreach (var parameter in Parameters)
				dictionary[parameter.Name] = parameter.Value;
			return dictionary;
		}

		/// <summary>
		/// Used to enumerate the parameters.
		/// </summary>
		public IEnumerator<(string Name, object? Value)> GetEnumerator() => Parameters.GetEnumerator();

		/// <summary>
		/// Used to enumerate the parameters.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() => Parameters.GetEnumerator();

		private DbParameters(IEnumerable<(string Name, object? Value)> parameters) => m_parameters = parameters.ToList();

		private IReadOnlyList<(string Name, object? Value)> Parameters => m_parameters ?? Array.Empty<(string Name, object? Value)>();

		private readonly IReadOnlyList<(string Name, object? Value)> m_parameters;
	}
}
