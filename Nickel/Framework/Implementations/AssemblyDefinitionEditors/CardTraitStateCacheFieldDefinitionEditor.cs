using Mono.Cecil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Nickel.Models.Content;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class CardTraitStateCacheFieldDefinitionEditor : IAssemblyDefinitionEditor
{
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName == "CobaltCore.dll";

	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var cardType = definition.MainModule.GetType("Card");
		
		var cardTraitEntryToCardTraitStateDictionaryTypeReference = definition.MainModule.ImportReference(typeof(Dictionary<ICardTraitEntry, CardTraitState>));
		var intTypeReference = definition.MainModule.ImportReference(typeof(int));
		
		var cardTraitStateCacheField = new FieldDefinition("__Nickel__CardTraitStateCache", FieldAttributes.Private, cardTraitEntryToCardTraitStateDictionaryTypeReference);
		var cardTraitStateCacheVersionField = new FieldDefinition("__Nickel__CardTraitStateCacheVersion", FieldAttributes.Private, intTypeReference);
		
		cardType.Fields.Add(cardTraitStateCacheField);
		cardType.Fields.Add(cardTraitStateCacheVersionField);

		return true;
	}
}
