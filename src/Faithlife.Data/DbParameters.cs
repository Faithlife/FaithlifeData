using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		[SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Intentional API.")]
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
		/// Creates a list of parameters from a single name and a collection of values.
		/// </summary>
		/// <remarks>The name of each parameter is <c>name_index</c>, where <c>name</c> is as specified and <c>index</c>
		/// is the zero-based index of the value.</remarks>
		public static DbParameters FromMany(string name, IEnumerable values)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			var index = 0;
			var parameters = new List<(string, object?)>();
			foreach (var value in values ?? throw new ArgumentNullException(nameof(values)))
				parameters.Add(($"{name}_{index++}", value));
			return new DbParameters(parameters);
		}

		/// <summary>
		/// Creates a list of parameters from a collection of values.
		/// </summary>
		/// <remarks>The name of each parameter is determined by calling the specified function with the zero-based index of the value.</remarks>
		public static DbParameters FromMany(Func<int, string> name, IEnumerable values)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			var index = 0;
			var parameters = new List<(string, object?)>();
			foreach (var value in values ?? throw new ArgumentNullException(nameof(values)))
				parameters.Add((name(index++), value));
			return new DbParameters(parameters);
		}

		/// <summary>
		/// Creates a list of parameters from the properties of a DTO.
		/// </summary>
		/// <remarks>The name of each parameter is the name of the corresponding DTO property.</remarks>
		public static DbParameters FromDto(object dto) =>
			new DbParameters(DtoInfo.GetInfo((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Properties.Select(x => (x.Name, x.GetValue(dto))));

		/// <summary>
		/// Creates a list of parameters from the properties of a DTO.
		/// </summary>
		/// <remarks>The name of each parameter is <c>name_prop</c>, where <c>name</c> is as specified and <c>prop</c> is the
		/// name of the corresponding DTO property.</remarks>
		public static DbParameters FromDto(string name, object dto)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			return new DbParameters(DtoInfo.GetInfo((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Properties.Select(x => ($"{name}_{x.Name}", x.GetValue(dto))));
		}

		/// <summary>
		/// Creates a list of parameters from the properties of a DTO.
		/// </summary>
		/// <remarks>The name of each parameter is determined by calling the function with the name of the corresponding DTO property.</remarks>
		public static DbParameters FromDto(Func<string, string> name, object dto)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			return new DbParameters(DtoInfo.GetInfo((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Properties.Select(x => (name(x.Name), x.GetValue(dto))));
		}

		/// <summary>
		/// Creates a list of parameters from the properties of a DTO whose names match the specified filter.
		/// </summary>
		/// <remarks>The name of each parameter is the name of the corresponding DTO property.</remarks>
		public static DbParameters FromDtoWhere(object dto, Func<string, bool> filter) =>
			new DbParameters(DtoInfo.GetInfo((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Properties.Where(x => filter(x.Name)).Select(x => (x.Name, x.GetValue(dto))));

		/// <summary>
		/// Creates a list of parameters from the properties of a DTO whose names match the specified filter.
		/// </summary>
		/// <remarks>The name of each parameter is <c>name_prop</c>, where <c>name</c> is as specified and <c>prop</c> is the
		/// name of the corresponding DTO property.</remarks>
		public static DbParameters FromDtoWhere(string name, object dto, Func<string, bool> filter)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			return new DbParameters(DtoInfo.GetInfo((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Properties.Where(x => filter(x.Name)).Select(x => ($"{name}_{x.Name}", x.GetValue(dto))));
		}

		/// <summary>
		/// Creates a list of parameters from the properties of a DTO whose names match the specified filter.
		/// </summary>
		/// <remarks>The name of each parameter is determined by calling the function with the name of the corresponding DTO property.</remarks>
		public static DbParameters FromDtoWhere(Func<string, string> name, object dto, Func<string, bool> filter)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			return new DbParameters(DtoInfo.GetInfo((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Properties.Where(x => filter(x.Name)).Select(x => (name(x.Name), x.GetValue(dto))));
		}

		/// <summary>
		/// Creates a list of parameters from the collective properties of a sequence of DTOs.
		/// </summary>
		/// <remarks>The name of each parameter is <c>prop_index</c>, where <c>prop</c> is the name of the corresponding DTO property
		/// and <c>index</c> is the zero-based index of the DTO.</remarks>
		public static DbParameters FromDtos(IEnumerable dtos)
		{
			var index = 0;
			var parameters = new List<(string, object?)>();
			foreach (var dto in dtos ?? throw new ArgumentNullException(nameof(dtos)))
			{
				parameters.AddRange(DtoInfo.GetInfo((dto ?? throw new ArgumentException("DTO is null.", nameof(dtos))).GetType()).Properties.Select(x => ($"{x.Name}_{index}", x.GetValue(dto))));
				index++;
			}
			return new DbParameters(parameters);
		}

		/// <summary>
		/// Creates a list of parameters from the collective properties of a sequence of DTOs.
		/// </summary>
		/// <remarks>The name of each parameter is <c>name_prop_index</c>, where <c>name</c> is as specified and <c>prop</c> is the name
		/// of the corresponding DTO property and <c>index</c> is the zero-based index of the DTO.</remarks>
		public static DbParameters FromDtos(string name, IEnumerable dtos)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			var index = 0;
			var parameters = new List<(string, object?)>();
			foreach (var dto in dtos ?? throw new ArgumentNullException(nameof(dtos)))
			{
				parameters.AddRange(DtoInfo.GetInfo((dto ?? throw new ArgumentException("DTO is null.", nameof(dtos))).GetType()).Properties.Select(x => ($"{name}_{x.Name}_{index}", x.GetValue(dto))));
				index++;
			}
			return new DbParameters(parameters);
		}

		/// <summary>
		/// Creates a list of parameters from the collective properties of a sequence of DTOs.
		/// </summary>
		/// <remarks>The name of each parameter is determined by calling the specified function with the name of the corresponding DTO property
		/// and the zero-based index of the DTO.</remarks>
		public static DbParameters FromDtos(Func<string, int, string> name, IEnumerable dtos)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			var index = 0;
			var parameters = new List<(string, object?)>();
			foreach (object dto in dtos ?? throw new ArgumentNullException(nameof(dtos)))
			{
				parameters.AddRange(DtoInfo.GetInfo((dto ?? throw new ArgumentException("DTO is null.", nameof(dtos))).GetType()).Properties.Select(x => (name(x.Name, index), x.GetValue(dto))));
				index++;
			}
			return new DbParameters(parameters);
		}

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
		/// Adds parameters from a single name and a collection of values.
		/// </summary>
		/// <remarks>The name of each parameter is <c>name_index</c>, where <c>name</c> is as specified and <c>index</c>
		/// is the zero-based index of the value.</remarks>
		public DbParameters AddMany(string name, IEnumerable values) => Add(FromMany(name, values));

		/// <summary>
		/// Adds parameters from a collection of values.
		/// </summary>
		/// <remarks>The name of each parameter is determined by calling the specified function with the zero-based index of the value.</remarks>
		public DbParameters AddMany(Func<int, string> name, IEnumerable values) => Add(FromMany(name, values));

		/// <summary>
		/// Adds parameters from the properties of a DTO.
		/// </summary>
		/// <remarks>The name of each parameter is the name of the corresponding DTO property.</remarks>
		public DbParameters AddDto(object dto) => Add(FromDto(dto));

		/// <summary>
		/// Adds parameters from the properties of a DTO.
		/// </summary>
		/// <remarks>The name of each parameter is <c>name_prop</c>, where <c>name</c> is as specified and <c>prop</c> is the
		/// name of the corresponding DTO property.</remarks>
		public DbParameters AddDto(string name, object dto) => Add(FromDto(name, dto));

		/// <summary>
		/// Adds parameters from the properties of a DTO.
		/// </summary>
		/// <remarks>The name of each parameter is determined by calling the function with the name of the corresponding DTO property.</remarks>
		public DbParameters AddDto(Func<string, string> name, object dto) => Add(FromDto(name, dto));

		/// <summary>
		/// Adds parameters from the properties of a DTO whose names match the specified filter.
		/// </summary>
		/// <remarks>The name of each parameter is the name of the corresponding DTO property.</remarks>
		public DbParameters AddDtoWhere(object dto, Func<string, bool> filter) => Add(FromDtoWhere(dto, filter));

		/// <summary>
		/// Adds parameters from the properties of a DTO whose names match the specified filter.
		/// </summary>
		/// <remarks>The name of each parameter is <c>name_prop</c>, where <c>name</c> is as specified and <c>prop</c> is the
		/// name of the corresponding DTO property.</remarks>
		public DbParameters AddDtoWhere(string name, object dto, Func<string, bool> filter) => Add(FromDtoWhere(name, dto, filter));

		/// <summary>
		/// Adds parameters from the properties of a DTO whose names match the specified filter.
		/// </summary>
		/// <remarks>The name of each parameter is determined by calling the function with the name of the corresponding DTO property.</remarks>
		public DbParameters AddDtoWhere(Func<string, string> name, object dto, Func<string, bool> filter) => Add(FromDtoWhere(name, dto, filter));

		/// <summary>
		/// Adds parameters from the collective properties of a sequence of DTOs.
		/// </summary>
		/// <remarks>The name of each parameter is <c>prop_index</c>, where <c>prop</c> is the name of the corresponding DTO property
		/// and <c>index</c> is the zero-based index of the DTO.</remarks>
		public DbParameters AddDtos(IEnumerable dtos) => Add(FromDtos(dtos));

		/// <summary>
		/// Adds parameters from the collective properties of a sequence of DTOs.
		/// </summary>
		/// <remarks>The name of each parameter is <c>name_prop_index</c>, where <c>name</c> is as specified and <c>prop</c> is the name
		/// of the corresponding DTO property and <c>index</c> is the zero-based index of the DTO.</remarks>
		public DbParameters AddDtos(string name, IEnumerable dtos) => Add(FromDtos(name, dtos));

		/// <summary>
		/// Adds parameters from the collective properties of a sequence of DTOs.
		/// </summary>
		/// <remarks>The name of each parameter is determined by calling the specified function with the name of the corresponding DTO property
		/// and the zero-based index of the DTO.</remarks>
		public DbParameters AddDtos(Func<string, int, string> name, IEnumerable dtos) => Add(FromDtos(name, dtos));

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

		private readonly IReadOnlyList<(string Name, object? Value)>? m_parameters;
	}
}
