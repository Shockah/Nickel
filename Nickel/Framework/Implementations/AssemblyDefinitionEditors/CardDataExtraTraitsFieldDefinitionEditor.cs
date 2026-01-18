using Mono.Cecil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nickel;

internal sealed class CardDataExtraTraitsFieldDefinitionEditor : IAssemblyDefinitionEditor
{
	internal const string FieldName = "__Nickel__ExtraTraits";

	public byte[] AssemblyEditorDescriptor
		=> Encoding.UTF8.GetBytes($"{this.GetType().FullName}, {NickelConstants.Name} {NickelConstants.Version}");

	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName == "CobaltCore.dll";

	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var cardDataType = definition.MainModule.GetType(nameof(CardData));
		
		var cardTraitEntryListTypeReference = definition.MainModule.ImportReference(typeof(List<ICardTraitEntry>));
		
		var extraTraitsFieldField = new FieldDefinition(FieldName, FieldAttributes.Public, cardTraitEntryListTypeReference);
		cardDataType.Fields.Add(extraTraitsFieldField);

		return true;
	}
}
