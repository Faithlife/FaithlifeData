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
	public struct DbParameters : IReadOnlyList<(string Name, object Value)>
	{
		/// <summary>
		/// An empty list of parameters.
		/// </summary>
		public static readonly DbParameters Empty = new DbParameters();

		/// <summary>
		/// Creates a list of parameters.
		/// </summary>
		public static DbParameters Create(params (string Name, object Value)[] parameters) => new DbParameters(parameters);

		/// <summary>
		/// Creates a list of parameters.
		/// </summary>
		public static DbParameters Create(IEnumerable<(string Name, object Value)> parameters) => new DbParameters(parameters);

		/// <summary>
		/// Creates a list of parameters from the properties of a DTO.
		/// </summary>
		public static DbParameters FromDto(object dto) => new DbParameters(DtoInfo.GetInfo(dto.GetType()).Properties.Select(x => (x.Name, x.GetValue(dto))));

		/// <summary>
		/// The number of parameters.
		/// </summary>
		public int Count => Parameters.Count;

		/// <summary>
		/// The parameter at the specified index.
		/// </summary>
		public (string Name, object Value) this[int index] => Parameters[index];

		/// <summary>
		/// Adds a parameter.
		/// </summary>
		public DbParameters Add(string name, object value) => new DbParameters(Parameters.Append((name, value)));

		/// <summary>
		/// Adds parameters.
		/// </summary>
		public DbParameters Add(params (string Name, object Value)[] parameters) => new DbParameters(Parameters.Concat(parameters));

		/// <summary>
		/// Adds parameters.
		/// </summary>
		public DbParameters Add(IEnumerable<(string Name, object Value)> parameters) => new DbParameters(Parameters.Concat(parameters));

		/// <summary>
		/// Adds the properties of a DTO as parameters.
		/// </summary>
		/// <remarks>Adds no parameters if <paramref name="dto"/> is <c>null</c>.</remarks>
		public DbParameters AddDto(object dto) => Add(FromDto(dto).Parameters);

		/// <summary>
		/// Used to enumerate the parameters.
		/// </summary>
		public IEnumerator<(string Name, object Value)> GetEnumerator() => Parameters.GetEnumerator();

		/// <summary>
		/// Used to enumerate the parameters.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() => Parameters.GetEnumerator();

		private DbParameters(IEnumerable<(string Name, object Value)> parameters) =>
			m_parameters = (parameters ?? throw new ArgumentNullException(nameof(parameters))).ToList();

		private IReadOnlyList<(string Name, object Value)> Parameters => m_parameters ?? Array.Empty<(string Name, object Value)>();

		private readonly IReadOnlyList<(string Name, object Value)> m_parameters;
	}
}
