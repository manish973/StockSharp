namespace StockSharp.Algo.Storages
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// The interface for serialize snapshots.
	/// </summary>
	/// <typeparam name="TMessage">Message type.</typeparam>
	public interface ISnapshotSerializer<TMessage>
		where TMessage : Message
	{
		/// <summary>
		/// Data type info.
		/// </summary>
		DataType DataType { get; }

		/// <summary>
		/// Version of data format.
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// File name.
		/// </summary>
		string FileName { get; }

		/// <summary>
		/// Get snapshot size in bytes.
		/// </summary>
		/// <param name="version">Version of data format.</param>
		/// <returns>Snapshot size in bytes.</returns>
		int GetSnapshotSize(Version version);

		/// <summary>
		/// Serialize the specified message to byte array.
		/// </summary>
		/// <param name="version">Version of data format.</param>
		/// <param name="message">Message.</param>
		/// <param name="buffer">Byte array.</param>
		void Serialize(Version version, TMessage message, byte[] buffer);

		/// <summary>
		/// Deserialize message from byte array.
		/// </summary>
		/// <param name="version">Version of data format.</param>
		/// <param name="buffer">Byte array.</param>
		/// <returns>Message.</returns>
		TMessage Deserialize(Version version, byte[] buffer);

		/// <summary>
		/// Get security identifier for the specified message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Security ID.</returns>
		SecurityId GetSecurityId(TMessage message);

		/// <summary>
		/// Update the specified message by new changes.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="changes">Changes.</param>
		void Update(TMessage message, TMessage changes);
	}
}