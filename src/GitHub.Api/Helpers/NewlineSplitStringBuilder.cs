using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitHub.Unity.Helpers
{
    class NewlineSplitStringBuilder
    {
        private StringBuilder builder;

        public string[] Append(string value)
        {
            if (value == null)
            {
                var singleResult = builder?.ToString();
                if (singleResult == null)
                {
                    return new string[0];
                }
                return new[] { singleResult };
            }

            var splitValues = value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (splitValues.Length == 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (builder == null)
            {
                builder = new StringBuilder();
            }

            builder.Append(splitValues[0]);

            if (splitValues.Length == 1)
            {
                return new string[0];
            }

            var results = new string[splitValues.Length - 1];
            results[0] = builder.ToString();

            for (var index = 1; index < splitValues.Length - 1; index++)
            {
                results[index] = splitValues[index];
            }

            builder = new StringBuilder(splitValues[splitValues.Length - 1]);

            return results;
        }
    }
}

