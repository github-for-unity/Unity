//The MIT License(MIT)
//=====================
//
//Copyright © `2015-2017` `Lucas Meijer`
//Copyright © `2017-2018` `Andreia Gaita`
//
//Permission is hereby granted, free of charge, to any person
//obtaining a copy of this software and associated documentation
//files (the “Software”), to deal in the Software without
//restriction, including without limitation the rights to use,
//copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the
//Software is furnished to do so, subject to the following
//conditions:
//
//The above copyright notice and this permission notice shall be
//included in all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
//EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GitHub.Unity
{
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct NPath : IEquatable<NPath>, IComparable
    {
        public static NPath Default;

        private readonly string[] _elements;
        private readonly bool _isRelative;
        private readonly string _driveLetter;
        private readonly bool _isInitialized;

        #region construction

        public NPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            _isInitialized = true;

            path = ParseDriveLetter(path, out _driveLetter);

            if (path == "/")
            {
                _isRelative = false;
                _elements = new string[] { };
            }
            else
            {
                var split = path.Split('/', '\\');

                _isRelative = _driveLetter == null && IsRelativeFromSplitString(split);

                _elements = ParseSplitStringIntoElements(split.Where(s => s.Length > 0).ToArray(), _isRelative);
            }
        }

        private NPath(string[] elements, bool isRelative, string driveLetter)
        {
            _elements = elements;
            _isRelative = isRelative;
            _driveLetter = driveLetter;
            _isInitialized = true;
        }

        private static string[] ParseSplitStringIntoElements(IEnumerable<string> inputs, bool isRelative)
        {
            var stack = new List<string>();

            foreach (var input in inputs.Where(input => input.Length != 0))
            {
                if (input == ".")
                {
                    if ((stack.Count > 0) && (stack.Last() != "."))
                        continue;
                }
                else if (input == "..")
                {
                    if (HasNonDotDotLastElement(stack))
                    {
                        stack.RemoveAt(stack.Count - 1);
                        continue;
                    }
                    if (!isRelative)
                        throw new ArgumentException("You cannot create a path that tries to .. past the root");
                }
                stack.Add(input);
            }
            return stack.ToArray();
        }

        private static bool HasNonDotDotLastElement(List<string> stack)
        {
            return stack.Count > 0 && stack[stack.Count - 1] != "..";
        }

        private static string ParseDriveLetter(string path, out string driveLetter)
        {
            if (path.Length >= 2 && path[1] == ':')
            {
                driveLetter = path[0].ToString();
                return path.Substring(2);
            }

            driveLetter = null;
            return path;
        }

        private static bool IsRelativeFromSplitString(string[] split)
        {
            if (split.Length < 2)
                return true;

            return split[0].Length != 0 || !split.Any(s => s.Length > 0);
        }

        public NPath Combine(params string[] append)
        {
            return Combine(append.Select(a => new NPath(a)).ToArray());
        }

        public NPath Combine(params NPath[] append)
        {
            ThrowIfNotInitialized();

            if (!append.All(p => p.IsRelative))
                throw new ArgumentException("You cannot .Combine a non-relative path");

            return new NPath(ParseSplitStringIntoElements(_elements.Concat(append.SelectMany(p => p._elements)), _isRelative), _isRelative, _driveLetter);
        }

        public NPath Parent
        {
            get
            {
                ThrowIfNotInitialized();

                if (_elements.Length == 0)
                    throw new InvalidOperationException("Parent is called on an empty path");

                var newElements = _elements.Take(_elements.Length - 1).ToArray();

                return new NPath(newElements, _isRelative, _driveLetter);
            }
        }

        public NPath RelativeTo(NPath path)
        {
            ThrowIfNotInitialized();

            if (!IsChildOf(path))
            {
                if (!IsRelative && !path.IsRelative && _driveLetter != path._driveLetter)
                    throw new ArgumentException("Path.RelativeTo() was invoked with two paths that are on different volumes. invoked on: " + ToString() + " asked to be made relative to: " + path);

                NPath commonParent = Default;
                foreach (var parent in RecursiveParents)
                {
                    commonParent = path.RecursiveParents.FirstOrDefault(otherParent => otherParent == parent);

                    if (commonParent.IsInitialized)
                        break;
                }

                if (!commonParent.IsInitialized)
                    throw new ArgumentException("Path.RelativeTo() was unable to find a common parent between " + ToString() + " and " + path);

                if (IsRelative && path.IsRelative && commonParent.IsEmpty)
                    throw new ArgumentException("Path.RelativeTo() was invoked with two relative paths that do not share a common parent.  Invoked on: " + ToString() + " asked to be made relative to: " + path);

                var depthDiff = path.Depth - commonParent.Depth;
                return new NPath(Enumerable.Repeat("..", depthDiff).Concat(_elements.Skip(commonParent.Depth)).ToArray(), true, null);
            }

            return new NPath(_elements.Skip(path._elements.Length).ToArray(), true, null);
        }

        public NPath GetCommonParent(NPath path)
        {
            ThrowIfNotInitialized();

            if (!IsChildOf(path))
            {
                if (!IsRelative && !path.IsRelative && _driveLetter != path._driveLetter)
                    return Default;

                NPath commonParent = Default;
                foreach (var parent in new List<NPath> { this }.Concat(RecursiveParents))
                {
                    commonParent = path.RecursiveParents.FirstOrDefault(otherParent => otherParent == parent);
                    if (commonParent.IsInitialized)
                        break;
                }

                if (IsRelative && path.IsRelative && (!commonParent.IsInitialized || commonParent.IsEmpty))
                    return Default;
                return commonParent;
            }
            return path;
        }

        public NPath ChangeExtension(string extension)
        {
            ThrowIfNotInitialized();
            ThrowIfRoot();

            var newElements = (string[])_elements.Clone();
            newElements[newElements.Length - 1] = FileSystem.ChangeExtension(_elements[_elements.Length - 1], WithDot(extension));
            if (extension == string.Empty)
                newElements[newElements.Length - 1] = newElements[newElements.Length - 1].TrimEnd('.');
            return new NPath(newElements, _isRelative, _driveLetter);
        }

        #endregion construction

        #region inspection

        public bool IsRelative
        {
            get { return _isRelative; }
        }

        public string FileName
        {
            get
            {
                ThrowIfNotInitialized();
                ThrowIfRoot();

                return _elements.Last();
            }
        }

        public string FileNameWithoutExtension
        {
            get
            {
                ThrowIfNotInitialized();

                return FileSystem.GetFileNameWithoutExtension(FileName);
            }
        }

        public IEnumerable<string> Elements
        {
            get
            {
                ThrowIfNotInitialized();
                return _elements;
            }
        }

        public int Depth
        {
            get
            {
                ThrowIfNotInitialized();
                return _elements.Length;
            }
        }

        public bool IsInitialized
        {
            get { return _isInitialized; }
        }

        public bool Exists()
        {
            ThrowIfNotInitialized();
            return FileExists() || DirectoryExists();
        }

        public bool Exists(string append)
        {
            ThrowIfNotInitialized();
            if (String.IsNullOrEmpty(append))
            {
                return Exists();
            }
            return Exists(new NPath(append));
        }

        public bool Exists(NPath append)
        {
            ThrowIfNotInitialized();
            if (!append.IsInitialized)
                return Exists();
            return FileExists(append) || DirectoryExists(append);
        }

        public bool DirectoryExists()
        {
            ThrowIfNotInitialized();
            return FSWrapper.DirectoryExists(this);
        }

        public bool DirectoryExists(string append)
        {
            ThrowIfNotInitialized();
            if (String.IsNullOrEmpty(append))
                return DirectoryExists();
            return DirectoryExists(new NPath(append));
        }

        public bool DirectoryExists(NPath append)
        {
            ThrowIfNotInitialized();
            if (!append.IsInitialized)
                return DirectoryExists();
            return FSWrapper.DirectoryExists(Combine(append));
        }

        public bool FileExists()
        {
            ThrowIfNotInitialized();
            return FSWrapper.FileExists(this);
        }

        public bool FileExists(string append)
        {
            ThrowIfNotInitialized();
            if (String.IsNullOrEmpty(append))
                return FileExists();
            return FileExists(new NPath(append));
        }

        public bool FileExists(NPath append)
        {
            ThrowIfNotInitialized();
            if (!append.IsInitialized)
                return FileExists();
            return FSWrapper.FileExists(Combine(append));
        }

        public string ExtensionWithDot
        {
            get
            {
                ThrowIfNotInitialized();
                if (IsRoot)
                    throw new ArgumentException("A root directory does not have an extension");

                var last = _elements.Last();
                var index = last.LastIndexOf(".");
                if (index < 0) return String.Empty;
                return last.Substring(index);
            }
        }

        public string InQuotes()
        {
            return "\"" + ToString() + "\"";
        }

        public string InQuotes(SlashMode slashMode)
        {
            return "\"" + ToString(slashMode) + "\"";
        }

        public override string ToString()
        {
            return ToString(SlashMode.Native);
        }

        public string ToString(SlashMode slashMode)
        {
            if (!_isInitialized)
                return String.Empty;

            // Check if it's linux root /
            if (IsRoot && string.IsNullOrEmpty(_driveLetter))
                return Slash(slashMode).ToString();

            if (_isRelative && _elements.Length == 0)
                return ".";

            var sb = new StringBuilder();
            if (_driveLetter != null)
            {
                sb.Append(_driveLetter);
                sb.Append(":");
            }
            if (!_isRelative)
                sb.Append(Slash(slashMode));
            var first = true;
            foreach (var element in _elements)
            {
                if (!first)
                    sb.Append(Slash(slashMode));

                sb.Append(element);
                first = false;
            }
            return sb.ToString();
        }

        public static implicit operator string(NPath path)
        {
            return path.ToString();
        }

        static char Slash(SlashMode slashMode)
        {
            switch (slashMode)
            {
                case SlashMode.Backward:
                    return '\\';
                case SlashMode.Forward:
                    return '/';
                default:
                    return FileSystem.DirectorySeparatorChar;
            }
        }

        public override bool Equals(Object other)
        {
            if (other is NPath)
            {
                return Equals((NPath)other);
            }
            return false;
        }

        public bool Equals(NPath p)
        {
            if (p._isInitialized != _isInitialized)
                return false;

            // return early if we're comparing two NPath.Default instances
            if (!_isInitialized)
                return true;

            if (p._isRelative != _isRelative)
                return false;

            if (!string.Equals(p._driveLetter, _driveLetter, PathStringComparison))
                return false;

            if (p._elements.Length != _elements.Length)
                return false;

            for (var i = 0; i != _elements.Length; i++)
                if (!string.Equals(p._elements[i], _elements[i], PathStringComparison))
                    return false;

            return true;
        }

        public static bool operator ==(NPath lhs, NPath rhs)
        {
            return lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + _isInitialized.GetHashCode();
                if (!_isInitialized)
                    return hash;
                hash = hash * 23 + _isRelative.GetHashCode();
                foreach (var element in _elements)
                    hash = hash * 23 + (IsUnix ? element : element.ToUpperInvariant()).GetHashCode();
                if (_driveLetter != null)
                    hash = hash * 23 + (IsUnix ? _driveLetter : _driveLetter.ToUpperInvariant()).GetHashCode();
                return hash;
            }
        }

        public int CompareTo(object other)
        {
            if (!(other is NPath))
                return -1;

            return ToString().CompareTo(((NPath)other).ToString());
        }

        public static bool operator !=(NPath lhs, NPath rhs)
        {
            return !(lhs.Equals(rhs));
        }

        public bool HasExtension(params string[] extensions)
        {
            ThrowIfNotInitialized();
            var extensionWithDotLower = ExtensionWithDot.ToLower();
            return extensions.Any(e => WithDot(e).ToLower() == extensionWithDotLower);
        }

        private static string WithDot(string extension)
        {
            return extension.StartsWith(".") ? extension : "." + extension;
        }

        public bool IsEmpty
        {
            get
            {
                ThrowIfNotInitialized();
                return _elements.Length == 0;
            }
        }

        public bool IsRoot
        {
            get
            {
                ThrowIfNotInitialized();
                return _elements.Length == 0 && !_isRelative;
            }
        }

        #endregion inspection

        #region directory enumeration

        public IEnumerable<NPath> Files(string filter, bool recurse = false)
        {
            return FSWrapper.GetFiles(this, filter, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select(s => new NPath(s));
        }

        public IEnumerable<NPath> Files(bool recurse = false)
        {
            return Files("*", recurse);
        }

        public IEnumerable<NPath> Contents(string filter, bool recurse = false)
        {
            return Files(filter, recurse).Concat(Directories(filter, recurse));
        }

        public IEnumerable<NPath> Contents(bool recurse = false)
        {
            return Contents("*", recurse);
        }

        public IEnumerable<NPath> Directories(string filter, bool recurse = false)
        {
            return FSWrapper.GetDirectories(this, filter, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select(s => new NPath(s));
        }

        public IEnumerable<NPath> Directories(bool recurse = false)
        {
            return Directories("*", recurse);
        }

        #endregion

        #region filesystem writing operations
        public NPath CreateFile()
        {
            ThrowIfNotInitialized();
            ThrowIfRelative();
            ThrowIfRoot();
            EnsureParentDirectoryExists();
            FSWrapper.WriteAllBytes(this, new byte[0]);
            return this;
        }

        public NPath CreateFile(string file)
        {
            return CreateFile(new NPath(file));
        }

        public NPath CreateFile(NPath file)
        {
            ThrowIfNotInitialized();
            if (!file.IsRelative)
                throw new ArgumentException("You cannot call CreateFile() on an existing path with a non relative argument");
            return Combine(file).CreateFile();
        }

        public NPath CreateDirectory()
        {
            ThrowIfNotInitialized();
            ThrowIfRelative();

            if (IsRoot)
                throw new NotSupportedException("CreateDirectory is not supported on a root level directory because it would be dangerous:" + ToString());

            FSWrapper.DirectoryCreate(this);
            return this;
        }

        public NPath CreateDirectory(string directory)
        {
            return CreateDirectory(new NPath(directory));
        }

        public NPath CreateDirectory(NPath directory)
        {
            ThrowIfNotInitialized();
            if (!directory.IsRelative)
                throw new ArgumentException("Cannot call CreateDirectory with an absolute argument");

            return Combine(directory).CreateDirectory();
        }

        public NPath Copy(string dest)
        {
            return Copy(new NPath(dest));
        }

        public NPath Copy(string dest, Func<NPath, bool> fileFilter)
        {
            return Copy(new NPath(dest), fileFilter);
        }

        public NPath Copy(NPath dest)
        {
            return Copy(dest, p => true);
        }

        public NPath Copy(NPath dest, Func<NPath, bool> fileFilter)
        {
            ThrowIfNotInitialized();
            ThrowIfNotInitialized(dest);

            if (dest.IsRelative)
                dest = Parent.Combine(dest);

            if (dest.DirectoryExists())
                return CopyWithDeterminedDestination(dest.Combine(FileName), fileFilter);

            return CopyWithDeterminedDestination(dest, fileFilter);
        }

        public NPath MakeAbsolute()
        {
            ThrowIfNotInitialized();

            if (!IsRelative)
                return this;

            return NPath.CurrentDirectory.Combine(this);
        }

        NPath CopyWithDeterminedDestination(NPath absoluteDestination, Func<NPath, bool> fileFilter)
        {
            if (absoluteDestination.IsRelative)
                throw new ArgumentException("absoluteDestination must be absolute");

            if (FileExists())
            {
                if (!fileFilter(absoluteDestination))
                    return Default;

                absoluteDestination.EnsureParentDirectoryExists();

                FSWrapper.FileCopy(this, absoluteDestination, true);
                return absoluteDestination;
            }

            if (DirectoryExists())
            {
                absoluteDestination.EnsureDirectoryExists();
                foreach (var thing in Contents())
                    thing.CopyWithDeterminedDestination(absoluteDestination.Combine(thing.RelativeTo(this)), fileFilter);
                return absoluteDestination;
            }

            throw new ArgumentException("Copy() called on path that doesnt exist: " + ToString());
        }

        public void Delete(DeleteMode deleteMode = DeleteMode.Normal)
        {
            ThrowIfNotInitialized();
            ThrowIfRelative();

            if (IsRoot)
                throw new NotSupportedException("Delete is not supported on a root level directory because it would be dangerous:" + ToString());

            var isFile = FileExists();
            var isDir = DirectoryExists();
            if (!isFile && !isDir)
                throw new InvalidOperationException("Trying to delete a path that does not exist: " + ToString());

            try
            {
                if (isFile)
                {
                    FSWrapper.FileDelete(this);
                }
                else
                {
                    FSWrapper.DirectoryDelete(this, true);
                }
            }
            catch (IOException)
            {
                if (deleteMode == DeleteMode.Normal)
                    throw;
            }
        }

        public void DeleteIfExists(DeleteMode deleteMode = DeleteMode.Normal)
        {
            ThrowIfNotInitialized();
            ThrowIfRelative();

            if (FileExists() || DirectoryExists())
                Delete(deleteMode);
        }

        public NPath DeleteContents()
        {
            ThrowIfNotInitialized();
            ThrowIfRelative();

            if (IsRoot)
                throw new NotSupportedException("DeleteContents is not supported on a root level directory because it would be dangerous:" + ToString());

            if (FileExists())
                throw new InvalidOperationException("It is not valid to perform this operation on a file");

            if (DirectoryExists())
            {
                try
                {
                    Files().Delete();
                    Directories().Delete();
                }
                catch (IOException)
                {
                    if (Files(true).Any())
                        throw;
                }

                return this;
            }

            return EnsureDirectoryExists();
        }

        public static NPath CreateTempDirectory(string myprefix)
        {
            var random = new Random();
            while (true)
            {
                var candidate = new NPath(FileSystem.GetTempPath() + "/" + myprefix + "_" + random.Next());
                if (!candidate.Exists())
                    return candidate.CreateDirectory();
            }
        }

        public static NPath GetTempFilename(string myprefix = "")
        {
            var random = new Random();
            var prefix = FileSystem.GetTempPath() + "/" + (String.IsNullOrEmpty(myprefix) ? "" : myprefix + "_");
            while (true)
            {
                var candidate = new NPath(prefix + random.Next());
                if (!candidate.Exists())
                    return candidate;
            }
        }

        public NPath Move(string dest)
        {
            return Move(new NPath(dest));
        }

        public NPath Move(NPath dest)
        {
            ThrowIfNotInitialized();
            ThrowIfNotInitialized(dest);

            if (IsRoot)
                throw new NotSupportedException("Move is not supported on a root level directory because it would be dangerous:" + ToString());

            if (dest.IsRelative)
                return Move(Parent.Combine(dest));

            if (dest.DirectoryExists())
                return Move(dest.Combine(FileName));

            if (FileExists())
            {
                dest.DeleteIfExists();
                dest.EnsureParentDirectoryExists();
                FSWrapper.FileMove(this, dest);
                return dest;
            }

            if (DirectoryExists())
            {
                FSWrapper.DirectoryMove(this, dest);
                return dest;
            }

            throw new ArgumentException("Move() called on a path that doesn't exist: " + ToProcessDirectory().ToString());
        }

        public NPath WriteAllText(string contents)
        {
            ThrowIfNotInitialized();
            EnsureParentDirectoryExists();
            FSWrapper.WriteAllText(this, contents);
            return this;
        }

        public string ReadAllText()
        {
            ThrowIfNotInitialized();
            return FSWrapper.ReadAllText(this);
        }

        public NPath WriteAllText(string contents, Encoding encoding)
        {
            ThrowIfNotInitialized();
            EnsureParentDirectoryExists();
            FSWrapper.WriteAllText(this, contents, encoding);
            return this;
        }

        public string ReadAllText(Encoding encoding)
        {
            ThrowIfNotInitialized();
            return FSWrapper.ReadAllText(this, encoding);
        }

        public NPath WriteLines(string[] contents)
        {
            ThrowIfNotInitialized();
            EnsureParentDirectoryExists();
            FSWrapper.WriteLines(this, contents);
            return this;
        }

        public NPath WriteAllLines(string[] contents)
        {
            ThrowIfNotInitialized();
            EnsureParentDirectoryExists();
            FSWrapper.WriteAllLines(this, contents);
            return this;
        }

        public string[] ReadAllLines()
        {
            ThrowIfNotInitialized();
            return FSWrapper.ReadAllLines(this);
        }

        public NPath WriteAllBytes(byte[] contents)
        {
            ThrowIfNotInitialized();
            EnsureParentDirectoryExists();
            FSWrapper.WriteAllBytes(this, contents);
            return this;
        }

        public byte[] ReadAllBytes()
        {
            ThrowIfNotInitialized();
            return FSWrapper.ReadAllBytes(this);
        }


        public IEnumerable<NPath> CopyFiles(NPath destination, bool recurse, Func<NPath, bool> fileFilter = null)
        {
            ThrowIfNotInitialized();
            ThrowIfNotInitialized(destination);

            destination.EnsureDirectoryExists();
            var _this = this;
            return Files(recurse).Where(fileFilter ?? AlwaysTrue).Select(file => file.Copy(destination.Combine(file.RelativeTo(_this)))).ToArray();
        }

        public IEnumerable<NPath> MoveFiles(NPath destination, bool recurse, Func<NPath, bool> fileFilter = null)
        {
            ThrowIfNotInitialized();
            ThrowIfNotInitialized(destination);

            if (IsRoot)
                throw new NotSupportedException("MoveFiles is not supported on this directory because it would be dangerous:" + ToString());

            destination.EnsureDirectoryExists();
            var _this = this;
            return Files(recurse).Where(fileFilter ?? AlwaysTrue).Select(file => file.Move(destination.Combine(file.RelativeTo(_this)))).ToArray();
        }
        #endregion

        #region special paths

        public static NPath CurrentDirectory
        {
            get
            {
                return new NPath(FileSystem.GetCurrentDirectory());
            }
        }

        public static NPath ProcessDirectory
        {
            get
            {
                return new NPath(FileSystem.GetProcessDirectory());
            }
        }

        public static NPath HomeDirectory
        {
            get
            {
                if (FileSystem.DirectorySeparatorChar == '\\')
                    return new NPath(Environment.GetEnvironmentVariable("USERPROFILE"));
                return new NPath(Environment.GetEnvironmentVariable("HOME"));
            }
        }

        private static NPath systemTemp;
        public static NPath SystemTemp
        {
            get
            {
                if (!systemTemp.IsInitialized)
                    systemTemp = new NPath(FileSystem.GetTempPath());
                return systemTemp;
            }
        }

        #endregion

        private void ThrowIfRelative()
        {
            if (_isRelative)
                throw new ArgumentException("You are attempting an operation on a Path that requires an absolute path, but the path is relative");
        }

        private void ThrowIfRoot()
        {
            if (IsRoot)
                throw new ArgumentException("You are attempting an operation that is not valid on a root level directory");
        }

        private void ThrowIfNotInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("You are attemping an operation on an null path");
        }

        private static void ThrowIfNotInitialized(NPath path)
        {
            path.ThrowIfNotInitialized();
        }

        public NPath ToProcessDirectory()
        {
            if (!IsRelative)
                return this;
            return MakeAbsolute().RelativeTo(NPath.ProcessDirectory);
        }

        public NPath EnsureDirectoryExists(string append = "")
        {
            ThrowIfNotInitialized();

            if (String.IsNullOrEmpty(append))
            {
                if (DirectoryExists())
                    return this;
                EnsureParentDirectoryExists();
                CreateDirectory();
                return this;
            }
            return EnsureDirectoryExists(new NPath(append));
        }

        public NPath EnsureDirectoryExists(NPath append)
        {
            ThrowIfNotInitialized();
            ThrowIfNotInitialized(append);

            var combined = Combine(append);
            if (combined.DirectoryExists())
                return combined;
            combined.EnsureParentDirectoryExists();
            combined.CreateDirectory();
            return combined;
        }

        public NPath EnsureParentDirectoryExists()
        {
            ThrowIfNotInitialized();

            var parent = Parent;
            parent.EnsureDirectoryExists();
            return parent;
        }

        public NPath FileMustExist()
        {
            ThrowIfNotInitialized();

            if (!FileExists())
                throw new FileNotFoundException("File was expected to exist : " + ToString());

            return this;
        }

        public NPath DirectoryMustExist()
        {
            ThrowIfNotInitialized();

            if (!DirectoryExists())
                throw new DirectoryNotFoundException("Expected directory to exist : " + ToString());

            return this;
        }

        public bool IsChildOf(string potentialBasePath)
        {
            return IsChildOf(new NPath(potentialBasePath));
        }

        public bool IsChildOf(NPath potentialBasePath)
        {
            ThrowIfNotInitialized();
            ThrowIfNotInitialized(potentialBasePath);

            if ((IsRelative && !potentialBasePath.IsRelative) || !IsRelative && potentialBasePath.IsRelative)
                throw new ArgumentException("You can only call IsChildOf with two relative paths, or with two absolute paths");

            // If the other path is the root directory, then anything is a child of it as long as it's not a Windows path
            if (potentialBasePath.IsRoot)
            {
                if (_driveLetter != potentialBasePath._driveLetter)
                    return false;
                return true;
            }

            if (IsEmpty)
                return false;

            if (Equals(potentialBasePath))
                return true;

            return Parent.IsChildOf(potentialBasePath);
        }

        public IEnumerable<NPath> RecursiveParents
        {
            get
            {
                ThrowIfNotInitialized();
                var candidate = this;
                while (true)
                {
                    if (candidate.IsEmpty)
                        yield break;

                    candidate = candidate.Parent;
                    yield return candidate;
                }
            }
        }

        public NPath ParentContaining(string needle)
        {
            return ParentContaining(new NPath(needle));
        }

        public NPath ParentContaining(NPath needle)
        {
            ThrowIfNotInitialized();
            ThrowIfNotInitialized(needle);
            ThrowIfRelative();

            return RecursiveParents.FirstOrDefault(p => p.Exists(needle));
        }

        static bool AlwaysTrue(NPath p)
        {
            return true;
        }

        private static IFileSystem _fileSystem;
        public static IFileSystem FileSystem
        {
            get
            {
                if (_fileSystem == null)
#if UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
                    FileSystem = new FileSystem(UnityEngine.Application.dataPath);
#else
                    FileSystem = new FileSystem(Directory.GetCurrentDirectory());
#endif
                return _fileSystem;
            }
            set
            {
                _fileSystem = value;
                FSWrapper = new FSWrapper(value);
            }
        }

        private static FSWrapper _fsWrapper;
        private static FSWrapper FSWrapper
        {
            get
            {
                if (_fsWrapper == null)
                {
                    // this will initialize both FileSystem and FSWrapper
                    var fs = FileSystem;
                }
                return _fsWrapper;
            }
            set
            {
                _fsWrapper = value;
            }
        }

        private static bool? _isUnix;
        internal static bool IsUnix
        {
            get
            {
                if (!_isUnix.HasValue)
                    _isUnix = Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix;
                return _isUnix.Value;
            }
        }

        private static StringComparison? _pathStringComparison;
        private static StringComparison PathStringComparison
        {
            get
            {
                // this is lazily evaluated because IsUnix uses the FileSystem object and that can be set
                // after static constructors happen here
                if (!_pathStringComparison.HasValue)
                    _pathStringComparison = IsUnix ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                return _pathStringComparison.Value;
            }
        }

        internal string DebuggerDisplay => ToString();
    }

    public static class Extensions
    {
        public static IEnumerable<NPath> Copy(this IEnumerable<NPath> self, string dest)
        {
            return Copy(self, new NPath(dest));
        }

        public static IEnumerable<NPath> Copy(this IEnumerable<NPath> self, NPath dest)
        {
            if (dest.IsRelative)
                throw new ArgumentException("When copying multiple files, the destination cannot be a relative path");
            dest.EnsureDirectoryExists();
            return self.Select(p => p.Copy(dest.Combine(p.FileName))).ToArray();
        }

        public static IEnumerable<NPath> Move(this IEnumerable<NPath> self, string dest)
        {
            return Move(self, new NPath(dest));
        }

        public static IEnumerable<NPath> Move(this IEnumerable<NPath> self, NPath dest)
        {
            if (dest.IsRelative)
                throw new ArgumentException("When moving multiple files, the destination cannot be a relative path");
            dest.EnsureDirectoryExists();
            return self.Select(p => p.Move(dest.Combine(p.FileName))).ToArray();
        }

        public static IEnumerable<NPath> Delete(this IEnumerable<NPath> self)
        {
            foreach (var p in self)
                p.Delete();
            return self;
        }

        public static IEnumerable<string> InQuotes(this IEnumerable<NPath> self, SlashMode forward = SlashMode.Native)
        {
            return self.Select(p => p.InQuotes(forward));
        }

        public static NPath ToNPath(this string path)
        {
            if (path == null)
                return NPath.Default;
            return new NPath(path);
        }

        public static NPath Resolve(this NPath path)
        {
            // Add a reference to Mono.Posix with an .rsp file in the Assets folder with the line "-r:Mono.Posix.dll" for this to work
#if ENABLE_MONO
			if (!path.IsInitialized || !NPath.IsUnix /* nothing to resolve on windows */ || path.IsRelative || !path.FileExists())
				return path;
			return new NPath(Mono.Unix.UnixPath.GetCompleteRealPath(path.ToString()));
#else
            return path;
#endif
        }

        public static string CalculateMD5(this NPath path)
        {
            return NPath.FileSystem.CalculateFileMD5(path.ToProcessDirectory());
        }

        public static NPath CreateTempDirectory(this NPath baseDir, string myprefix = "")
        {
            var random = new Random();
            while (true)
            {
                var candidate = baseDir.Combine(myprefix + "_" + random.Next());
                if (!candidate.Exists())
                    return candidate.CreateDirectory();
            }
        }

    }

    public enum SlashMode
    {
        Native,
        Forward,
        Backward
    }

    public enum DeleteMode
    {
        Normal,
        Soft
    }


    class FSWrapper
    {
        private readonly IFileSystem fileSystem;

        public FSWrapper(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void DirectoryCreate(NPath path)
        {
            fileSystem.DirectoryCreate(path.ToProcessDirectory().ToString());
        }

        public void DirectoryDelete(NPath path, bool recursive)
        {
            fileSystem.DirectoryDelete(path.ToProcessDirectory().ToString(), recursive);
        }

        public bool DirectoryExists(NPath path)
        {
            return fileSystem.DirectoryExists(path.ToProcessDirectory().ToString());
        }
        public void DirectoryMove(NPath from, NPath to)
        {
            fileSystem.DirectoryMove(from.ToProcessDirectory().ToString(), to.ToProcessDirectory().ToString());
        }
        public bool ExistingPathIsDirectory(NPath path)
        {
            return fileSystem.ExistingPathIsDirectory(path.ToProcessDirectory().ToString());
        }
        public void FileCopy(NPath from, NPath to, bool overwrite)
        {
            fileSystem.FileCopy(from.ToProcessDirectory().ToString(), to.ToProcessDirectory().ToString(), overwrite);
        }
        public void FileDelete(NPath path)
        {
            fileSystem.FileDelete(path.ToProcessDirectory().ToString());
        }
        public bool FileExists(NPath path)
        {
            return fileSystem.FileExists(path.ToProcessDirectory().ToString());
        }
        public void FileMove(NPath from, NPath to)
        {
            fileSystem.FileMove(from.ToProcessDirectory().ToString(), to.ToProcessDirectory().ToString());
        }
        public IEnumerable<string> GetDirectories(NPath path)
        {
            return fileSystem.GetDirectories(path.ToProcessDirectory().ToString());
        }
        public IEnumerable<string> GetDirectories(NPath path, string pattern)
        {
            return fileSystem.GetDirectories(path.ToProcessDirectory().ToString(), pattern);
        }
        public IEnumerable<string> GetDirectories(NPath path, string pattern, SearchOption searchOption)
        {
            return fileSystem.GetDirectories(path.ToProcessDirectory().ToString(), pattern, searchOption);
        }
        public IEnumerable<string> GetFiles(NPath path)
        {
            return fileSystem.GetFiles(path.ToProcessDirectory().ToString());
        }
        public IEnumerable<string> GetFiles(NPath path, string pattern)
        {
            return fileSystem.GetFiles(path.ToProcessDirectory().ToString(), pattern);
        }
        public IEnumerable<string> GetFiles(NPath path, string pattern, SearchOption searchOption)
        {
            return fileSystem.GetFiles(path.ToProcessDirectory().ToString(), pattern, searchOption);
        }
        public Stream OpenRead(NPath path)
        {
            return fileSystem.OpenRead(path.ToProcessDirectory().ToString());
        }
        public Stream OpenWrite(NPath path, FileMode mode)
        {
            return fileSystem.OpenWrite(path.ToProcessDirectory().ToString(), mode);
        }
        public byte[] ReadAllBytes(NPath path)
        {
            return fileSystem.ReadAllBytes(path.ToProcessDirectory().ToString());
        }
        public string[] ReadAllLines(NPath path)
        {
            return fileSystem.ReadAllLines(path.ToProcessDirectory().ToString());
        }
        public string ReadAllText(NPath path)
        {
            return fileSystem.ReadAllText(path.ToProcessDirectory().ToString());
        }
        public string ReadAllText(NPath path, Encoding encoding)
        {
            return fileSystem.ReadAllText(path.ToProcessDirectory().ToString(), encoding);
        }
        public void WriteAllBytes(NPath path, byte[] bytes)
        {
            fileSystem.WriteAllBytes(path.ToProcessDirectory().ToString(), bytes);
        }
        public void WriteAllLines(NPath path, string[] contents)
        {
            fileSystem.WriteAllLines(path.ToProcessDirectory().ToString(), contents);
        }
        public void WriteAllText(NPath path, string contents)
        {
            fileSystem.WriteAllText(path.ToProcessDirectory().ToString(), contents);
        }
        public void WriteAllText(NPath path, string contents, Encoding encoding)
        {
            fileSystem.WriteAllText(path.ToProcessDirectory().ToString(), contents, encoding);
        }
        public void WriteLines(NPath path, string[] contents)
        {
            fileSystem.WriteLines(path.ToProcessDirectory().ToString(), contents);
        }
    }
}
