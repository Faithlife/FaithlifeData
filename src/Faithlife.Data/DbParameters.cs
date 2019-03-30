using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Faithlife.Reflection;

namespace Faithlife.Data
{
	public sealed class DbParameters : IReadOnlyList<(string Name, object Value)>
	{
		/// <summary>
		/// Creates an empty list of parameters.
		/// </summary>
		public DbParameters() =>
			m_parameters = new List<(string Name, object Value)>();

		/// <summary>
		/// Creates a list of parameters.
		/// </summary>
		public DbParameters(IEnumerable<(string Name, object Value)> parameters) =>
			m_parameters = (parameters ?? throw new ArgumentNullException(nameof(parameters))).ToList();

		/// <summary>
		/// Creates a list of parameters.
		/// </summary>
		public static DbParameters Create(params (string Name, object Value)[] parameters) =>
			new DbParameters(parameters);

		/// <summary>
		/// Creates a list of parameters from the properties of a DTO.
		/// </summary>
		public static DbParameters FromDto(object dto) =>
			new DbParameters().AddDto(dto);

		/// <summary>
		/// The number of parameters.
		/// </summary>
		public int Count => m_parameters.Count;

		/// <summary>
		/// The parameter at the specified index.
		/// </summary>
		public (string Name, object Value) this[int index] => m_parameters[index];

		/// <summary>
		/// Adds a parameter.
		/// </summary>
		public DbParameters Add(string name, object value)
		{
			m_parameters.Add((name, value));
			return this;
		}

		/// <summary>
		/// Adds parameters.
		/// </summary>
		public DbParameters Add(params (string Name, object Value)[] parameters)
		{
			m_parameters.AddRange(parameters ?? throw new ArgumentNullException(nameof(parameters)));
			return this;
		}

		/// <summary>
		/// Adds the properties of a DTO as parameters.
		/// </summary>
		public DbParameters AddDto(object dto)
		{
			if (dto != null)
			{
				foreach (var property in DtoInfo.GetInfo(dto.GetType()).Properties)
					Add(property.Name, property.GetValue(dto));
			}
			return this;
		}

		/// <summary>
		/// Used to enumerate the parameters.
		/// </summary>
		public IEnumerator<(string Name, object Value)> GetEnumerator() => m_parameters.GetEnumerator();

		/// <summary>
		/// Used to enumerate the parameters.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() => m_parameters.GetEnumerator();

		private readonly List<(string Name, object Value)> m_parameters;
	}
}
