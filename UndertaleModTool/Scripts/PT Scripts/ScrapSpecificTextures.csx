// Originally created by JQBUNI, modified for optimization by Raltyro

using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

List<string> spritesToScrap = GetUserInputSprites();

bool delete = ScriptQuestion("Delete the inputted sprite texture page items?");
bool deleteEmbeds = false;
if (delete) deleteEmbeds = ScriptQuestion("Delete the inputted sprite embedded textures? (DON'T DO THIS UNLESS YOU KNOW WHAT YOU'RE DOING)");

List<string> texturesToDelete = new List<string>();
List<string> embedTexturesToDelete = new List<string>();

SetProgressBar(null, "Scrapping Sprites", 0, spritesToScrap.Count);
StartProgressBarUpdater();

await ScrapSpriteTextures();
await StopProgressBarUpdater();

if (delete) {
	SetProgressBar(null, "Deleting Texture Page Items", 0, texturesToDelete.Count);
	StartProgressBarUpdater();

	for (int i = Data.TexturePageItems.Count - 1; i >= 0; i--) {
		if (!texturesToDelete.Contains(Data.TexturePageItems[i].Name.Content)) continue;

		Data.TexturePageItems[i]?.Dispose();
		Data.TexturePageItems.Remove(Data.TexturePageItems[i]);
	}
	await StopProgressBarUpdater();
}
if (deleteEmbeds) {
	SetProgressBar(null, "Deleting Embedded Textures", 0, embedTexturesToDelete.Count);
	StartProgressBarUpdater();

	for (int i = Data.EmbeddedTextures.Count - 1; i >= 0; i--) {
		if (!embedTexturesToDelete.Contains(Data.EmbeddedTextures[i].Name.Content)) continue;

		Data.EmbeddedTextures[i]?.Dispose();
		Data.EmbeddedTextures.Remove(Data.EmbeddedTextures[i]);
	}
	await StopProgressBarUpdater();
}

HideProgressBar();
ScriptMessage("Texture Scraps Complete.");

async Task ScrapSpriteTextures() {await Task.Run(() => Parallel.ForEach(Data.Sprites, ScrapSpriteTexture));}

void ScrapSpriteTexture(UndertaleSprite sprite) {
	string spriteName = sprite.Name.Content.Trim().ToLower();
	if (!spritesToScrap.Contains(spriteName)) return;

	// Remove all texture entries in sprite
	foreach (UndertaleSprite.TextureEntry entry in sprite.Textures) {
		if (entry == null) continue;
		entry.Dispose();

		if (delete) {
			if (entry.Texture == null || texturesToDelete.Contains(entry.Texture?.Name.Content)) continue;
			texturesToDelete.Add(entry.Texture.Name.Content);

			if (deleteEmbeds) {
				UndertaleEmbeddedTexture embedTexture = entry.Texture.TexturePage;

				if (embedTexture == null || embedTexturesToDelete.Contains(embedTexture?.Name.Content)) continue;
				embedTexturesToDelete.Add(embedTexture.Name.Content);
			}
		}
	}

	IncrementProgressParallel();
}

List<string> GetUserInputSprites() {
	List<string> spriteNames = new List<string>();
	string inputtedText = "";

	inputtedText = SimpleTextInput("Sprites to Scrap", "Enter sprite names (one per line)", inputtedText, true);
	string[] individualLineArray = inputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

	foreach (var line in individualLineArray) spriteNames.Add(line.Trim().ToLower());
	return spriteNames;
}