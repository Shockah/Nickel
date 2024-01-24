using daisyowl.text;
using OneOf;
using Pidgin;
using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Linq;
using static Pidgin.Parser;

namespace Nickel;

public sealed class TextTagParser
{
	private static readonly Parser<char, char> OpenTagChar = Char('<');
	private static readonly Parser<char, char> CloseTagChar = Char('>');
	private static readonly Parser<char, char> SlashChar = Char('/');
	private static readonly Parser<char, char> EqualsChar = Char('=');
	private static readonly Parser<char, char> StringChar = AnyCharExcept('<', '>', '/', '=');

	private static readonly Parser<char, string> NonEmptyText =
		Pidgin.Parser.OneOf(Try(StringChar), Try(Whitespace)).AtLeastOnce()
			.Select(s => string.Concat(s));

	private static readonly Parser<char, KeyValuePair<string, string>> NamedAttribute =
		Map(
			(name, value) => new KeyValuePair<string, string>(string.Concat(name), string.Concat(value)),
			NonEmptyText.Between(SkipWhitespaces),
			EqualsChar.Then(NonEmptyText.Between(SkipWhitespaces))
		);

	private static readonly Parser<char, IReadOnlyDictionary<string, string>> NamedAttributes =
		NamedAttribute
			.Separated(Whitespaces)
			.Select(parameters => (IReadOnlyDictionary<string, string>)parameters.ToDictionary());

	private static readonly Parser<char, string?> MainAttribute =
		EqualsChar.Between(SkipWhitespaces).Then(NonEmptyText.Between(SkipWhitespaces))
			.Optional()
			.Select(s => s.HasValue ? string.Concat(s.Value) : null);

	private static readonly Parser<char, (string? MainAttribute, IReadOnlyDictionary<string, string> NamedAttributes)> Attributes =
		Map(
			(mainAttribute, namedAttributes) => (mainAttribute, namedAttributes),
			MainAttribute,
			NamedAttributes
		);

	private static readonly Parser<char, (string TagName, string? MainAttribute, IReadOnlyDictionary<string, string> NamedAttributes)> TagOpen =
		Map(
			(tagName, attributes) => (string.Concat(tagName), attributes.MainAttribute, attributes.NamedAttributes),
			NonEmptyText.Between(SkipWhitespaces),
			Attributes.Between(SkipWhitespaces)
		)
		.Between(OpenTagChar.Between(SkipWhitespaces), CloseTagChar.Between(SkipWhitespaces));

	private static readonly Parser<char, string> TagClose =
		OpenTagChar.Between(SkipWhitespaces)
			.Then(SlashChar.Between(SkipWhitespaces))
			.Then(NonEmptyText.Between(SkipWhitespaces))
			.Before(CloseTagChar.Between(SkipWhitespaces))
			.Select(string.Concat);

	private static readonly Parser<char, ParsedTextTag> Tag = null!;

	private static readonly Parser<char, OneOf<string, ParsedTextTag>> TextOrTag =
		Pidgin.Parser.OneOf(
			Try(Rec(() => Tag).Select(OneOf.OneOf<string, ParsedTextTag>.FromT1)),
			Try(NonEmptyText.Select(OneOf.OneOf<string, ParsedTextTag>.FromT0))
		);

	private static readonly Parser<char, IEnumerable<OneOf<string, ParsedTextTag>>> TextOrTags =
		TextOrTag.Many();

	static TextTagParser()
	{
		Tag = TagOpen.Bind(
			open => Rec(() => TextOrTags)
				.Before(TagClose.Where(closeTagName => closeTagName == open.TagName))
				.Select(content => new ParsedTextTag(
					Tag: open.TagName,
					Content: content.ToList(),
					MainAttribute: open.MainAttribute,
					NamedAttributes: open.NamedAttributes
				))
		);
	}

	public IEnumerable<OneOf<string, ParsedTextTag>> ParseTextTags(string text)
		=> TextOrTags.Parse(text)
			.Match(
				success => success,
				_ => [OneOf.OneOf<string, ParsedTextTag>.FromT0(text)]
			);

	public GlyphPlan LayoutGlyphs(string str, double size, FontMetrics metrics, Color color, Color? colorForce = null, Func<string, uint?>? lookupColor = null, double? maxWidth = null, double? lineHeight = null, TAlign align = TAlign.Left, float letterSpacing = 0f)
	{
		var tags = this.ParseTextTags(str).ToList();
		return new GlyphPlan();
	}
}

public record struct ParsedTextTag(
	string Tag,
	IReadOnlyList<OneOf<string, ParsedTextTag>> Content,
	string? MainAttribute,
	IReadOnlyDictionary<string, string> NamedAttributes
);
