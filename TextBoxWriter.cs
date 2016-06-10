using System;
using System.Collections;
using System.Text;
using System.Windows.Forms;

namespace System.IO
{
    // TextBoxWriter version, oh, say 6
    // v6 (2015-05-27) Add 'FixBareLF' property

    /// <summary>
    /// TextWriter that writes all output to a TextBox
    /// </summary>
    public class TextBoxWriter : TextWriter
    {
        private TextBoxBase tb;
        private string _prefix;
        /// <summary>
        /// set to 'true' when the last string written ended in a newline ('\n').
        /// </summary>
        private bool _writeprefix = true;
        bool _fixBareLF;

        public bool FixBareLF { get { return _fixBareLF; } set { _fixBareLF = value; } }

        string AddMissingCRs(string s)
        {
            if (!FixBareLF) return s;

            if (string.IsNullOrEmpty(s)) return s;
            int count = GetMissingCRCount(s);
            if (count == 0) return s;
            StringBuilder sb = new StringBuilder(s, s.Length + count);
            AddCRs(sb, 0);
            return sb.ToString();
        }

        int GetMissingCRCount(string s)
        {
            if (!FixBareLF) return 0;
            int n = 0;
            int idx = s.IndexOf('\n');
            while (idx >= 0)
            {
                if (idx == 0 || s[idx - 1] != '\r') { n++; }
                idx = s.IndexOf('\n', idx + 1);
            }
            return n;
        }

        /// <summary>
        /// Changes bare LFs to CR-LF, starting at startOffset
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="startOffset"></param>
        static void AddCRs(StringBuilder sb, int startOffset)
        {
            for (; startOffset < sb.Length; startOffset++)
            {
                if (sb[startOffset] != '\n') continue;
                if (startOffset == 0 || sb[startOffset - 1] != '\r')
                {
                    sb.Insert(startOffset, '\r');
                    startOffset++;
                }
            }
        }

        class TbwWriteBuffer
        {
            readonly TextBoxWriter _owner;
            string _stringToWrite;
            StringBuilder _stringBuffer;
            public TbwWriteBuffer(TextBoxWriter owner)
            {
                this._owner = owner;
            }
            public void QueueWrite(string s)
            {
                if (string.IsNullOrEmpty(s)) return;
                lock (this)
                {
                    if (_stringToWrite == null) { _stringToWrite = s; return; }
                    if (_stringBuffer == null)
                    {
                        _stringBuffer = new StringBuilder(_stringToWrite, _stringToWrite.Length + s.Length);
                    }
                    _stringBuffer.Append(s);
                }
            }
            public void WriteToTextBox()
            {
                lock (this)
                {
                    string s;
                    if (_stringBuffer != null) { s = _stringBuffer.ToString(); }
                    else { s = _stringToWrite; }
                    if (!string.IsNullOrEmpty(s))
                        _owner.tb.AppendText(s);
                    _stringBuffer = null;
                    _stringToWrite = null;
                }
            }
        }

        /// <summary>
        /// If <see cref='SyncInvoke' /> is false, this buffer contains a set of strings to be written out.
        /// </summary>
        readonly TbwWriteBuffer _writeBuffer;
        volatile bool _writeQueued = false;

        private bool _syncInvoke = false;

        /// <summary>
        /// Gets/sets whether or not to call Control.Invoke synchronously for writing output.
        /// </summary>
        public bool SyncInvoke
        {
            get { return _syncInvoke; }
            set { _syncInvoke = value; }
        }

        protected void WritePrefixIfNeeded()
        {
            if (_prefix != null && _writeprefix)
            {
                _writeprefix = false;
                _innerWrite(_prefix);  // bypass our own processing
            }
        }

        /// <summary>
        /// Sets the string that is prepended to every line that is output.
        /// </summary>
        /// <param name="newPrefix">The new prefix (null means
        /// no prefix)</param>
        /// <returns>The old prefix string.</returns>
        protected string SetPrefix(string newPrefix)
        {
            string _tmp = _prefix;
            if (newPrefix == String.Empty)
                newPrefix = null;
            _prefix = newPrefix;
            return _tmp;
        }

        public TextBoxWriter(TextBoxBase textbox)
        {
            if (textbox == null)
                throw new ArgumentNullException("textbox");
            _writeBuffer = new TbwWriteBuffer(this);
            asyncWriteSync = _innerWrite;
            asyncWriteAsync = _asyncWriteAsync;
            tb = textbox;
        }

        // according to MSDN this is used only when a TextWriter is passed
        // to certain XML functions
        public override System.Text.Encoding Encoding
        {
            get
            {
                return System.Text.Encoding.Unicode;
            }
        }

        protected override void Dispose(bool disposing)
        {
        }

        delegate void WriteStringDelegate(string data);
        delegate void WriteAsyncDelegate();

        readonly WriteStringDelegate asyncWriteSync;
        readonly WriteAsyncDelegate asyncWriteAsync;

        void _asyncWriteAsync()
        {
            _writeQueued = false;
            _writeBuffer.WriteToTextBox();
        }

        /// <summary>
        /// Internal implementation of Write; bypasses \n checking for the prefix
        /// </summary>
        /// <param name="data">The text to write</param>
        /// <remarks>
        /// This method exists so it can be called when we need to write the prefix.
        /// </remarks>
        protected virtual void _innerWrite(string data)
        {
            data = AddMissingCRs(data);
            if (tb.InvokeRequired)
            {
                if (this.SyncInvoke)
                {
                    tb.Invoke(asyncWriteSync, new object[] { data });
                }
                else
                {
                    // Okay, an async invocation is needed
                    _writeBuffer.QueueWrite(data);
                    if (!_writeQueued)
                    {
                        _writeQueued = true;
                        tb.BeginInvoke(asyncWriteAsync);
                    }
                }
                return;
            }
            // Okay, no invoke required. That means we're executing on the main thread.
            // If there are any queued writes, write them *right now*.
            _asyncWriteAsync();
            tb.AppendText(data);
        }

        /// <summary>
        /// Writes a string (without appending a newline) to any clients connected to the SocketReporter.
        /// </summary>
        /// <param name="data"></param>
        public override void Write(string data)
        {
            if (data == null || data.Length == 0) return;  // gahhh!

            if (_prefix == null)
            {
                // common, optimal case: no prefix. Just hand it off with no processing.
                _writeprefix = data[data.Length - 1] == '\n';
                _innerWrite(data);
                return;
            }

            // TODO: Stuff everything into a StringBuilder in order to minimize the number of marshalling operations required

            // damn. we have to do some work now
            WritePrefixIfNeeded();  // in case the previous Write() ended in \n, write the prefix now.

            // I believe this is correct code...
            int start, i;
            for (start = 0, i = data.IndexOf('\n', start);
                 i >= 0 && i < data.Length - 1;
                 start = i + 1, i = data.IndexOf('\n', start)
                )
            {
                /* at this point, it is my sincere hope that the following are ALL true:
                 * (1) 0 <= start <= i < data.Length - 1
                 *      Note the < on the last case: I do NOT want this block to 
                 *      execute for the case where i == data.Length - 1
                 *      (i.e. start..i encompass the last "line" in the string,
                 *       with the string ending in a newline)
                 *      Again, if that is the case, this code should NOT be
                 *      executing.
                 * (2) data[start] .. data[i], inclusive, comprise a separate "line"
                 *     of text, with data[i] being the terminating \n character
                 */
                _innerWrite(data.Substring(start, i - start + 1));
                // we also know that there IS text immediately following this
                // (because: i < data.Length - 1)
                _innerWrite(_prefix);
            }
            // at this point, data[start] to data[data.Length-1] is the rest to be written
            _innerWrite(data.Substring(start));
            // there are two possible values for i: -1, and data.Length-1.
            // In the former case, the string did NOT end in a newline character.
            // In the latter, it DID end in a newline character, and we must write
            // out the prefix string before the next block of text is output.
            _writeprefix = (i >= 0);
        }

        public override void Write(char value)
        {
            Write(new string(value, 1));
        }

        public override void Write(char[] buffer)
        {
            Write(new string(buffer));
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Write(new string(buffer, index, count));
        }

        public void WriteObject(string name, object o, bool PropertiesOnly)
        {
            WriteObject(name, o, PropertiesOnly, new string[] { });
        }

        /// <summary>
        /// Writes an object out to the SocketReporter, using reflection to obtain all fields and properties.
        /// </summary>
        /// <param name="o">The object to dump</param>
        /// <param name="name">A name that will be used to label the dump</param>
        /// <param name="PropertiesOnly">If true, only properties are dumped.  If false, both fields and properties are dumped.</param>
        /// <param name="recurse">Recurse on the given field/property names </param>
        /// <remarks>How to make it easy to automatically handle objects inside?</remarks>
        public void WriteObject(string name, object o, bool PropertiesOnly, string[] recurse)
        {
            WriteObject("", name, o, PropertiesOnly, recurse);
        }

        /// <summary>
        /// Writes an object out to the SocketReporter, using reflection to obtain all fields and properties.
        /// </summary>
        /// <param name="prefix">String prefixed before each line of text.</param>
        /// <param name="o">The object to dump</param>
        /// <param name="name">A name that will be used to label the dump</param>
        /// <param name="PropertiesOnly">If true, only properties are dumped.  If false, both fields and properties are dumped.</param>
        /// <param name="recurse">Recurse on the given field/property names </param>
        /// <remarks>How to make it easy to automatically handle objects inside?</remarks>
        public void WriteObject(string prefix, string name, object o, bool PropertiesOnly, string[] recurse)
        {
            string oldPrefix = SetPrefix(prefix);
            try
            {
                if (o == null)
                {
                    WriteLine("Dumping {0}: [null reference]", name);
                    return;
                }
                WriteLine("Dumping {0} ['{1}', hashcode {2}]", name, o.ToString(), o.GetHashCode());

                System.Reflection.MemberInfo[] MemberList = o.GetType().GetMembers(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);  // what, you don't have a 10-foot-wide screen to view this code on?

                // this method was copied from the SocketReporter class
                // however, under 2.0, the WTFery I needed for suitable sorting can go away

                Array.Sort<System.Reflection.MemberInfo>(
                        MemberList,
                        delegate (System.Reflection.MemberInfo a, System.Reflection.MemberInfo b)
                        {
                            return a.Name.CompareTo(b.Name);
                        }
                );
                //Array.Sort(MemberList, new MemberInfoSorter());

                StringBuilder output = new StringBuilder();

                foreach (System.Reflection.MemberInfo mi in MemberList)
                {
                    // Only proceed if this is a field (and we are allowed to dump fields),
                    // or if this is a 
                    if (!(
                            (mi.MemberType == System.Reflection.MemberTypes.Field && !PropertiesOnly) ||
                            (mi.MemberType == System.Reflection.MemberTypes.Property)
                        ))
                        continue;

                    output.AppendFormat("\t");

                    // if it was inherited, prepend the type
                    if (mi.DeclaringType != null &&
                        mi.DeclaringType != o.GetType())
                    {

                        output.AppendFormat("{0}.", mi.DeclaringType.Name);

                    }

                    output.AppendFormat("{0} = ", mi.Name);

                    bool dontdumpvalue = false;
                    object val = null;
                    if (mi.MemberType == System.Reflection.MemberTypes.Field)
                    {
                        System.Reflection.FieldInfo fi = (System.Reflection.FieldInfo)mi;
                        val = fi.GetValue(o);
                    }
                    else
                    {
                        System.Reflection.PropertyInfo pi = (System.Reflection.PropertyInfo)mi;
                        if (pi.GetIndexParameters().Length > 0)
                        {
                            output.Append("(cannot dump indexed property)");
                            dontdumpvalue = true;
                        }
                        else if (!pi.CanRead)
                        {
                            output.Append("WTF? Write-only property?!?");
                            dontdumpvalue = true;
                        }
                        else
                        {
                            try
                            {
                                val = pi.GetValue(o, null);
                            }
                            catch (System.Reflection.TargetInvocationException tiex)
                            {
                                Exception realex = tiex.InnerException;
                                if (realex == null) throw;	// redmond, we have a problem
                                output.AppendFormat("[{0}: {1}]", realex.GetType(), realex.Message);
                                dontdumpvalue = true;
                            }
                        }
                    }

                    if (!dontdumpvalue)
                    {
                        if (val == null)
                            output.Append("null");
                        else if (val.GetType().IsPrimitive)	// true for Booleans and builtin numeric types
                            output.Append(val.ToString());
                        else
                        {
                            // do some heuristics to determine if ToString() does anything special.
                            System.Reflection.MethodInfo tostr = val.GetType().GetMethod("ToString", new Type[] { });

                            if (tostr.DeclaringType == typeof(object))
                                // Since we're using the standard ToString() implementation, it must be a regular object.
                                output.AppendFormat("{{{0}}}", val.ToString());
                            else
                                output.AppendFormat("'{0}'", val.ToString());
                        }

                        output.Append("\r\n");

                        if (((IList)recurse).Contains(mi.Name)) // recurse into this guy?
                        {
                            Write(output.ToString());   // flush the output
                            output = new StringBuilder();

                            WriteObject(prefix + ">",
                                        string.Format("{0}.{1}", name, mi.Name),
                                        val,
                                        PropertiesOnly,
                                        new string[] { });
                        }
                    }
                }

                Write(output.ToString());
            }
            finally
            {
                SetPrefix(oldPrefix);
            }
        }
    }
}
