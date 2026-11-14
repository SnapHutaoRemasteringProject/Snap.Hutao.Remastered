// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.UI.Xaml.Data.Converter;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Snap.Hutao.Remastered.Model.Metadata.Converter;

public sealed partial class DescriptionsParametersDescriptor : ValueConverter<DescriptionsParameters, IList<ParameterLevel>>
{
    [GeneratedRegex("{param([1-9][0-9]*?):(.+?)}")]
    private static partial Regex ParamRegex { get; }

    public static LevelParameters<string, ParameterDescription> Convert(DescriptionsParameters from, uint level)
    {
        return new(LevelFormat.Format(level), GetParameterDescription(from.Descriptions, from.Parameters[(SkillLevel)level]));
    }

    public override IList<ParameterLevel> Convert(DescriptionsParameters from)
    {
        ImmutableArray<LevelParameters<string, ParameterDescription>> result = from.Parameters.Convert(from.Descriptions, GetParameterDescription);

        if (from.SpecialDescriptionList is { Length: > 0 })
        {
            HashSet<int> specialSet = new(from.SpecialDescriptionList);
            if (from.DescriptionList is { Length: > 0 })
            {
                specialSet.ExceptWith(from.DescriptionList);
            }

            ImmutableArray<LevelParameters<string, ParameterDescription>>.Builder builder = ImmutableArray.CreateBuilder<LevelParameters<string, ParameterDescription>>(result.Length);
            foreach (LevelParameters<string, ParameterDescription> item in result)
            {
                ImmutableArray<ParameterDescription> marked = item.Parameters.SelectAsArray((p, j) =>
                    specialSet.Contains(j + 1) ? new ParameterDescription(p.Parameter, p.Description) { IsSpecial = true } : p);
                builder.Add(new(item.Level, marked));
            }

            result = builder.ToImmutable();
        }

        return result.Select(item => new ParameterLevel(item.Level, item.Parameters)).ToList();
    }

    private static ImmutableArray<ParameterDescription> GetParameterDescription(ImmutableArray<string> descriptions, ImmutableArray<float> paramArray)
    {
        ReadOnlySpan<string> span = descriptions.AsSpan();
        ImmutableArray<ParameterDescription>.Builder results = ImmutableArray.CreateBuilder<ParameterDescription>(span.Length);

        foreach (ref readonly string desc in span)
        {
            if (desc.AsSpan().TrySplitIntoTwo('|', out ReadOnlySpan<char> description, out ReadOnlySpan<char> format))
            {
                if (description[0] is not '#')
                {
                    // Fast path
                    string resultFormatted = ParamRegex.Replace(format.ToString(), match => ReplaceParamInMatch(match, paramArray));
                    results.Add(new(resultFormatted, description.ToString()));
                }
                else
                {
                    string descriptionString = SpecialNameHandling.Handle(description.ToString());
                    string formatString = SpecialNameHandling.Handle(format.ToString());

                    string resultFormatted = ParamRegex.Replace(formatString, match => ReplaceParamInMatch(match, paramArray));
                    results.Add(new(resultFormatted, descriptionString));
                }
            }
            else
            {
                HutaoException.InvalidOperation($"ParameterFormat failed, value: `{desc}`");
            }
        }

        return results.ToImmutable();
    }

    private static string ReplaceParamInMatch(Match match, ImmutableArray<float> paramArray)
    {
        if (match.Success)
        {
            int index = int.Parse(match.Groups[1].Value, CultureInfo.CurrentCulture) - 1;
            return ParameterFormat.FormatInvariant($"{{0:{match.Groups[2].Value}}}", paramArray[index]);
        }

        return string.Empty;
    }
}