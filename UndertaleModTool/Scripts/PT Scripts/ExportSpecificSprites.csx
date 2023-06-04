// Modified code to allow exporting specific sprites by JQBUNI
// Modified again for optimization by Raltyro

using System;
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

//string texFolder = GetFolder(FilePath) + "Export_Sprites" + Path.DirectorySeparatorChar;
string texFolder = PromptChooseDirectory() + Path.DirectorySeparatorChar;
Directory.CreateDirectory(texFolder);

List<string> spritesToExport = GetUserInputSprites();

bool padding = ScriptQuestion("Export all sprites with padding? (Without padding, it will have the offsets wrong)");

SetProgressBar(null, "Exporting Sprites", 0, spritesToExport.Count);
StartProgressBarUpdater();

TextureWorker worker = new TextureWorker();

await DumpSprites();
worker.Cleanup();

StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.");

async Task DumpSprites() {await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));}

void DumpSprite(UndertaleSprite sprite) {
	string spriteName = sprite.Name.Content.Trim().ToLower();
	if (!spritesToExport.Contains(spriteName)) return;

	// Dump the Sprite Textures to the Folder
	string spriteFolder = texFolder + sprite.Name.Content + Path.DirectorySeparatorChar;
	Directory.CreateDirectory(spriteFolder);

	for (int i = 0; i < sprite.Textures.Count; i++) {
		string frameName = sprite.Name.Content + "_" + i;
		if (sprite.Textures[i]?.Texture != null) try {
			worker.ExportAsPNG(sprite.Textures[i].Texture, spriteFolder + frameName + ".png", null, padding);
		}
		catch(Exception e) {
			try {
				worker.ExportAsPNG(sprite.Textures[i].Texture, spriteFolder + frameName + ".png", null, false);
				ScriptMessage("WARNING: \"" + frameName + "\" can only be exported without padding, because the bounding box is larger than the texture itself");
			}
			catch {
				ScriptMessage("ERROR: \"" + frameName + "\" cannot be exported (" + e + ")");
			}
		}
	}

	IncrementProgressParallel();
}

List<string> GetUserInputSprites() {
	List<string> spriteNames = new List<string>();
	string inputtedText = "";

	inputtedText = SimpleTextInput("Sprites to Export", "Enter sprite names (one per line)", inputtedText, true);
	string[] individualLineArray = inputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

	foreach (var line in individualLineArray) spriteNames.Add(line.Trim().ToLower());
	return spriteNames;
}

string GetFolder(string path) {return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;}