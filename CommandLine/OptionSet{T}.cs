using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.DotNet.Cli.CommandLine
{
    [DebuggerStepThrough]
    public abstract class OptionSet<T> : IReadOnlyCollection<T>
    {
        private readonly HashSet<T> options = new HashSet<T>();

        protected OptionSet()
        {
        }

        protected OptionSet(IReadOnlyCollection<Option> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            foreach (var option in this.options)
            {
                Add(option);
            }
        }

        public T this[string alias] => options.Single(o => HasAlias(o, alias));

        public int Count => options.Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return options.GetEnumerator();
        }

        internal void AddRange(IEnumerable<T> options)
        {
            foreach (var option in options)
            {
                Add(option);
            }
        }

        internal void TryAdd(T option)
        {
            if (!ContainsOptionWithAnyAliasOf(option))
            {
                options.Add(option);
            }
        }

        public abstract bool HasAlias(T option, string alias);

        internal bool ContainsOptionWithAnyAliasOf(T option)
        {
            return AliasesFor(option)
                .Any(alias => options
                         .Any(o => HasAlias(o, alias)));
        }

        internal void Add(T option)
        {
            foreach (var alias in AliasesFor(option))
            {
                if (options.Any(o => HasAlias(o, alias)))
                {
                    throw new ArgumentException($"Alias '{alias}' is already in use.");
                }
            }

            options.Add(option);
        }

        protected abstract IReadOnlyCollection<string> AliasesFor(T option);

        public bool Contains(string alias)
        {
            return options.Any(option => HasAlias(option, alias));
        }
    }
}