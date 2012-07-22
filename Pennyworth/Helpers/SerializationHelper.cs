using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;

namespace Pennyworth.Helpers {
	public enum SerializationType {
		Xml,
		Binary
	}

	public static class SerializationHelper {
		/// <summary>
		/// Serializes data of type <typeparamref name="T"/> to file at
		/// <paramref name="path"/> using <paramref name="type"/> formatter
		/// </summary>
		/// <typeparam name="T">type to serialize</typeparam>
		/// <param name="source">object graph</param>
		/// <param name="path">path to file to store the data</param>
		/// <param name="type">determines the formatter to use</param>
		/// <returns>
		/// <c>true </c>if <paramref name="source"/> has been persisted;
		/// <c>false</c> otherwise
		/// </returns>
		public static Boolean Serialize<T>(T source, String path, SerializationType type) where T : class {
			if (source == null || String.IsNullOrEmpty(path)) return false;

			using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write)) {
				Action<Stream, Object> writer = null;
				if (type == SerializationType.Xml) {
					writer = new DataContractSerializer(typeof(T)).WriteObject;
				} else if (type == SerializationType.Binary) {
					writer = new BinaryFormatter().Serialize;
				}

				if (writer != null) {
					try {
						writer(stream, source);
						return true;
					} catch (SerializationException ex) {
						Debug.Print("Serialization error: {0}", ex.Message);
					} catch (SecurityException ex) {
						Debug.Print("Permission error: {0}", ex.Message);
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Deserializes data of type <typeparamref name="T"/> from file at
		/// <paramref name="path"/> using <paramref name="type"/> formatter
		/// </summary>
		/// <typeparam name="T">type to deserialize</typeparam>
		/// <param name="path">path to file to store the data</param>
		/// <param name="type">determines the formatter to use</param>
		/// <returns>
		/// Object of <typeparamref name="T"/> on success; <c>null</c> otherwise
		/// </returns>
		public static T Deserialize<T>(String path, SerializationType type) where T : class {
			if (String.IsNullOrEmpty(path)) return null;

			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
				Func<Stream, Object> reader = null;
				if (type == SerializationType.Xml) {
					reader = new DataContractSerializer(typeof(T)).ReadObject;
				} else if (type == SerializationType.Binary) {
					reader = new BinaryFormatter().Deserialize;
				}

				if (reader != null) {
					try {
						return reader(stream) as T;
					} catch (ArgumentException ex) {
						Debug.Print("Deserialization error: {0}", ex.Message);
					} catch (InvalidOperationException ex) {
						Debug.Print("Deserialization error; wrong file? {0}", ex.Message);
					} catch (SecurityException ex) {
						Debug.Print("Permission error: {0}", ex.Message);
					} catch (SerializationException ex) {
						Debug.Print("Deserialization error; wrong format? {0}", ex.Message);
					}
				}
			}

			return null;
		}
	}
}
