using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats.Attributes;

namespace Viewer.Data.SQLite
{
    /// <summary>
    /// This service provides access to the table of attributes.
    /// </summary>
    public interface IAttributes : IDisposable
    {
        /// <summary>
        /// Add a new attribute to the file <paramref name="file"/>
        /// </summary>
        /// <param name="attribute">Added attribute</param>
        /// <param name="file">ID of a file</param>
        void Insert(Attribute attribute, long file);
        
        /// <summary>
        /// Set thumbnail attribute of <paramref name="file"/>
        /// </summary>
        /// <param name="thumbnail">Encoded thumbnail of <paramref name="file"/></param>
        /// <param name="file">ID of a file</param>
        void SetThumbnail(byte[] thumbnail, long file);
    }

    public class Attributes : IAttributes
    {
        private readonly SQLiteConnection _connection;
        private readonly InsertAttributeCommand _insertAttribute;
        private readonly UpdateThumbnailCommand _updateThumbnail;

        public Attributes(SQLiteConnection connection)
        {
            _connection = connection;
            _insertAttribute = new InsertAttributeCommand(_connection);
            _updateThumbnail = new UpdateThumbnailCommand(_connection);
        }

        private class InsertAttributeCommand : IValueVisitor, IDisposable
        {
            private readonly SQLiteParameter _name = new SQLiteParameter(":name");
            private readonly SQLiteParameter _type = new SQLiteParameter(":type");
            private readonly SQLiteParameter _source = new SQLiteParameter(":source");
            private readonly SQLiteParameter _value = new SQLiteParameter(":value");
            private readonly SQLiteParameter _owner = new SQLiteParameter(":owner");

            private readonly SQLiteCommand _command;

            public InsertAttributeCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "INSERT INTO attributes (name, source, type, value, file_id)" +
                                       "VALUES (:name, :source, :type, :value, :owner)";
                _command.Parameters.Add(_name);
                _command.Parameters.Add(_type);
                _command.Parameters.Add(_source);
                _command.Parameters.Add(_value);
                _command.Parameters.Add(_owner);
            }

            public void Execute(Attribute attr, long owner)
            {
                _name.Value = attr.Name;
                _type.Value = (int)attr.Value.Type;
                _source.Value = (int)attr.Source;
                _owner.Value = owner;
                _value.ResetDbType();
                attr.Value.Accept(this);
                _command.ExecuteNonQuery();
            }

            void IValueVisitor.Visit(IntValue attr)
            {
                _value.Value = attr.Value;
            }

            void IValueVisitor.Visit(RealValue attr)
            {
                _value.Value = attr.Value;
            }

            void IValueVisitor.Visit(StringValue attr)
            {
                _value.Value = attr.Value;
            }

            void IValueVisitor.Visit(DateTimeValue attr)
            {
                _value.Value = attr.Value?.ToString(DateTimeValue.Format);
            }

            void IValueVisitor.Visit(ImageValue attr)
            {
                _value.Value = attr.Value;
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        private class UpdateThumbnailCommand : IDisposable
        {
            private readonly SQLiteParameter _source = new SQLiteParameter(":source");
            private readonly SQLiteParameter _type = new SQLiteParameter(":type");
            private readonly SQLiteParameter _value = new SQLiteParameter(":value");
            private readonly SQLiteParameter _owner = new SQLiteParameter(":owner");
            private readonly SQLiteCommand _command;

            public UpdateThumbnailCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = @" 
                INSERT OR REPLACE INTO attributes (name, source, type, value, file_id)
                VALUES('thumbnail', :source, :type, :value, :owner)";
                _command.Parameters.Add(_source);
                _command.Parameters.Add(_type);
                _command.Parameters.Add(_value);
                _command.Parameters.Add(_owner);
            }

            public void Execute(long owner, byte[] thumbnail)
            {
                _owner.Value = owner;
                _value.Value = thumbnail;
                _type.Value = (int)AttributeType.Image;
                _source.Value = (int)AttributeSource.Metadata;
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }
        
        public void Insert(Attribute attribute, long file)
        {
            _insertAttribute.Execute(attribute, file);
        }

        public void SetThumbnail(byte[] thumbnail, long file)
        {
            _updateThumbnail.Execute(file, thumbnail);
        }

        public void Dispose()
        {
            _updateThumbnail?.Dispose();
            _insertAttribute?.Dispose();
            _connection?.Dispose();
        }
    }
}
