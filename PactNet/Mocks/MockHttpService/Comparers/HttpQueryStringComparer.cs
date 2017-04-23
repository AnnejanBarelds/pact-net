using System;
using System.Linq;
using System.Text.RegularExpressions;
using PactNet.Comparers;
using Microsoft.AspNetCore.WebUtilities;

namespace PactNet.Mocks.MockHttpService.Comparers
{
    internal class HttpQueryStringComparer : IHttpQueryStringComparer
    {
        public ComparisonResult Compare(string expected, string actual)
        {
            if (String.IsNullOrEmpty(expected) && String.IsNullOrEmpty(actual))
            {
                return new ComparisonResult("has no query strings");
            }

            var normalisedExpectedQuery = NormaliseUrlEncodingAndTrimTrailingAmpersand(expected);
            var normalisedActualQuery = NormaliseUrlEncodingAndTrimTrailingAmpersand(actual);
            var result = new ComparisonResult("has query {0}", normalisedExpectedQuery ?? "null");

            if (expected == null || actual == null)
            {
                result.RecordFailure(new DiffComparisonFailure(expected, actual));
                return result;
            }

            var expectedQueryItems = QueryHelpers.ParseQuery(normalisedExpectedQuery);
            var actualQueryItems = QueryHelpers.ParseQuery(normalisedActualQuery);

            if (expectedQueryItems.Count != actualQueryItems.Count)
            {
                result.RecordFailure(new DiffComparisonFailure(normalisedExpectedQuery, normalisedActualQuery));
                return result;
            }

            foreach (var item in expectedQueryItems)
            {
                if (!actualQueryItems.Keys.Contains(item.Key))
                {
                    result.RecordFailure(new DiffComparisonFailure(normalisedExpectedQuery, normalisedActualQuery));
                    return result;
                }

                var expectedValue = expectedQueryItems[item.Key];
                var actualValue = actualQueryItems[item.Key];

                if (expectedValue != actualValue)
                {
                    result.RecordFailure(new DiffComparisonFailure(normalisedExpectedQuery, normalisedActualQuery));
                    return result;
                }
            }

            return result;
        }

        private string NormaliseUrlEncodingAndTrimTrailingAmpersand(string query)
        {
            return query != null ?
                Regex.Replace(query, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper()).TrimEnd('&') :
                query;
        }
    }
}