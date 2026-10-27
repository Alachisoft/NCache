//  Copyright (c) 2018 Alachisoft
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Alachisoft.NCache.SocketServer.Http
{
    /// <summary>
    /// Tiny indenting JSON writer for endpoint responses, so no serializer dependency is needed on
    /// any of the target frameworks the service is built for.
    /// </summary>
    public sealed class JsonWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly Stack<bool> _scopeHasItems = new Stack<bool>();
        private int _indent;
        private bool _afterPropertyName;

        public void BeginObject() { StartItem(); _sb.Append('{'); _scopeHasItems.Push(false); _indent++; }
        public void EndObject() { EndScope('}'); }
        public void BeginArray() { StartItem(); _sb.Append('['); _scopeHasItems.Push(false); _indent++; }
        public void EndArray() { EndScope(']'); }

        public void PropertyName(string name)
        {
            StartItem();
            WriteString(name);
            _sb.Append(": ");
            _afterPropertyName = true;
        }

        public void Property(string name, string value)
        {
            PropertyName(name);
            Value(value);
        }

        public void Property(string name, bool value) { PropertyName(name); Raw(value ? "true" : "false"); }
        public void Property(string name, long value) { PropertyName(name); Raw(value.ToString(CultureInfo.InvariantCulture)); }

        /// <summary>Writes a string value (or null) as the next array item or property value.</summary>
        public void Value(string value)
        {
            if (value == null)
            {
                Raw("null");
            }
            else
            {
                StartItem();
                WriteString(value);
            }
        }

        public void Value(bool value) { Raw(value ? "true" : "false"); }
        public void Value(long value) { Raw(value.ToString(CultureInfo.InvariantCulture)); }

        public override string ToString() { return _sb.ToString(); }

        /// <summary>Convenience for the common single-message body: {"error": "..."}.</summary>
        public static string ErrorObject(string message)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.Property("error", message);
            writer.EndObject();
            return writer.ToString();
        }

        private void Raw(string raw)
        {
            StartItem();
            _sb.Append(raw);
        }

        /// <summary>
        /// Called before every item (object, array, property name or scalar). Writes the separator and
        /// line break for the enclosing scope; a value directly following a property name is part of
        /// that property and gets neither.
        /// </summary>
        private void StartItem()
        {
            if (_afterPropertyName)
            {
                _afterPropertyName = false;
                return;
            }
            if (_scopeHasItems.Count == 0)
                return;

            if (_scopeHasItems.Pop())
                _sb.Append(',');
            _scopeHasItems.Push(true);
            NewLine();
        }

        private void EndScope(char closer)
        {
            bool hadItems = _scopeHasItems.Pop();
            _indent--;
            if (hadItems)
                NewLine();
            _sb.Append(closer);
        }

        private void NewLine()
        {
            _sb.Append('\n');
            _sb.Append(' ', _indent * 2);
        }

        private void WriteString(string value)
        {
            _sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': _sb.Append("\\\""); break;
                    case '\\': _sb.Append("\\\\"); break;
                    case '\n': _sb.Append("\\n"); break;
                    case '\r': _sb.Append("\\r"); break;
                    case '\t': _sb.Append("\\t"); break;
                    default:
                        if (c < ' ') _sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else _sb.Append(c);
                        break;
                }
            }
            _sb.Append('"');
        }
    }
}
