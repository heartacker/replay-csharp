﻿using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Replay.Model;
using Replay.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Replay.Services
{
    /// <summary>
    /// Uses roslyn's Classifier API to syntax highlight code.
    /// </summary>
    /// <remarks>
    /// There's not much documentation about this API, so this is based off of the following sample code:
    /// https://github.com/dotnet/roslyn/blob/master/src/Samples/CSharp/ConsoleClassifier/Program.cs
    /// </remarks>
    public class SyntaxHighlighter
    {
        private readonly IReadOnlyDictionary<string, Color> theme;
        public Color ForegroundColor { get; }
        public Color BackgroundColor { get; }

        public SyntaxHighlighter(string themeFilename)
        {
            this.theme = ThemeReader.GetTheme(themeFilename);
            this.ForegroundColor = theme[ThemeReader.Foreground];
            this.BackgroundColor = theme[ThemeReader.Background];
        }

        public async Task<IReadOnlyCollection<ColorSpan>> HighlightAsync(ReplSubmission submission)
        {
            IEnumerable<ClassifiedSpan> classified = await Classifier.GetClassifiedSpansAsync(
                submission.Document,
                TextSpan.FromBounds(0, submission.Code.Length)
            );

            return classified
                .GroupBy(span => span.TextSpan)
                .Select(spans => LookUpColorFromTheme(spans.ToArray()))
                .ToList();
        }

        /// <summary>
        /// The Classifier can return multiple classifications for a single span.
        /// For example, "Console" will return ["class name", "static symbol"].
        /// Select the first one that we have color for; the assumption is
        /// the first one is more specific.
        /// </summary>
        private ColorSpan LookUpColorFromTheme(ClassifiedSpan[] spans)
        {
            return
                spans.Select(span =>
                {
                    if (theme.TryGetValue(span.ClassificationType, out Color color)
                        || (fallbacks.TryGetValue(span.ClassificationType, out string fallback)
                            && theme.TryGetValue(fallback, out color)))
                    {
                        return new ColorSpan(color, span.TextSpan.Start, span.TextSpan.End);
                    }
                    return null;
                })
                .FirstOrDefault()
                ??
                new ColorSpan(ForegroundColor, spans[0].TextSpan.Start, spans[0].TextSpan.End);
        }

        private static readonly IReadOnlyDictionary<string, string> fallbacks = new Dictionary<string, string>
        {
            // fallbacks from https://github.com/dotnet/roslyn/blob/master/src/EditorFeatures/Core/Implementation/Classification/ClassificationTypeDefinitions.cs
            { "keyword - control", "keyword" },
            { "operator - overloaded", "operator" }
        };
    }

    public class ColorSpan
    {
        public ColorSpan(Color color, int start, int end)
        {
            Color = color;
            Start = start;
            End = end;
        }

        public Color Color { get; }
        public int Start { get; }
        public int End { get; }
    }
}
